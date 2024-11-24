using System;
using System.Linq;
using Serilog;
using Serilog.Core;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

using Managment;
using InstagramService;
using database.context;
using Models.Common;
using Models.GettingSubscribes;
using Controllers;

namespace WebAPI.Controllers
{
    [Route("v1.0/[controller]/[action]/")]
    [ApiController]
    public class TaskGSController : ControllerBase
    {
        public TaskGSManager taskManager;
        private IHandlerGS handlerGS;
        private JsonHandler jsonHandler;
        public Context context;
        public Logger log = new LoggerConfiguration()
            .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        public TaskGSController(Context context)
        {
            this.context = context;
            taskManager = new TaskGSManager(context);
            CreateControllerHandlers();
        }
        /// <summary>
        /// Create task to regular perform by server side.
        /// </summary>
        [HttpPost]
        [ActionName("Create")]
        public ActionResult<dynamic> Create(JObject json)
        {
            string message = null;
            JToken userToken, sessionId;
            if ((userToken = jsonHandler.handle(ref json, "user_token", JTokenType.String, ref message)) != null)
            {
                if ((sessionId = jsonHandler.handle(ref json, "session_id", JTokenType.Integer, ref message)) != null)
                {
                    User user = GetUserByToken(userToken.ToString(), ref message);
                    if (user != null)
                    {
                        TaskGS task = new TaskGS();
                        task.sessionId = sessionId.ToObject<long>();
                        task.createdAt = DateTimeOffset.Now.ToUnixTimeSeconds();
                        if (taskManager.CheckUsableSession(task.sessionId, ref message))
                            if (handlerGS.handle(ref json, ref task, ref message))
                                return new { success = true, data = taskManager.SelectCurrentTask(task) };
                    }
                }
            }
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("Delete")]
        public ActionResult<dynamic> Delete(TaskCache cache)
        {
            string message = null;
            User user = GetUserByToken(cache.user_token, ref message);
            if (user != null)
                if (taskManager.Delete(cache.task_id, user.userId, ref message))
                    return new { success = true, message };
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("SelectAll")]
        public ActionResult<dynamic> SelectAll(TaskCache taskCache)
        {
            string message = null;
            User user = GetUserByToken(taskCache.user_token, ref message);
            if (user != null)
            {
                List<dynamic> data = taskManager.SelectTasks(user.userId,
                    taskCache.session_id, ref message);
                if (data != null)
                    return new { success = true, data };
            }
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("Select")]
        public ActionResult<dynamic> Select(TaskCache cache)
        {
            string message = null;
            User user = GetUserByToken(cache.user_token, ref message);
            if (user != null)
            {
                TaskGS task = taskManager.SelectTask(cache.task_id, user.userId, ref message);
                if (task != null)
                    return new { success = true, data = taskManager.SelectCurrentTask(task) };
            }
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("StartStop")]
        public ActionResult<dynamic> StartStop(TaskCache taskCache)
        {
            string message = null;
            User user = GetUserByToken(taskCache.user_token, ref message);
            if (user != null)
                if (taskManager.StartStopTask(taskCache.task_id, user.userId, ref message))
                    return new { success = true, message };
            return StatusCode(500, message);
        }

        public void CreateControllerHandlers()
        {
            jsonHandler = new JsonHandler(log);
            IHandlerGS defineTaskGS = new TaskDefine(log);
            IHandlerGS according = new TaskAccording(log);
            IHandlerGS checkTaskGS = new TaskCheck(log, context);
            IHandlerGS taskData = new TaskCondition(log);
            IHandlerGS checkTask = new TaskDataCondition(log,
                new SessionManager(context),
                new SessionStateHandler(context));
            IHandlerGS taskOptions = new OptionsCondition(log);
            IHandlerGS taskFilters = new FiltersCondition(log);
            IHandlerGS saveTask = new TaskSave(log, context);
            taskFilters.setNext(saveTask);
            taskOptions.setNext(taskFilters);
            checkTask.setNext(taskOptions);
            taskData.setNext(checkTask);
            checkTaskGS.setNext(taskData);
            according.setNext(checkTaskGS);
            defineTaskGS.setNext(according);
            handlerGS = defineTaskGS;
        }
        public User GetUserByToken(string userToken, ref string message)
        {
            User user = context.Users.Where(u
                => u.userToken == userToken).FirstOrDefault();
            if (user == null)
                message = "Server can't define user.";
            return user;
        }
    }
}