using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Controllers;
using database.context;
using Models.Statistics;

namespace InstagramService.Statistics
{
    public class PostingStatistics : IStatistics
    {
        public PostingStatistics(StatisticsService service, Context context, JsonHandler handler)
        {
            this.context = context;
            this.service = service;
            this.handler = handler;
        }
        public PostingStatistics(StatisticsService service, Context context, JsonHandler handler, int gettingDays)
        {
            this.context = context;
            this.service = service;
            this.handler = handler;
            this.gettingDays = gettingDays;
        }
        public JsonHandler handler;
        public StatisticsService service;
        public Context context;
        int gettingDays = -1;

        public void GetStatistics(BusinessAccount account)
        {
            string url, response;

            if ((url = GetURL(account.businessAccountId, account.longLiveAccessToken)) != null) {
                response = service.GetFacebookRequest(url);
                if (!string.IsNullOrEmpty(response))
                    ReceivePosts(JsonConvert.DeserializeObject<JObject>(response), account.businessId);
            }
        }
        public string GetURL(string IGId, string accessToken)
        {
            if (CheckTokenAndIG(accessToken, IGId)) {
                return IGId + "/media?fields=comments{timestamp,id},like_count,media_url," + 
                "comments_count,timestamp,id,media_type&access_token=" + accessToken;
            }
            return null;
        }
        public void ReceivePosts(JObject json, long accountId)
        {
            JToken data = handler.handle(json, "data", JTokenType.Array);
            if (data != null) {
                List<PostValues> dataArray = data.ToObject<List<PostValues>>();
                if (gettingDays == -1)
                    SavePosts(dataArray, accountId);
                else
                    SavePosts(dataArray, accountId, DateTime.Now.AddDays(-gettingDays));
            }
        }
        public void SavePosts(List<PostValues> statistics, long accountId, DateTime from)
        {
            PostStatistics post;
            foreach(PostValues value in statistics) {
                if (value.timestamp > from) {
                    post = context.PostStatistics.Where(p => p.IGMediaId == value.id && p.accountId == accountId).FirstOrDefault();
                    if (post == null) {
                        post = new PostStatistics() {
                            accountId = accountId,
                            likeCount = value.like_count,
                            postUrl = value.media_url,
                            commentsCount = value.comments_count,
                            mediaType = value.media_type,
                            IGMediaId = value.id,
                            timestamp = value.timestamp
                        };
                        context.PostStatistics.Add(post);
                        context.SaveChanges();
                    }
                    else {
                        post.likeCount = value.like_count;
                        post.commentsCount = value.comments_count;
                        context.PostStatistics.Update(post);
                        context.SaveChanges();
                    }
                    if (value.comments != null)
                        post.Comments = SaveCommentsStatistics(value.comments, post.postId);
                }
            }
        }
        public void SavePosts(List<PostValues> statistics, long accountId)
        {
            PostStatistics post;

            foreach(PostValues value in statistics) {
                post = context.PostStatistics.Where(p => p.IGMediaId == value.id && p.accountId == accountId).FirstOrDefault();
                if (post == null) {
                    post = new PostStatistics() {
                        accountId = accountId,
                        likeCount = value.like_count,
                        postUrl = value.media_url,
                        commentsCount = value.comments_count,
                        mediaType = value.media_type,
                        IGMediaId = value.id,
                        timestamp = value.timestamp
                    };
                    context.PostStatistics.Add(post);
                    context.SaveChanges();
                }
                else {
                    post.likeCount = value.like_count;
                    post.commentsCount = value.comments_count;
                    context.PostStatistics.Update(post);
                    context.SaveChanges();
                }
                if (value.comments != null)
                    SaveCommentsStatistics(value.comments, post.postId);
            }
        }
        public ICollection<CommentStatistics> SaveCommentsStatistics(JObject json, long postId)
        {
            CommentStatistics comment;
            List<CommentStatistics> comments = new List<CommentStatistics>();
            var data = handler.handle(json, "data", JTokenType.Array);
            List<CommentValue> commentValues = data.ToObject<List<CommentValue>>();
            foreach (CommentValue value in commentValues) {
                if ((comment = context.CommentStatistics.Where(c => c.mediaId == postId 
                    && c.commentIGId == value.id).FirstOrDefault()) == null) {
                        comment = new CommentStatistics();
                        comment.mediaId = postId;
                        comment.commentIGId = value.id;
                        comment.timestamp = value.timestamp;
                        comments.Add(comment);
                }
            }
            context.CommentStatistics.AddRange(comments);
            context.SaveChanges();
            return comments;
        }
        public bool CheckTokenAndIG(string accessToken, string IGId)
        {
            if (!string.IsNullOrEmpty(accessToken)) {
                if (!string.IsNullOrEmpty(IGId))
                    return true;
            }
            return false;
        }
    }
}
