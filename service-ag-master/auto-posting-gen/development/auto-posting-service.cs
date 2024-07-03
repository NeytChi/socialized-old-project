using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using Serilog;
using Serilog.Core;
using Microsoft.Extensions.Configuration;

using auto_posting_gen;
using database.context;
using Models.AutoPosting;

namespace nautoposting
{
    public class AutoPostingService
    {
        public string s3UploadedFiles;
        public long millisecondsToCheck;
        Logger log = new LoggerConfiguration()
            .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        public Timer checker;
        public Context contextChecking;
        public AutoPostingService(bool enableChecker, Context context)
        {
            var configuration = Program.GetConfiguration();
            this.s3UploadedFiles = configuration.GetValue<string>("s3_uploaded_files");
            this.millisecondsToCheck = configuration.GetValue<long>("milliseconds_to_check");
            this.contextChecking = context;
            if (enableChecker)
                SetUpChecker(null);
        }
        public AutoPostingService(bool enableChecker)
        {
            this.contextChecking = new Context(false);
            if (enableChecker)
                SetUpChecker(null);
        }
        /// <summary>
        /// Create Timer that start to check auto-post and auto-delete to perform and repeats
        /// it every 30 seconds.
        /// </summary>
        public void SetUpChecker(object input)
        {
            if (checker != null)
                CheckToRun();
            checker = new Timer(SetUpChecker, null, millisecondsToCheck, Timeout.Infinite);
        }
        /// <summary>
        /// Check auto-post and auto-delete to perform and start it by time.
        /// </summary>
        public void CheckToRun()
        {
            log.Information("Check to run auto-posts.");
            ICollection<AutoPost> autoPosts = GetAutoPosts();
            ICollection<AutoPost> autoDelete = GetAutoDelete();
            foreach(AutoPost post in autoPosts)
                post.files = GetPostFiles(post.postId);
            StartAutoPosts(autoPosts);
            StartAutoDelete(autoDelete);         
            contextChecking = new Context(false);  
        }
        /// <summary>
        /// Get auto-posts witch is non-executed and non-deleted by user.
        /// </summary>
        public ICollection<AutoPost> GetAutoPosts()
        {
            return contextChecking.AutoPosts.Where(a 
                => a.postExecuted == false
                && DateTime.Now.AddMinutes(2) > a.executeAt 
                && a.postDeleted == false
            ).OrderBy(a => a.executeAt).ToList();
        }
        public ICollection<PostFile> GetPostFiles(long postId)
        {
            return contextChecking.PostFiles.Where(f 
                => f.postId == postId
                && f.fileDeleted == false)
            .OrderBy(f => f.fileOrder).ToList();
        }
        /// <summary>
        /// Create and start thread for execute auto-post.
        /// </summary>
        public void StartAutoPosts(ICollection<AutoPost> posts)
        {
            AutoPosting autoPosting;
            
            foreach(AutoPost post in posts) {
                post.postExecuted = true;
                contextChecking.AutoPosts.Attach(post).Property(p => p.postExecuted).IsModified = true;
                contextChecking.SaveChanges();
                autoPosting = new AutoPosting(s3UploadedFiles);
                Thread thread = new Thread(() => autoPosting.PerformAutoPost(post));
                thread.IsBackground = true;
                thread.Start();
            }
            log.Information(posts.Count + " auto posts was started.");
        }
        /// <summary>
        /// Get executed auto-post with auto-delete state.
        /// </summary>
        public ICollection<AutoPost> GetAutoDelete()
        {
            return contextChecking.AutoPosts.Where(a 
                => a.autoDelete == true
                && a.postExecuted == true
                && a.postAutoDeleted == false
                && a.deleteAfter < DateTime.Now.AddMinutes(2)
                && a.postDeleted == false
            ).OrderBy(a => a.deleteAfter).ToList();
        }
        /// <summary>
        /// Create thread to perform auto-delete.
        /// </summary>
        public void StartAutoDelete(ICollection<AutoPost> posts)
        {
            AutoDeleting autoDeleting;

            foreach(AutoPost post in posts) {
                post.postAutoDeleted = true;
                contextChecking.AutoPosts.Attach(post).Property(p => p.postAutoDeleted).IsModified = true;
                contextChecking.SaveChanges();
                autoDeleting = new AutoDeleting();
                Thread thread = new Thread(() => autoDeleting.PerformAutoDelete(post));
                thread.IsBackground = true;
                thread.Start();
            }
            log.Information(posts.Count + " auto deletes was started.");
        }
    }
}