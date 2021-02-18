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
//using System.Management.Automation;
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
            string result = "OK";

            try
            {            
                OracleObect oracleobect = new OracleObect();
                var pathGit = Configuration["PathGit"];
                var urlGit = Configuration["UrlGit"];
                var keyserver = Configuration["ApiKey"];  //parusyp to MD5 

                //считывание параметров из заголовка запроса
                string headercoding = Request.Headers["HeaderBase64"].ToString();  //кодировка заголовка
                oracleobect.NameDB = Request.Headers["NameDB"].ToString();
                oracleobect.NameObject = Request.Headers["NameObject"].ToString();
            
                string apikey = Request.Headers["ApiKey"].ToString();
                string username = "\"" + Request.Headers["UserName"].ToString() + "\"";
                string usermail = "\"" + Request.Headers["UserMail"].ToString() + "\"";
                string commitmes = "\"" + Base64Decode(Request.Headers["CommitMes"].ToString(), headercoding) + "\"";
                string directory = Base64Decode(Request.Headers["Directory"].ToString(), headercoding);
                string path = pathGit + oracleobect.NameDB;
                string repository = path;
                string writePath = "";

                if (!string.IsNullOrEmpty(directory)) 
                {
                    path = path + "\\" + directory + @"\";
                    writePath = path + oracleobect.NameObject;
                }
                else
			    {
                    path = path + @"\";
                    writePath = path + @"\" + oracleobect.NameObject; 
                }

                repository = repository.Replace("\\", @"\"); // каталог к git repository
                string commithash = "";
                string gitcommands = "";
                string gitoutput = "";
                string stroutput = "";

                //проверка APIKEY
                if (keyserver != apikey)
                {
                    result = "Error ApiKey " + apikey;
                    return result;
                }

                    //создание файла на диске
                    //поиск и создание каталога на диске
                    DirectoryInfo dirInfo = new DirectoryInfo(path);
                    if (!dirInfo.Exists)
                    {
                        dirInfo.Create();
                    }

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
                    startInfo.RedirectStandardError = true;
                    startInfo.FileName = "cmd.exe";
                    startInfo.Verb = "runas";
                    startInfo.WorkingDirectory = repository;
   

                    gitcommands = @"/C git init" + " & " +
                                  "git config user.name " + username + " & " +
                                  "git config user.email " + usermail + " & " +
                                  "git add *" + " & " +
                                  "git commit -m " + commitmes + " & " +
                                  "git remote add " + oracleobect.NameDB + " " + urlGit + oracleobect.NameDB + ".git" + " & " + //SSH
                                  "git remote set-url " + oracleobect.NameDB + " " + urlGit + oracleobect.NameDB + ".git" + " & " + //SSH
                                  "git push " + oracleobect.NameDB + " master" + " & " +
                                  "git log -1 --pretty=format:" + "\"" + "%H" + "\"";
                
                    startInfo.Arguments = gitcommands;

                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    process.StartInfo = startInfo;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.RedirectStandardOutput = true;

                    process.Start();

                    while (!process.StandardOutput.EndOfStream)
                    {
                        stroutput = process.StandardOutput.ReadLine();
                        gitoutput = gitoutput + " " + stroutput;
                    }
                    commithash = stroutput;
                    gitoutput = gitoutput.Substring(2);
                    string errors = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    process.Close();


                    result = result + '|' + commithash + '|' + gitoutput + '|' + errors + '|' + gitcommands;

                    /*  //использование PowerShell
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

        public static string Base64Decode(string base64EncodedData, string headerCoding64)
        {
            if (headerCoding64 == "1")
            {
                var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
                return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }
			else 
            {
                return base64EncodedData;
            }
        }

    }
}
