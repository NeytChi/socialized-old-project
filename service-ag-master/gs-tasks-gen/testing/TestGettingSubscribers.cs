using nmediator;
using ngettingsubscribers;

using NUnit.Framework;
using database.context;
using Instasoft.Testing;
using Models.GettingSubscribes;
using Models.SessionComponents;
using System.Collections.Generic;
using InstagramApiSharp.API.Builder;

namespace InstagramService.Test
{

    [TestFixture]
    public class TestGettingSubscribers
    {
        public ServiceMediator mediator;
        public Context context;
        public GettingSubscribers gettingSubscribers;
        public long firstUserPk = 19330388085;  /// user - nikachikapika2 
        
        public TestGettingSubscribers()
        {
            context = new Context(true, true);
            mediator = ServiceMediator.GetInstance(new Context(true, true));
            gettingSubscribers = new GettingSubscribers(new Context(true, true));
            TestMockingContext.context = context;
            gettingSubscribers.receiver.context = new Context(true, true);
        }
        #region  Starting and handling branch.
        
        [Test]
        public void RunBranch()
        {
            UnitGS unit = TestMockingContext.CreateUnitGSEnviroment();
            unit.userPk = firstUserPk;
            TaskBranch branch = unit.Data.Task.Branch;
            branch.currentTask = unit.Data.Task;
            branch.currentTaskData = unit.Data;
            branch.currentUnit = unit;
            branch.currentTask.taskFilter = null;
            branch.currentTask.taskOption.autoUnfollow = true;
            branch.session = mediator.sessionManager.LoadSession(branch.sessionId);
            gettingSubscribers.RunBranch(context, branch);
            gettingSubscribers.RunBranch(null, branch);
        }
        [Test]
        public void RunUnits()
        {
            UnitGS unit = TestMockingContext.CreateUnitGSEnviroment();
            TaskBranch branch = unit.Data.Task.Branch;
            branch.currentTask = unit.Data.Task;
            branch.currentTaskData = unit.Data;
            branch.currentUnit = unit;
            branch.currentTask.taskFilter = null;
            BaseModeGS modeGS = new BaseModeGS(OptionsGS.GetInstance(new SessionStateHandler(context)));
            gettingSubscribers.RunUnits(context, branch, modeGS);
            gettingSubscribers.RunUnits(null, branch, null);
        }
        [Test]
        public void HandleTask()
        {
            UnitGS unit = TestMockingContext.CreateUnitGSEnviroment();
            unit.unitHandled = true;
            TaskBranch branch = unit.Data.Task.Branch;
            branch.currentTask = unit.Data.Task;
            branch.currentTaskData = unit.Data;
            branch.currentUnit = unit;
            branch.currentTask.taskFilter = null;
            BaseModeGS modeGS = new BaseModeGS(OptionsGS.GetInstance(new SessionStateHandler(context)));
            bool success = gettingSubscribers.HandleTask(context, ref branch, modeGS);
            bool unsuccess = gettingSubscribers.HandleTask(null, ref branch, null);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CheckToEndHandle()
        {
            UnitGS unit = TestMockingContext.CreateUnitGSEnviroment();
            unit.handleAgain = false;
            unit.unitHandled = true;
            bool successHandled = gettingSubscribers.CheckToEndHandle(context, unit,
            unit.Data.taskId, true);
            bool successTaskError = gettingSubscribers.CheckToEndHandle(context, unit,
            unit.Data.taskId, true);
            unit.unitHandled = false;
            bool unsuccessNonHandled = gettingSubscribers.CheckToEndHandle(null, null,
            unit.Data.taskId, true);
            bool unsuccessNullable = gettingSubscribers.CheckToEndHandle(null, null,
            unit.Data.taskId, true);
            Assert.AreEqual(successHandled, true);
            Assert.AreEqual(successTaskError, true);
            Assert.AreEqual(unsuccessNonHandled, false);
            Assert.AreEqual(unsuccessNullable, false);
        }
        [Test]
        public void StopBranchToNextHandle()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            TaskBranch branch = task.Branch;
            branch.currentTask = task;
            branch.session = mediator.sessionManager.LoadSession(branch.sessionId);
            gettingSubscribers.StopBranchToNextHandle(context, branch);
            gettingSubscribers.StopBranchToNextHandle(null, branch);
        }
        [Test]
        public void DefineNextStartAt()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            gettingSubscribers.DefineNextStartAt(task.Session.Times, task.taskActions);
            gettingSubscribers.DefineNextStartAt(null, null);
        }
        [Test]
        public void DefineNextTask()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            TaskGS nextTask = new TaskGS()
            {
                sessionId = task.sessionId,
                userId = task.userId,
                branchId = task.branchId
            };
            context.TaskGS.Add(nextTask);
            context.SaveChanges();
            long success = gettingSubscribers.DefineNextTask(context, task.Branch.branchId, task.taskId);
            long unsuccess = gettingSubscribers.DefineNextTask(context, 0, task.taskId);
            long unsuccessNullable =  gettingSubscribers.DefineNextTask(null, 0, task.taskId);
            Assert.AreEqual(success, nextTask.taskId);
            Assert.AreEqual(unsuccess, 0);
            Assert.AreEqual(unsuccessNullable, 0);
        }
        [Test]
        public void GetNextTaskId()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            TaskGS nextTask = new TaskGS()
            {
                sessionId = task.sessionId,
                userId = task.userId,
                branchId = task.branchId
            };
            context.TaskGS.Add(nextTask);
            context.SaveChanges();
            long success = gettingSubscribers.GetNextTaskId(context, task.Branch.branchId, task.taskId);
            long unsuccess = gettingSubscribers.GetNextTaskId(context, 0, task.taskId);
            long unsuccessNullable =  gettingSubscribers.GetNextTaskId(null, 0, task.taskId);
            Assert.AreEqual(success, nextTask.taskId);
            Assert.AreEqual(unsuccess, 0);
            Assert.AreEqual(unsuccessNullable, 0);
        }
        [Test]
        public void GetAnyTaskId()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            long success = gettingSubscribers.GetAnyTaskId(context, task.Branch.branchId);
            long unsuccess = gettingSubscribers.GetAnyTaskId(context, 0);
            long unsuccessNullable =  gettingSubscribers.GetAnyTaskId(null, 0);
            Assert.AreEqual(success, task.taskId);
            Assert.AreEqual(unsuccess, 0);
            Assert.AreEqual(unsuccessNullable, 0);
        }
        [Test]
        public void EndTaskGS()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            gettingSubscribers.EndTaskGS(context, task);
            gettingSubscribers.EndTaskGS(null, task);
        }
        [Test]
        public void DefineNextTaskData()
        {
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            TaskData nextData = new TaskData();
            nextData.taskId = data.taskId;
            context.TaskData.Add(nextData);
            context.SaveChanges();
            long success = gettingSubscribers.DefineNextTaskData(context, data.taskId, data.dataId);
            long unsuccess = gettingSubscribers.DefineNextTaskData(context, 0, data.dataId);
            long unsuccessNullable =  gettingSubscribers.DefineNextTaskData(null, 0, data.dataId);
            Assert.AreEqual(success, nextData.dataId);
            Assert.AreEqual(unsuccess, 0);
            Assert.AreEqual(unsuccessNullable, 0);
        }
        [Test]
        public void GetNextTaskDataId()
        {
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            TaskData nextData = new TaskData();
            nextData.taskId = data.taskId;
            context.TaskData.Add(nextData);
            context.SaveChanges();
            long success = gettingSubscribers.GetNextTaskDataId(context, data.taskId, data.dataId);
            long unsuccess = gettingSubscribers.GetNextTaskDataId(context, 0, data.dataId);
            long unsuccessNullable =  gettingSubscribers.GetNextTaskDataId(null, 0, data.dataId);
            Assert.AreEqual(success, nextData.dataId);
            Assert.AreEqual(unsuccess, 0);
            Assert.AreEqual(unsuccessNullable, 0);
        }
        [Test]
        public void GetAnyTaskDataId()
        {
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            data.dataComment = null;
            context.TaskData.Update(data);
            context.SaveChanges();
            long success = gettingSubscribers.GetAnyTaskDataId(context, data.taskId);
            long unsuccess = gettingSubscribers.GetAnyTaskDataId(context, 0);
            long unsuccessNullable =  gettingSubscribers.GetAnyTaskDataId(null, 0);
            Assert.AreEqual(success, data.dataId);
            Assert.AreEqual(unsuccess, 0);
            Assert.AreEqual(unsuccessNullable, 0);
        }
        #endregion
        #region Preparing branch to handling.
        [Test]
        public void SetUpBranch()
        {
            TaskBranch branch = TestMockingContext.CreateTaskGSEnviroment().Branch;
            bool success = gettingSubscribers.SetUpBranch(context, ref branch);
            bool unsuccess = gettingSubscribers.SetUpBranch(null, ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);    
        }
        [Test]
        public void CheckBranchTasks()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            bool success = gettingSubscribers.CheckBranchTasks(context, task.Branch.branchId);
            task.taskStopped = true;
            context.TaskGS.Update(task);
            context.SaveChanges();
            bool unsuccess = gettingSubscribers.CheckBranchTasks(context, task.Branch.branchId);
            bool unsuccessNullable = gettingSubscribers.CheckBranchTasks(null, task.Branch.branchId);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
            Assert.AreEqual(unsuccessNullable, false);
        }
        [Test]
        public void StopBranch()
        {
            TaskBranch branch = TestMockingContext.CreateTaskGSEnviroment().Branch;
            gettingSubscribers.StopBranch(context, branch.branchId);
            gettingSubscribers.StopBranch(null, branch.branchId);
        }
        [Test]
        public void DeleteBranch()
        {
            TaskBranch branch = TestMockingContext.CreateTaskGSEnviroment().Branch;
            gettingSubscribers.DeleteBranch(context, branch.branchId);
            gettingSubscribers.DeleteBranch(null, branch.branchId);
        }
        [Test]
        public void UpdateBranchToRun()
        {
            TaskBranch branch = TestMockingContext.CreateTaskGSEnviroment().Branch;
            branch.branchRunning = false;
            gettingSubscribers.UpdateBranchToRun(null, ref branch);
            bool unsuccess = branch.branchRunning;
            gettingSubscribers.UpdateBranchToRun(context,ref branch);
            bool success = branch.branchRunning;
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        #endregion
        #region Preparing task.
        [Test]
        public void GetTask()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            TaskGS success = gettingSubscribers.GetTask(context, task.branchId, 0);
            TaskGS unsuccess = gettingSubscribers.GetTask(context, -1, task.Branch.nextTask);
            Assert.AreEqual(success.taskId, task.taskId);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetFullTask()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            var success = gettingSubscribers.GetFullTask(context, ref task);
            var unsuccess = gettingSubscribers.GetFullTask(null, ref task);
            Assert.AreEqual(success, success);
            Assert.AreEqual(unsuccess, unsuccess);
        }
        [Test]
        public void GetTaskToExecute()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            TaskGS success = gettingSubscribers.GetTaskToExecute(context, task.Branch.branchId, 0);
            TaskGS unsuccess = gettingSubscribers.GetTaskToExecute(context, -1, 0);
            Assert.AreEqual(success.taskId, task.taskId);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetTaskFilter()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            TaskFilter success = gettingSubscribers.GetTaskFilter(context, task.taskId);
            TaskFilter unsuccess = gettingSubscribers.GetTaskFilter(context, 0);
            Assert.AreEqual(success.filterId, task.taskFilter.filterId);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetNextUsableTask()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            TaskGS success = gettingSubscribers.GetNextUsableTask(context, task.branchId, task.taskId - 1);
            TaskGS unsuccess = gettingSubscribers.GetNextUsableTask(context, 0, 0);
            Assert.AreEqual(success.taskId, task.taskId);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetUsableTask()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            TaskGS success = gettingSubscribers.GetUsableTask(context, task.branchId, task.taskId);
            TaskGS unsuccess = gettingSubscribers.GetUsableTask(context, 0, 0);
            Assert.AreEqual(success.taskId, task.taskId);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetUsableTaskByBranch()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            TaskGS success = gettingSubscribers.GetUsableTask(context, task.branchId);
            TaskGS unsuccess = gettingSubscribers.GetUsableTask(context, 0);
            Assert.AreEqual(success.taskId, task.taskId);
            Assert.AreEqual(unsuccess, null);
        }
        #endregion
        #region Preparing task data.
        [Test]
        public void GetTaskData()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            var success = gettingSubscribers.GetTaskData(context, task.taskId, task.nextTaskData);
            var unsuccess = gettingSubscribers.GetTaskData(null, task.taskId, task.nextTaskData);
            Assert.AreEqual(success.taskId, task.taskId);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetUsableTaskData()
        {
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            TaskData success = gettingSubscribers.GetUsableTaskData(context, data.taskId, data.dataId);
            TaskData unsuccess = gettingSubscribers.GetUsableTaskData(context, 0, data.dataId);
            Assert.AreEqual(success.dataId, data.dataId);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetUsableTaskDataByData()
        {
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            TaskData success = gettingSubscribers.GetUsableTaskData(context, data.taskId);
            TaskData unsuccess = gettingSubscribers.GetUsableTaskData(context, 0);
            Assert.AreEqual(success.dataId, data.dataId);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetNextUsableTaskData()
        {
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            TaskData success = gettingSubscribers.GetNextUsableTaskData(context, data.taskId, data.dataId - 1);
            TaskData unsuccess = gettingSubscribers.GetNextUsableTaskData(context, 0, data.dataId);
            Assert.AreEqual(success.dataId, data.dataId);
            Assert.AreEqual(unsuccess, null);
        }
        #endregion
        #region  Preparing unitGS to handle
        [Test]
        public void SetUpUnits()
        {
            TaskSubtype subtype = TaskSubtype.ByUserFollowers;
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            Session session = mediator.sessionManager.LoadSession(data.Task.Session.sessionId);
            data.dataNames = session.User.UserName;
            var success = gettingSubscribers.SetUpUnits(context, ref session, data, subtype);
            var unsuccess = gettingSubscribers.SetUpUnits(null, ref session, null, subtype);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CheckExistUnits()
        {
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            var unsuccess = gettingSubscribers.CheckExistUnits(context, data.dataId);
            UnitGS unit = new UnitGS();
            unit.dataId = data.dataId;
            context.Units.Add(unit);
            context.SaveChanges();
            var success = gettingSubscribers.CheckExistUnits(context, data.dataId);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void UpdateUnitToHandled()
        {
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            UnitGS unit = new UnitGS();
            unit.dataId = data.dataId;
            context.Units.Add(unit);
            context.SaveChanges();
            gettingSubscribers.UpdateUnitToHandled(context, unit);
            gettingSubscribers.UpdateUnitToHandled(context, null);
        }
        [Test]
        public void GetNonHandlerUnit()
        {
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            UnitGS unit = new UnitGS();
            unit.dataId = data.dataId;
            context.Units.Add(unit);
            context.SaveChanges();
            var success = gettingSubscribers.GetNonHandlerUnit(context, data.dataId);
            var unsuccess = gettingSubscribers.GetNonHandlerUnit(context, 0);
            Assert.AreEqual(success.unitId, unit.unitId);
            Assert.AreEqual(unsuccess, null);
        }
        #endregion
        [Test]
        public void AccessPerformAction()
        {
            TimesAction times = new TimesAction();
            times.accountOld = false;
            times.followCount = 100;
            List<TaskAction> actions = new List<TaskAction>();
            TaskAction action = new TaskAction();
            action.actionNumber = 1;
            actions.Add(action);
            bool success = gettingSubscribers.AccessPerformAction(actions,times);
            times.followCount = 1000;
            bool unsuccess = gettingSubscribers.AccessPerformAction(actions, times);
            bool unsuccessNullable = gettingSubscribers.AccessPerformAction(null, null);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
            Assert.AreEqual(unsuccessNullable, false);
        }
        [Test]
        public void UpdateTimesActionsCount()
        {
            gettingSubscribers.UpdateTimesActionsCount();
        }
        [Test]
        public void SaveHistory()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            gettingSubscribers.SaveHistory(context, "test", task.taskId);
            gettingSubscribers.SaveHistory(null, null, task.taskId);
        }
    }
}








