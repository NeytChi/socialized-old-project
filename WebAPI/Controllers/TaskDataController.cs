using Serilog;
using Serilog.Core;
using database.context;
using InstagramService;
using Models.GettingSubscribes;
using Microsoft.AspNetCore.Mvc;
using Managment;
using Controllers;

namespace WebAPI.Controllers
{
    /// <summary>
    /// This class necessary to manage user instagram functional.
    /// </summary>
    [Route("v1.0/[controller]/[action]/")]
    [ApiController]
    public class TaskDataController : ControllerBase
    {
        private TaskDataCondition taskDataHandler;
        private readonly Context context;
        private TaskDataManager dataManager;
        public Logger log = new LoggerConfiguration()
            .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        public TaskDataController(Context context)
        {
            this.context = context;
            dataManager = new TaskDataManager(log, context);
            taskDataHandler = new TaskDataCondition(log,
                new SessionManager(context),
                new SessionStateHandler(context));
        }
        [HttpPost]
        [ActionName("Create")]
        public ActionResult<dynamic> Create(TaskCache taskCache)
        {
            string message = null;
            TaskGS task = dataManager.GetNonDeleteTask(taskCache.task_id, taskCache.user_token, ref message);
            if (task != null)
            {
                TaskData taskData = dataManager.GetTaskData((TaskSubtype)task.taskSubtype, taskCache, ref message);
                if (taskData != null)
                    if (taskDataHandler.handle((TaskSubtype)task.taskSubtype, task.sessionId, taskData, ref message))
                    {
                        dataManager.SaveTaskData(task, taskData);
                        return new { success = true, data = dataManager.GetTaskDataOutput(taskData) };
                    }
            }
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("Delete")]
        public ActionResult<dynamic> Delete(TaskCache taskCache)
        {
            string message = null;
            TaskData data = dataManager.GetNonDeleteData(taskCache.data_id,
            taskCache.user_token, ref message);
            if (data != null)
            {
                dataManager.DeleteData(data, ref message);
                return new { success = true, message };
            }
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("StartStop")]
        public ActionResult<dynamic> StartStop(TaskCache taskCache)
        {
            string message = null;
            TaskData data = dataManager.GetNonDeleteData(taskCache.data_id,
            taskCache.user_token, ref message);
            if (data != null)
            {
                dataManager.StartStopData(data, ref message);
                return new { success = true, message };
            }
            return Return500Error(message);
        }
        public dynamic Return500Error(string message)
        {
            if (Response != null)
                Response.StatusCode = 500;
            log.Warning(message + " IP -> " + HttpContext?.Connection.RemoteIpAddress.ToString() ?? "");
            return new { success = false, message };
        }
    }
}