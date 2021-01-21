using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using GitApi;
using Microsoft.Extensions.Configuration;
using System.Management.Automation;
//PowerShell.SDK

namespace GitApi.Controllers
{

    [Route("ObjectToGit")]
	[ApiController]
	public class ObjectToGit : ControllerBase
	{
        private readonly IConfiguration Configuration;

        public ObjectToGit(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        [HttpPost]
        public string ObjectPush()
        {
            OracleObect oracleobect = new OracleObect();
            var pathGit = Configuration["PathGit"];
            var urlGit = Configuration["UrlGit"];
            var keyserver = Configuration["ApiKey"];  //parusyp to MD5 

            //считывание параметров из заголовка запроса
            oracleobect.NameDB = Request.Headers["NameDB"].ToString();
            oracleobect.NameObject = Request.Headers["NameObject"].ToString();
            string apikey = Request.Headers["ApiKey"].ToString();
            string username = "\"" + Request.Headers["UserName"].ToString() + "\"";
            string usermail = "\"" + Request.Headers["UserMail"].ToString() + "\"";
            string commitmes = "'" + Request.Headers["CommitMes"].ToString() + "'";

            string repository = pathGit + oracleobect.NameDB; // каталог к git repository
            string path = pathGit + oracleobect.NameDB + @"\";
            string writePath = path + oracleobect.NameObject;
            string result = "OK";
            string commithash = "";

            //проверка APIKEY
            if (keyserver != apikey)
            {
                result = "Error ApiKey " + apikey;
                return result;
            }

            //поиск и создание каталога на диске
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }

            //создание файла на диске
            try
            {
                //считывание тела запроса
                using (var reader = new StreamReader(Request.Body))
				{
                    oracleobect.TextObject = reader.ReadToEnd();
                }   
                //запись на диск
                using (StreamWriter sw = new StreamWriter(writePath, false, System.Text.Encoding.Default))
                {
                    sw.WriteLine(oracleobect.TextObject);
                    sw.Close();
                }

                //отправка в репозиторий GIT
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                
                startInfo.FileName = "cmd.exe";
                startInfo.Verb = "runas";
                //startInfo.Arguments = "cd " + repository.Substring(0, 2);

                startInfo.WorkingDirectory = @"E:\GIT\MIAC";
                //startInfo.Arguments = "cd /d {repository}";
                startInfo.Arguments = @"git init";
                /*
                startInfo.Arguments = @"git config user.name " + username;
                startInfo.Arguments = @"git config user.email " + usermail;
                startInfo.Arguments = @"git init";
                startInfo.Arguments = @"git add *";
                startInfo.Arguments = @"git commit -m " + commitmes;
                //startInfo.Arguments = "git remote add " + oracleobect.NameDB + " https://github.com/slepovep/" + oracleobect.NameDB + ".git"; //https
                startInfo.Arguments = @"git remote add " + oracleobect.NameDB + urlGit + oracleobect.NameDB + ".git"; //SSH
                startInfo.Arguments = @"git remote set-url " + oracleobect.NameDB + urlGit + oracleobect.NameDB + ".git"; //SSH
                startInfo.Arguments = @"git push " + oracleobect.NameDB + " master";
                startInfo.Arguments = @"git log -1 --pretty=format:" + "\"" + "%H" + "\"";
                startInfo.Arguments = @"exit;";
                */

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo = startInfo;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();

                process.StandardInput.WriteLine("git config user.name " + username);
                process.StandardInput.WriteLine("git config user.email " + usermail);
                process.StandardInput.WriteLine("git init");
                process.StandardInput.WriteLine("git add *");
                process.StandardInput.WriteLine("git commit -m " + commitmes);
                process.StandardInput.WriteLine("git remote add " + oracleobect.NameDB + urlGit + oracleobect.NameDB + ".git");
                process.StandardInput.WriteLine("git remote set-url " + oracleobect.NameDB + urlGit + oracleobect.NameDB + ".git");
                process.StandardInput.WriteLine("git push " + oracleobect.NameDB + " master");
                process.StandardInput.WriteLine("git log -1 --pretty=format:" + "\"" + "%H" + "\"");

                
                while (!process.StandardOutput.EndOfStream)
                {
                    commithash = process.StandardOutput.ReadLine();
                }

                process.StandardInput.WriteLine("exit");
                process.WaitForExit();
  
                
                result = result + '|' + commithash;
                /*
                using (PowerShell powershell = PowerShell.Create())
                {
                    // this changes from the user folder that PowerShell starts up with to your git repository
                    powershell.AddScript($"cd {repository}");
                    //авторизация
                    powershell.AddScript(@"git config user.name " + username);
                    powershell.AddScript(@"git config user.email " + usermail);

                    powershell.AddScript(@"git init");
                    powershell.AddScript(@"git add *");
                    powershell.AddScript(@"git commit -m " + commitmes);
                    //powershell.AddScript(@"git remote add "+ oracleobect.NameDB +" https://github.com/slepovep/" + oracleobect.NameDB + ".git");  //https
                    powershell.AddScript(@"git remote set-url " + oracleobect.NameDB + urlGit + oracleobect.NameDB + ".git"); //SSH
                    powershell.AddScript(@"git push "+ oracleobect.NameDB +" master");
                    powershell.AddScript(@"git log -1 --pretty=format:" + "\"" + "%H" + "\"");
                    //powershell.Invoke();

                    var psresults = powershell.Invoke();
                    foreach (var psresult in psresults)
                    {
                        commithash = psresult.ToString();
                    }
                    result = result + '|' + commithash;
                }*/

            }
            catch (Exception ex)
            {
                result = ex.Message;
                //throw ex;
            }
            return result;
        }
    }
}
