using System;
using System.Net;
using Serilog.Core;
using System.Net.Http;
using Newtonsoft.Json;
using System.Globalization;
using System.Threading.Tasks;
using InstagramApiSharp.Enums;
using InstagramApiSharp.Helpers;
using InstagramApiSharp.Classes;
using System.Collections.Generic;
using InstagramApiSharp.Converters;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Classes.ResponseWrappers;
using Serilog;

namespace InstagramApiSharp.API.Processors
{
    public class LocationProcessor
    {
        private ILogger Logger;
        private readonly HttpHelper _httpHelper;
        private readonly HttpRequestProcessor _httpRequestProcessor;
        
        public LocationProcessor(ILogger log)
        {
            Logger = log;
            _httpRequestProcessor = HttpRequestProcessor.GetInstance();
            _httpHelper = HttpHelper.GetInstance();
        }
        /// <summary>
        ///     Get recent location media feeds.
        ///     <para>Important note: Be careful of using this function, because it's an POST request</para>
        /// </summary>
        /// <param name="locationId">Location identifier (location pk, external id, facebook id)</param>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        public IResult<InstaSectionMedia> GetRecentLocationFeeds(ref Session _user, long locationId, PaginationParameters paginationParameters)
        {
            return GetSection(ref _user, locationId, paginationParameters, InstaSectionType.Recent);
        }
        /// <summary>
        ///     Searches for specific location by provided geo-data or search query.
        /// </summary>
        /// <param name="latitude">Latitude</param>
        /// <param name="longitude">Longitude</param>
        /// <param name="query">Search query</param>
        /// <returns>
        ///     List of locations (short format)
        /// </returns>
        public IResult<InstaLocationShortList> SearchLocation(ref Session session, double latitude, double longitude, string query)
        {
            try
            {
                Uri uri = UriCreator.GetLocationSearchUri();
                Dictionary<string, string> fields = new Dictionary<string, string>
                {
                    {"_uuid", session.device.DeviceGuid.ToString()},
                    {"_uid", session.User.LoggedInUser.Pk.ToString()},
                    {"_csrftoken", session.User.CsrfToken},
                    {"latitude", latitude.ToString(CultureInfo.InvariantCulture)},
                    {"longitude", longitude.ToString(CultureInfo.InvariantCulture)},
                    {"rank_token", session.User.RankToken}
                };
                if (!string.IsNullOrEmpty(query))
                {
                    fields.Add("search_query", query);
                }
                else
                {
                    fields.Add("timestamp", DateTimeHelper.GetUnixTimestampSeconds().ToString());
                }
                if (!Uri.TryCreate(uri, fields.AsQueryString(), out var newuri))
                {
                    IResult<InstaLocationShortList> result = Result.Fail<InstaLocationShortList>
                    ("Unable to create uri for location search");
                    result.unexceptedResponse = false;
                    Logger.Error("Unable to create uri for location search; from LocationProcessor -> SearchLocation. id ->" 
                    + session.userId);
                    return result;
                }
                HttpRequestMessage request = _httpHelper.GetDefaultRequest(HttpMethod.Get, newuri, ref session.device);
                HttpResponseMessage response = _httpRequestProcessor.SendAsync(request, session.httpClient);
                Task<string> json = response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    IResult<InstaLocationShortList> result = Result.UnExpectedResponse<InstaLocationShortList>
                    (response, json.Result);
                    result.unexceptedResponse = true;
                    Logger.Error("Unexpected response from LocationProcessor -> SearchLocation. Json result ->" 
                    + json.Result);
                    return result;
                }
                InstaLocationSearchResponse locations = JsonConvert.DeserializeObject<InstaLocationSearchResponse>(json.Result);
                if (locations.Locations.Count <= 0)
                {
                    IResult<InstaLocationShortList> result = Result.Fail<InstaLocationShortList>
                    ("Can't find locations by query ->" + query + ".");
                    result.unexceptedResponse = false;
                    Logger.Warning("Can't find locations by query ->" + query);
                    return result;
                }
                IObjectConverter<InstaLocationShortList, InstaLocationSearchResponse> converter = ConvertersFabric
                .Instance.GetLocationsSearchConverter(locations);
                return Result.Success(converter.Convert());
            }
            catch (HttpRequestException httpException)
            {
                IResult<InstaLocationShortList> result = Result.Fail
                (httpException, default(InstaLocationShortList), ResponseType.NetworkProblem);
                result.unexceptedResponse = true;
                Logger.Error("Exception response from LocationProcessor -> SearchLocation. Message ->" 
                + httpException.Message);
                return result;
            }
            catch (Exception ex)
            {
                IResult<InstaLocationShortList> result = Result.Fail
                (ex, default(InstaLocationShortList), ResponseType.NetworkProblem);
                result.unexceptedResponse = true;
                Logger.Error("Exception response from LocationProcessor -> SearchLocation. Message ->" 
                + ex.Message);
                return result;
            }
        }
            private IResult<InstaSectionMedia> GetSection(ref Session session, long locationId,
        PaginationParameters paginationParameters, InstaSectionType sectionType)
        {
            try
            {
                if (paginationParameters == null)
                {
                    paginationParameters = PaginationParameters.MaxPagesToLoad(1);
                }
                InstaSectionMedia Convert(InstaSectionMediaListResponse sectionMediaListResponse)
                {
                    return ConvertersFabric.Instance.GetHashtagMediaListConverter(sectionMediaListResponse).Convert();
                }
                var mediaResponse = GetSectionMedia(ref session, sectionType, 
                locationId, paginationParameters.NextMaxId,
                paginationParameters.NextPage, paginationParameters.NextMediaIds);
                if (!mediaResponse.Succeeded)
                {
                    if (mediaResponse.Value != null)
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
                    var moreMedias = GetSectionMedia(ref session, sectionType,
                    locationId, paginationParameters.NextMaxId, 
                    mediaResponse.Value.NextPage, mediaResponse.Value.NextMediaIds);
                    if (!moreMedias.Succeeded)
                    {
                        if (mediaResponse.Value.Sections?.Count > 0)
                        {
                            return Result.Success(Convert(mediaResponse.Value));
                        }
                        else
                        {
                            return Result.Fail(moreMedias.Info, Convert(mediaResponse.Value));
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
         private IResult<InstaSectionMediaListResponse> GetSectionMedia(ref Session _user, InstaSectionType sectionType, 
            long locationId,
            string maxId = null,
            int? page = null,
            List<long> nextMediaIds = null)
        {
            try
            {
                var instaUri = UriCreator.GetLocationSectionUri(locationId.ToString());
                var data = new Dictionary<string, string>
                {
                    {"rank_token", _user.device.DeviceGuid.ToString()},
                    {"_uuid", _user.device.DeviceGuid.ToString()},
                    {"_csrftoken", _user.User.CsrfToken},
                    {"session_id", Guid.NewGuid().ToString()},
                    {"tab", sectionType.ToString().ToLower()}
                };

                if (!string.IsNullOrEmpty(maxId))
                {
                    data.Add("max_id", maxId);
                }
                if (page != null && page > 0)
                {
                    data.Add("page", page.ToString());
                }
                if (nextMediaIds?.Count > 0)
                {
                    var mediaIds = $"[{string.Join(",", nextMediaIds)}]";
                    if (sectionType == InstaSectionType.Ranked)
                    {
                        data.Add("next_media_ids", mediaIds.EncodeUri());
                    }
                    else
                    {
                        data.Add("next_media_ids", mediaIds);
                    }
                }

                var request = _httpHelper.GetDefaultRequest(HttpMethod.Post, instaUri, _user.device, data);
                var response = _httpRequestProcessor.SendAsync(request, _user.httpClient);
                var json = response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    IResult<InstaSectionMediaListResponse> result = Result.UnExpectedResponse
                    <InstaSectionMediaListResponse>(response, json.Result);
                    result.unexceptedResponse = true;
                    return result;
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