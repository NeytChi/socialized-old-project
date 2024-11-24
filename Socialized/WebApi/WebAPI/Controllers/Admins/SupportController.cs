using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using Managment;
using database.context;
using Models.AdminPanel;

namespace WebAPI.Controllers.Admins
{
    [Route("v1.0/[controller]/[action]/")]
    [ApiController]
    public class SupportController : ControllerResponseHandler
    {
        public Support support;

        [HttpPost]
        [ActionName("CreateAppeal")]
        public ActionResult<dynamic> CreateAppeal(SupportCache cache)
        {
            string message = string.Empty;

            Appeal appeal;
            if ((appeal = support.CreateAppeal(cache, ref message)) != null)
                return new
                {
                    success = true,
                    data = new
                    {
                        appeal_id = appeal.appealId,
                        appeal_subject = appeal.appealSubject,
                        appeal_state = appeal.appealState,
                        created_at = appeal.createdAt,
                    }
                };
            return StatusCode500(message);
        }
        [HttpGet]
        [ActionName("UAppeals")]
        public ActionResult<dynamic> UGetAppeals([FromQuery] int since = 0, [FromQuery] int count = 15)
        {
            string userToken = HttpContext?.Request.Headers.Where(h
                => h.Key == "Authorization").Select(h => h.Value)
                .FirstOrDefault();

            return new { success = true, data = new { appeals = support.GetAppealsByUser(userToken, since, count) } };
        }
        [HttpGet]
        [Authorize]
        [ActionName("AAppeals")]
        public ActionResult<dynamic> AGetAppeals([FromQuery] int since = 0, [FromQuery] int count = 15)
        {
            return new { success = true, data = new { appeals = support.GetAppealsByAdmin(since, count) } };
        }
        [HttpGet]
        [ActionName("UMessages")]
        public ActionResult<dynamic> UMessages([FromQuery] int appeal_id,
            [FromQuery] int since, [FromQuery] int count)
        {
            string message = string.Empty;
            string userToken = HttpContext?.Request.Headers.Where(h
                => h.Key == "Authorization").Select(h => h.Value)
                .FirstOrDefault();

            if (support.GetNonDeleteUser(userToken, ref message) != null)
            {
                return new
                {
                    success = true,
                    data = new
                    {
                        messages = support.GetAppealMessages(appeal_id, since, count)
                    }
                };
            }
            return StatusCode500(message);
        }
        [HttpGet]
        [Authorize]
        [ActionName("AMessages")]
        public ActionResult<dynamic> AMessages([FromQuery] int appeal_id,
            [FromQuery] int since, [FromQuery] int count)
        {
            string message = string.Empty;

            if (support.UpdateReadAppeal(appeal_id, ref message))
            {
                return new
                {
                    success = true,
                    data = new
                    {
                        messages = support.GetAppealMessages(appeal_id, since, count)
                    }
                };
            }
            return StatusCode500(message);
        }
        [HttpPost]
        [ActionName("USendMessage")]
        public ActionResult<dynamic> USendMessage(IFormCollection data)
        {
            string message = string.Empty;
            SupportCache cache = new SupportCache();
            AppealMessage appealMessage;

            if (support.GetCacheFromData(data, ref cache, ref message))
            {
                if ((appealMessage = support.SendMessage(cache, ref message)) != null)
                {
                    return new
                    {
                        success = true,
                        data = new
                        {
                            message = new
                            {
                                message_id = appealMessage.messageId,
                                message_text = appealMessage.messageText,
                                created_at = appealMessage.createdAt,
                                files = appealMessage.files.Select(f
                                => new
                                {
                                    file_id = f.fileId,
                                    file_url = support.fileDomen + f.relativePath
                                }).ToArray()
                            }
                        }
                    };
                }
            }
            return StatusCode500(message);
        }
        [HttpPost]
        [Authorize]
        [ActionName("ASendMessage")]
        public ActionResult<dynamic> ASendMessage(IFormCollection data)
        {
            string message = string.Empty;
            SupportCache cache = new SupportCache();
            AppealMessage appealMessage;

            if (support.GetCacheFromData(data, ref cache, ref message))
            {

                cache.admin_id = HttpContext != null ?
                    int.Parse(HttpContext.User?.Claims.FirstOrDefault().Value) : 0;

                if ((appealMessage = support.SendMessage(cache, ref message)) != null)
                {
                    return new
                    {
                        success = true,
                        data = new
                        {
                            message = new
                            {
                                message_id = appealMessage.messageId,
                                message_text = appealMessage.messageText,
                                created_at = appealMessage.createdAt,
                                files = appealMessage.files.Select(f => new
                                {
                                    file_id = f.fileId,
                                    file_url = support.fileDomen + f.relativePath
                                }).ToArray()
                            }
                        }
                    };
                }
            }
            return StatusCode500(message);
        }
        [HttpPost]
        [ActionName("UEndAppeal")]
        public ActionResult<dynamic> UEndAppeal(SupportCache cache)
        {
            string message = string.Empty;

            if (support.GetNonDeleteUser(cache.user_token, ref message) != null)
            {
                if (support.EndAppeal(cache.appeal_id, ref message))
                    return new { success = true, message = "Appeal completed." };
            }
            return StatusCode500(message);
        }
        [HttpPost]
        [Authorize]
        [ActionName("AEndAppeal")]
        public ActionResult<dynamic> AEndAppeal(SupportCache cache)
        {
            string message = string.Empty;

            if (support.EndAppeal(cache.appeal_id, ref message))
                return new { success = true, message = "Appeal completed." };
            return StatusCode500(message);
        }
    }
}
