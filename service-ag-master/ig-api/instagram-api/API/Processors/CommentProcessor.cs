using System;
using System.Net;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Helpers;
using System.Collections.Generic;
using InstagramApiSharp.Converters;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Converters.Json;
using InstagramApiSharp.Classes.ResponseWrappers;

namespace InstagramApiSharp.API.Processors
{
    /// <summary>
    ///     Comments api functions.
    /// </summary>
    public class CommentProcessor
    {
        private readonly HttpHelper _httpHelper;
        private readonly HttpRequestProcessor _httpRequestProcessor;
        public CommentProcessor()
        {
            _httpRequestProcessor = HttpRequestProcessor.GetInstance();
            _httpHelper = HttpHelper.GetInstance();
        }
        /// <summary>
        ///     Comment media
        /// </summary>
        /// <param name="mediaId">Media id</param>
        /// <param name="text">Comment text</param>
        public IResult<InstaComment> CommentMedia(ref Session user, string mediaId, string text)
        {
            try
            {
                var instaUri = UriCreator.GetPostCommetUri(mediaId);
                var breadcrumb = CryptoHelper.GetCommentBreadCrumbEncoded(text);
                var fields = new Dictionary<string, string>
                {
                    {"user_breadcrumb", breadcrumb},
                    {"idempotence_token", Guid.NewGuid().ToString()},
                    {"_uuid", user.device.DeviceGuid.ToString()},
                    {"_uid", user.User.LoggedInUser.Pk.ToString()},
                    {"_csrftoken", user.User.CsrfToken},
                    {"comment_text", text},
                    {"containermodule", "comments_feed_timeline"},
                    {"radio_type", "wifi-none"}
                };
                var request = _httpHelper.GetSignedRequest(HttpMethod.Post, instaUri, user.device, fields);
                HttpResponseMessage response =_httpRequestProcessor.SendAsync(request, user.httpClient);
                Task<string> json = response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    IResult<InstaComment> result = Result.UnExpectedResponse<InstaComment>(response, json.Result);
                    result.unexceptedResponse = true;
                    return result;
                }
                var commentResponse = JsonConvert.DeserializeObject<InstaCommentResponse>(json.Result, new InstaCommentDataConverter());
                var converter = ConvertersFabric.Instance.GetCommentConverter(commentResponse);
                return Result.Success(converter.Convert());
            }
            catch (HttpRequestException httpException)
            {
                IResult<InstaComment> result = Result.Fail(httpException, default(InstaComment), ResponseType.NetworkProblem);
                result.unexceptedResponse = true;
                return result;
            }
            catch (Exception exception)
            {
                IResult<InstaComment> result = Result.Fail<InstaComment>(exception);
                result.unexceptedResponse = true;
                return result;
            }
        }

        /// <summary>
        ///     Get media comments
        /// </summary>
        /// <param name="mediaId">Media id</param>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        public IResult<InstaCommentList> GetMediaComments(ref Session user, string mediaId,
        PaginationParameters paginationParameters)
        {
            try
            {
                if (paginationParameters == null)
                {
                    paginationParameters = PaginationParameters.MaxPagesToLoad(1);
                }
                var commentsUri = UriCreator.GetMediaCommentsUri(mediaId, paginationParameters.NextMaxId);
                if (!string.IsNullOrEmpty(paginationParameters.NextMinId))
                {
                    commentsUri = UriCreator.GetMediaCommentsMinIdUri(mediaId, paginationParameters.NextMinId);
                }
                var request = _httpHelper.GetDefaultRequest(HttpMethod.Get, commentsUri,ref user.device);
                var response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                var json = response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    IResult<InstaCommentList> result = Result.UnExpectedResponse<InstaCommentList>(response, json.Result);
                    result.unexceptedResponse = true;
                    return result;
                }
                var commentListResponse = JsonConvert.DeserializeObject<InstaCommentListResponse>(json.Result);
                var pagesLoaded = 1;
                InstaCommentList Convert(InstaCommentListResponse commentsResponse)
                {
                    return ConvertersFabric.Instance.GetCommentListConverter(commentsResponse).Convert();
                }

                while (commentListResponse.MoreCommentsAvailable
                       && !string.IsNullOrEmpty(commentListResponse.NextMaxId)
                       && pagesLoaded < paginationParameters.MaximumPagesToLoad ||

                       commentListResponse.MoreHeadLoadAvailable
                       && !string.IsNullOrEmpty(commentListResponse.NextMinId)
                       && pagesLoaded < paginationParameters.MaximumPagesToLoad)
                {
                    IResult<InstaCommentListResponse> nextComments;
                    if(!string.IsNullOrEmpty(commentListResponse.NextMaxId))
                        nextComments = GetCommentListWithMaxIdAsync(ref user, mediaId, commentListResponse.NextMaxId,null);
                    else 
                        nextComments = GetCommentListWithMaxIdAsync(ref user,mediaId, null, commentListResponse.NextMinId);

                    if (!nextComments.Succeeded)
                        return Result.Fail(nextComments.Info, Convert(commentListResponse));
                    commentListResponse.NextMaxId = nextComments.Value.NextMaxId;
                    commentListResponse.NextMinId = nextComments.Value.NextMinId;
                    commentListResponse.MoreCommentsAvailable = nextComments.Value.MoreCommentsAvailable;
                    commentListResponse.MoreHeadLoadAvailable = nextComments.Value.MoreHeadLoadAvailable;
                    commentListResponse.Comments.AddRange(nextComments.Value.Comments);
                    paginationParameters.NextMaxId = nextComments.Value.NextMaxId;
                    paginationParameters.NextMinId = nextComments.Value.NextMinId;
                    pagesLoaded++;
                }
                paginationParameters.NextMaxId = commentListResponse.NextMaxId;
                paginationParameters.NextMinId = commentListResponse.NextMinId;
                var converter = ConvertersFabric.Instance.GetCommentListConverter(commentListResponse);
                return Result.Success(converter.Convert());
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaCommentList), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail<InstaCommentList>(exception);
            }
        }
        /// <summary>
        ///     Like media comment
        /// </summary>
        /// <param name="commentId">Comment id</param>
        public IResult<bool> LikeComment(ref Session user, string commentId)
        {
            try
            {
                var instaUri = UriCreator.GetLikeCommentUri(commentId);
                var fields = new Dictionary<string, string>
                {
                    {"_uuid", user.device.DeviceGuid.ToString()},
                    {"_uid", user.User.LoggedInUser.Pk.ToString()},
                    {"_csrftoken", user.User.CsrfToken}
                };
                var request = _httpHelper.GetSignedRequest(HttpMethod.Post, instaUri, user.device, fields);
                var response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                var json = response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return Result.Success(true);
                }
                else
                {
                    IResult<bool> result = Result.UnExpectedResponse<bool>(response, json.Result);
                    result.unexceptedResponse = true;
                    return result;
                }
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(bool), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail(exception, false);
            }
        }
        private IResult<InstaCommentListResponse> GetCommentListWithMaxIdAsync(ref Session user, string mediaId, string nextMaxId, string nextMinId)
        {
            try
            {
                var commentsUri = UriCreator.GetMediaCommentsUri(mediaId, nextMaxId);
                if (!string.IsNullOrEmpty(nextMinId))
                    commentsUri = UriCreator.GetMediaCommentsMinIdUri(mediaId, nextMinId);

                var request = _httpHelper.GetDefaultRequest(HttpMethod.Get, commentsUri,ref user.device);
                var response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                var json = response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return Result.UnExpectedResponse<InstaCommentListResponse>(response, json.Result);
                }
                var comments = JsonConvert.DeserializeObject<InstaCommentListResponse>(json.Result);
                return Result.Success(comments);
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaCommentListResponse), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail<InstaCommentListResponse>(exception);
            }
        }
    }
}