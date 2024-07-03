using System;
using System.Linq;
using Serilog;
using Serilog.Core;
using System.Threading;
using System.Collections.Generic;

using Common;
using Managment;
using nmediator;
using database.context;
using Models.GettingSubscribes;
using Models.SessionComponents;
using InstagramService;
using InstagramApiSharp.API.Builder;

namespace ngettingsubscribers
{
    /// <summary>
    /// This class handle 'Getting Subscribers' tasks by timer.
    /// <remarks>
    /// For testing this class you need:
    /// a) Instance of InstasoftContext (in memory required);
    /// b) ServiceMediator instance of class to handle unexpected responce from Instagram service;
    /// </remarks>
    /// </summary>
    public class GettingSubscribers
    {
        public FiltersGS filters;
        public List<BaseModeGS> ModeGS = new List<BaseModeGS>();
        public ReceiverUnitsGS receiver;
        public Timer starterTask;
        public Context contextGS;
        public InstagramTiming timing;
        public SessionManager sessionManager;
        Logger log = new LoggerConfiguration()
            .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        public GettingSubscribers(Context context)
        {
            this.contextGS = context;
            filters = FiltersGS.GetInstance(new SessionStateHandler(context));
            receiver = ReceiverUnitsGS.GetInstance();
            timing = new InstagramTiming(log);
            sessionManager = SessionManager.CreateInstance(contextGS);
            CreateModeGS();
            //starterTask = new Timer(StartBranchByTimer, null, 5000, 15000);
        }
        public GettingSubscribers()
        {
            this.contextGS = new Context(true);
            filters = FiltersGS.GetInstance(new SessionStateHandler(contextGS));
            receiver = ReceiverUnitsGS.GetInstance();
            timing = new InstagramTiming(log);
            sessionManager = SessionManager.CreateInstance(contextGS);
            CreateModeGS();
            //starterTask = new Timer(StartBranchByTimer, null, 5000, 15000);
        }
        public void CreateModeGS()
        {
            SessionStateHandler sessionState = new SessionStateHandler(contextGS);
            OptionsGS options = OptionsGS.GetInstance(sessionState);
            ModeGS.Add(new LikingGS(options,log, sessionState));
            ModeGS.Add(new BlockingGS(options,log, sessionState));
            ModeGS.Add(new CommentsGS(options,log, sessionState));
            ModeGS.Add(new FollowingGS(options,log, sessionState));
            ModeGS.Add(new UnfollowingGS(options,log, sessionState));
            ModeGS.Add(new WatchStoriesGS(options,log, sessionState));
        }
        #region  Starting and handling branch.
        public void StartBranchByTimer(object input)
        {
            UpdateTimesActionsCount();
            List<TaskBranch> branches = GetBranchesToExecute();
            foreach(TaskBranch branch in branches) {
                Thread start = new Thread(() => RunBranch(new Context(true), branch));
                start.Start();
            }
            log.Information("Start branches by timer. Branches count -> " + branches.Count + ".");
        }
        public List<TaskBranch> GetBranchesToExecute()
        {
            return contextGS.Branches.Where(b 
                => b.branchStopped == false
                && b.branchRunning == false
                && b.startRunAt < DateTime.Now.AddSeconds(30)
                && b.branchDeleted == false
            ).ToList();
        }
        public void RunBranch(Context context, TaskBranch branch)
        {
            log.Information("Start handle branch; branchId -> " + branch.branchId);
            UpdateBranchToRun(context, ref branch);
            if (branch.startRunAt > DateTime.Now)
                Thread.Sleep(branch.startRunAt - DateTime.Now);
            if (SetUpBranch(context, ref branch)) {
                TimesAction action = context.TimesAction.Where(a => a.sessionId == branch.sessionId).First();
                if (AccessPerformAction(branch.currentTask.taskActions, action)) {
                    IModeGS mode = ModeGS.Where(m => m.typeMode == branch.currentTask.taskType).First();
                    RunUnits(context, branch, mode);
                    StopBranchToNextHandle(context, branch);
                }
                else
                    StopBranchToNextHandle(context, branch);
            }
        }
        /// <summary>
        /// This method get non-handled units GS and run them with one of mode GS.
        /// </summary>
        public void RunUnits(Context context, TaskBranch branch, IModeGS modeGS)
        {   
            bool end = false;
            while(!end) {
                branch.currentUnit = GetNonHandlerUnit(context, branch.currentTaskData.dataId);
                if (branch.currentUnit != null) {
                    end = HandleTask(context, ref branch, modeGS);
                    context.Units.Update(branch.currentUnit);
                    context.SaveChanges();
                }
                else 
                    end = receiver.SetInstagramUnits(ref branch.session, branch.currentTaskData,
                    (TaskSubtype)branch.currentTask.taskSubtype) ? false : true;
            }
            log.Information("Handle task, id -> " + branch.currentTask.taskId);
        }
        /// <summary>
        /// This method provide handling of task, including filter's checking, handling by one of GS mode
        /// and saving historyGS.
        /// </summary>
        public bool HandleTask(Context context, ref TaskBranch branch, IModeGS mode)
        {
            if (filters.CheckByAllFilters(ref branch.session,
            branch.currentTask.taskFilter, branch.currentUnit.userPk)) {
                bool result = mode.HandleTask(context, ref branch);
                return CheckToEndHandle(context, branch.currentUnit, 
                branch.currentTask.taskId, result);
            }
            return false;
        }
        /// <summary>
        /// True - when need to end handling task(It's when unit was completely 
        /// handled or something went wrong with handling task).
        /// False - when need new current unit to handle. 
        /// <summary>
        public bool CheckToEndHandle(Context context, UnitGS unit, long taskId, bool handleResult)
        {
            if (handleResult) {
                if (!unit.handleAgain)
                    SaveHistory(context, unit.username, taskId);
                return true;
            }
            else {
                if (!unit.unitHandled)
                    log.Information("Can't handle unit, unitId ->" + unit.unitId);
                else
                    return true;
            }
            return false;
        }
        public void StopBranchToNextHandle(Context context, TaskBranch branch)
        {
            EndTaskGS(context, branch.currentTask);
            TimesAction action = context.TimesAction.Where(a => a.sessionId == branch.sessionId).First();
            branch.startRunAt = DefineNextStartAt(action, branch.currentTask.taskActions);
            branch.branchRunning = false;
            context.Branches.Attach(branch).Property(b => b.branchRunning).IsModified = true;
            context.SaveChanges();
            context.Branches.Attach(branch).Property(b => b.startRunAt).IsModified = true;
            context.SaveChanges();
            branch.nextTask = DefineNextTask(context, branch.branchId, branch.nextTask);
            context.Branches.Attach(branch).Property(b => b.nextTask).IsModified = true;
            context.SaveChanges();
            log.Information("Stop branch tread to next handle, id ->" + branch.branchId);
        }
        public DateTime DefineNextStartAt(TimesAction action, ICollection<TaskAction> actions)
        {
            if (AccessPerformAction(actions, action)) {
                int timeStart = timing.GetBiggerActionTime(actions, action.accountOld);
                return DateTime.Now.AddMilliseconds(timeStart);
            }
            return DateTime.Now.AddDays(1);
        }
        public long DefineNextTask(Context context, long branchId, long handledTask)
        {
            long nextTask = 0;
            nextTask = GetNextTaskId(context, branchId, handledTask);
            if (nextTask == 0)
            {
                nextTask = GetAnyTaskId(context, branchId);
            }
            return nextTask;
        }
        public long GetNextTaskId(Context context, long branchId, long handledTask)
        {
            return context.TaskGS.Where(t 
            => t.branchId == branchId
            && t.taskId > handledTask
            && t.taskStopped == false
            && t.taskDeleted == false
            ).Select(t => t.taskId).FirstOrDefault();
        }
        public long GetAnyTaskId(Context context, long branchId)
        {
            return context.TaskGS.Where(t 
            => t.branchId == branchId
            && t.taskStopped == false
            && t.taskDeleted == false
            ).Select(t => t.taskId).FirstOrDefault();
        }
        public void EndTaskGS(Context context, TaskGS task)
        {
            task.taskRunning = false;
            context.TaskGS.Attach(task).Property(t => t.taskRunning).IsModified = true;
            context.SaveChanges();
            task.lastDoneAt = (int)(DateTimeOffset.Now.ToUnixTimeSeconds());
            context.TaskGS.Attach(task).Property(t => t.lastDoneAt).IsModified = true;
            context.SaveChanges();
            task.nextTaskData = DefineNextTaskData(context, task.taskId, task.nextTaskData);
            context.TaskGS.Attach(task).Property(t => t.nextTaskData).IsModified = true;
            context.SaveChanges();
        }
        public long DefineNextTaskData(Context context, long taskId, long handledData)
        {
            long nextTaskData = 0;
            nextTaskData = GetNextTaskDataId(context, taskId, handledData);
            if (nextTaskData == 0)
                nextTaskData = GetAnyTaskDataId(context, taskId);
            return nextTaskData;
        }
        public long GetNextTaskDataId(Context context, long taskId, long handledData)
        {
            return context.TaskData.Where(d 
            => d.taskId == taskId
            && d.dataId > handledData
            && d.dataStopped == false
            && d.dataDeleted == false
            && d.dataComment == null
            ).Select(d => d.dataId).FirstOrDefault();
        }
        public long GetAnyTaskDataId(Context context, long taskId)
        {
            if (context != null)
            {
                return context.TaskData.Where(d 
                => d.taskId == taskId
                && d.dataStopped == false
                && d.dataDeleted == false
                && d.dataComment == null
                ).Select(d => d.dataId).FirstOrDefault();
            }
            return 0;
        }
        #endregion
        #region Preparing branch to handling.
        public bool SetUpBranch(Context context, ref TaskBranch branch)
        {
            branch.session = sessionManager.LoadSession(branch.sessionId);
            if (branch.session != null)
            {
                if (CheckBranchTasks(context, branch.branchId))
                {
                    branch.currentTask = GetTask(context, branch.branchId, branch.nextTask);
                    if (branch.currentTask != null)
                    {
                        branch.currentTaskData = GetTaskData(context, 
                        branch.currentTask.taskId, branch.currentTask.nextTaskData);
                        if (branch.currentTask != null)
                            return true;
                    }
                }
            }
            log.Information("Can't set up branch, id ->" + branch.branchId);
            return false;
        }
        public bool CheckBranchTasks(Context context, long branchId)
        {
            List<TaskGS> tasks = context.TaskGS.Where(t 
                => t.branchId == branchId).ToList();
            if (tasks.All(t => t.taskStopped))
                StopBranch(context, branchId);
            else if (tasks.All(t => t.taskDeleted))
                DeleteBranch(context, branchId);
            else
                return true;
            return false;
        }
        public void StopBranch(Context context, long branchId)
        {
            TaskBranch branch = context.Branches.Where(b
            => b.branchId == branchId).First();
            branch.branchStopped = true;
            branch.branchRunning = false;
            context.Branches.Update(branch);
            context.SaveChanges();
        }
        public void DeleteBranch(Context context, long branchId)
        {
            TaskBranch branch = context.Branches.Where(b
            => b.branchId == branchId).First();
            branch.branchStopped = true;
            branch.branchRunning = false;
            branch.branchDeleted = true;
            context.Branches.Update(branch);
            context.SaveChanges();
        }
        public void UpdateBranchToRun(Context context, ref TaskBranch branch)
        {
            branch.branchRunning = true;
            context.Branches.Attach(branch).Property(b => b.branchRunning)
            .IsModified = true;
            context.SaveChanges();
        }
        #endregion
        #region Preparing task.
        public TaskGS GetTask(Context context, long branchId, long nextTask)
        {
            TaskGS currentTask = GetTaskToExecute(context, branchId, nextTask);
            if (currentTask != null)
                if (GetFullTask(context, ref currentTask))
                    return currentTask;
            return null;
        }
        public bool GetFullTask(Context context, ref TaskGS task)
        {
            long taskId = task.taskId;
            task.taskFilter = GetTaskFilter(context, task.taskId);
            task.taskActions = context.TaskActions.Where(a => a.taskId == taskId).ToList();
            task.taskOption = context.TaskOptions.Where(o => o.taskId == taskId).FirstOrDefault();
            return true;
        }
        public TaskGS GetTaskToExecute(Context context, long branchId, long nextTaskId)
        {
            TaskGS currentTask = null;
            if (nextTaskId <= 0)
                currentTask = GetUsableTask(context, branchId);
            else
            {
                currentTask = GetUsableTask(context, branchId, nextTaskId);
                if (currentTask == null)
                {
                    currentTask = GetNextUsableTask(context, branchId, nextTaskId);
                    if (currentTask == null)
                        currentTask = GetUsableTask(context, branchId);
                }
            }
            log.Information("Select taskGS by id -> " + branchId);
            return currentTask;
        }
        public TaskFilter GetTaskFilter(Context context, long taskId)
        {
            TaskFilter filter = context.TaskFilters.Where(f 
                => f.taskId == taskId).FirstOrDefault();
            if (filter != null)
                filter.words = context.FilterWords.Where(w 
                    => w.filterId == filter.filterId).ToList();
            return filter;
        }
        public TaskGS GetNextUsableTask(Context context, long branchId, long nextTaskId)
        {
            return context.TaskGS.Where(t 
            => t.branchId == branchId
            && t.taskDeleted == false
            && t.taskStopped == false
            && nextTaskId < t.taskId).FirstOrDefault();
        }
        public TaskGS GetUsableTask(Context context, long branchId, long nextTaskId)
        {
            return context.TaskGS.Where(task 
            => task.branchId == branchId 
            && task.taskDeleted == false 
            && task.taskStopped == false
            && task.taskId == nextTaskId).FirstOrDefault();
        }
        public TaskGS GetUsableTask(Context context, long branchId)
        {
            return context.TaskGS.Where(task 
            => task.branchId == branchId
            && task.taskStopped == false 
            && task.taskDeleted == false)
            .FirstOrDefault();
        }
        #endregion
        #region Preparing to handle TaskData
        public TaskData GetTaskData(Context context, long taskId, long nextTaskData)
        {
            TaskData data = GetUsableTaskData(context, taskId, nextTaskData);
            if (data == null)
            {
                data = GetNextUsableTaskData(context, taskId, nextTaskData);
                if (data == null)
                    data = GetUsableTaskData(context, taskId);
            }
            return data;
        }
        public TaskData GetUsableTaskData(Context context, long taskId, long nextDataId)
        {
            return context.TaskData.Where(d 
            => d.taskId == taskId
            && d.dataId == nextDataId
            && d.dataDeleted == false
            && d.dataStopped == false).FirstOrDefault();
        }
        public TaskData GetUsableTaskData(Context context, long taskId)
        {           
            return context.TaskData.Where(d 
            => d.taskId == taskId
            && d.dataDeleted == false
            && d.dataStopped == false).FirstOrDefault();
        }
        public TaskData GetNextUsableTaskData(Context context, long taskId, long nextDataId)
        {
            return context.TaskData.Where(d 
            => d.taskId == taskId
            && d.dataId > nextDataId
            && d.dataDeleted == false
            && d.dataStopped == false).FirstOrDefault();
        }
        #endregion
        #region  Preparing unitGS to handle
        public bool SetUpUnits(Context context, ref Session session, TaskData data, TaskSubtype subtype)
        {
            if (!CheckExistUnits(context, data.dataId))
                return receiver.SetInstagramUnits(ref session, data, subtype);
            return true;
        }
        public bool CheckExistUnits(Context context, long dataId)
        {
            return context.Units.Any(u 
            => u.dataId == dataId
            && u.unitHandled == false);  
        }
        public void UpdateUnitToHandled(Context context, UnitGS unit)
        {
            unit.unitHandled = true;
            unit.handledAt = (int)(DateTimeOffset.Now.ToUnixTimeSeconds());
            context.Units.Update(unit);
            context.SaveChanges();        
        }
        public UnitGS GetNonHandlerUnit(Context context, long dataId)
        {
            UnitGS unit = context.Units.Where(i 
            => i.dataId == dataId 
            && i.unitHandled == false).FirstOrDefault();
            if (unit != null)
                UpdateUnitToHandled(context, unit);
            return unit;
        }
        #endregion         
        public bool AccessPerformAction(ICollection<TaskAction> actions, TimesAction times)
        {
            foreach (TaskAction action in actions)
            {
                if (timing.CompareActionCount((TaskActions)action.actionNumber, times))
                    return false;
            }
            return true;
        }
        public void UpdateTimesActionsCount()
        {
            List<TimesAction> times = timing.GetNonUpdatedTimesAction(contextGS);
            if (times.Count > 0)
                timing.UpdateToStart(new Context(true), times);
            log.Information("Update times actions count");
        }
        
        public void SaveHistory(Context context, string username, long taskId)
        {
            HistoryGS history = new HistoryGS();
            history.taskId = taskId;
            history.createdAt = (int)(DateTimeOffset.Now.ToUnixTimeSeconds());
            history.historyUrl = "https://www.instagram.com/" + username + "/";
            context.History.Add(history);
            context.SaveChanges();                   
        }
    }
}