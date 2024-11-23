using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Domain.Users;
using Domain.Admins;
using Domain.AutoPosting;
using Domain.GettingSubscribes;
using Domain.SessionComponents;
using Domain.Statistics;
using Microsoft.Extensions.Configuration;

namespace Infrastructure
{
    public partial class Context : DbContext
    {
        private bool useInMemoryDatabase = false;
        private bool useConfiguration = false;
        public Context()
        {

        }
        public Context(bool useInMemoryDatabase)
        {
            this.useInMemoryDatabase = useInMemoryDatabase;
            this.useConfiguration = true;
        }
        public Context(DbContextOptions<Context> options) : base(options)
        {
            
        }
        public virtual DbSet<Country> countries { get; set; }
        public virtual DbSet<Culture> Cultures { get; set; }
        public virtual DbSet<Follower> Followers { get; set; }
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
        public virtual DbSet<PostFile> PostFiles { get; set; }
        public virtual DbSet<BusinessAccount> BusinessAccounts { get; set; }
        public virtual DbSet<DayStatistics> Statistics { get; set; }
        public virtual DbSet<OnlineFollowers> OnlineFollowers { get; set; }
        public virtual DbSet<PostStatistics> PostStatistics { get; set; }
        public virtual DbSet<StoryStatistics> StoryStatistics { get; set; }
        public virtual DbSet<CommentStatistics> CommentStatistics { get; set; }
        public virtual DbSet<Admin> Admins { get; set; }
        public virtual DbSet<BlogPost> BlogPosts { get; set; }
        public virtual DbSet<Appeal> Appeals { get; set; }
        public virtual DbSet<AppealMessage> AppealMessages { get; set; }
        public virtual DbSet<AppealFile> AppealFiles { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (useInMemoryDatabase) {
                optionsBuilder.UseInMemoryDatabase(
                    databaseConfiguration().GetValue<string>("Database"));
            }
            optionsBuilder.EnableSensitiveDataLogging();
            if (useConfiguration) {
                if (!optionsBuilder.IsConfigured) {
                    optionsBuilder.UseMySql(databaseConnection());
                }
            }
        }
        public static IConfigurationRoot databaseConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .AddJsonFile("database.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"database.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", 
                    optional: true, reloadOnChange: false)
                .Build();
        }
        public static string databaseConnection()
        {
            var sqlConfig = databaseConfiguration();
            return "Server=" + sqlConfig.GetValue<string>("Server") +
                ";Database=" + sqlConfig.GetValue<string>("Database") + 
                ";User=" + sqlConfig.GetValue<string>("User") + 
                ";Pwd=" + sqlConfig.GetValue<string>("Password") + 
                ";Charset=utf8;";
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Culture>(entity =>
            {
                entity.HasKey(c => c.cultureId)
                    .HasName("PRIMARY");

                entity.ToTable("cultures");

                entity.Property(c => c.cultureId)
                    .HasColumnName("culture_id")
                    .HasColumnType("int(11)");
                
                entity.Property(c => c.cultureKey)
                    .HasColumnName("culture_key")
                    .HasColumnType("varchar(256) CHARACTER SET utf8 COLLATE utf8_general_ci");
                
                entity.Property(c => c.cultureName)
                    .HasColumnName("culture_name")
                    .HasColumnType("varchar(256) CHARACTER SET utf8 COLLATE utf8_general_ci");
                
                entity.Property(c => c.cultureValue)
                    .HasColumnName("culture_value")
                    .HasColumnType("varchar(256) CHARACTER SET utf8 COLLATE utf8_general_ci");
                    
            });
            modelBuilder.Entity<Follower>(entity =>
            {
                entity.HasKey(f => f.followerId)
                    .HasName("PRIMARY");

                entity.ToTable("lending_followers");

                entity.Property(f => f.followerId)
                    .HasColumnName("follower_id")
                    .HasColumnType("bigint(20)");

                entity.Property(f => f.userId)
                    .HasColumnName("user_id")
                    .HasColumnType("int(11)");

                entity.Property(f => f.followerEmail)
                    .HasColumnName("follower_email")
                    .HasColumnType("varchar(100)");

                entity.Property(f => f.createdAt)
                    .HasColumnName("created_at")
                    .HasColumnType("DATETIME");

                entity.Property(f => f.enableMailing)
                    .HasColumnName("enable_mailing")
                    .HasColumnType("boolean");
            });
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.userId)
                    .HasName("PRIMARY");

                entity.ToTable("users");

                entity.HasIndex(e => e.userEmail)
                    .HasName("user_email")
                    .IsUnique();

                entity.Property(e => e.userId)
                    .HasColumnName("user_id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.activate)
                    .HasColumnName("activate")
                    .HasColumnType("boolean");

                entity.Property(e => e.deleted)
                    .HasColumnName("deleted")
                    .HasColumnType("boolean");

                entity.Property(e => e.createdAt)
                    .HasColumnName("created_at")
                    .HasColumnType("int(11)");

                entity.Property(e => e.lastLoginAt)
                    .HasColumnName("last_login_at")
                    .HasColumnType("int(11)");

                entity.Property(e => e.userEmail)
                    .HasColumnName("user_email")
                    .HasColumnType("varchar(256)");

                entity.Property(e => e.userFullName)
                    .HasColumnName("user_fullname")
                    .HasColumnType("varchar(256) CHARACTER SET utf8 COLLATE utf8_general_ci");

                entity.Property(e => e.userHash)
                    .HasColumnName("user_hash")
                    .HasColumnType("varchar(120)");

                entity.Property(e => e.userPassword)
                    .HasColumnName("user_password")
                    .HasColumnType("varchar(256)");

                entity.Property(e => e.userToken)
                    .HasColumnName("user_token")
                    .HasColumnType("varchar(50)");

                entity.Property(e => e.recoveryCode)
                    .HasColumnName("recovery_code")
                    .HasColumnType("int(11)");
                    
                entity.Property(e => e.recoveryToken)
                    .HasColumnName("recovery_token")
                    .HasColumnType("varchar(50)");
            });
            modelBuilder.Entity<Profile>(entity =>
            {
                entity.HasKey(p => p.profileId)
                    .HasName("PRIMARY");

                entity.ToTable("user_profile");

                entity.HasIndex(p => p.userId)
                    .HasName("user_id");

                entity.Property(p => p.profileId)
                    .HasColumnName("profile_id")
                    .HasColumnType("int(11)");

                entity.Property(p => p.country)
                    .HasColumnName("country")
                    .HasColumnType("varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci");

                entity.Property(p => p.timezone)
                    .HasColumnName("timezone")
                    .HasColumnType("bigint(20)");

                entity.HasOne(p => p.user)
                    .WithOne(u => u.profile)
                    .HasConstraintName("user_profile_ibfk_1");
            });
            modelBuilder.Entity<TaskGS>(entity =>
            {
                entity.HasKey(e => e.taskId)
                    .HasName("PRIMARY");

                entity.ToTable("task_gs");

                entity.HasIndex(e => e.sessionId)
                    .HasName("session_id");
                
                entity.Property(e => e.taskId)
                    .HasColumnName("task_id")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.sessionId)
                    .HasColumnName("session_id")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.taskType)
                    .HasColumnName("task_type")
                    .HasColumnType("tinyint(4)");
                
                entity.Property(e => e.taskSubtype)
                    .HasColumnName("task_subtype")
                    .HasColumnType("tinyint(4)");

                entity.Property(e => e.createdAt)
                    .HasColumnName("created_at")
                    .HasColumnType("int(11)");

                entity.Property(e => e.lastDoneAt)
                    .HasColumnName("last_done_at")
                    .HasColumnType("int(11)");

                entity.Property(e => e.taskRunning)
                    .HasColumnName("task_running")
                    .HasColumnType("boolean");

                entity.Property(e => e.taskUpdated)
                    .HasColumnName("task_updated")
                    .HasColumnType("boolean");

                entity.Property(e => e.taskStopped)
                    .HasColumnName("task_stopped")
                    .HasColumnType("boolean");

                entity.Property(e => e.taskDeleted)
                    .HasColumnName("task_deleted")
                    .HasColumnType("boolean");
                
                entity.Property(e => e.nextTaskData)
                    .HasColumnName("next_task_data")
                    .HasColumnType("bigint(20)");

                entity.HasOne(d => d.account)
                    .WithMany(p => p.Tasks)
                    .HasForeignKey(d => d.sessionId)
                    .HasConstraintName("task_gs_ibfk_1");
            });
            modelBuilder.Entity<TaskData>(entity =>
            {
                entity.HasKey(e => e.dataId)
                    .HasName("PRIMARY");

                entity.ToTable("task_data");

                entity.Property(e => e.dataId)
                    .HasColumnName("data_id")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.taskId)
                    .HasColumnName("task_id")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.dataNames)
                    .HasColumnName("data_names")
                    .HasColumnType("varchar(100)");

                entity.Property(e => e.dataLongitute)
                    .HasColumnName("data_longitute")
                    .HasColumnType("double");

                entity.Property(e => e.dataLatitute)
                    .HasColumnName("data_latitute")
                    .HasColumnType("double");

                entity.Property(e => e.dataComment)
                    .HasColumnName("data_comment")
                    .HasColumnType("varchar(500)");

                entity.Property(e => e.dataDeleted)
                    .HasColumnName("data_deleted")
                    .HasColumnType("boolean");

                entity.Property(e => e.dataStopped)
                    .HasColumnName("data_stopped")
                    .HasColumnType("boolean");

                entity.Property(e => e.nextPage)
                    .HasColumnName("next_page")
                    .HasColumnType("int(11) DEFAULT '1'");

                entity.HasOne(d => d.Task)
                    .WithMany(p => p.taskData)
                    .HasForeignKey(d => d.taskId)
                    .HasConstraintName("task_data_ibfk_1");
            });
            modelBuilder.Entity<TaskOption>(entity =>
            {
                entity.HasKey(e => e.optionId)
                    .HasName("PRIMARY");

                entity.ToTable("task_options");

                entity.Property(e => e.optionId)
                    .HasColumnName("option_id")
                    .HasColumnType("bigint(11)");

                entity.Property(e => e.taskId)
                    .HasColumnName("task_id")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.dontFollowOnPrivate)
                    .HasColumnName("dont_follow_on")
                    .HasColumnType("boolean");

                entity.Property(e => e.watchStories)
                    .HasColumnName("watch_stories")
                    .HasColumnType("boolean");

                entity.Property(e => e.likeUsersPost)
                    .HasColumnName("like_users_post")
                    .HasColumnType("boolean");

                entity.Property(e => e.autoUnfollow)
                    .HasColumnName("auto_unfollow")
                    .HasColumnType("boolean");

                entity.Property(e => e.unfollowNonReciprocal)
                    .HasColumnName("unfollow_non_reciprocal")
                    .HasColumnType("boolean");

                entity.Property(e => e.nextUnlocking)
                    .HasColumnName("next_unlocking")
                    .HasColumnType("boolean");

                entity.Property(e => e.likesOnUser)
                    .HasColumnName("likes_on_user")
                    .HasColumnType("int(11)");

                entity.HasOne(d => d.Task)
                    .WithOne(p => p.taskOption)
                    .HasConstraintName("task_options_ibfk_1");
            });
            modelBuilder.Entity<TaskFilter>(entity =>
            {
                entity.HasKey(e => e.filterId)
                    .HasName("PRIMARY");

                entity.ToTable("task_filters");

                entity.Property(e => e.filterId)
                    .HasColumnName("filter_id")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.taskId)
                    .HasColumnName("task_id")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.range_subscribers_from)
                    .HasColumnName("range_subscribers_from")
                    .HasColumnType("int(11)");

                entity.Property(e => e.range_subscribers_to)
                    .HasColumnName("range_subscribers_to")
                    .HasColumnType("int(11)");
                    
                entity.Property(e => e.range_following_from)
                    .HasColumnName("range_following_from")
                    .HasColumnType("int(11)");

                entity.Property(e => e.range_following_to)
                    .HasColumnName("range_following_to")
                    .HasColumnType("int(11)");
                
                entity.Property(e => e.publication_count)
                    .HasColumnName("publication_count")
                    .HasColumnType("int(11)");
                
                entity.Property(e => e.latest_publication_no_younger)
                    .HasColumnName("latest_publication_no_younger")
                    .HasColumnType("int(11)");
                
                entity.Property(e => e.without_profile_photo)
                    .HasColumnName("without_profile_photo")
                    .HasColumnType("boolean");
                
                entity.Property(e => e.with_profile_url)
                    .HasColumnName("with_profile_url")
                    .HasColumnType("boolean");
                
                entity.Property(e => e.english)
                    .HasColumnName("english")
                    .HasColumnType("boolean");
                
                entity.Property(e => e.ukrainian)
                    .HasColumnName("ukrainian")
                    .HasColumnType("boolean");
                
                entity.Property(e => e.russian)
                    .HasColumnName("russian")
                    .HasColumnType("boolean");
                
                entity.Property(e => e.arabian)
                    .HasColumnName("arabian")
                    .HasColumnType("boolean");
                
                entity.HasOne(filter => filter.Task)
                    .WithOne(task => task.taskFilter)
                    .HasConstraintName("task_filters_ibfk_1");
            });

            modelBuilder.Entity<FilterWord>(entity =>
            {
                entity.HasKey(e => e.wordId)
                    .HasName("PRIMARY");

                entity.ToTable("filter_words");

                entity.HasIndex(e => e.wordId)
                    .HasName("word_id");

                entity.Property(e => e.wordId)
                    .HasColumnName("word_id")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.filterId)
                    .HasColumnName("filter_id")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.wordValue)
                    .HasColumnName("word_value")
                    .HasColumnType("varchar(256)");

                entity.Property(e => e.wordUse)
                    .HasColumnName("word_use")
                    .HasColumnType("boolean");

                entity.HasOne(word => word.Filter)
                    .WithMany(filter => filter.words)
                    .HasForeignKey(word => word.filterId)
                    .HasConstraintName("filter_word_ibfk_2");
            });
            modelBuilder.Entity<UnitGS>(entity =>
            {
                entity.HasKey(e => e.unitId)
                    .HasName("PRIMARY");

                entity.ToTable("units_gs");

                entity.HasIndex(e => e.unitId)
                    .HasName("unit_id");

                entity.Property(e => e.unitId)
                    .HasColumnName("unit_id")
                    .HasColumnType("bigint(20)");
                
                entity.Property(e => e.dataId)
                    .HasColumnName("data_id")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.userPk)
                    .HasColumnName("user_pk")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.userIsPrivate)
                    .HasColumnName("user_is_private")
                    .HasColumnType("boolean");

                entity.Property(e => e.username)
                    .HasColumnName("username")
                    .HasColumnType("varchar(100)");

                entity.Property(e => e.commentPk)
                    .HasColumnName("comment_pk")
                    .HasColumnType("varchar(100)");

                entity.Property(e => e.createdAt)
                    .HasColumnName("created_at")
                    .HasColumnType("bigint(20)");
                
                entity.Property(e => e.unitHandled)
                    .HasColumnName("unit_handled")
                    .HasColumnType("boolean");

                entity.Property(e => e.handledAt)
                    .HasColumnName("handled_at")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.handleAgain)
                    .HasColumnName("handle_again")
                    .HasColumnType("boolean");
                
                entity.HasOne(unit => unit.Data)
                    .WithMany(data => data.Units)
                    .HasForeignKey(unit => unit.dataId)
                    .HasConstraintName("unit_gs_ibfk_1");
            });
            modelBuilder.Entity<MediaGS>(entity =>
            {
                entity.HasKey(media => media.mediaId)
                    .HasName("PRIMARY");

                entity.ToTable("media_gs");

                entity.HasIndex(media => media.mediaId)
                    .HasName("media_id");

                entity.Property(media => media.mediaId)
                    .HasColumnName("media_id")
                    .HasColumnType("bigint(20)");
                
                entity.Property(media => media.unitId)
                    .HasColumnName("unit_id")
                    .HasColumnType("bigint(20)");

                entity.Property(media => media.mediaPk)
                    .HasColumnName("media_pk")
                    .HasColumnType("varchar(256)");

                entity.Property(media => media.mediaQueue)
                    .HasColumnName("media_queue")
                    .HasColumnType("int(11)");

                entity.Property(media => media.mediaHandled)
                    .HasColumnName("media_handled")
                    .HasColumnType("boolean");

                entity.Property(media => media.handledAt)
                    .HasColumnName("handled_at")
                    .HasColumnType("bigint(20)");

                entity.HasOne(media => media.unit)
                    .WithMany(unit => unit.medias)
                    .HasForeignKey(media => media.unitId)
                    .HasConstraintName("media_gs_ibfk_1");
            });
            
            modelBuilder.Entity<HistoryGS>(entity =>
            {
                entity.HasKey(e => e.historyId)
                    .HasName("PRIMARY");

                entity.ToTable("history_gs");

                entity.HasIndex(e => e.historyId)
                    .HasName("history_id");

                entity.Property(e => e.historyId)
                    .HasColumnName("history_id")
                    .HasColumnType("bigint(20)");
                
                entity.Property(e => e.taskId)
                    .HasColumnName("task_id")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.createdAt)
                    .HasColumnName("created_at")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.historyUrl)
                    .HasColumnName("history_url")
                    .HasColumnType("varchar(100)");

                entity.HasOne(history => history.task)
                    .WithMany(task => task.Histories)
                    .HasForeignKey(history => history.taskId)
                    .HasConstraintName("history_gs_ibfk_1");
            });
            modelBuilder.Entity<IGAccount>(entity =>
            {
                entity.ToTable("ig_accounts");
                
                entity.HasKey(e => e.accountId)
                    .HasName("PRIMARY");

                entity.HasIndex(e => e.userId)
                    .HasName("user_id");

                entity.Property(e => e.accountId)
                    .HasColumnName("account_id")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.userId)
                    .HasColumnName("user_id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.createdAt)
                    .HasColumnName("created_at")
                    .HasColumnType("int(11)");
                
                entity.Property(e => e.sessionSave)
                    .HasColumnName("session_save")
                    .HasColumnType("text(2000) CHARACTER SET utf8 COLLATE utf8_general_ci");
                
                entity.Property(e => e.accountUsername)
                    .HasColumnName("account_username")
                    .HasColumnType("varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL");
                
                entity.Property(e => e.accountDeleted)
                    .HasColumnName("account_deleted")
                    .HasColumnType("boolean");
                    
                entity.HasOne(account => account.State)
                    .WithOne(state => state.account)
                    .HasConstraintName("account_state_ibfk_1");

                entity.HasOne(account => account.Profile)
                    .WithOne(profile => profile.account)
                    .HasConstraintName("account_profile_ibfk_1");

                entity.HasOne(account => account.timeAction)
                    .WithOne(time => time.account)
                    .HasConstraintName("account_time_ibfk_1");

                // entity.HasOne(account => account.Business)
                //     .WithOne(business => business.igAccount)
                //     .HasConstraintName("account_business_ibfk_1");

                entity.HasOne(account => account.User)
                    .WithMany(user => user.IGAccounts)
                    .HasForeignKey(account => account.userId)
                    .HasConstraintName("user_accounts_ibfk_1");
            }); 
            modelBuilder.Entity<SessionState>(entity =>
            {
                entity.ToTable("session_state");
                
                entity.HasKey(e => e.stateId)
                    .HasName("PRIMARY");

                entity.HasIndex(e => e.accountId)
                    .HasName("account_id");

                entity.Property(e => e.stateId)
                    .HasColumnName("state_id")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.accountId)
                    .HasColumnName("account_id")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.stateUsable)
                    .HasColumnName("state_usable")
                    .HasColumnType("boolean");

                entity.Property(e => e.stateChallenger)
                    .HasColumnName("state_challenger")
                    .HasColumnType("boolean");

                entity.Property(e => e.stateRelogin)
                    .HasColumnName("state_relogin")
                    .HasColumnType("boolean");

                entity.Property(e => e.stateSpammed)
                    .HasColumnName("state_spammed")
                    .HasColumnType("boolean");

                entity.Property(e => e.spammedStarted)
                    .HasColumnName("spammed_started")
                    .HasColumnType("DATETIME");

                entity.Property(e => e.spammedEnd)
                    .HasColumnName("spam_end")
                    .HasColumnType("DATETIME");
                
            }); 
            modelBuilder.Entity<TimeAction>(entity =>
            {
                entity.HasKey(e => e.timeId)
                    .HasName("PRIMARY");

                entity.ToTable("times_action");

                entity.HasIndex(e => e.accountId)
                    .HasName("account_id");

                entity.Property(e => e.timeId)
                    .HasColumnName("time_id")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.accountId)
                    .HasColumnName("account_id")
                    .HasColumnType("bigint(20)");
                
                entity.Property(e => e.accountOld)
                    .HasColumnName("account_old")
                    .HasColumnType("boolean");

                entity.Property(e => e.followCount)
                    .HasColumnName("follow_count")
                    .HasColumnType("int(11)");

                entity.Property(e => e.unfollowCount)
                    .HasColumnName("unfollow_count")
                    .HasColumnType("int(11)");

                entity.Property(e => e.likeCount)
                    .HasColumnName("like_count")
                    .HasColumnType("int(11)");

                entity.Property(e => e.commentCount)
                    .HasColumnName("comment_count")
                    .HasColumnType("int(11)");
                
                entity.Property(e => e.mentionsCount)
                    .HasColumnName("mentions_count")
                    .HasColumnType("int(11)");

                entity.Property(e => e.blockCount)
                    .HasColumnName("block_count")
                    .HasColumnType("int(11)");

                entity.Property(e => e.publicationCount)
                    .HasColumnName("publication_count")
                    .HasColumnType("int(11)");
                                
                entity.Property(e => e.messageDirectCount)
                    .HasColumnName("message_direct_count")
                    .HasColumnType("int(11)");
                
                entity.Property(e => e.watchingStoriesCount)
                    .HasColumnName("watching_stories_count")
                    .HasColumnType("int(11)");

                entity.Property(e => e.followLastAt)
                    .HasColumnName("follow_last_at")
                    .HasColumnType("datetime");

                entity.Property(e => e.unfollowLastAt)
                    .HasColumnName("unfollow_last_at")
                    .HasColumnType("datetime");
                
                entity.Property(e => e.likeLastAt)
                    .HasColumnName("like_last_at")
                    .HasColumnType("datetime");

                entity.Property(e => e.commentLastAt)
                    .HasColumnName("comment_last_at")
                    .HasColumnType("datetime");
                
                entity.Property(e => e.mentionsLastAt)
                    .HasColumnName("mentions_last_at")
                    .HasColumnType("datetime");

                entity.Property(e => e.blockLastAt)
                    .HasColumnName("block_last_at")
                    .HasColumnType("datetime");

                entity.Property(e => e.publicationLastAt)
                    .HasColumnName("publication_last_at")
                    .HasColumnType("datetime");

                entity.Property(e => e.messageDirectLastAt)
                    .HasColumnName("message_direct_last_at")
                    .HasColumnType("datetime");

                entity.Property(e => e.watchingStoriesLastAt)
                    .HasColumnName("watching_stories_last_at")
                    .HasColumnType("datetime");
            });
            modelBuilder.Entity<AutoPost>(entity =>
            {
                entity.HasKey(post => post.postId)
                    .HasName("PRIMARY");

                entity.ToTable("auto_posts");

                entity.HasIndex(post => post.sessionId)
                    .HasName("session_id");

                entity.Property(post => post.postId)
                    .HasColumnName("post_id")
                    .HasColumnType("bigint(20)");

                entity.Property(post => post.sessionId)
                    .HasColumnName("session_id")
                    .HasColumnType("bigint(20)");
                
                entity.Property(post => post.postType)
                    .HasColumnName("post_type")
                    .HasColumnType("boolean");
                
                entity.Property(post => post.postExecuted)
                    .HasColumnName("post_executed")
                    .HasColumnType("boolean");
                
                entity.Property(post => post.postDeleted)
                    .HasColumnName("post_deleted")
                    .HasColumnType("boolean");
                
                entity.Property(post => post.postStopped)
                    .HasColumnName("post_stopped")
                    .HasColumnType("boolean");
                
                entity.Property(post => post.postAutoDeleted)
                    .HasColumnName("post_auto_deleted")
                    .HasColumnType("boolean");
                
                entity.Property(post => post.createdAt)
                    .HasColumnName("created_at")
                    .HasColumnType("DATETIME");
                
                entity.Property(post => post.executeAt)
                    .HasColumnName("execute_at")
                    .HasColumnType("DATETIME");
                
                entity.Property(post => post.autoDelete)
                    .HasColumnName("auto_delete")
                    .HasColumnType("bigint(20)");
                
                entity.Property(post => post.deleteAfter)
                    .HasColumnName("delete_after")
                    .HasColumnType("DATETIME");
                
                entity.Property(post => post.postLocation)
                    .HasColumnName("post_location")
                    .HasColumnType("varchar(256) CHARACTER SET utf8 COLLATE utf8_general_ci");
                
                entity.Property(post => post.postDescription)
                    .HasColumnName("post_description")
                    .HasColumnType("text(2200) CHARACTER SET utf8 COLLATE utf8_general_ci");
                
                entity.Property(post => post.postComment)
                    .HasColumnName("post_comment")
                    .HasColumnType("varchar(256) CHARACTER SET utf8 COLLATE utf8_general_ci");
                
                entity.Property(post => post.categoryId)
                    .HasColumnName("category_id")
                    .HasColumnType("bigint(20) DEFAULT '0'");

                entity.HasOne(post => post.account)
                    .WithMany(session => session.AutoPosts)
                    .HasForeignKey(post => post.sessionId)
                    .HasConstraintName("auto_posts_ibfk_1");
            });
            modelBuilder.Entity<Category>(entity => 
            {
                entity.ToTable("categories");

                entity.HasKey(category => category.categoryId)
                    .HasName("PRIMARY");

                entity.HasIndex(category => category.accountId)
                    .HasName("account_id");

                entity.Property(category => category.categoryId)
                    .HasColumnName("category_id")
                    .HasColumnType("bigint(20)");

                entity.Property(category => category.accountId)
                    .HasColumnName("account_id")
                    .HasColumnType("bigint(20)");

                entity.Property(category => category.categoryName)
                    .HasColumnName("category_name")
                    .HasColumnType("varchar(20) CHARACTER SET utf8 COLLATE utf8_general_ci");

                entity.Property(category => category.categoryColor)
                    .HasColumnName("category_color")
                    .HasColumnType("varchar(20) CHARACTER SET utf8 COLLATE utf8_general_ci");

                entity.Property(category => category.categoryDeleted)
                    .HasColumnName("category_deleted")
                    .HasColumnType("boolean");
                
                entity.Property(category => category.createdAt)
                    .HasColumnName("created_at")
                    .HasColumnType("datetime");

                entity.HasOne(category => category.account)
                    .WithMany(account => account.Categories)
                    .HasForeignKey(category => category.accountId)
                    .HasConstraintName("account_category_ibfk_1");
            });
            modelBuilder.Entity<PostFile>(entity =>
            {
                entity.HasKey(file => file.fileId)
                    .HasName("PRIMARY");

                entity.ToTable("post_files");

                entity.HasIndex(file => file.postId)
                    .HasName("post_id");

                entity.Property(file => file.fileId)
                    .HasColumnName("file_id")
                    .HasColumnType("bigint(20)");

                entity.Property(file => file.postId)
                    .HasColumnName("post_id")
                    .HasColumnType("bigint(20)");
                
                entity.Property(file => file.filePath)
                    .HasColumnName("file_path")
                    .HasColumnType("varchar(256)");
                
                entity.Property(file => file.fileDeleted)
                    .HasColumnName("file_deleted")
                    .HasColumnType("boolean");
                
                entity.Property(file => file.fileOrder)
                    .HasColumnName("file_order")
                    .HasColumnType("int(11)");
                
                entity.Property(file => file.createdAt)
                    .HasColumnName("created_at")
                    .HasColumnType("DATETIME");
                
                entity.Property(file => file.mediaId)
                    .HasColumnName("media_id")
                    .HasColumnType("varchar(256)");
                
                entity.Property(file => file.videoThumbnail)
                    .HasColumnName("video_thumbnail")
                    .HasColumnType("varchar(256)");

                entity.HasOne(file => file.post)
                    .WithMany(post => post.files)
                    .HasForeignKey(file => file.postId)
                    .HasConstraintName("post_files_ibfk_1");
            });
            modelBuilder.Entity<AccountProfile>(entity =>
            {
                entity.ToTable("session_profiles");
                
                entity.HasKey(profile => profile.profileId)
                    .HasName("PRIMARY");

                entity.HasIndex(profile => profile.accountId)
                    .HasName("account_id");

                entity.Property(profile => profile.profileId)
                    .HasColumnName("profile_id")
                    .HasColumnType("bigint(20)");

                entity.Property(profile => profile.accountId)
                    .HasColumnName("account_id")
                    .HasColumnType("bigint(20)");

                entity.Property(profile => profile.updatedAt)
                    .HasColumnName("updated_at")
                    .HasColumnType("DATETIME");

                entity.Property(profile => profile.username)
                    .HasColumnName("username")
                    .HasColumnType("varchar(30)");

                entity.Property(profile => profile.postsCount)
                    .HasColumnName("posts_count")
                    .HasColumnType("bigint(20)");
                
                entity.Property(profile => profile.followingCount)
                    .HasColumnName("following_count")
                    .HasColumnType("bigint(20)");

                entity.Property(profile => profile.subscribersCount)
                    .HasColumnName("subscribers_count")
                    .HasColumnType("bigint(20)");

                entity.Property(profile => profile.avatarUrl)
                    .HasColumnName("avatar_url")
                    .HasColumnType("varchar(256)");

                entity.Property(profile => profile.subscribersGS)
                    .HasColumnName("subscribers_gs")
                    .HasColumnType("bigint(20)");
                
                entity.Property(profile => profile.subscribersTodayGS)
                    .HasColumnName("subscribers_today_gs")
                    .HasColumnType("bigint(20)");

                entity.Property(profile => profile.conversionGS)
                    .HasColumnName("conversion_gs")
                    .HasColumnType("bigint(20)");
            }); 
            modelBuilder.Entity<BusinessAccount>(entity =>
            {
                entity.ToTable("business_account");

                entity.HasKey(account => account.businessId)
                    .HasName("PRIMARY");

                entity.HasIndex(account => account.userId)
                    .HasName("user_id");

                entity.Property(account => account.businessId)
                    .HasColumnName("business_id")
                    .HasColumnType("bigint(20)");

                entity.Property(account => account.igAccountId)
                    .HasColumnName("ig_account_id")
                    .HasColumnType("bigint(20) DEFAULT '0'");
                
                entity.Property(account => account.userId)
                    .HasColumnName("user_id")
                    .HasColumnType("int(11)");
                
                entity.Property(account => account.accountUsername)
                    .HasColumnName("account_username")
                    .HasColumnType("varchar(100)");
                
                entity.Property(account => account.profilePicture)
                    .HasColumnName("profile_picture")
                    .HasColumnType("varchar(400) CHARACTER SET utf8 COLLATE utf8_general_ci");
                
                entity.Property(account => account.accessToken)
                    .HasColumnName("access_token")
                    .HasColumnType("varchar(300)");

                entity.Property(account => account.longLiveAccessToken)
                    .HasColumnName("long_live_access_token")
                    .HasColumnType("varchar(300)");

                entity.Property(account => account.facebookId)
                    .HasColumnName("facebook_id")
                    .HasColumnType("varchar(256)");

                entity.Property(account => account.businessAccountId)
                    .HasColumnName("business_account_id")
                    .HasColumnType("varchar(256)");

                entity.Property(account => account.createdAt)
                    .HasColumnName("created_at")
                    .HasColumnType("DateTime");

                entity.Property(account => account.longTokenExpiresIn)
                    .HasColumnName("long_token_expires_in")
                    .HasColumnType("DateTime");
                
                entity.Property(account => account.tokenCreated)
                    .HasColumnName("token_created")
                    .HasColumnType("DateTime");

                entity.Property(account => account.followersCount)
                    .HasColumnName("followers_count")
                    .HasColumnType("bigint(20)");

                entity.Property(account => account.mediaCount)
                    .HasColumnName("media_count")
                    .HasColumnType("int(11) DEFAULT '0'");

                entity.Property(account => account.deleted)
                    .HasColumnName("deleted")
                    .HasColumnType("bool");

                entity.Property(account => account.received)
                    .HasColumnName("received")
                    .HasColumnType("bool");
                
                entity.Property(account => account.startProcess)
                    .HasColumnName("start_process")
                    .HasColumnType("bool");
                
                entity.Property(account => account.startedProcess)
                    .HasColumnName("started_process")
                    .HasColumnType("DateTime");

                entity.HasOne(account => account.user)
                    .WithMany(user => user.bIGAccounts)
                    .HasForeignKey(account => account.userId)
                    .HasConstraintName("user_bigaccounts_ibfk_1");
            });
            modelBuilder.Entity<DayStatistics>(entity =>
            {
                entity.ToTable("statistics");

                entity.HasKey(statistics => statistics.statisticsId)
                    .HasName("PRIMARY");

                entity.HasIndex(statistics => statistics.accountId)
                    .HasName("account_id");

                entity.Property(statistics => statistics.statisticsId)
                    .HasColumnName("statistics_id")
                    .HasColumnType("bigint(20)");

                entity.Property(statistics => statistics.accountId)
                    .HasColumnName("account_id")
                    .HasColumnType("bigint(20)");
                
                entity.Property(statistics => statistics.followerCount)
                    .HasColumnName("follower_count")
                    .HasColumnType("int(11)");

                entity.Property(statistics => statistics.emailContacts)
                    .HasColumnName("email_contacts")
                    .HasColumnType("int(11)");

                entity.Property(statistics => statistics.profileViews)
                    .HasColumnName("profile_views")
                    .HasColumnType("bigint(20)");

                entity.Property(statistics => statistics.getDirectionsClicks)
                    .HasColumnName("directions_clicks")
                    .HasColumnType("int(11)");

                entity.Property(statistics => statistics.phoneCallClicks)
                    .HasColumnName("phone_call_clicks")
                    .HasColumnType("int(11)");

                entity.Property(statistics => statistics.textMessageClicks)
                    .HasColumnName("text_message_clicks")
                    .HasColumnType("int(11)");

                entity.Property(statistics => statistics.websiteClicks)
                    .HasColumnName("website_clicks")
                    .HasColumnType("int(11)");

                entity.Property(statistics => statistics.impressions)
                    .HasColumnName("impressions")
                    .HasColumnType("bigint(20)");

                entity.Property(statistics => statistics.reach)
                    .HasColumnName("reach")
                    .HasColumnType("bigint(20)");

                entity.Property(statistics => statistics.endTime)
                    .HasColumnName("end_time")
                    .HasColumnType("DateTime");

                entity.HasOne(statistics => statistics.Account)
                    .WithMany(account => account.Statistics)
                    .HasForeignKey(statistics => statistics.accountId)
                    .HasConstraintName("statistics_ibfk_1");
            });
            modelBuilder.Entity<OnlineFollowers>(entity =>
            {
                entity.ToTable("online_followers");

                entity.HasKey(followers => followers.followersId)
                    .HasName("PRIMARY");

                entity.HasIndex(followers => followers.accountId)
                    .HasName("account_id");

                entity.Property(followers => followers.followersId)
                    .HasColumnName("followers_id")
                    .HasColumnType("bigint(20)");

                entity.Property(followers => followers.accountId)
                    .HasColumnName("account_id")
                    .HasColumnType("bigint(20)");
                
                entity.Property(followers => followers.value)
                    .HasColumnName("value")
                    .HasColumnType("bigint(20)");

                entity.Property(followers => followers.endTime)
                    .HasColumnName("end_time")
                    .HasColumnType("DateTime");

                entity.HasOne(followers => followers.Account)
                    .WithMany(account => account.OnlineFollowers)
                    .HasForeignKey(followers => followers.accountId)
                    .HasConstraintName("online_followers_ibfk_1");
            });
            modelBuilder.Entity<StoryStatistics>(entity =>
            {
                entity.ToTable("story_statistics");

                entity.HasKey(story => story.storyId)
                    .HasName("PRIMARY");

                entity.HasIndex(story => story.accountId)
                    .HasName("account_id");

                entity.Property(story => story.storyId)
                    .HasColumnName("story_id")
                    .HasColumnType("bigint(20)");

                entity.Property(story => story.accountId)
                    .HasColumnName("account_id")
                    .HasColumnType("bigint(20)");
                
                entity.Property(story => story.mediaId)
                    .HasColumnName("media_id")
                    .HasColumnType("varchar(100)");

                entity.Property(story => story.storyUrl)
                    .HasColumnName("story_url")
                    .HasColumnType("varchar(256)");
                
                entity.Property(story => story.storyType)
                    .HasColumnName("story_type")
                    .HasColumnType("varchar(100)");

                entity.Property(story => story.replies)
                    .HasColumnName("replies")
                    .HasColumnType("int(11)");
                
                entity.Property(story => story.exists)
                    .HasColumnName("story_exists")
                    .HasColumnType("boolean");
                
                entity.Property(story => story.impressions)
                    .HasColumnName("impressions")
                    .HasColumnType("bigint(20)");
                
                entity.Property(story => story.reach)
                    .HasColumnName("reach")
                    .HasColumnType("bigint(20)");
                
                entity.Property(story => story.timestamp)
                    .HasColumnName("timestamp")
                    .HasColumnType("DateTime");

                entity.HasOne(story => story.Account)
                    .WithMany(account => account.Stories)
                    .HasForeignKey(story => story.accountId)
                    .HasConstraintName("story_statistics_ibfk_1");
            });
            modelBuilder.Entity<PostStatistics>(entity =>
            {
                entity.ToTable("post_statistics");

                entity.HasKey(post => post.postId)
                    .HasName("PRIMARY");

                entity.HasIndex(post => post.accountId)
                    .HasName("account_id");

                entity.Property(post => post.postId)
                    .HasColumnName("post_id")
                    .HasColumnType("bigint(20)");

                entity.Property(post => post.accountId)
                    .HasColumnName("account_id")
                    .HasColumnType("bigint(20)");
                
                entity.Property(post => post.likeCount)
                    .HasColumnName("like_count")
                    .HasColumnType("bigint(20)");

                entity.Property(post => post.IGMediaId)
                    .HasColumnName("ig_media_id")
                    .HasColumnType("varchar(50)");
                
                entity.Property(post => post.postUrl)
                    .HasColumnName("post_url")
                    .HasColumnType("varchar(256)");

                entity.Property(post => post.commentsCount)
                    .HasColumnName("comments_count")
                    .HasColumnType("int(11)");

                entity.Property(post => post.mediaType)
                    .HasColumnName("media_type")
                    .HasColumnType("varchar(20)");

                entity.Property(post => post.engagement)
                    .HasColumnName("engagement")
                    .HasColumnType("bigint(20)");

                entity.Property(post => post.impressions)
                    .HasColumnName("impressions")
                    .HasColumnType("bigint(20)");

                entity.Property(post => post.reach)
                    .HasColumnName("reach")
                    .HasColumnType("bigint(20)");

                entity.Property(post => post.saved)
                    .HasColumnName("saved")
                    .HasColumnType("bigint(20)");

                entity.Property(post => post.videoViews)
                    .HasColumnName("video_views")
                    .HasColumnType("bigint(20)");

                entity.Property(post => post.timestamp)
                    .HasColumnName("timestamp")
                    .HasColumnType("DateTime");

                entity.HasOne(post => post.Account)
                    .WithMany(account => account.Posts)
                    .HasForeignKey(post => post.accountId)
                    .HasConstraintName("post_statistics_ibfk_1");
            });
            modelBuilder.Entity<CommentStatistics>(entity =>
            {
                entity.ToTable("comment_statistics");

                entity.HasKey(comment => comment.commentId)
                    .HasName("PRIMARY");

                entity.HasIndex(comment => comment.mediaId)
                    .HasName("media_id");

                entity.Property(comment => comment.commentId)
                    .HasColumnName("comment_id")
                    .HasColumnType("bigint(20)");

                entity.Property(comment => comment.mediaId)
                    .HasColumnName("media_id")
                    .HasColumnType("bigint(20)");
                
                entity.Property(comment => comment.commentIGId)
                    .HasColumnName("comment_ig_id")
                    .HasColumnType("varchar(30)");
                
                entity.Property(comment => comment.timestamp)
                    .HasColumnName("timestamp")
                    .HasColumnType("DateTime");

                entity.HasOne(comment => comment.Post)
                    .WithMany(media => media.Comments)
                    .HasForeignKey(comment => comment.mediaId)
                    .HasConstraintName("comment_statistics_ibfk_1");
            });
            modelBuilder.Entity<Admin>(entity =>
            {
                entity.ToTable("admins");

                entity.HasKey(admin => admin.adminId)
                    .HasName("PRIMARY");

                entity.Property(admin => admin.adminId)
                    .HasColumnName("admin_id")
                    .HasColumnType("int(11)");

                entity.Property(admin => admin.adminEmail)
                    .HasColumnName("admin_email")
                    .HasColumnType("varchar(255)");
                
                entity.Property(admin => admin.adminFullname)
                    .HasColumnName("admin_fullname")
                    .HasColumnType("varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci");
                
                entity.Property(admin => admin.adminRole)
                    .HasColumnName("admin_role")
                    .HasColumnType("varchar(100)");
                
                entity.Property(admin => admin.adminPassword)
                    .HasColumnName("admin_password")
                    .HasColumnType("varchar(255)");

                entity.Property(admin => admin.passwordToken)
                    .HasColumnName("password_token")
                    .HasColumnType("varchar(10)");
                
                entity.Property(admin => admin.createdAt)
                    .HasColumnName("created_at")
                    .HasColumnType("bigint(20)");
                
                entity.Property(admin => admin.lastLoginAt)
                    .HasColumnName("last_login_at")
                    .HasColumnType("bigint(20)");
                
                entity.Property(admin => admin.recoveryCode)
                    .HasColumnName("recovery_code")
                    .HasColumnType("int(11)");

                entity.Property(admin => admin.deleted)
                    .HasColumnName("deleted")
                    .HasColumnType("boolean");
            });
            modelBuilder.Entity<BlogPost>(entity =>
            {
                entity.ToTable("blog_posts");

                entity.HasKey(post => post.postId)
                    .HasName("PRIMARY");

                entity.HasIndex(post => post.adminId)
                    .HasName("admin_id");

                entity.Property(post => post.postId)
                    .HasColumnName("post_id")
                    .HasColumnType("int(11)");

                entity.Property(post => post.adminId)
                    .HasColumnName("admin_id")
                    .HasColumnType("int(11)");

                entity.Property(post => post.postSubject)
                    .HasColumnName("post_subject")
                    .HasColumnType("varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci");
                
                entity.Property(post => post.postHtmlText)
                    .HasColumnName("post_htmltext")
                    .HasColumnType("text CHARACTER SET utf8 COLLATE utf8_general_ci");
                
                entity.Property(post => post.postLanguage)
                    .HasColumnName("post_language")
                    .HasColumnType("int(11)");
                
                entity.Property(post => post.createdAt)
                    .HasColumnName("created_at")
                    .HasColumnType("DateTime");
                
                entity.Property(post => post.updatedAt)
                    .HasColumnName("updated_at")
                    .HasColumnType("DateTime");
                
                entity.Property(post => post.deleted)
                    .HasColumnName("deleted")
                    .HasColumnType("boolean");
                
                entity.HasOne(post => post.admin)
                    .WithMany(admin => admin.posts)
                    .HasForeignKey(post => post.adminId)
                    .HasConstraintName("admin_posts_ibfk_1");
            });
            modelBuilder.Entity<Appeal>(entity =>
            {
                entity.ToTable("appeals");

                entity.HasKey(appeal => appeal.appealId)
                    .HasName("PRIMARY");

                entity.HasIndex(appeal => appeal.userId)
                    .HasName("user_id");

                entity.Property(appeal => appeal.appealId)
                    .HasColumnName("appeal_id")
                    .HasColumnType("int(11)");

                entity.Property(appeal => appeal.userId)
                    .HasColumnName("user_id")
                    .HasColumnType("int(11)");

                entity.Property(appeal => appeal.appealSubject)
                    .HasColumnName("appeal_subject")
                    .HasColumnType("varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci");
                
                entity.Property(appeal => appeal.appealState)
                    .HasColumnName("appeal_state")
                    .HasColumnType("int(11)");
                
                entity.Property(appeal => appeal.createdAt)
                    .HasColumnName("created_at")
                    .HasColumnType("DateTime");
                
                entity.Property(appeal => appeal.lastActivity)
                    .HasColumnName("last_activity")
                    .HasColumnType("DateTime");

                entity.HasOne(appeal => appeal.user)
                    .WithMany(user => user.Appeals)
                    .HasForeignKey(appeal => appeal.userId)
                    .HasConstraintName("user_appeals_ibfk_1");
            });
            modelBuilder.Entity<AppealMessage>(entity =>
            {
                entity.ToTable("appeal_messages");

                entity.HasKey(message => message.messageId)
                    .HasName("PRIMARY");

                entity.HasIndex(message => message.appealId)
                    .HasName("appeal_id");

                entity.Property(message => message.messageId)
                    .HasColumnName("message_id")
                    .HasColumnType("bigint(20)");

                entity.Property(message => message.appealId)
                    .HasColumnName("appeal_id")
                    .HasColumnType("int(11)");

                entity.Property(message => message.adminId)
                    .HasColumnName("admin_id")
                    .HasColumnType("int(11)");
                
                entity.Property(message => message.messageText)
                    .HasColumnName("message_text")
                    .HasColumnType("text CHARACTER SET utf8 COLLATE utf8_general_ci");
                
                entity.Property(message => message.createdAt)
                    .HasColumnName("created_at")
                    .HasColumnType("DateTime");
                
                entity.Property(message => message.updatedAt)
                    .HasColumnName("updated_at")
                    .HasColumnType("DateTime");
                
                entity.HasOne(message => message.appeal)
                    .WithMany(appeal => appeal.messages)
                    .HasForeignKey(message => message.appealId)
                    .HasConstraintName("appeal_messages_ibfk_1");

                entity.HasOne(message => message.admin)
                    .WithMany(admin => admin.messages)
                    .HasForeignKey(message => message.adminId)
                    .HasConstraintName("admin_messages_ibfk_1");
            });
            modelBuilder.Entity<AppealFile>(entity =>
            {
                entity.ToTable("appeal_files");

                entity.HasKey(file => file.fileId)
                    .HasName("PRIMARY");

                entity.HasIndex(file => file.messageId)
                    .HasName("message_id");

                entity.Property(file => file.fileId)
                    .HasColumnName("file_id")
                    .HasColumnType("bigint(20)");

                entity.Property(file => file.messageId)
                    .HasColumnName("message_id")
                    .HasColumnType("bigint(20)");

                entity.Property(file => file.relativePath)
                    .HasColumnName("relative_path")
                    .HasColumnType("varchar(255)");
                
                entity.HasOne(file => file.message)
                    .WithMany(message => message.files)
                    .HasForeignKey(file => file.messageId)
                    .HasConstraintName("appeal_files_ibfk_1");
            });
            modelBuilder.Entity<ServiceAccess>(entity =>
            {
                entity.ToTable("service_access");

                entity.HasKey(access => access.accessId)
                    .HasName("PRIMARY");

                entity.HasIndex(access => access.userId)
                    .HasName("user_id");

                entity.Property(access => access.accessId)
                    .HasColumnName("access_id")
                    .HasColumnType("int(11)");

                entity.Property(access => access.userId)
                    .HasColumnName("user_id")
                    .HasColumnType("int(11)");
                
                entity.Property(access => access.available)
                    .HasColumnName("available")
                    .HasColumnType("boolean");
                 
                entity.Property(access => access.packageType)
                    .HasColumnName("package_type")
                    .HasColumnType("int(11)");

                entity.Property(access => access.paid)
                    .HasColumnName("paid")
                    .HasColumnType("boolean");

                entity.Property(access => access.paidAt)
                    .HasColumnName("paid_at")
                    .HasColumnType("datetime");

                entity.Property(access => access.disableAt)
                    .HasColumnName("disable_at")
                    .HasColumnType("datetime");

                entity.HasOne(access => access.User)
                    .WithOne(user => user.access)
                    .HasConstraintName("user_access_ibfk_1");
            });
        }
    }
}
