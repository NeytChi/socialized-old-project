using System;
using Serilog;
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
using Domain.AutoPosting;

namespace AutoPosting
{
    public class AutoPosting
    {
        private IAutoPostRepository AutoPostRepository;
        private IPostFileRepository PostFileRepository;

        private ILogger Logger;
        public string s3UploadedFiles;
        public SessionManager smanager;
        public SessionStateHandler stateHandler;
        public InstagramApi api;
        public AutoPosting(IAutoPostRepository autoPostRepository, 
            IPostFileRepository postFileRepository)
        {
            AutoPostRepository = autoPostRepository;
            PostFileRepository = postFileRepository;

            this.s3UploadedFiles = s3UploadedFiles;
            this.smanager = new SessionManager(contextPosting);
            this.stateHandler = new SessionStateHandler(contextPosting);
            this.api = InstagramApi.GetInstance();
            PostFileRepository = postFileRepository;
         }
        /// <summary>
        /// Perform auto-post at the appointed time.
        /// </summary>
        public void PerformAutoPost(AutoPost post)
        {
            if (post.executeAt > DateTime.Now) 
            {
                Logger.Information("Auto post was stoped on " + (post.executeAt - DateTime.Now).TotalSeconds + " seconds");
                Thread.Sleep(post.executeAt - DateTime.Now);
            }
            if (RouteAutoPost(post)) 
            {
                post.postExecuted = true;
                AutoPostRepository.Update(post);
                Logger.Information("Set auto post executed, id -> " + post.postId);
                return;
            }
            Logger.Warning("Can't perform auto-post, id -> " + post.postId);
        }
        public bool RouteAutoPost(AutoPost post)
        {
            var session = smanager.LoadSession(post.sessionId);
            if (session != null) 
            {
                if (post.postType)
                {
                    return PerformPost(post, session);
                }
                else
                {
                    return PerformStories(post, session);
                }
            }
            return false;
        }
        public bool PerformPost(AutoPost post, Session session)
        {   
            InstaLocationShort location = null; InstaAlbumUpload[] album;

            if ((album = CreateAlbum(post.files)) == null)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(post.postLocation))
            {
                location = GetLocationShort(ref session, post.postLocation);
            }
            var mediaId = CreatePost(ref session, album, post.postDescription, location);
            if (string.IsNullOrEmpty(mediaId))
            {
                return false;
            }
            if (!string.IsNullOrEmpty(post.postComment))
            {
                CreateComment(ref session, mediaId, post.postComment);
            }
            if (post.autoDelete) 
            {
                foreach (var file in post.files)
                {
                    file.mediaId = mediaId;
                    PostFileRepository.Update(file);
                }
            }
            Logger.Information("Perform posting, id -> " + post.postId);
            return true;
        }
        public string CreatePost(ref Session session, InstaAlbumUpload[] album, string caption, InstaLocationShort location)
        {
            var result = SendPostByAlbum(ref session, album, caption, location);
            if (result.Succeeded)
            {
                Logger.Information("Create a new post by session, user id ->" + session.userId);
                return result.Value.Pk;
            }
            else
            {
                stateHandler.HandleState(result.Info.ResponseType, session);
            }
            Logger.Error("Can't create a post, message ->" + result.Info.Message + ", user id -> " + session.userId);
            return null;
        }
        public IResult<InstaMedia> SendPostByAlbum(ref Session session, InstaAlbumUpload[] album, string caption, InstaLocationShort location)
        {
            if (album.Length == 1) 
            {
                if (album[0].ImageToUpload != null)
                {
                    return api.helper.SendMediaPhotoAsync(ref session, album[0].ImageToUpload, caption, location);
                }
                else
                {
                    return api.media.UploadVideo(ref session, album[0].VideoToUpload, caption, location);
                }
            }
            return api.media.UploadAlbum(ref session, album, caption, location);
        }
        public bool CreateComment(ref Session session, string mediaId, string comment)
        {
            var result = api.comment.CommentMedia(ref session, mediaId, comment);
            if (result.Succeeded)
            {
                Logger.Information("Create a comment for post, user id ->" + session.userId);
                return true;
            }
            else
            {
                stateHandler.HandleState(result.Info.ResponseType, session);
            }
            Logger.Error("Can't create a comment, message ->" + result.Info.Message + ", user id -> " + session.userId);
            return false;
        }
        public InstaLocationShort GetLocationShort(ref Session session, string location)
        {
            var result = api.location.SearchLocation(ref session, 0, 0, location);
            if (result.Succeeded)
            {
                if (result.Value.Count > 0)
                {
                    Logger.Information("Get location short, user id -> " + session.userId);
                    return result.Value[0];
                }
            }
            else
            {
                stateHandler.HandleState(result.Info.ResponseType, session);
            }
            Logger.Error("Can't get location short, message -> " + result.Info.Message + " user id -> " + session.userId);
            return null;
        }   
        public InstaAlbumUpload[] CreateAlbum(ICollection<PostFile> files)
        {
            int index = 0;
            var album = new InstaAlbumUpload[files.Count];

            foreach (var file in files)
            {
                album[index++] = GetPostFileObject(file);
            }
            Logger.Information("Create album for post");
            return album;
        }
        public bool PerformStories(AutoPost post, Session session)
        {   
            foreach (var file in post.files) 
            {
                string mediaId = CreateStories(ref session, file);
                if (mediaId == null)
                {
                    return false;
                }
                if (post.autoDelete)
                {
                    file.mediaId = mediaId;
                    PostFileRepository.Update(file);
                }
            }
            Logger.Information("Perform post, id -> " + post.postId);
            return true;
        }
        public string CreateStories(ref Session session, PostFile file)
        {
            if (file.fileType)
            {
                return CreateStoryVideo(ref session, GetPostFileObject(file).VideoToUpload);
            }
            else
            {
                var client = new WebClient();
                return CreateStoryImage(ref session,
                    new InstaImage()
                    {
                        Height = 0,
                        Width = 0,
                        ImageBytes = client.DownloadData(s3UploadedFiles + file.filePath)
                    });
            }
        }
        public InstaAlbumUpload GetPostFileObject(PostFile file)
        {
            var client = new WebClient();
            
            if (file.fileType) 
            {
                return  new InstaAlbumUpload() 
                {
                    VideoToUpload = new InstaVideoUpload() 
                    {
                        Video = new InstaVideo() 
                        {
                            Height = 0, Width = 0, VideoBytes = client.DownloadData(s3UploadedFiles + file.filePath)
                        },
                        VideoThumbnail = new InstaImage() 
                        {
                            Height = 0, Width = 0, ImageBytes = client.DownloadData(s3UploadedFiles + file.videoThumbnail)
                        }
                    }
                };
            }
            else
                return new InstaAlbumUpload 
                {
                    ImageToUpload = new InstaImageUpload 
                    {
                    
                        Height = 0, 
                        Width = 0, 
                        ImageBytes = client.DownloadData(s3UploadedFiles + file.filePath) 
                    }
                };
        }
        /// <summary>
        /// Create story with image.
        /// </summary>
        public string CreateStoryImage(ref Session session, InstaImage image)
        {
            var result = api.story.UploadStoryPhoto(ref session, image, string.Empty);
            if (result.Succeeded)
            {
                return result.Value.Media.Pk.ToString();
            }
            else
            {
                stateHandler.HandleState(result.Info.ResponseType, session);
            }
            Logger.Warning("Can't create story image, session id ->" + session.sessionId);
            return null;
        }

        /// <summary>
        /// Create story with video.
        /// </summary>
        public string CreateStoryVideo(ref Session session, InstaVideoUpload video)
        {
            var result = api.story.UploadStoryVideo(ref session, video, string.Empty);
            if (result.Succeeded)
            {
                Logger.Information("Create a story, user id -> " + session.userId);
                return result.Value.Media.Pk.ToString();
            }
            else
            {
                stateHandler.HandleState(result.Info.ResponseType, session);
            }
            Logger.Warning("Can't create story video, user id ->" + session.userId + " message -> " + result.Info.Message);
            return null;
        }
    }
}