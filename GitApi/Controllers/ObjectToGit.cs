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
