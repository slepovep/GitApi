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
        //[Produces("application/json")]
        public string ObjectPush()
        {
            OracleObect oracleobect = new OracleObect();
            var pathGit = Configuration["PathGit"];

            //считывание параметров из заголовка запроса
            oracleobect.NameDB = Request.Headers["NameDB"].ToString();
            oracleobect.NameObject = Request.Headers["NameObject"].ToString();

            string repository = pathGit + oracleobect.NameDB; // каталог к git repository
            string path = pathGit + oracleobect.NameDB + @"\";
            string writePath = path + oracleobect.NameObject;
            string result = "OK";

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
                }
                //отправка в репозиторий GIT
                string username = "\""+"SLEPOV"+"\"";
                string usermail = "\""+"slepov1.ep@gmail.com"+ "\"";

                using (PowerShell powershell = PowerShell.Create())
                {
                    // this changes from the user folder that PowerShell starts up with to your git repository
                    powershell.AddScript($"cd {repository}");
                    //авторизация
                    powershell.AddScript(@"git config user.name " + username);
                    powershell.AddScript(@"git config user.email " + usermail);


                    powershell.AddScript(@"git init");
                    powershell.AddScript(@"git add *");
                    powershell.AddScript(@"git commit -m 'git commit from PowerShell in C#'");
                    //powershell.AddScript(@"git remote add "+ oracleobect.NameDB +" https://github.com/slepovep/" + oracleobect.NameDB + ".git");  //https
                    powershell.AddScript(@"git remote set-url " + oracleobect.NameDB + " git@bitbucket.org:parustest/" + oracleobect.NameDB + ".git"); //SSH

                    powershell.AddScript(@"git push "+ oracleobect.NameDB +" master");

                    powershell.Invoke();
                }

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
