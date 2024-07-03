using Serilog;
using Serilog.Core;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

using Managment;
using database.context;
using Models.AutoPosting;

namespace Controllers
{
    /// <summary>
    /// This class is needed to provide an API for AutoPosting.
    /// </summary>
    [Route("v1.0/[controller]/[action]/")]
    [ApiController]
    public class AutoPostController : ControllerBase
    {
        private readonly Context context;
        private AutoPostingManager manager;
        private PackageCondition access;
        public Logger log = new LoggerConfiguration()
            .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
		
        public AutoPostController(Context context)
        {
            this.context = context;
            this.manager = new AutoPostingManager(log, context);
            this.access = new PackageCondition(context, log);
        }
        [HttpPost]
        [ActionName("Create")]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        public ActionResult<dynamic> Create(List<IFormFile> files, IFormCollection post)
        {
            string postData, message = null;
            if (files != null && post != null) {
                postData = post["post"];
                if (!string.IsNullOrEmpty(postData)) {
                    AutoPostCache cache = JsonConvert.DeserializeObject<AutoPostCache>(postData);
                    cache.files = files;
                    if (manager.CreatePost(cache, ref message))
                        return new { success = true };
                }
                else
                    message = "Field 'post' is null or empty.";
            }
            else
                message = "File && post can't be null.";
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("StartStop")]
        public ActionResult<dynamic> StartStop(AutoPostCache autoPost)
        {
            string message = null;
            if (manager.StartStop(autoPost, ref message))
                return new { success = true };
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("GetByCategory")]
        public ActionResult<dynamic> GetByCategory(AutoPostCache autoPost)
        {
            string message = null;
            var data = manager.GetByCategory(autoPost, ref message);
            if (data != null)
                return new { success = true, data = data };
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("Update")]
        public ActionResult<dynamic> Update(AutoPostCache cache)
        {
            string message = null;    
            if (manager.UpdateAutoPost(cache, ref message))
                return new { success = true };
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("Delete")]
        public ActionResult<dynamic> Delete(AutoPostCache autoPost)
        {
            string message = null;
            if (manager.Delete(autoPost, ref message))
                return new { success = true };
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("AddFiles")]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        public ActionResult<dynamic> AddFiles(List<IFormFile> files, IFormCollection post)
        {
            List<PostFile> postFiles;
            string message = null, postData = post["post"].ToString();

            if (!string.IsNullOrEmpty(postData)) {
                AutoPostCache cache = JsonConvert.DeserializeObject<AutoPostCache>(postData);
                cache.files = files;
                if ((postFiles = manager.AddFilesToPost(cache, ref message)) != null)
                    return new { success = true, data = new { files = manager.GetPostFilesToOutput(postFiles) } };
            }
            else
                message = "Field 'post' is null or empty.";
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("Recovery")]
        public ActionResult<dynamic> Recovery(AutoPostCache cache)
        {
            string message = null;    
            if (manager.Recovery(cache, ref message))
                return new { success = true };
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("UpdateOrder")]
        public ActionResult<dynamic> UpdateOrder(AutoPostCache autoPost)
        {
            string message = null;
            if (manager.UpdateOrderFile(autoPost, ref message))
                return new { success = true };
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("DeleteFile")]
        public ActionResult<dynamic> DeleteFile(AutoPostCache autoPost)
        {
            string message = null;
            if (manager.DeletePostFile(autoPost, ref message))
                return new { success = true };
            return Return500Error(message);
        }
        public dynamic Return500Error(string message)
        {
            if (Response != null)
                Response.StatusCode = 500;
            log.Warning(message + " IP -> " + 
            HttpContext?.Connection.RemoteIpAddress.ToString() ?? "");
            return new { success = false, message = message };
        }
    }
}