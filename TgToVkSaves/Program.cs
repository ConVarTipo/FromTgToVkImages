using System;
using System.Text;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VkNet.AudioBypassService.Extensions;
using Microsoft.Extensions.DependencyInjection;
using VkNet.Abstractions;
using System.IO;
using System.Net;
using Telegram.Bot;
using System.Threading;
using VkNet.Model.Attachments;
using System.Linq;
using System.Collections.Generic;

namespace TgToVkSaves
{
    class Program
    {
        static void Main(string[] args)
        {
            Vk.AuthVK();
        }

        #region Others
        public static void BotStart()
        {
            Checks();
            TG.BotMessageReceiving();
            Console.ReadLine();        
        }

        static void Checks()
        {
            if (!Directory.Exists("D:/tgphoto/"))
            {
                Directory.CreateDirectory("D:/tgphoto/");
                Console.WriteLine("Dir created!");
            }

        }
        #endregion
    }

    class Vk
    {
        private static IVkApi _api;
        static Random random = new Random();

        public static void AuthVK()
        {
            string username, password, fa;

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddAudioBypass();

            _api = new VkApi(serviceCollection);

            if (System.IO.File.Exists(System.Windows.Forms.Application.UserAppDataPath + "\\login.info"))
            {
                string file = System.IO.File.ReadAllText(System.Windows.Forms.Application.UserAppDataPath + "\\login.info");
                try
                {
                    username = RC4.Decrypt(Setting.key, file).Split('<')[0];
                    password = RC4.Decrypt(Setting.key, file).Split('<')[1];
                    fa = RC4.Decrypt(Setting.key, file).Split('<')[2];

                    _api.Authorize(new ApiAuthParams
                    {
                        ApplicationId = 7713073,
                        Login = username,
                        Password = password,
                        Settings = Settings.All,
                        TwoFactorAuthorization = () =>
                        {
                            Console.WriteLine("Enter code:");
                            return Console.ReadLine();
                        }
                    });

                    Console.WriteLine("Auto login from: " + System.Windows.Forms.Application.UserAppDataPath + "\\login.info");
                    Console.WriteLine("Successful auth!");
                    Program.BotStart();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("2FA on account ? true/false");
                fa = Console.ReadLine();

                Console.WriteLine("Phone number without +:");
                username = Console.ReadLine();

                Console.WriteLine("Password:");
                password = Console.ReadLine();

                if (fa == "true")
                {
                    try
                    {
                        _api.Authorize(new ApiAuthParams
                        {
                            ApplicationId = 7713073,
                            Login = username,
                            Password = password,
                            Settings = Settings.All,
                            TwoFactorAuthorization = () =>
                            {
                                Console.WriteLine("Enter code:");
                                return Console.ReadLine();
                            }
                        });

                        string file = RC4.Encrypt(Setting.key, username + "<" + password + "<" + "true");
                        System.IO.File.WriteAllText(System.Windows.Forms.Application.UserAppDataPath + "\\login.info", file);

                        Console.Clear();
                        Console.WriteLine("Auto login from: " + System.Windows.Forms.Application.UserAppDataPath + "\\login.info");
                        Console.WriteLine("Successful auth!");
                        Program.BotStart();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                else
                {
                    try
                    {
                        _api.Authorize(new ApiAuthParams
                        {
                            ApplicationId = 7713073,
                            Login = username,
                            Password = password,
                            Settings = Settings.All
                        });

                        string file = RC4.Encrypt(Setting.key, username + "<" + password + "<" + "true");
                        System.IO.File.WriteAllText(System.Windows.Forms.Application.UserAppDataPath + "\\login.info", file);

                        Console.Clear();
                        Console.WriteLine("Auto login from: " + System.Windows.Forms.Application.UserAppDataPath + "\\login.info");
                        Console.WriteLine("Succsesful auth!");
                        Program.BotStart();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            Console.ReadLine();
        }
        public static void SendToVk(string Path, string fileType)
        {
            switch(fileType)
            {
                case ".jpg":
                    try
                    {
                        var uploadServer = _api.Photo.GetMessagesUploadServer(Setting.VkID);

                        var wc = new WebClient();
                        var result = Encoding.ASCII.GetString(wc.UploadFile(uploadServer.UploadUrl, Path));
                        var photo = _api.Photo.SaveMessagesPhoto(result);

                        _api.Messages.Send(new MessagesSendParams()
                        {
                            RandomId = random.Next(0, 999999999),
                            UserId = Setting.VkID,
                            Attachments = new List<MediaAttachment>
                    {
                    photo.FirstOrDefault()
                    }
                        });

                        Console.WriteLine("Send to " + Setting.VkID + " Succsesful!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    break;
                case ".mp4":
                    try
                    {
                        var video = _api.Video.Save(new VideoSaveParams
                        {
                            IsPrivate = false,
                            Repeat = false,
                            Description = random.Next(0, 999999999).ToString(),
                            Name = System.IO.Path.GetRandomFileName()
                        });

                        // Console.WriteLine(responseFile);  // {"size":15966484,"owner_id":330464853,"video_id":456239204,"video_hash":"d9e969f3020a227db9"}
                        var wc = new WebClient();
                        var responseFile = Encoding.ASCII.GetString(wc.UploadFile(video.UploadUrl, Path));

                        _api.Messages.Send(new MessagesSendParams()
                        {
                            RandomId = random.Next(0, 999999999),
                            UserId = Setting.VkID,
                            Attachments = new List<MediaAttachment> { video }
                        });

                        Console.WriteLine("Send to " + Setting.VkID + " Succsesful!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    break;
            }
        }
    }

    class TG
    {
        static TelegramBotClient _bot = new TelegramBotClient(Setting.ApiKey);

        public async static void SaveFile(string fileId, string fileType)
        {
            switch(fileType)
            {
                case ".jpg":
                    try
                    {
                        string fileName = Path.GetRandomFileName();
                        string outputPath = Path.Combine("D:/tgphoto/", fileName + ".jpg");

                        var file = await _bot.GetFileAsync(fileId);
                        FileStream fs = new FileStream(outputPath, FileMode.Create);
                        await _bot.DownloadFileAsync(file.FilePath, fs);
                        fs.Close();
                        fs.Dispose();

                        Thread ToVK = new Thread(() => Vk.SendToVk(outputPath, fileType));
                        ToVK.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error downloading: " + ex.Message);
                    }
                    break;
                case ".mp4":
                    try
                    {
                        string fileName = Path.GetRandomFileName();
                        string outputPath = Path.Combine("D:/tgphoto/", fileName + ".mp4");

                        var file = await _bot.GetFileAsync(fileId);
                        FileStream fs = new FileStream(outputPath, FileMode.Create);
                        await _bot.DownloadFileAsync(file.FilePath, fs);
                        fs.Close();
                        fs.Dispose();

                        Thread ToVK = new Thread(() => Vk.SendToVk(outputPath, fileType));
                        ToVK.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error downloading: " + ex.Message);
                    }
                    break;
            }
        }
        public async static void BotMessageReceiving()
        {
            await _bot.SetWebhookAsync("");
            int offset = 0;
            while(true)
            {
                var updates = await _bot.GetUpdatesAsync(offset);

                foreach(var update in updates)
                {
                    var message = update.Message;
                    if(message?.Type == Telegram.Bot.Types.Enums.MessageType.Document)
                    {
                        Thread save = new Thread(() => SaveFile(message.Document.FileId, ".jpg"));
                        save.Start();
                    }
                    if(message?.Type == Telegram.Bot.Types.Enums.MessageType.Photo)
                    {
                        Thread save = new Thread(() => SaveFile(message.Photo[message.Photo.Length - 1].FileId, ".jpg"));
                        save.Start();
                    }
                    if(message?.Type == Telegram.Bot.Types.Enums.MessageType.Video)
                    {
                        Thread save = new Thread(() => SaveFile(message.Video.FileId, ".mp4"));
                        save.Start();
                    }
                    offset = update.Id + 1;
                }
            }
        }

    }
}
