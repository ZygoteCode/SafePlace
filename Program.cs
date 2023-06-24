using Discord.WebSocket;
using System.Text;
using System.Runtime;

public class Program
{
    public static LegitHttpServer.HttpServer server;
    public static DiscordSocketClient client;
    public static ProtoRandom random;
    public static RequestChecker requestChecker;
    public static byte[] codeVerify, codeIcon, codeStyle, codeBackground, codeVerification;

    public static void Main()
    {
        Console.Title = "SafePlace";

        if (!System.IO.Directory.Exists("data"))
        {
            System.IO.Directory.CreateDirectory("data");
        }

        if (!System.IO.Directory.Exists("data/guilds"))
        {
            System.IO.Directory.CreateDirectory("data/guilds");
        }
        
        if (!System.IO.Directory.Exists("data/verifications"))
        {
            System.IO.Directory.CreateDirectory("data/verifications");
        }

        codeVerify = System.IO.File.ReadAllBytes("client/verify.html");
        codeIcon = System.IO.File.ReadAllBytes("icon.ico");
        codeStyle = System.IO.File.ReadAllBytes("client/style.css");
        codeBackground = System.IO.File.ReadAllBytes("client/background.jpg");
        codeVerification = System.IO.File.ReadAllBytes("client/verification.js");

        requestChecker = new RequestChecker();
        random = new ProtoRandom(5);

        server = new LegitHttpServer.HttpServer(80);
        server.Start();

        Thread thread = new Thread(ReceiveRequests);
        thread.Priority = ThreadPriority.Highest;
        thread.Start();

        client = new DiscordSocketClient(new DiscordSocketConfig()
        {
            UdpSocketProvider = Discord.Net.Udp.DefaultUdpSocketProvider.Instance,
            WebSocketProvider = Discord.Net.WebSockets.DefaultWebSocketProvider.Instance,
            RestClientProvider = Discord.Net.Rest.DefaultRestClientProvider.Instance,
            MessageCacheSize = 50,
            DefaultRetryMode = Discord.RetryMode.AlwaysRetry,
            GatewayIntents = Discord.GatewayIntents.Guilds | Discord.GatewayIntents.GuildMembers | Discord.GatewayIntents.GuildBans | Discord.GatewayIntents.GuildEmojis | Discord.GatewayIntents.GuildIntegrations | Discord.GatewayIntents.GuildWebhooks | Discord.GatewayIntents.GuildInvites | Discord.GatewayIntents.GuildVoiceStates | Discord.GatewayIntents.GuildPresences | Discord.GatewayIntents.GuildMessages | Discord.GatewayIntents.GuildMessageReactions | Discord.GatewayIntents.GuildMessageTyping | Discord.GatewayIntents.GuildScheduledEvents | Discord.GatewayIntents.DirectMessages | Discord.GatewayIntents.DirectMessageReactions | Discord.GatewayIntents.DirectMessageTyping
        });

        client.LoggedIn += Client_LoggedIn;
        client.MessageReceived += Client_MessageReceived;
        client.UserJoined += Client_UserJoined;

        client.LoginAsync(Discord.TokenType.Bot, "DISCORD_BOT_TOKEN_HERE", false);
        client.StartAsync();

        while (true)
        {
            Console.ReadLine();
        }
    }

    public static void ClearRAM()
    {
        while (true)
        {
            Thread.Sleep(10000);

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();
        }
    }

    private static Task Client_LoggedIn()
    {
        Console.WriteLine("[!] Program is now operative!");
        return Task.CompletedTask;
    }

    public static void ConfigureGuild(ulong guildId)
    {
        if (System.IO.Directory.Exists("data/guilds/" + guildId))
        {
            System.IO.Directory.CreateDirectory("data/guilds/" + guildId);
        }

        if (!System.IO.File.Exists("data/guilds/" + guildId + "/role.txt"))
        {
            System.IO.File.Create("data/guilds/" + guildId + "/role.txt");
        }
    }

    private static Task Client_UserJoined(SocketGuildUser arg)
    {
        try
        {
            ConfigureGuild(arg.Guild.Id);

            if (System.IO.File.Exists("data/guilds/" + arg.Guild.Id + "/role.txt"))
            {
                string verificationCode = random.GetRandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray(), 120);
                System.IO.File.WriteAllText("data/verifications/" + verificationCode + ".txt", arg.Id + "|" + arg.Guild.Id + "|" + System.IO.File.ReadAllText("data/guilds/" + arg.Guild.Id + "/role.txt"));
                client.GetUser(arg.Id).CreateDMChannelAsync().Result.SendMessageAsync(null, false, GetEmbed("Verify yourself", "Hello! Welcome in the Server. Please, verify your identity at this link: https://example.com/verify/" + verificationCode + "/" + "\r\n\r\n**Allowed browsers**: Google Chrome, Mozilla Firefox, Opera Browser, Opera GX Browser, Brave."));
                new Thread(() => DeleteVerification(verificationCode)).Start();
            }
        }
        catch
        {

        }

        return Task.CompletedTask;
    }

    public static void DeleteVerification(string verificationCode)
    {
        try
        {
            Thread.Sleep(60000);
            
            if (System.IO.File.Exists("data/verifications/" + verificationCode + ".txt"))
            {
                System.IO.File.Delete("data/verifications/" + verificationCode + ".txt");
            }
        }
        catch
        {

        }
    }

    private static Task Client_MessageReceived(SocketMessage arg)
    {
        try
        {
            string content = arg.Content;
            SocketTextChannel textChannel = (SocketTextChannel)client.GetChannel(arg.Channel.Id);
            SocketGuildUser user = (SocketGuildUser)arg.Author;
            SocketGuild guild = textChannel.Guild;
            ConfigureGuild(guild.Id);

            if (System.IO.File.ReadAllText("data/guilds/" + guild.Id + "/antiexploit.txt").Equals("y"))
            {
                if (content.Length > (user.PremiumSince != null ? 4000 : 2000))
                {
                    guild.AddBanAsync(user.Id, 7, "[SAFEPLACE] Anti Exploit");
                    return Task.CompletedTask;
                }
            }

            if (!user.Guild.OwnerId.Equals(user.Id))
            {
                return Task.CompletedTask;
            }

            bool role = false;

            if (!System.IO.File.ReadAllText("data/guilds/" + guild.Id + "/role.txt").Equals(""))
            {
                role = true;
            }

            if (content.StartsWith("sp!"))
            {
                content = content.Substring(3);

                if (content.Equals("help"))
                {
                    string message = "Here is the list of all available commands for SafePlace BOT:\r\n\r\n" +

                        "**sp!help** - *Get the list of all SafePlace commands.*\r\n" +
                        "**sp!setrole** <id> - *Set the role to assign to every user that went through the verification process.*\r\n" +
                        "**sp!getrole** - *Get the role assigned for every verified user.*\r\n" +

                        (!role ? "\r\n\r\n**WARNING**: You must set the verification role with the command **sp!setrole**." : "");

                    textChannel.SendMessageAsync(null, false, GetEmbed("List of commands", message));
                }
                else if (content.Equals("getrole"))
                {
                    if (role)
                    {
                        textChannel.SendMessageAsync(null, false, GetEmbed("Get role for passing verification process succesfully", "The role to assign to every verified user is: <@&" + System.IO.File.ReadAllText("data/guilds/" + guild.Id + "/role.txt") + ">."));
                    }
                    else
                    {
                        textChannel.SendMessageAsync(null, false, GetEmbed("Get role for passing verification process succesfully", "Hey! There is no set a role to give to the users that have been passed the verification process succesfully. Set it with the command **sp!setrole** to make the BOT protecting your Server against malicious attacks."));
                    }
                }
                else if (content.StartsWith("setrole "))
                {
                    content = content.Substring("setrole ".Length);

                    if (content.Length == 18 && Microsoft.VisualBasic.Information.IsNumeric(content))
                    {
                        ulong roleId = ulong.Parse(content);

                        if (roleId <= 0)
                        {
                            return Task.CompletedTask;
                        }

                        bool exists = false;

                        foreach (SocketRole guildRole in guild.Roles)
                        {
                            if (guildRole.Id.Equals(roleId) && !guildRole.Name.Equals("@everyone") && !guildRole.Name.Equals("SafePlace"))
                            {
                                exists = true;
                                break;
                            }
                        }

                        if (exists)
                        {
                            System.IO.File.WriteAllText("data/guilds/" + guild.Id + "/role.txt", content);
                            textChannel.SendMessageAsync(null, false, GetEmbed("Set role for succesful verification", "Succesfully set the role that will be assigned to verified users to: <@&" + roleId + ">."));
                        }
                        else
                        {
                            textChannel.SendMessageAsync(null, false, GetEmbed("Set role for succesful verification", "The specified role does not exists, please try again."));
                        }
                    }
                }
            }
        }
        catch
        {

        }

        return Task.CompletedTask;
    }

    public static Discord.Embed GetEmbed(string title, string text)
    {
        var embed = new Discord.EmbedBuilder();

        embed.WithColor(Discord.Color.DarkGrey);
        embed.WithTitle(title);
        embed.WithDescription(text);
        embed.WithCurrentTimestamp();

        return embed.Build();
    }

    public static void DoneVerification(string verificationCode, bool success)
    {
        try
        {
            if (success)
            {
                if (System.IO.File.Exists("data/verifications/" + verificationCode + ".txt"))
                {
                    string[] splitted = System.IO.File.ReadAllText("data/verifications/" + verificationCode + ".txt").Split('|');
                    System.IO.File.Delete("data/verifications/" + verificationCode + ".txt");
                    client.GetGuild(ulong.Parse(splitted[1])).GetUser(ulong.Parse(splitted[0])).AddRoleAsync(ulong.Parse(splitted[2]));
                    client.GetUser(ulong.Parse(splitted[0])).CreateDMChannelAsync().Result.SendMessageAsync(null, false, GetEmbed("Verify yourself", "Thank you for verifying your identity! Welcome in the Server! Have fun! :)"));
                }
            }
            else
            {
                if (System.IO.File.Exists("data/verifications/" + verificationCode + ".txt"))
                {
                    string[] splitted = System.IO.File.ReadAllText("data/verifications/" + verificationCode + ".txt").Split('|');
                    System.IO.File.Delete("data/verifications/" + verificationCode + ".txt");
                    client.GetUser(ulong.Parse(splitted[0])).CreateDMChannelAsync().Result.SendMessageAsync(null, false, GetEmbed("Verify yourself", "Verification failed. We are very sorry for that issue."));
                }
            }
        }
        catch
        {

        }
    }

    public static string Base64Decode(string base64)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    public static void ReceiveRequests()
    {
        while (true)
        {
            try
            {
                LegitHttpServer.HttpRequest request = server.HandleRequest();
                LegitHttpServer.HttpResponse response = new LegitHttpServer.HttpResponse();

                if (request.GetMethodStr().Equals("OPTIONS"))
                {
                    response.SetStatusCode(200);
                    response.SetStatusDescription("OK");
                    response.SetBody("");
                }
                else
                {
                    response.SetStatusCode(404);
                    response.SetStatusDescription("Resource Not Found");
                    response.SetBody("");
                }

                response.AddHeader("Vary", "origin");
                response.AddHeader("Access-Control-Allow-Headers", "*");
                response.AddHeader("Access-Control-Allow-Methods", "*");
                response.AddHeader("Access-Control-Allow-Origin", "*");
                response.AddHeader("Connection", "keep-alive");
                response.AddHeader("Via", "1.1 google");
                response.AddHeader("Content-Type", "charset=utf-8");

                try
                {
                    string URI = request.GetURI();

                    if (URI.StartsWith("/verify/") && URI.EndsWith("/") && request.GetMethodStr().Equals("GET"))
                    {
                        URI = URI.Substring("/verify/".Length);
                        URI = URI.Substring(0, URI.Length - 1);

                        if (URI.Length == 120)
                        {
                            if (System.IO.File.Exists("data/verifications/" + URI + ".txt"))
                            {
                                response.AddHeader("Set-Cookie", "TOKEN=" + CryptoUtils.GetMD5("EKRJEKRKWEJRLKWEJRKLJWERLKJWELRKJWELKRJ" + URI + "LJERKEWKLRJLWKERJLKWEJRLKWEJRLKJWERKLJW"));
                                response.SetStatusCode(200);
                                response.SetStatusDescription("OK");
                                response.SetBody(codeVerify);
                            }
                        }
                    }
                    else if (URI.Equals("/favicon.ico") && request.GetMethodStr().Equals("GET"))
                    {
                        response.SetStatusCode(200);
                        response.SetStatusDescription("OK");
                        response.SetBody(codeIcon);
                    }
                    else if (URI.EndsWith("/style.css") && request.GetMethodStr().Equals("GET") && URI.StartsWith("/verify/"))
                    {
                        response.SetStatusCode(200);
                        response.SetStatusDescription("OK");
                        response.SetBody(codeStyle);
                    }
                    else if (URI.EndsWith("/background.jpg") && request.GetMethodStr().Equals("GET") && URI.StartsWith("/verify/"))
                    {
                        response.SetStatusCode(200);
                        response.SetStatusDescription("OK");
                        response.SetBody(codeBackground);
                    }
                    else if (URI.EndsWith("/verification.js") && request.GetMethodStr().Equals("GET") && URI.StartsWith("/verify/"))
                    {
                        response.SetStatusCode(200);
                        response.SetStatusDescription("OK");
                        response.SetBody(codeVerification);
                    }
                    else if (URI.StartsWith("/verify/") && URI.EndsWith("/") && request.GetMethodStr().Equals("POST"))
                    {
                        string verificationCode = "";

                        URI = URI.Substring("/verify/".Length);
                        URI = URI.Substring(0, URI.Length - 1);

                        if (URI.Length != 153)
                        {
                            continue;
                        }

                        if (!URI.Contains("/"))
                        {
                            continue;
                        }

                        string[] splitted = URI.Split('/');

                        if (splitted[0].Length != 120)
                        {
                            continue;
                        }

                        if (splitted[1].Length != 32)
                        {
                            DoneVerification(verificationCode, false);
                            continue;
                        }

                        if (request.GetHeader("Proxy-Authorization") != null || request.GetHeader("proxy-authorization") != null)
                        {
                            DoneVerification(verificationCode, false);
                            continue;
                        }

                        verificationCode = splitted[0];

                        if (splitted[1] != CryptoUtils.GetMD5("ERJLKERJLKRJWKEJRKJWELKRJWELKRJLKWEJR" + ("https://example.com/verify/" + splitted[0] + "/") + "JHERJHEKJRHKJWHRKJHWERKJHWEKJRHWEKJHR"))
                        {
                            DoneVerification(verificationCode, false);
                            continue;
                        }

                        string cookie = request.GetHeader("Cookie");

                        if (cookie == null)
                        {
                            cookie = request.GetHeader("cookie");
                        }

                        string hashed = CryptoUtils.GetMD5("EKRJEKRKWEJRLKWEJRKLJWERLKJWELRKJWELKRJ" + verificationCode + "LJERKEWKLRJLWKERJLKWEJRLKWEJRLKJWERKLJW");

                        if (!cookie.Contains(hashed))
                        {
                            DoneVerification(verificationCode, false);
                            continue;
                        }

                        string origin = request.GetHeader("Origin");

                        if (origin == null)
                        {
                            origin = request.GetHeader("origin");
                        }

                        string referer = request.GetHeader("Referer");

                        if (referer == null)
                        {
                            referer = request.GetHeader("referer");
                        }

                        if (origin != "https://example.com")
                        {
                            DoneVerification(verificationCode, false);
                            continue;
                        }

                        if (referer != "https://example.com/verify/" + verificationCode + "/")
                        {
                            DoneVerification(verificationCode, false);
                            continue;
                        }

                        bool verified = true;
                        string entityBody = request.GetBody().Replace(" ", "").Replace('\t'.ToString(), "");

                        if (entityBody == "")
                        {
                            verified = false;
                            goto Here;
                        }

                        string contentType = request.GetHeader("Content-Type");

                        if (contentType == null)
                        {
                            contentType = request.GetHeader("content-type");
                        }

                        if (contentType != "application/json")
                        {
                            DoneVerification(verificationCode, false);
                            continue;
                        }

                        string contentLength = request.GetHeader("Content-Length");

                        if (contentLength == null)
                        {
                            contentLength = request.GetHeader("content-length");
                        }

                        if (contentLength != entityBody.Length.ToString())
                        {
                            DoneVerification(verificationCode, false);
                            continue;
                        }

                        try
                        {
                            string sec_ch_ua = "";

                            try
                            {
                                sec_ch_ua = request.GetHeader("sec-ch-ua");
                            }
                            catch
                            {

                            }

                            string xBrv = request.GetHeader("X-BRV");
                            string xDid = request.GetHeader("X-DID");
                            string userAgent = request.GetHeader("User-Agent");
                            string xVld = request.GetHeader("X-VLD");
                            string xIdf = request.GetHeader("X-IDF");
                            string xPrp = request.GetHeader("X-PRP");
                            string xChk = request.GetHeader("X-CHK");

                            if (xBrv == null)
                            {
                                xBrv = request.GetHeader("x-brv");
                            }

                            if (xDid == null)
                            {
                                xDid = request.GetHeader("x-did");
                            }

                            if (userAgent == null)
                            {
                                userAgent = request.GetHeader("user-agent");
                            }

                            if (xVld == null)
                            {
                                xVld = request.GetHeader("x-vld");
                            }

                            if (xIdf == null)
                            {
                                xIdf = request.GetHeader("x-idf");
                            }

                            if (xPrp == null)
                            {
                                xPrp = request.GetHeader("x-prp");
                            }

                            if (xChk == null)
                            {
                                xChk = request.GetHeader("x-chk");
                            }

                            if (xBrv == null)
                            {
                                verified = false;
                                goto Here;
                            }

                            if (xDid == null)
                            {
                                verified = false;
                                goto Here;
                            }

                            if (userAgent == null)
                            {
                                verified = false;
                                goto Here;
                            }

                            if (xVld == null)
                            {
                                verified = false;
                                goto Here;
                            }

                            if (xIdf == null)
                            {
                                verified = false;
                                goto Here;
                            }

                            if (xPrp == null)
                            {
                                verified = false;
                                goto Here;
                            }

                            if (xChk == null)
                            {
                                verified = false;
                                goto Here;
                            }

                            xBrv = CryptoJS.DecryptHeader(xBrv);
                            xDid = CryptoJS.DecryptHeader(xDid);
                            xVld = CryptoJS.DecryptHeader(xVld);
                            xIdf = CryptoJS.DecryptHeader(xIdf);
                            xPrp = CryptoJS.DecryptHeader(xPrp);

                            string otherBrowser = SecurityUtils.CheckAndGetBrowser(xBrv, xDid, userAgent, sec_ch_ua, xVld, xIdf, xPrp);

                            if (otherBrowser == "NULL")
                            {
                                verified = false;
                                goto Here;
                            }

                            if (otherBrowser == "Chrome" && (request.GetHeader("SEC-GPC") != null || request.GetHeader("sec-gpc") != null))
                            {
                                otherBrowser = "Brave";
                            }

                            string theTimestamp = otherBrowser.Split('|')[1], deviceUUID = otherBrowser.Split('|')[2], requestUUID = otherBrowser.Split('|')[3];

                            if (entityBody != "")
                            {
                                if (xChk != CryptoUtils.GetMD5("ERJHEJKRHWKJEHRKJWEHRJKEWHREJRHWKEJRHKWJEHR" + entityBody + "KJEKRJLKEJRLKWEJRLKJWERLKJWELRKJEWLKRJWELKRJWELRKJ" + theTimestamp + "WJWJWHWJKHKWJHWKJHWKJHWKJ"))
                                {
                                    verified = false;
                                    goto Here;
                                }
                            }

                            if (requestChecker.IsRequestUUIDAdded(requestUUID))
                            {
                                verified = false;
                                goto Here;
                            }
                            else
                            {
                                requestChecker.AddRequestUUID(requestUUID);

                                Thread thread = new Thread(() =>
                                {
                                    Thread.Sleep(7000);
                                    requestChecker.DeleteRequestUUID(requestUUID);
                                });

                                thread.Priority = ThreadPriority.Highest;
                                thread.Start();
                            }

                            if (entityBody != "" && request.HasBody())
                            {
                                entityBody = CryptoJS.DecryptStringAES(entityBody, xChk);
                                string toDecode = entityBody.Substring(0, entityBody.Length - 32);
                                string md5ToCheck = entityBody.Substring(toDecode.Length, entityBody.Length - toDecode.Length);

                                if (md5ToCheck != CryptoUtils.GetMD5("EKJHRLKERKLHWEKRHKJWEHRKJHWEJKRHWEKJHRKJWEHRKJHWERKJHWEKJHRKJWEHR" + toDecode + "MEJREJERKJERKERJHERJHERHERJEJRHJERKWHRJKWREH" + theTimestamp))
                                {
                                    verified = false;
                                    goto Here;
                                }

                                entityBody = Base64Decode(toDecode);
                            }
                            else
                            {
                                goto Here;
                            }
                        }
                        catch
                        {
                            goto Here;
                        }

                        Tuple<bool, string> thing = SecurityUtils.CheckBody(entityBody);

                        if (thing.Item2 != verificationCode)
                        {
                            verified = false;
                            goto Here;
                        }

                        verificationCode = thing.Item2;

                        if (!System.IO.File.Exists("data/verifications/" + verificationCode + ".txt"))
                        {
                            verificationCode = "";
                            verified = false;
                            goto Here;
                        }

                        if (!thing.Item1)
                        {
                            verified = false;
                            goto Here;
                        }

                        Here: if (verified)
                        {
                            DoneVerification(verificationCode, true);
                            response.SetStatusCode(200);
                            response.SetStatusDescription("OK");
                            response.SetBody("You have been succesfully verified!");
                        }
                        else
                        {
                            if (verificationCode != "")
                            {
                                DoneVerification(verificationCode, false);
                            }

                            response.SetStatusCode(200);
                            response.SetStatusDescription("OK");
                            response.SetBody("Failed to get verified! Please, try again now.");
                        }
                    }
                }
                catch
                {
                    response.SetStatusCode(400);
                    response.SetStatusDescription("Bad Request");
                    response.SetBody("");
                }

                request.WriteResponse(response);
            }
            catch
            {

            }
        }
    }
}