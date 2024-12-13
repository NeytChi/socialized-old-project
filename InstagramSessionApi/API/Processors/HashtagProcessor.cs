using System;
using System.Net;
using System.Linq;
using Serilog.Core;
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
using InstagramApiSharp.Classes.Models.Hashtags;
using InstagramApiSharp.Classes.ResponseWrappers;
using Serilog;

namespace InstagramApiSharp.API.Processors
{
    /// <summary>
    ///     Hashtag api functions.
    /// </summary>
    public class HashtagProcessor
    {
        private readonly HttpHelper _httpHelper;
        private readonly HttpRequestProcessor _httpRequestProcessor;
        private ILogger log;
        public HashtagProcessor(ILogger log)
        {
            this.log = log;
            _httpRequestProcessor = HttpRequestProcessor.GetInstance();
            _httpHelper = HttpHelper.GetInstance();
        }
        /// <summary>
        ///     Gets the hashtag information by user tagname.
        /// </summary>
        /// <param name="tagname">Tagname</param>
        /// <returns>Hashtag information</returns>
        public IResult<InstaHashtag> GetHashtagInfo(string tagname, ref Session session)
        {
            try
            {
                Uri userUri = UriCreator.GetTagInfoUri(tagname);
                HttpRequestMessage request = _httpHelper.GetDefaultRequest(HttpMethod.Get, userUri,ref session.device);
                HttpResponseMessage response = _httpRequestProcessor.SendAsync(request, session.httpClient);
                Task<string> json = response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    IResult<InstaHashtag> result = Result.UnExpectedResponse<InstaHashtag>(response, json.Result);
                    result.unexceptedResponse = true;
                    log.Error("Unexpected response from HashtagProcessor -> GetHashtagInfoAsync. Json result ->" +
                    json.Result, session.userId);
                    return result;
                }
                InstaHashtagResponse tagInfoResponse = JsonConvert.DeserializeObject<InstaHashtagResponse>(json.Result);
                if (tagInfoResponse.Id == 0)
                {
                    IResult<InstaHashtag> result = Result.Fail<InstaHashtag>("Can't find hashtag by tagname ->" + tagname + ".");
                    result.unexceptedResponse = false;
                    log.Warning("Can't find hashtag by tagname ->" + tagname, session.userId);
                    return result;
                }
                InstaHashtag tagInfo = ConvertersFabric.Instance.GetHashTagConverter(tagInfoResponse).Convert();
                return Result.Success(tagInfo);
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaHashtag), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail<InstaHashtag>(exception);
            }
        }

        /// <summary>
        /// Get recent hashtag media list.
        /// </summary>
        /// <param name="tagname">Tag name</param>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        public IResult<InstaSectionMedia> GetRecentHashtagMediaList(string tagname, PaginationParameters paginationParameters, ref Session _user)
        {
            try
            {
                if (paginationParameters == null)
                {
                    paginationParameters = PaginationParameters.MaxPagesToLoad(1);
                }
                InstaSectionMedia Convert(InstaSectionMediaListResponse hashtagMediaListResponse)
                {
                    return ConvertersFabric.Instance.GetHashtagMediaListConverter(hashtagMediaListResponse).Convert();
                }
                var mediaResponse = GetHashtagSection(tagname, ref _user,
                     Guid.NewGuid().ToString(),
                    paginationParameters.NextMaxId, true);
                if (mediaResponse == null)
                {
                    if (mediaResponse != null)
                    {
                        Result.Fail(mediaResponse.Info, Convert(mediaResponse.Value));
                    }
                    else
                    {
                        Result.Fail(mediaResponse.Info, default(InstaSectionMedia));
                    }
                }
                paginationParameters.NextMediaIds = mediaResponse.Value.NextMediaIds;
                paginationParameters.NextPage = mediaResponse.Value.NextPage;
                paginationParameters.NextMaxId = mediaResponse.Value.NextMaxId;
                while (mediaResponse.Value.MoreAvailable
                && !string.IsNullOrEmpty(paginationParameters.NextMaxId)
                && paginationParameters.PagesLoaded < paginationParameters.MaximumPagesToLoad)
                {
                    IResult<InstaSectionMediaListResponse> moreMedias = GetHashtagSection(tagname,ref _user, Guid.NewGuid().ToString(),
                    paginationParameters.NextMaxId, true);
                    if (moreMedias != null)
                    {
                        if (mediaResponse.Value.Sections != null && mediaResponse.Value.Sections.Any())
                        {
                            return Result.Success(Convert(mediaResponse.Value));
                        }
                        else
                        {
                            return Result.Fail(moreMedias.Value.ToString(), Convert(mediaResponse.Value));
                        }
                    }
                    mediaResponse.Value.MoreAvailable = moreMedias.Value.MoreAvailable;
                    mediaResponse.Value.NextMaxId = paginationParameters.NextMaxId = moreMedias.Value.NextMaxId;
                    mediaResponse.Value.AutoLoadMoreEnabled = moreMedias.Value.AutoLoadMoreEnabled;
                    mediaResponse.Value.NextMediaIds = paginationParameters.NextMediaIds = moreMedias.Value.NextMediaIds;
                    mediaResponse.Value.NextPage = paginationParameters.NextPage = moreMedias.Value.NextPage;
                    mediaResponse.Value.Sections.AddRange(moreMedias.Value.Sections);
                    paginationParameters.PagesLoaded++;
                }
                return Result.Success(ConvertersFabric.Instance.GetHashtagMediaListConverter(mediaResponse.Value).Convert());
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaSectionMedia), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail<InstaSectionMedia>(exception);
            }
        }
        private IResult<InstaSectionMediaListResponse> GetHashtagSection(string tagname, ref Session _user,
            string rankToken = null,
            string maxId = null, bool recent = false)
        {
            try
            {
                var instaUri = UriCreator.GetHashtagSectionUri(tagname);

                var data = new Dictionary<string, string>
                {
                    {"_csrftoken", _user.User.CsrfToken},
                    {"_uuid", _user.device.DeviceGuid.ToString()},
                    {"include_persistent", !recent ? "true" : "false"},
                    {"rank_token", rankToken},
                };
                if (recent)
                    data.Add("tab", "recent");
                else
                    data.Add("supported_tabs", new JArray("top", "recent", "places", "discover").ToString());

                if (!string.IsNullOrEmpty(maxId))
                    data.Add("max_id", maxId);
                var request =
                    _httpHelper.GetDefaultRequest(HttpMethod.Post, instaUri, _user.device, data);
                var response = _httpRequestProcessor.SendAsync(request, _user.httpClient);
                var json = response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return Result.UnExpectedResponse<InstaSectionMediaListResponse>(response, json.Result);
                }
                var obj = JsonConvert.DeserializeObject<InstaSectionMediaListResponse>(json.Result);

                return Result.Success(obj);
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaSectionMediaListResponse), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail<InstaSectionMediaListResponse>(exception);
            }
        }
    }
}