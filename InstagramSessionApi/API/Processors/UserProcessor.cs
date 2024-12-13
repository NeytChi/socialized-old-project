using System;
using System.Net;
using System.Linq;
using Serilog.Core;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Helpers;
using System.Collections.Generic;
using InstagramApiSharp.Converters;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Converters.Json;
using InstagramApiSharp.Classes.ResponseWrappers;
using InstagramApiSharp.Classes.Android.DeviceInfo;
using Serilog;

namespace InstagramApiSharp.API.Processors
{
    public class UserProcessor
    {
        private ILogger Logger;
        private readonly HttpHelper _httpHelper;
        private readonly HttpRequestProcessor _httpRequestProcessor;
        
        public UserProcessor(ILogger log)
        {
            Logger = log;
            _httpRequestProcessor =  HttpRequestProcessor.GetInstance();
            _httpHelper = HttpHelper.GetInstance();
        }
        /// <summary>
        ///     Follow user
        /// </summary>
        /// <param name="userId">User id</param>
        public IResult<InstaFriendshipFullStatus> FollowUser(ref Session session, long userId)
        {
            return FollowUnfollowUserInternal(ref session, userId, UriCreator.GetFollowUserUri(userId));
        }
        /// <summary>
        ///     Get friendship status for given user id.
        /// </summary>
        /// <param name="userId">User identifier (PK)</param>
        /// <returns>
        ///     <see cref="InstaStoryFriendshipStatus" />
        /// </returns>
        public IResult<InstaStoryFriendshipStatus> GetFriendshipStatus(ref Session session, long user_pk)
        {
            try
            {
                var userUri = UriCreator.GetUserFriendshipUri(user_pk);
                var request = _httpHelper.GetDefaultRequest(HttpMethod.Get, userUri,ref session.device);
                var response = _httpRequestProcessor.SendAsync(request, session.httpClient);
                var json = response.Content.ReadAsStringAsync().Result;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    IResult<InstaStoryFriendshipStatus> result = Result.UnExpectedResponse<InstaStoryFriendshipStatus>(response, json);
                    result.unexceptedResponse = true;
                    return result;
                }
                var friendshipStatusResponse = JsonConvert.DeserializeObject<InstaStoryFriendshipStatusResponse>(json);
                var converter = ConvertersFabric.Instance.GetStoryFriendshipStatusConverter(friendshipStatusResponse);
                return Result.Success(converter.Convert());
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaStoryFriendshipStatus), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail<InstaStoryFriendshipStatus>(exception);
            }
        }
        /// <summary>
        ///     Get full user info (user info, feeds, stories, broadcasts)
        /// </summary>
        /// <param name="userId">User id (pk)</param>
        public IResult<InstaFullUserInfo> GetFullUserInfo(ref Session session, long userId)
        {
            try
            {
                var instaUri = UriCreator.GetFullUserInfoUri(userId);
                var request = _httpHelper.GetDefaultRequest(HttpMethod.Get, instaUri,ref session.device);
                var response =  _httpRequestProcessor.SendAsync(request, session.httpClient);
                var json = response.Content.ReadAsStringAsync().Result;
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaFullUserInfo>(response, json);
                var fullUserInfoResponse = JsonConvert.DeserializeObject<InstaFullUserInfoResponse>(json);
                var converter = ConvertersFabric.Instance.GetFullUserInfoConverter(fullUserInfoResponse);
                return Result.Success(converter.Convert());
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaFullUserInfo), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail<InstaFullUserInfo>(exception);
            }
        }
        /// <summary>
        /// Get user info by user name.
        /// </summary>
        /// <param name="session">Session</param>
        /// <param name="username">Instagram user ID without '@' char.</param>
        /// <returns>
        ///     <see cref="InstaUser" />
        /// </returns>
        public IResult<InstaUser> GetUser(ref Session session, string username)
        {
            try
            {
                var userUri = UriCreator.GetUserUri(username);
                var request = _httpHelper.GetDefaultRequest(HttpMethod.Get, userUri,ref session.device);
                request.Properties.Add(new KeyValuePair<string, object>(InstaApiConstants.HEADER_TIMEZONE, InstaApiConstants.TIMEZONE_OFFSET.ToString()));
                request.Properties.Add(new KeyValuePair<string, object>(InstaApiConstants.HEADER_COUNT, "1"));
                request.Properties.Add(new KeyValuePair<string, object>(InstaApiConstants.HEADER_RANK_TOKEN, session.User.RankToken));
                HttpResponseMessage response = _httpRequestProcessor.SendAsync(request, session.httpClient);
                Task<string> json = response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    IResult<InstaUser> result = Result.UnExpectedResponse<InstaUser>(response, json.Result);
                    result.unexceptedResponse = true;
                    Logger.Error("Unexpected response from UserProcessor -> GetUser. Json result ->" + json.Result);
                    return result;
                }
                InstaSearchUserResponse userInfo = JsonConvert.DeserializeObject<InstaSearchUserResponse>(json.Result);
                InstaUserResponse user = userInfo.Users?.FirstOrDefault(u => u.UserName.ToLower() == username.ToLower().Replace("@", ""));
                if (user == null)
                {
                    IResult<InstaUser> result = Result.Fail<InstaUser>("Can't find user by user ID ->" + username + ".");
                    result.unexceptedResponse = false;
                    Logger.Warning("Can't find user by user ID ->" + username + ".");
                    return result;
                }
                if (user.Pk < 1)
                {
                    IResult<InstaUser> result = Result.Fail<InstaUser>("User ID is incorrect ->" + username + ".");
                    result.unexceptedResponse = false;
                    Logger.Warning("User ID is incorrect ->" + username + ".");
                    return result;
                }
                var converter = ConvertersFabric.Instance.GetUserConverter(user);
                return Result.Success(converter.Convert());
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaUser), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail<InstaUser>(exception);
            }
        }
        /// <summary>
        ///     Get followers list by username asynchronously
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        /// <param name="searchQuery">Search string to locate specific followers</param>
        /// <returns>
        ///     <see cref="InstaUserShortList" />
        /// </returns>
        public IResult<InstaUserShortList> GetUserFollowers(ref Session _user, string username,
            PaginationParameters paginationParameters, string searchQuery, bool mutualsfirst = false)
        {
            try
            {
                var user = GetUser(ref _user, username);
                if (user.Succeeded)
                {
                    if (user.Value.FriendshipStatus.IsPrivate && user.Value.UserName != _user.User.LoggedInUser.UserName && !user.Value.FriendshipStatus.Following)
                    {
                        return Result.Fail("You must be a follower of private accounts to be able to get user's followers", default(InstaUserShortList));
                    }
                    return GetUserFollowersByIdAsync(ref _user, user.Value.Pk, paginationParameters, searchQuery, mutualsfirst);
                }
                else
                {
                    return Result.Fail(user.Info, default(InstaUserShortList));
                }
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaUserShortList), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail(exception, default(InstaUserShortList));
            }
        }

        /// <summary>
        ///     Get followers list by user id(pk) asynchronously
        /// </summary>
        /// <param name="userId">User id (pk)</param>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        /// <param name="searchQuery">Search string to locate specific followers</param>
        /// <returns>
        ///     <see cref="InstaUserShortList" />
        /// </returns>
        public IResult<InstaUserShortList> GetUserFollowersByIdAsync(ref Session session, long userId,
        PaginationParameters paginationParameters, string searchQuery, bool mutualsfirst = false)
        {
            var followers = new InstaUserShortList();
            try
            {
                if (paginationParameters == null)
                {
                    paginationParameters = PaginationParameters.MaxPagesToLoad(1);
                }
                var userFollowersUri = UriCreator.GetUserFollowersUri(userId, 
                session.User.RankToken, searchQuery, mutualsfirst, 
                paginationParameters.NextMaxId);
                var followersResponse = GetUserListByUriAsync(session.httpClient, 
                session.device, userFollowersUri);
                if (!followersResponse.Succeeded)
                {
                    IResult<InstaUserShortList> result = Result.Fail(followersResponse.Info, (InstaUserShortList)null);
                    result.unexceptedResponse = true;
                    return result;
                }
                followers.AddRange( followersResponse.Value.Items?
                .Select(ConvertersFabric.Instance.GetUserShortConverter)
                .Select(converter => converter.Convert()));
                paginationParameters.NextMaxId = followers.NextMaxId = followersResponse.Value.NextMaxId;
                int pagesLoaded = 1;
                while (!string.IsNullOrEmpty(followersResponse.Value.NextMaxId)
                && pagesLoaded < paginationParameters.MaximumPagesToLoad)
                {
                    var nextFollowersUri = UriCreator.GetUserFollowersUri(userId, session.User.RankToken, searchQuery, mutualsfirst,
                    followersResponse.Value.NextMaxId);
                    followersResponse = GetUserListByUriAsync(session.httpClient, session.device, nextFollowersUri);
                    if (!followersResponse.Succeeded)
                    {
                        return Result.Fail(followersResponse.Info, followers);
                    }
                    followers.AddRange(followersResponse.Value.Items?.Select(ConvertersFabric.Instance.GetUserShortConverter)
                    .Select(converter => converter.Convert()));
                    pagesLoaded++;
                    paginationParameters.PagesLoaded = pagesLoaded;
                    paginationParameters.NextMaxId = followers.NextMaxId = followersResponse.Value.NextMaxId;
                }
                return Result.Success(followers);
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, followers, ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail(exception, followers);
            }
        }

        /// <summary>
        ///     Get following list by username asynchronously
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        /// <param name="searchQuery">Search string to locate specific followings</param>
        /// <returns>
        ///     <see cref="InstaUserShortList" />
        /// </returns>
        public IResult<InstaUserShortList> GetUserFollowing(ref Session _user, string username,
            PaginationParameters paginationParameters, string searchQuery)
        {
            try
            {
                var user = GetUser(ref _user, username);
                if (user.Succeeded)
                {
                    if (user.Value.FriendshipStatus.IsPrivate && user.Value.UserName != _user.User.LoggedInUser.UserName && !user.Value.FriendshipStatus.Following)
                        return Result.Fail("You must be a follower of private accounts to be able to get user's followings", default(InstaUserShortList));

                    return GetUserFollowingByIdAsync(ref _user, user.Value.Pk,ref paginationParameters, searchQuery);
                }
                else
                    return Result.Fail(user.Info, default(InstaUserShortList));
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaUserShortList), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail(exception, default(InstaUserShortList));
            }
        }
        /// <summary>
        ///     Get following list by user id(pk) asynchronously
        /// </summary>
        /// <param name="userId">User id(pk)</param>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        /// <param name="searchQuery">Search string to locate specific followings</param>
        /// <returns>
        ///     <see cref="InstaUserShortList" />
        /// </returns>
        public IResult<InstaUserShortList> GetUserFollowingByIdAsync(ref Session _user, long userId, 
        ref PaginationParameters paginationParameters, string searchQuery)
        {
            InstaUserShortList following = new InstaUserShortList();
            try
            {
                if (paginationParameters == null)
                {
                    paginationParameters = PaginationParameters.MaxPagesToLoad(1);
                }
                var uri = UriCreator.GetUserFollowingUri(userId, _user.User.RankToken, searchQuery,
                paginationParameters.NextMaxId);
                var userListResponse = GetUserListByUriAsync(_user.httpClient, _user.device, uri);
                if (!userListResponse.Succeeded)
                {
                    IResult<InstaUserShortList> result = Result.Fail(userListResponse.Info, (InstaUserShortList)null);
                    result.unexceptedResponse = true;
                    return result;
                }
                following.AddRange(
                userListResponse.Value.Items.Select(ConvertersFabric.Instance.GetUserShortConverter)
                .Select(converter => converter.Convert()));
                paginationParameters.NextMaxId = following.NextMaxId = userListResponse.Value.NextMaxId;
                int pages = 1;
                while (!string.IsNullOrEmpty(following.NextMaxId)
                && pages < paginationParameters.MaximumPagesToLoad)
                {
                    var nextUri = UriCreator.GetUserFollowingUri(userId, _user.User.RankToken, searchQuery,
                    userListResponse.Value.NextMaxId);
                    userListResponse = GetUserListByUriAsync(_user.httpClient, _user.device, nextUri);
                    if (!userListResponse.Succeeded)
                    {
                        IResult<InstaUserShortList> result = Result.Fail(userListResponse.Info, following);
                        result.unexceptedResponse = true;
                        return result;
                    }
                    following.AddRange(
                    userListResponse.Value.Items.Select(ConvertersFabric.Instance.GetUserShortConverter)
                    .Select(converter => converter.Convert()));
                    pages++;
                    paginationParameters.PagesLoaded = pages;
                    paginationParameters.NextMaxId = following.NextMaxId = userListResponse.Value.NextMaxId;
                }
                return Result.Success(following);
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, following, ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail(exception, following);
            }
        }

        /// <summary>
        ///     Gets the user extended information (followers count, following count, bio, etc) by username.
        /// </summary>
        /// <param name="username">Username, like "instagram"</param>
        /// <returns></returns>
        public IResult<InstaUserInfo> GetUserInfoByUsernameAsync(ref Session user, string username)
        {
            try
            {
                var userUri = UriCreator.GetUserInfoByUsernameUri(username);
                return GetUserInfoAsync(ref user.httpClient,ref user.device, userUri);
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaUserInfo), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail<InstaUserInfo>(exception);
            }
        }
        /// <summary>
        ///     Get all user media by username asynchronously
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        /// <returns>
        ///     <see cref="InstaMediaList" />
        /// </returns>
        public IResult<InstaMediaList> GetUserMedia(ref Session _user, string username,
        PaginationParameters paginationParameters)
        {
            IResult<InstaUserShort> user = GetUser(ref _user, username);
            if (!user.Succeeded)
            {
                return Result.Fail<InstaMediaList>("Unable to get user for load media");
            }
            return GetUserMediaById(ref _user, user.Value.Pk, paginationParameters);
        }

        /// <summary>
        ///     Get all user media by user id (pk) asynchronously
        /// </summary>
        /// <param name="userId">User id (pk)</param>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        /// <returns>
        ///     <see cref="InstaMediaList" />
        /// </returns>
        public IResult<InstaMediaList> GetUserMediaById(ref Session _user, long userId, PaginationParameters paginationParameters)
        {
            InstaMediaList mediaList = new InstaMediaList();
            try
            {
                if (paginationParameters == null)
                {
                    paginationParameters = PaginationParameters.MaxPagesToLoad(1);
                }
                InstaMediaList Convert(InstaMediaListResponse mediaListResponse)
                {
                    return ConvertersFabric.Instance.GetMediaListConverter(mediaListResponse).Convert();
                }
                IResult<InstaMediaListResponse> mediaResult = GetUserMedia(ref _user, userId, paginationParameters);
                if (!mediaResult.Succeeded)
                {
                    if (mediaResult.Value != null)
                    {
                        return Result.Fail(mediaResult.Info, Convert(mediaResult.Value));
                    }
                    else
                    {
                        return Result.Fail(mediaResult.Info, default(InstaMediaList));
                    }
                }
                IResult<InstaMediaListResponse> mediaResponse = mediaResult;

                mediaList = Convert(mediaResponse.Value);
                mediaList.NextMaxId = paginationParameters.NextMaxId = mediaResponse.Value.NextMaxId;
                paginationParameters.PagesLoaded++;

                while (mediaResponse.Value.MoreAvailable
                       && !string.IsNullOrEmpty(paginationParameters.NextMaxId)
                       && paginationParameters.PagesLoaded < paginationParameters.MaximumPagesToLoad)
                {

                    var nextMedia = GetUserMedia(ref _user, userId, paginationParameters);
                    if (!nextMedia.Succeeded)
                        return Result.Fail(nextMedia.Info, mediaList);
                    mediaResponse.Value.MoreAvailable = nextMedia.Value.MoreAvailable;
                    mediaResponse.Value.ResultsCount += nextMedia.Value.ResultsCount;
                    mediaList.NextMaxId = mediaResponse.Value.NextMaxId = paginationParameters.NextMaxId = nextMedia.Value.NextMaxId;
                    mediaList.AddRange(Convert(nextMedia.Value));
                    paginationParameters.PagesLoaded++;
                }

                mediaList.Pages = paginationParameters.PagesLoaded;
                mediaList.PageSize = mediaResponse.Value.ResultsCount;
                return Result.Success(mediaList);
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaMediaList), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail(exception, mediaList);
            }
        }
        /// <summary>
        ///     Stop follow user
        /// </summary>
        /// <param name="userId">User id</param>
        public IResult<InstaFriendshipFullStatus> UnFollowUser(ref Session _user, long userId)
        {
            return FollowUnfollowUserInternal(ref _user, userId, UriCreator.GetUnFollowUserUri(userId));
        }
         /// <summary>
        ///     Block user
        /// </summary>
        /// <param name="userId">User id</param>
        public IResult<InstaFriendshipFullStatus> BlockUser(ref Session user, long userId)
        {
            return BlockUnblockUserInternal(ref user, userId, UriCreator.GetBlockUserUri(userId));
        }
        /// <summary>
        ///     Stop block user
        /// </summary>
        /// <param name="userId">User id</param>
        public IResult<InstaFriendshipFullStatus> UnBlockUser(ref Session user, long userId)
        {
            return BlockUnblockUserInternal(ref user, userId, UriCreator.GetUnBlockUserUri(userId));
        }
        private IResult<InstaFriendshipFullStatus> BlockUnblockUserInternal(ref Session user, long userId, Uri instaUri)
        {
            try
            {
                var fields = new Dictionary<string, string>
                {
                    {"_uuid", user.device.DeviceGuid.ToString()},
                    {"_uid", user.User.LoggedInUser.Pk.ToString()},
                    {"_csrftoken", user.User.CsrfToken},
                    {"user_id", userId.ToString()},
                    {"radio_type", "wifi-none"}
                };
                var request = _httpHelper.GetSignedRequest(HttpMethod.Post, instaUri, user.device, fields);
                var response = _httpRequestProcessor.SendAsync(request,user.httpClient);
                var json = response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK || string.IsNullOrEmpty(json.Result))
                {
                    IResult<InstaFriendshipFullStatus> result = Result.UnExpectedResponse<InstaFriendshipFullStatus>(response, json.Result);
                    result.unexceptedResponse = true;
                    return result;
                }
                var friendshipStatus = JsonConvert.DeserializeObject<InstaFriendshipFullStatusContainerResponse>(json.Result);
                var converter = ConvertersFabric.Instance.GetFriendshipFullStatusConverter(friendshipStatus.FriendshipStatus);
                return Result.Success(converter.Convert());
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaFriendshipFullStatus), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail<InstaFriendshipFullStatus>(exception);
            }
        }
        private IResult<InstaFriendshipFullStatus> FollowUnfollowUserInternal(ref Session _user, long userId, Uri instaUri)
        {
            try
            {
                var fields = new Dictionary<string, string>
                {
                    {"_uuid", _user.device.DeviceGuid.ToString()},
                    {"_uid", _user.User.LoggedInUser.Pk.ToString()},
                    {"_csrftoken",_user.User.CsrfToken},
                    {"user_id", userId.ToString()},
                    {"radio_type", "wifi-none"}
                };
                var request =
                    _httpHelper.GetSignedRequest(HttpMethod.Post, instaUri, _user.device, fields);
                var response = _httpRequestProcessor.SendAsync(request, _user.httpClient);
                var json = response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK || string.IsNullOrEmpty(json.Result))
                {
                    IResult<InstaFriendshipFullStatus> result = Result.UnExpectedResponse<InstaFriendshipFullStatus>(response, json.Result);
                    result.unexceptedResponse = true;
                    return result;
                }
                var friendshipStatus = JsonConvert.DeserializeObject<InstaFriendshipFullStatusContainerResponse>(json.Result);
                var converter = ConvertersFabric.Instance.GetFriendshipFullStatusConverter(friendshipStatus.FriendshipStatus);
                return Result.Success(converter.Convert());
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaFriendshipFullStatus), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail<InstaFriendshipFullStatus>(exception);
            }
        }
        private IResult<InstaUserInfo> GetUserInfoAsync(ref HttpClient _httpClient,ref AndroidDevice _device , Uri userUri)
        {
            try
            {
                var request = _httpHelper.GetDefaultRequest(HttpMethod.Get, userUri,ref _device);
                var response = _httpRequestProcessor.SendAsync(request, _httpClient);
                var json = response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return Result.UnExpectedResponse<InstaUserInfo>(response, json.Result);
                }
                var userInfo = JsonConvert.DeserializeObject<InstaUserInfoContainerResponse>(json.Result);
                var converter = ConvertersFabric.Instance.GetUserInfoConverter(userInfo);
                return Result.Success(converter.Convert());
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaUserInfo), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail<InstaUserInfo>(exception);
            }
        }
        private IResult<InstaUserListShortResponse> GetUserListByUriAsync(HttpClient _httpClient, AndroidDevice _device, Uri uri)
        {
            try
            {
                var request = _httpHelper.GetDefaultRequest(HttpMethod.Get, uri,ref _device);
                var response = _httpRequestProcessor.SendAsync(request, _httpClient);
                var json = response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return Result.UnExpectedResponse<InstaUserListShortResponse>(response, json.Result);
                }
                var instaUserListResponse = JsonConvert.DeserializeObject<InstaUserListShortResponse>(json.Result);
                if (instaUserListResponse.IsOk())
                {
                    return Result.Success(instaUserListResponse);
                }
                return Result.UnExpectedResponse<InstaUserListShortResponse>(response, json.Result);
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaUserListShortResponse), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail<InstaUserListShortResponse>(exception);
            }
        }
        private IResult<InstaMediaListResponse> GetUserMedia(ref Session _user, long userId,
        PaginationParameters paginationParameters)
        {
            try
            {
                var instaUri = UriCreator.GetUserMediaListUri(userId, paginationParameters.NextMaxId);
                var request = _httpHelper.GetDefaultRequest(HttpMethod.Get, instaUri,ref _user.device);
                var response = _httpRequestProcessor.SendAsync(request, _user.httpClient);
                var json = response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    IResult<InstaMediaListResponse> result = Result.UnExpectedResponse<InstaMediaListResponse>(
                    response, json.Result);
                    result.unexceptedResponse = true;
                    return result;
                }
                var mediaResponse = JsonConvert.DeserializeObject<InstaMediaListResponse>(json.Result,
                new InstaMediaListDataConverter());
                return Result.Success(mediaResponse);
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaMediaListResponse), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail(exception, default(InstaMediaListResponse));
            }
        }
    }
}