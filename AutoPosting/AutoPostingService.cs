using Serilog;
using Domain.AutoPosting;

namespace AutoPosting
{
    public class AutoPostingService
    {
        private ILogger Logger;
        private IAutoPostRepository AutoPostRepository;
        private IPostFileRepository PostFileRepository;
        public string s3UploadedFiles;
        public long millisecondsToCheck;
        public Timer checker;

        public AutoPostingService(IAutoPostRepository autoPostRepository,
            IPostFileRepository postFileRepository,
            bool enableChecker, long millisecondsToCheck)
        {
            this.millisecondsToCheck = millisecondsToCheck;
            AutoPostRepository = autoPostRepository;
            PostFileRepository = postFileRepository;
            if (enableChecker)
            {
                SetUpChecker(null);
            }
        }
        public AutoPostingService(bool enableChecker)
        {
            if (enableChecker)
            {
                SetUpChecker(null);
            }
        }
        public void SetUpChecker(object input)
        {
            if (checker != null)
            {
                CheckToRun();
            }
            checker = new Timer(SetUpChecker, null, millisecondsToCheck, Timeout.Infinite);
        }
        public void CheckToRun()
        {
            Logger.Information("Check to run auto-posts.");
            var autoPosts = AutoPostRepository.GetBy(
                DateTime.Now.AddMinutes(2), false, false);
            var autoDelete = AutoPostRepository.GetBy(DateTime.Now.AddMinutes(2),
                true, true, false, false);
            foreach (var post in autoPosts)
            {
                post.files = PostFileRepository.GetBy(post.postId);
            }
            StartAutoPosts(autoPosts);
            StartAutoDelete(autoDelete);
        }
        public void StartAutoPosts(ICollection<AutoPost> posts)
        {
            AutoPosting autoPosting;
            
            foreach(var post in posts) 
            {
                post.postExecuted = true;
                AutoPostRepository.Update(post);
                autoPosting = new AutoPosting(AutoPostRepository, PostFileRepository);
                var thread = new Thread(() => autoPosting.PerformAutoPost(post))
                {
                    IsBackground = true
                };
                thread.Start();
            }
            Logger.Information(posts.Count + " auto posts was started.");
        }
        public void StartAutoDelete(ICollection<AutoPost> posts)
        {
            foreach(var post in posts) 
            {
                post.postAutoDeleted = true;
                AutoPostRepository.Update(post);
                var autoDeleting = new AutoDeleting(Logger);
                var thread = new Thread(() => autoDeleting.PerformAutoDelete(post));
                thread.IsBackground = true;
                thread.Start();
            }
            Logger.Information(posts.Count + " auto deletes was started.");
        }
    }
}