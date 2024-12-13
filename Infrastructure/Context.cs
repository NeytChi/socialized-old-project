using Microsoft.EntityFrameworkCore;
using Domain.Users;
using Domain.Admins;
using Domain.AutoPosting;
using Domain.GettingSubscribes;
using Domain.Statistics;
using Domain.Packages;
using Domain.InstagramAccounts;
using Domain;

namespace Infrastructure
{
    public partial class Context : DbContext
    {
        private bool useInMemoryDatabase = false;
        private bool useConfiguration = false;
        
        public virtual DbSet<Country> countries { get; set; }
        public virtual DbSet<Culture> Cultures { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<ServiceAccess> ServiceAccess { get; set; }
        public virtual DbSet<Profile> UserProfile { get; set; }
        public virtual DbSet<TaskGS> TaskGS { get; set; }
        public virtual DbSet<TaskData> TaskData { get; set; }
        public virtual DbSet<FilterWord> FilterWords { get; set; }
        public virtual DbSet<TaskFilter> TaskFilters { get; set; }
        public virtual DbSet<TaskOption> TaskOptions { get; set; } 
        public virtual DbSet<IGAccount> IGAccounts { get; set; }
        public virtual DbSet<AccountProfile> SessionProfiles { get; set; }
        
        public virtual DbSet<HistoryGS> History { get; set; }
        public virtual DbSet<UnitGS> Units { get; set; }
        public virtual DbSet<MediaGS> Medias { get; set; }
        public virtual DbSet<TimeAction> timeAction { get; set; }
        public virtual DbSet<SessionState> States { get; set; }
        public virtual DbSet<AutoPost> AutoPosts { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<AutoPostFile> AutoPostFiles { get; set; }
        public virtual DbSet<BusinessAccount> BusinessAccounts { get; set; }
        public virtual DbSet<DayStatistics> Statistics { get; set; }
        public virtual DbSet<OnlineFollowers> OnlineFollowers { get; set; }
        public virtual DbSet<PostStatistics> PostStatistics { get; set; }
        public virtual DbSet<StoryStatistics> StoryStatistics { get; set; }
        public virtual DbSet<CommentStatistics> CommentStatistics { get; set; }
        public virtual DbSet<Admin> Admins { get; set; }
        public virtual DbSet<Appeal> Appeals { get; set; }
        public virtual DbSet<AppealMessage> AppealMessages { get; set; }
        public virtual DbSet<AppealFile> AppealFiles { get; set; }
        
        public Context()
        {

        }
        public Context(bool useInMemoryDatabase)
        {
            this.useInMemoryDatabase = useInMemoryDatabase;
            useConfiguration = true;
        }
        public Context(DbContextOptions<Context> options) : base(options)
        {

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (useInMemoryDatabase) 
            {
                optionsBuilder.UseInMemoryDatabase(databaseConfiguration().GetValue<string>("Database"));
            }
            optionsBuilder.EnableSensitiveDataLogging();
            if (useConfiguration) {
                if (!optionsBuilder.IsConfigured) 
                {
                    optionsBuilder.UseMySql(databaseConnection());
                }
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
        }
    }
}
