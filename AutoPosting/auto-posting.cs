using System;
using System.Net;
using System.Threading;
using System.Collections.Generic;

using Serilog;
using Serilog.Core;

using Managment;
using database.context;
using Models.AutoPosting;

using InstagramService;
using InstagramApiSharp.API;
using InstagramApiSharp.Classes;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;

namespace nautoposting
{
    public class AutoPosting
    {
        Logger log = new LoggerConfiguration()
            .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        public string s3UploadedFiles;
        public SessionManager smanager;
        public SessionStateHandler stateHandler;
        public InstagramApi api;
        public Context contextPosting;
        
        public AutoPosting(string s3UploadedFiles)
        {
            this.contextPosting = new Context(false);
            this.s3UploadedFiles = s3UploadedFiles;
            this.smanager = new SessionManager(contextPosting);
            this.stateHandler = new SessionStateHandler(contextPosting);
            this.api = InstagramApi.GetInstance();
        }
        /// <summary>
        /// Perform auto-post at the appointed time.
        /// </summary>
        public void PerformAutoPost(AutoPost post)
        {
            if (post.executeAt > DateTime.Now) {
                log.Information("Auto post was stoped on " + (post.executeAt - DateTime.Now).TotalSeconds + " seconds");
                Thread.Sleep(post.executeAt - DateTime.Now);
            }
            if (RouteAutoPost(post)) {
                EndAutoPost(post);
                return;
            }
            log.Warning("Can't perform auto-post, id -> " + post.postId);
        }
        /// <summary>
        /// Route on what type of auto-post it would be.
        /// </summary>
        public bool RouteAutoPost(AutoPost post)
        {
            Session session = smanager.LoadSession(post.sessionId);
            if (session != null) {
                if (post.postType)
                    return PerformPost(post, session);
                else
                    return PerformStories(post, session);
            }
            return false;
        }
        public bool PerformPost(AutoPost post, Session session)
        {   
            string mediaId;
            InstaLocationShort location = null; InstaAlbumUpload[] album;
        
            if ((album = CreateAlbum(post.files)) != null) {
                if (!string.IsNullOrEmpty(post.postLocation))
                    location = GetLocationShort(ref session, post.postLocation);
                mediaId = CreatePost(ref session, album, post.postDescription, location);
                if (!string.IsNullOrEmpty(mediaId)) {
                    if (!string.IsNullOrEmpty(post.postComment))
                        CreateComment(ref session, mediaId, post.postComment);
                    if (post.autoDelete) {
                        foreach (PostFile file in post.files)
                            UpdatePostMediaId(file, mediaId);
                    }
                    log.Information("Perform posting, id -> " + post.postId);
                    return true;
                }
            }
            return false;
        }
        public string CreatePost(ref Session session, InstaAlbumUpload[] album, string caption, InstaLocationShort location)
        {
            IResult<InstaMedia> result = SendPostByAlbum(ref session, album, caption, location);
            if (result.Succeeded) {
                log.Information("Create a new post by session, user id ->" + session.userId);
                return result.Value.Pk;
            }
            else
                stateHandler.HandleState(result.Info.ResponseType, session);
            log.Error("Can't create a post, message ->" + result.Info.Message + ", user id -> " + session.userId);
            return null;
        }
        public IResult<InstaMedia> SendPostByAlbum(ref Session session, InstaAlbumUpload[] album, string caption, InstaLocationShort location)
        {
            if (album.Length == 1) {
                if (album[0].ImageToUpload != null)
                    return api.helper.SendMediaPhotoAsync(ref session, album[0].ImageToUpload, caption, location);
                else
                    return api.media.UploadVideo(ref session, album[0].VideoToUpload, caption, location);
            }
            return api.media.UploadAlbum(ref session, album, caption, location);
        }
        public bool CreateComment(ref Session session, string mediaId, string comment)
        {
            IResult<InstaComment> result = api.comment.CommentMedia(ref session, mediaId, comment);
            if (result.Succeeded) {
                log.Information("Create a comment for post, user id ->" + session.userId);
                return true;
            }
            else
                stateHandler.HandleState(result.Info.ResponseType, session);
            log.Error("Can't create a comment, message ->" + result.Info.Message + ", user id -> " + session.userId);
            return false;
        }
        public InstaLocationShort GetLocationShort(ref Session session, string location)
        {
            IResult<InstaLocationShortList> result = api.location.SearchLocation(ref session, 
                0, 0, location);
            if (result.Succeeded) {
                if (result.Value.Count > 0) {
                    log.Information("Get location short, user id -> " + session.userId);
                    return result.Value[0];
                }    
            }
            else
                stateHandler.HandleState(result.Info.ResponseType, session);
            log.Error("Can't get location short, message -> " + result.Info.Message + " user id -> " + session.userId);
            return null;
        }   
        /// <summary>
        /// Create album for posting.
        /// </summary>
        public InstaAlbumUpload[] CreateAlbum(ICollection<PostFile> files)
        {
            int index = 0;
            InstaAlbumUpload[] album = new InstaAlbumUpload[files.Count];

            foreach (PostFile file in files)
                album[index++] = GetPostFileObject(file);
            log.Information("Create album for post");
            return album;
        }
        public bool PerformStories(AutoPost post, Session session)
        {   
            foreach (PostFile file in post.files) {
                string mediaId = CreateStories(ref session, file);
                if (mediaId == null)
                    return false;
                if (post.autoDelete)
                    UpdatePostMediaId(file, mediaId);
            }
            log.Information("Perform post, id -> " + post.postId);
            return true;
        }
        public string CreateStories(ref Session session, PostFile file)
        {
            if (file.fileType)
                return CreateStoryVideo(ref session, GetPostFileObject(file).VideoToUpload);
            else {
                WebClient client = new WebClient();
                return CreateStoryImage(ref session, 
                    new InstaImage() { 
                        Height = 0, Width = 0, 
                        ImageBytes = client.DownloadData(s3UploadedFiles + file.filePath) 
                    });
            }
        }
        public InstaAlbumUpload GetPostFileObject(PostFile file)
        {
            WebClient client = new WebClient();
            
            if (file.fileType) {
                return  new InstaAlbumUpload() {
                    VideoToUpload = new InstaVideoUpload() {
                        Video = new InstaVideo() {
                            Height = 0, Width = 0, VideoBytes = client.DownloadData(s3UploadedFiles + file.filePath)
                        },
                        VideoThumbnail = new InstaImage() {
                            Height = 0, Width = 0, ImageBytes = client.DownloadData(s3UploadedFiles + file.videoThumbnail)
                        }
                    }
                };
            }
            else
                return new InstaAlbumUpload {
                    ImageToUpload = new InstaImageUpload {
                    Height = 0, Width = 0, ImageBytes = client.DownloadData(s3UploadedFiles + file.filePath)
            }};
        }
        /// <summary>
        /// Create story with image.
        /// </summary>
        public string CreateStoryImage(ref Session session, InstaImage image)
        {
            IResult<InstaStoryMedia> result = api.story.UploadStoryPhoto(ref session, 
                image, string.Empty);
            if (result.Succeeded)
                return result.Value.Media.Pk.ToString();
            else
                stateHandler.HandleState(result.Info.ResponseType, session);
            log.Warning("Can't create story image, session id ->" + session.sessionId);
            return null;
        }

        /// <summary>
        /// Create story with video.
        /// </summary>
        public string CreateStoryVideo(ref Session session, InstaVideoUpload video)
        {
            IResult<InstaStoryMedia> result = api.story.UploadStoryVideo(ref session, 
            video, string.Empty);
            if (result.Succeeded) {
                log.Information("Create a story, user id -> " + session.userId);
                return result.Value.Media.Pk.ToString();
            }
            else
                stateHandler.HandleState(result.Info.ResponseType, session);
            log.Warning("Can't create story video, user id ->" + session.userId + " message -> " + result.Info.Message);
            return null;
        }
        public void EndAutoPost(AutoPost post)
        {
            post.postExecuted = true;
            contextPosting.AutoPosts.Attach(post).Property(p => p.postExecuted).IsModified = true;
            contextPosting.SaveChanges();    
            log.Information("Set auto post executed, id -> " + post.postId);
        }
        /// <summary>
        /// Add media id for post file, this need to perform auto-delete post.
        /// </summary>
        public void UpdatePostMediaId(PostFile file, string mediaId)
        {
            file.mediaId = mediaId;
            contextPosting.PostFiles.Attach(file).Property(p => p.mediaId).IsModified = true;
            contextPosting.SaveChanges();
        }
    }
}