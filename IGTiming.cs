using System;
using System.IO;
using System.Linq;
using System.Text;
using Serilog.Core;
using database.context;
using Newtonsoft.Json.Linq;
using Models.GettingSubscribes;
using Models.SessionComponents;
using System.Collections.Generic;

namespace InstagramService
{
    public class InstagramTiming
    {
        public Logger log;
        public InstagramTiming(Logger log)
        {
            this.log = log;
        }
        public InstagramTimes times = new InstagramTimes();
        public string fileName = "timingConf.json";
        private string ReadConfigData()
        {
            if (File.Exists(fileName)) {
                using (var fstream = File.OpenRead(fileName)) {
                    byte[] array = new byte[fstream.Length];
                    fstream.Read(array, 0, array.Length);
                    string textFromFile = Encoding.Default.GetString(array);
                    fstream.Close();
                    return textFromFile;
                }
            }
            else
                log.Error("Can't read file -> " + fileName);
            return string.Empty;
        }
        public void InitiationTiming()
        {
            FileInfo fileExist = new FileInfo(Directory.GetCurrentDirectory() + "/" + fileName);
            if (fileExist.Exists) {
                string data = ReadConfigData();
                JObject json = JObject.Parse(data);
                if (json != null) {
                    times = json.ToObject<InstagramTimes>();
                    log.Information("Set up timing getting subscribes.");
                }
                else
                    log.Information("Start with default timing gs setting.");
            }
            else
                log.Information("Start with default timing gs setting.");
        }
        public int GetMillisecondsByType(TaskType type, bool oldAccount)
        {
            int time = 0;
            if (oldAccount)
                time = GetFromOld(type);
            else
                time = GetFromNonOld(type);
            if (time > 0)
                time = time * 1000;
            return time;
        }
        public int GetFromOld(TaskType type)
        {
            switch(type)
            {
                case TaskType.Following: 
                    return times.oldFollowFrom;
                case TaskType.Unfollowing:
                    return times.oldUnfollowFrom;
                case TaskType.Liking: 
                    return times.oldLikeFrom;
                case TaskType.Comments: 
                    return times.oldCommentFrom;
                case TaskType.Blocking: 
                    return times.oldBlockFrom;
                case TaskType.WatchStories: 
                    return times.oldWatchingStoriesFrom;
                default : return 0;
            }
        }
        public int GetFromNonOld(TaskType type)
        {
            switch(type)
            {
                case TaskType.Following: 
                    return times.followFrom;
                case TaskType.Unfollowing: 
                    return times.unfollowFrom;
                case TaskType.Liking: 
                    return times.likeFrom;
                case TaskType.Comments: 
                    return times.commentFrom;
                case TaskType.Blocking: 
                    return times.blockFrom;
                case TaskType.WatchStories: 
                    return times.watchingStoriesFrom;
                default : return 0;
            }
        }
        public int GetCountByType(TaskType type, bool oldAccount)
        {
            if (oldAccount)
                return GetCountOld(type);
            else
                return GetCountNonOld(type);
        }
        public int GetCountOld(TaskType type)
        {
            switch(type)
            {
                case TaskType.Following: 
                    return times.oldFollowCount;
                case TaskType.Unfollowing:
                    return times.oldUnfollowCount;
                case TaskType.Liking:
                    return times.oldLikeCount;
                case TaskType.Comments:
                    return times.oldCommentCount;
                case TaskType.Blocking:
                    return times.oldBlockCount;
                case TaskType.WatchStories:
                    return times.oldWatchingStoriesCount;
                default : return 0;
            }
        }
        public int GetCountNonOld(TaskType type)
        {
            switch(type)
            {
                case TaskType.Following: 
                    return times.followCount;
                case TaskType.Unfollowing:
                    return times.unfollowCount;
                case TaskType.Liking:
                    return times.likeCount;
                case TaskType.Comments:
                    return times.commentCount;
                case TaskType.Blocking:
                    return times.blockCount;
                case TaskType.WatchStories:
                    return times.watchingStoriesCount;
                default : return 0;
            }
        }
        public bool CompareTypeCount(TaskType type, TimeAction times)
        {
            switch(type)
            {
                case TaskType.Following:
                    return times.followCount >= GetCountByType(type, times.accountOld);
                case TaskType.Liking:
                    return times.likeCount >= GetCountByType(type, times.accountOld);
                case TaskType.Comments:
                    return times.commentCount >= GetCountByType(type, times.accountOld);
                case TaskType.Blocking:
                    return times.blockCount >= GetCountByType(type, times.accountOld);
                case TaskType.Unfollowing:
                    return times.unfollowCount >= GetCountByType(type, times.accountOld);
                case TaskType.WatchStories:
                    return times.watchingStoriesCount >= GetCountByType(type, times.accountOld);
                case TaskType.Unknow:
                default: log.Error("Can't define task actions type.");
                    return false;
            }
        }
        public List<TimeAction> GetNonUpdatedTimesAction(Context context)
        {
            DateTime lastDay = DateTime.Now.AddDays(-1);
            return context.timeAction.Where(t
            => (t.followCount >= times.followCount && t.followLastAt < lastDay)
                || (t.unfollowCount >= times.unfollowCount && t.unfollowLastAt < lastDay)
                || (t.likeCount >= times.likeCount && t.likeLastAt < lastDay)
                || (t.commentCount >= times.commentCount && t.commentLastAt < lastDay)
                || (t.mentionsCount >= times.mentionsCount && t.mentionsLastAt < lastDay)
                || (t.blockCount >= times.blockCount && t.blockLastAt < lastDay)
                || (t.publicationCount >= times.publicationCount && t.publicationLastAt < lastDay)
                || (t.messageDirectCount >= times.messageDirectCount && t.messageDirectLastAt < lastDay)
                || (t.watchingStoriesCount >= times.watchingStoriesCount && t.watchingStoriesLastAt < lastDay
            )).ToList();
        }
        public void UpdateToStart(Context context, List<TimeAction> timesActions)
        {
            DateTime lastDay = DateTime.Now.AddDays(-1);
            foreach(TimeAction time in timesActions)
                UpdateCountToStart(context, time, lastDay);
            context.SaveChanges();
        }
        public void UpdateCountToStart(Context context, TimeAction times, DateTime lastDay)
        {
            times.followCount   = times.followLastAt < lastDay ? 0 : times.followCount;
            times.unfollowCount = times.unfollowLastAt < lastDay ? 0 : times.unfollowCount;
            times.likeCount     = times.likeLastAt < lastDay ? 0 : times.likeCount;
            times.commentCount  = times.commentLastAt < lastDay ? 0 : times.commentCount;
            times.mentionsCount = times.mentionsLastAt < lastDay ? 0 : times.mentionsCount;
            times.blockCount    = times.blockLastAt < lastDay ? 0 : times.blockCount;
            times.publicationCount      = times.publicationLastAt < lastDay ? 0 : times.publicationCount;
            times.messageDirectCount    = times.messageDirectLastAt < lastDay ? 0 : times.messageDirectCount;
            times.watchingStoriesCount  = times.watchingStoriesLastAt < lastDay ? 0 : times.watchingStoriesCount;
            context.timeAction.Update(times);
        }
    }
}