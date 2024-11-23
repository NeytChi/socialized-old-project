using System;
using System.Linq;
using database.context;
using Models.Statistics;
using System.Collections.Generic;

namespace InstagramService.Statistics
{
    public class GetterStatistics
    {
        public Context context;
        
        public GetterStatistics()
        {
            context = new Context(false);
        }
        public GetterStatistics(Context context)
        {
            this.context = context;
        }
        public dynamic SelectByTime(BusinessAccount account, DateTime fromDay, DateTime to)
        {
            DateTime lastFromDay = new DateTime(fromDay.Ticks - (to.Ticks - fromDay.Ticks));
            int excessFollowers = context.Statistics.Where(s => s.accountId == account.businessId
                && s.endTime > to).Select(x => x.followerCount).Sum();
            List<DayStatistics> dailyStatistics = GetDayStatistics(account.businessId, fromDay, to);
            List<OnlineFollowers> onlineFollowers = GetOnlineFollowers(account.businessId, fromDay);
            
            List<PostStatistics> posts = GetPostStatistics(account.businessId, fromDay, to);
            List<PostStatistics> lastPeriodPosts = GetPostStatistics(account.businessId, lastFromDay, fromDay);
            
            List<StoryStatistics> stories = GetStoryStatistics(account.businessId, fromDay, to);
            
            DateTime lastFrom = fromDay.AddTicks(-(to - fromDay).Ticks);
            List<PostStatistics> last = GetPostStatistics(account.businessId, lastFrom, fromDay);
            return new {
                general = General(account, dailyStatistics, posts, excessFollowers),

                followers_grow = GrowFollowers(dailyStatistics, account.followersCount - excessFollowers),

                engagement_grow = posts.Select(post => new {
                        end_time = post.timestamp,
                        value = post.engagement
                    }).ToArray(),

                posts_statistics = ColumnGraphic(last.Count, posts.Count, 
                    lastFrom, fromDay, to),

                likes_statistics = ColumnGraphic(last.Sum(p => p.likeCount), 
                    posts.Sum(p => p.likeCount), lastFrom, fromDay, to),

                comments_statistics = ColumnGraphic(last.Sum(p => p.commentsCount), 
                    posts.Sum(p => p.commentsCount), lastFrom, fromDay, to),

                history_likes = posts.Select(post 
                    => new {
                        value = post.likeCount,
                        end_time = post.timestamp
                    }).ToArray(),

                history_comments = posts.Select(post 
                    => new {
                        value = post.commentsCount,
                        end_time = post.timestamp
                    }).ToArray(),

                post_activity = GetPostActivity(lastPeriodPosts, posts, account.followersCount - excessFollowers),

                profile_activity = GetProfileActivity(dailyStatistics),

                online_followers = onlineFollowers.Select(value => new {
                    value = value.value,
                    end_time = value.endTime 
                }).ToArray(),

                posts = posts.Select(post => new {
                    like_count = post.likeCount,
                    media_url = post.postUrl,
                    media_type = post.mediaType,
                    comments_count = post.commentsCount,
                    timestamp = post.timestamp
                }).ToArray(),

                stories = stories.Select(s => new {
                    story_url = s.storyUrl,
                    story_type = s.storyType,
                    exists = s.exists,
                    impressions = s.impressions,
                    timestamp = s.timestamp
                })
            };
        }
        public List<DayStatistics> GetDayStatistics(long accountId, DateTime fromDay, DateTime toDay)
        {
            return context.Statistics.Where(s 
                => s.accountId == accountId
                && s.endTime > fromDay 
                && s.endTime < toDay).ToList();
        }
        public List<OnlineFollowers> GetOnlineFollowers(long accountId, DateTime fromDay)
        {
            DateTime fromTime = new DateTime(fromDay.Year, fromDay.Month, fromDay.Day, 0, 0, 0);
            return context.OnlineFollowers.Where(o
                => o.accountId == accountId
                && o.endTime >= fromTime 
                && o.endTime < fromTime.AddDays(7)).OrderBy(o => o.endTime).ToList();
        }
        public List<PostStatistics> GetPostStatistics(long accountId, DateTime fromDay, DateTime toDay)
        {
            return context.PostStatistics.Where(m 
                => m.accountId == accountId
                && m.timestamp > fromDay 
                && m.timestamp < toDay).OrderBy(m => m.timestamp).ToList();
        }
        public List<StoryStatistics> GetStoryStatistics(long accountId, DateTime fromDay, DateTime toDay)
        {
            return context.StoryStatistics.Where(m 
                => m.accountId == accountId
                && m.timestamp > fromDay 
                && m.timestamp < toDay).OrderBy(m => m.timestamp).ToList();
        }
        public dynamic General(BusinessAccount account, List<DayStatistics> daily, List<PostStatistics> posts, int excessFollowers)
        {
            PostStatistics firstDayPost = posts.FirstOrDefault();
            PostStatistics lastDayPost = posts.Where(s 
                => s.postId != firstDayPost.postId).LastOrDefault();
            DayStatistics firstDay = daily.FirstOrDefault();
            DayStatistics lastDay = daily.Where(s
                => s.statisticsId != firstDay.statisticsId).LastOrDefault();
            return new {
                followers = new {
                    followers_count = account.followersCount - excessFollowers,
                    followers_grow = daily.Sum(d => d.followerCount),
                },
                media_count = account.mediaCount,
                new_medias = posts.Count,
                views = new {
                    views_count = daily.Sum(d => d.impressions),
                    coefficient = CountCoefficient(firstDay?.impressions ?? 0, lastDay?.impressions ?? 0),
                    graphic = daily.Select(d => new  { value = d.impressions, end_time = d.endTime }).ToArray()
                },
                unique_views = new {
                    views_count = daily.Sum(d => d.reach),
                    coefficient = CountCoefficient(firstDay?.reach ?? 0, lastDay?.reach ?? 0),
                    graphic = daily.Select(d => new  { value = d.reach, end_time = d.endTime }).ToArray()
                }
            };
        }
        public dynamic GetProfileActivity(List<DayStatistics> statistics)
        {
            DayStatistics firstDay = statistics.FirstOrDefault();
            DayStatistics lastDay = statistics.Where(s 
                => s.statisticsId != (firstDay.statisticsId)).LastOrDefault();
            return new {
                profile_views = new {
                    value = statistics.Sum(f => f.profileViews),
                    coefficient = CountCoefficient(firstDay?.profileViews ?? 0, lastDay?.profileViews ?? 0)
                },
                website_clicks = new {
                    value = statistics.Sum(f => f.websiteClicks),
                    coefficient = CountCoefficient(firstDay?.websiteClicks ?? 0, lastDay?.websiteClicks ?? 0)
                },
                email_contacts = new {
                    value = statistics.Sum(f => f.emailContacts),
                    coefficient = CountCoefficient(firstDay?.emailContacts ?? 0, lastDay?.emailContacts ?? 0)
                },
                message_clicks = new {
                    value = statistics.Sum(f => f.textMessageClicks),
                    coefficient = CountCoefficient(firstDay?.textMessageClicks ?? 0, lastDay?.textMessageClicks ?? 0)
                },
                phone_clicks = new {
                    value = statistics.Sum(f => f.phoneCallClicks),
                    coefficient = CountCoefficient(firstDay?.phoneCallClicks ?? 0, lastDay?.phoneCallClicks ?? 0)
                },
                direction_clicks = new {
                    value = statistics.Sum(f => f.getDirectionsClicks),
                    coefficient = CountCoefficient(firstDay?.getDirectionsClicks ?? 0, lastDay?.getDirectionsClicks ?? 0)
                }
            };
        }
        public double CountCoefficient(double AverageLastPeriod, double AverageCurrentPeriod)
        {
            double percent = AverageLastPeriod / 100, coefficient;
            
            if (percent == 0.0)
                percent = 0.01;
            coefficient = AverageCurrentPeriod / percent;
            if (coefficient > 100)
                coefficient = 100;
            return coefficient;
        }
        public Dictionary<DateTime, int> SelectPostHistory(List<PostStatistics> posts)
        {
            int value = 0;
            Dictionary<DateTime, int> postHistory = new Dictionary<DateTime, int>();
            if (posts.Count > 0) {
                PostStatistics firstPost = posts.First();
                int allDays = (firstPost.timestamp - posts.Last().timestamp).Days;
                if (allDays == 0)
                    ++allDays;
                for (int i = 0; i < allDays; i++) {
                    DateTime tempDay = firstPost.timestamp.AddDays(i);
                    foreach(PostStatistics post in posts) {
                        if (post.timestamp.Year == tempDay.Year
                        && post.timestamp.Month == tempDay.Month
                        && post.timestamp.Day == tempDay.Day) {
                            ++value;
                        }
                    }
                    postHistory.Add(tempDay, value);
                    value = 0;
                }
            }
            return postHistory;
        }
        public List<dynamic> GetPostHistory(Dictionary<DateTime, int> postHistory)
        {
            List<dynamic> data = new List<dynamic>();
            foreach(KeyValuePair<DateTime, int> history in postHistory) {
                data.Add(new 
                {
                    value = history.Value,
                    end_time = history.Key
                });
            }
            return data;
        }
        public List<dynamic> GrowFollowers(List<DayStatistics> statistics, long followersGrow)
        {
            List<dynamic> growHistory = new List<dynamic>();
            if (statistics.Count > 0) {
                statistics = statistics.OrderByDescending(s => s.endTime).ToList();
                foreach(DayStatistics day in statistics) {
                    growHistory.Add(new {
                        value = followersGrow,
                        end_time = day.endTime
                    });
                    followersGrow -= day.followerCount;
                }
            }
            return growHistory;
        }
        public dynamic GetPostActivity(List<PostStatistics> lastPeriodPosts, List<PostStatistics> posts, long followerCount)
        {
            if (followerCount == 0)
                followerCount = 1;
            return new
            {
                post_engagement = new 
                {
                    value = (double)posts.Sum(f => f.engagement) / (double)followerCount * 100,
                    coefficient = CountCoefficient( 
                        (double)lastPeriodPosts.Sum(p => p.engagement) / (double)followerCount  * 100, 
                        (double)posts.Sum(p => p.engagement)  / (double) followerCount  * 100)
                },
                post_saved = new 
                {
                    value = posts.Sum(f => f.saved),
                    coefficient = CountCoefficient(lastPeriodPosts.Sum(p => p.saved), posts.Sum(p => p.saved))
                }
            };
        }
        public dynamic ColumnGraphic(long lastValue, long value,
            DateTime lastFrom, DateTime from, DateTime to)
        {
            return new {
                last = new {
                    value = lastValue,
                    from_time = lastFrom.ToShortDateString(),
                    to_time = from.ToShortDateString()
                },
                current = new {
                    value = value,
                    from_time = from.ToShortDateString(),
                    to_time = to.ToShortDateString()
                }
            };
        }
        public sbyte GetColorOnlineFollowers(long followerCount, long value)
        {
            double percent, color;
            percent = followerCount / 100;
            if (percent == 0.0)
                percent = 0.01;
            color = value / percent;
            if (color >= 0 && color < 5)
                return 1;
            else if (color >= 5 && color < 10)
                return 2;
            else if (color >= 10 && color < 15)
                return 3;
            else if (color >= 15 && color < 20)
                return 4;
            else if (color >= 20 && color < 45)
                return 5;
            else if (color >= 45)
                return 5;
            else return 1;
        }
    }
}