using System;
using System.IO;
using System.Net;
using System.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using InstagramApiSharp.Helpers;
using InstagramApiSharp.Classes;
using System.Collections.Generic;
using InstagramApiSharp.Converters;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Converters.Json;
using InstagramApiSharp.Classes.ResponseWrappers;
using InstagramApiSharp.Classes.Android.DeviceInfo;

namespace InstagramApiSharp.API.Processors
{
    /// <summary>
    ///     Media api functions.
    /// </summary>
    public class MediaProcessor
    {
        private readonly HttpHelper _httpHelper;
        private readonly HttpRequestProcessor _httpRequestProcessor;
        private readonly UserProcessor userProcessor;
        
        public MediaProcessor(UserProcessor userProcessor)
        {
            _httpRequestProcessor = HttpRequestProcessor.GetInstance();
            this.userProcessor = userProcessor;
            _httpHelper = HttpHelper.GetInstance();
        }
        /// <summary>
        ///     Delete a media (photo, video or album)
        /// </summary>
        /// <param name="mediaId">Media id (<see cref="InstaMedia.InstaIdentifier"/>)</param>
        /// <param name="mediaType">The type of the media</param>
        /// <returns>Return true if the media is deleted</returns>
        public IResult<bool> DeleteMedia(ref Session user, string mediaId, InstaMediaType mediaType)
        {
            try
            {
                var deleteMediaUri = UriCreator.GetDeleteMediaUri(mediaId, mediaType);

                var data = new JObject
                {
                    {"_uuid", user.device.DeviceGuid.ToString()},
                    {"_uid", user.User.LoggedInUser.Pk},
                    {"_csrftoken", user.User.CsrfToken},
                    {"media_id", mediaId}
                };

                var request = _httpHelper.GetSignedRequest(HttpMethod.Get, deleteMediaUri, user.device, data);
                var response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                var json = response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    IResult<bool> result = Result.UnExpectedResponse<bool>(response, json.Result);
                    result.unexceptedResponse = true;
                    return result;
                }
                var deletedResponse = JsonConvert.DeserializeObject<DeleteResponse>(json.Result);
                return Result.Success(deletedResponse.IsDeleted);
            }
            catch (HttpRequestException httpException)
            {
                IResult<bool> result = Result.Fail(httpException, default(bool), ResponseType.NetworkProblem);
                result.unexceptedResponse = true;
                return result;
            }
            catch (Exception exception)
            {
                IResult<bool> result = Result.Fail<bool>(exception);
                result.unexceptedResponse = true;
                return result;
            }
        }
        /// <summary>
        ///     Get users (short) who liked certain media. Normaly it return around 1000 last users.
        /// </summary>
        /// <param name="mediaId">Media id</param>
        public IResult<InstaLikersList> GetMediaLikers(ref Session session, string mediaId)
        {
            try {
                var likers = new InstaLikersList();
                var likersUri = UriCreator.GetMediaLikersUri(mediaId);
                var request = _httpHelper.GetDefaultRequest(HttpMethod.Get, likersUri,ref session.device);
                var response = _httpRequestProcessor.SendAsync(request, session.httpClient);
                var json = response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK) {
                    IResult<InstaLikersList> result = Result
                    .UnExpectedResponse<InstaLikersList>(response, json.Result);
                    result.unexceptedResponse = true;
                    return result;
                }
                var mediaLikersResponse = JsonConvert.DeserializeObject<InstaMediaLikersResponse>(json.Result);
                likers.UsersCount = mediaLikersResponse.UsersCount;
                if (mediaLikersResponse.UsersCount < 1) return Result.Success(likers);
                likers.AddRange(
                    mediaLikersResponse.Users.Select(ConvertersFabric.Instance.GetUserShortConverter)
                    .Select(converter => converter.Convert()));
                return Result.Success(likers);
            }
            catch (HttpRequestException httpException) {
                IResult<InstaLikersList> result = Result.Fail(httpException, 
                default(InstaLikersList), ResponseType.NetworkProblem);
                result.unexceptedResponse = true;
                return result;
            }
            catch (Exception exception) {
                IResult<InstaLikersList> result = Result.Fail<InstaLikersList>(exception);
                result.unexceptedResponse = true;
                return result;
            }
        }
        public IResult<bool> LikeMedia(ref Session session, string mediaId)
        {
            return LikeUnlikeArchiveUnArchiveMediaInternal(ref session, mediaId, UriCreator.GetLikeMediaUri(mediaId));
        }
        public IResult<bool> UnLikeMediaAsync(ref Session session, string mediaId)
        {
            return LikeUnlikeArchiveUnArchiveMediaInternal(ref session, mediaId, UriCreator.GetUnLikeMediaUri(mediaId));
        }
        /// <summary>
        ///     Upload album (videos and photos)
        /// </summary>
        /// <param name="progress">Progress action</param>
        /// <param name="images">Array of photos to upload</param>
        /// <param name="videos">Array of videos to upload</param>
        /// <param name="caption">Caption</param>
        /// <param name="location">Location => Optional (get it from <seealso cref="LocationProcessor.SearchLocationAsync"/></param>
        public IResult<InstaMedia> UploadAlbum(ref Session session,  
            InstaImageUpload[] images, 
            InstaVideoUpload[] videos, 
            string caption, 
            InstaLocationShort location = null)
        {
            try {
                Dictionary<string, InstaImageUpload> imagesUploadIds = new Dictionary<string, InstaImageUpload>();
                if (images?.Length > 0) {
                    foreach (var image in images) {
                        if (image.UserTags?.Count > 0) {
                            foreach (var t in image.UserTags) {
                                try {
                                    bool tried = false;
                                TryLabel:
                                    var u = userProcessor.GetUser(ref session, t.Username);
                                    if (!u.Succeeded)
                                    {
                                        if (!tried) {
                                            tried = true;
                                            goto TryLabel;
                                        }
                                    }
                                    else
                                        t.Pk = u.Value.Pk;
                                }
                                catch { }
                            }
                        }
                    }
                    foreach (var image in images) {
                        var uploadId = UploadSinglePhoto(ref session, image);
                        if (uploadId.Succeeded)
                            imagesUploadIds.Add(uploadId.Value, image);
                        else 
                            return Result.Fail<InstaMedia>(uploadId.Info.Message);
                    }
                }

                var videosDic = new Dictionary<string, InstaVideoUpload>();
                var vidIndex = 1;
                if (videos?.Length > 0) {
                    foreach (var video in videos) {
                        foreach (var t in video.UserTags) {
                            if (t.Pk <= 0) {
                                try {
                                    bool tried = false;
                                TryLabel:
                                    var u = userProcessor.GetUser(ref session, t.Username);
                                    if (!u.Succeeded) {
                                        if (!tried) {
                                            tried = true;
                                            goto TryLabel;
                                        }
                                    }
                                    else
                                        t.Pk = u.Value.Pk;
                                    
                                }
                                catch { }
                            }
                        }
                    }

                    foreach (var video in videos) {
                        var uploadId = UploadSingleVideo(ref session, video);
                        var thumb = UploadSinglePhoto(ref session, 
                            video.VideoThumbnail.ConvertToImageUpload(), uploadId.Value);
                        videosDic.Add(uploadId.Value, video);
                        vidIndex++;
                    }
                }
                return ConfigureAlbum(ref session, imagesUploadIds, videosDic, caption, location);
            }
            catch (HttpRequestException httpException) {
                return Result.Fail(httpException, default(InstaMedia), ResponseType.NetworkProblem);
            }
            catch (Exception exception) {
                return Result.Fail<InstaMedia>(exception);
            }
        }
        /// <summary>
        ///     Upload album (videos and photos) with progress
        /// </summary>
        /// <param name="progress">Progress action</param>
        /// <param name="album">Array of photos or videos to upload</param>
        /// <param name="caption">Caption</param>
        /// <param name="location">Location => Optional (get it from <seealso cref="LocationProcessor.SearchLocationAsync"/></param>
        public IResult<InstaMedia> UploadAlbum(ref Session session, InstaAlbumUpload[] album, string caption, InstaLocationShort location = null)
        {
            try
            {
                var uploadIds = new Dictionary<string, InstaAlbumUpload>();
                var index = 1;

                foreach (var al in album)
                {
                    if (al.IsImage)
                    {
                        var image = al.ImageToUpload;
                        if (image.UserTags?.Count > 0)
                        {
                            foreach (var t in image.UserTags)
                            {
                                if (t.Pk <= 0)
                                {
                                    try
                                    {
                                        bool tried = false;
                                    TryLabel:
                                        var u = userProcessor.GetUser(ref session, t.Username);
                                        if (!u.Succeeded)
                                        {
                                            if (!tried)
                                            {
                                                tried = true;
                                                goto TryLabel;
                                            }
                                        }
                                        else
                                            t.Pk = u.Value.Pk;
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                    else if (al.IsVideo)
                    {
                        var video = al.VideoToUpload;
                        if (video.UserTags?.Count > 0)
                        {
                            foreach (var t in video.UserTags)
                            {
                                if (t.Pk <= 0)
                                {
                                    try
                                    {
                                        bool tried = false;
                                    TryLabel:
                                        var u = userProcessor.GetUser(ref session, t.Username);
                                        if (!u.Succeeded)
                                        {
                                            if (!tried)
                                            {
                                                tried = true;
                                                goto TryLabel;
                                            }
                                        }
                                        else
                                            t.Pk = u.Value.Pk;
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
                foreach (var al in album)
                {
                    if (al.IsImage)
                    {
                        IResult<string> image = UploadSinglePhoto(ref session, al.ImageToUpload);
                        if (image.Succeeded)
                        {
                            uploadIds.Add(image.Value, al);
                        }
                    }
                    else if (al.IsVideo)
                    {
                        IResult<string> video = UploadSingleVideo(ref session, al.VideoToUpload);
                        if (video.Succeeded)
                        {
                            IResult<string> image = UploadSinglePhoto(ref session, 
                            al.VideoToUpload.VideoThumbnail.ConvertToImageUpload(), video.Value);
                            uploadIds.Add(video.Value, al);
                        }
                    }
                    index++;
                }
                return ConfigureAlbum(ref session, uploadIds, caption, location);
            }
            catch (HttpRequestException httpException)
            {
                IResult<InstaMedia> result = Result.Fail (httpException, 
                    default(InstaMedia), ResponseType.NetworkProblem);
                result.unexceptedResponse = true;
                return result;
            }
            catch (Exception exception)
            {
                IResult<InstaMedia> result = Result.Fail<InstaMedia>(exception);
                result.unexceptedResponse = true;
                return result;
            }
        }
        public IResult<string> UploadSinglePhoto(ref Session user, InstaImageUpload image, string uploadId = null, bool album = true)
        {
            if (string.IsNullOrEmpty(uploadId))
                uploadId = ApiRequestMessageF.GenerateUploadId();
            var photoHashCode = Path.GetFileName(image.Uri ?? $"C:\\{13.GenerateRandomString()}.jpg").GetHashCode();
            var photoEntityName = $"{uploadId}_0_{photoHashCode}";
            var photoUri = UriCreator.GetStoryUploadPhotoUri(uploadId, photoHashCode);
            var photoUploadParamsObj = new JObject
            {
                {"upload_id", uploadId},
                {"media_type", "1"},
                {"retry_context", HelperProcessor.GetRetryContext()},
                {"image_compression", "{\"lib_name\":\"moz\",\"lib_version\":\"3.1.m\",\"quality\":\"95\"}"},
                {"xsharing_user_ids", "[]"},
            };
            if (album)
                photoUploadParamsObj.Add("is_sidecar", "1");
            var photoUploadParams = JsonConvert.SerializeObject(photoUploadParamsObj);
            var imageBytes = image.ImageBytes ?? File.ReadAllBytes(image.Uri);
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.Add("Content-Transfer-Encoding", "binary");
            imageContent.Headers.Add("Content-Type", "application/octet-stream");
            HttpRequestMessage request = _httpHelper.GetDefaultRequest(HttpMethod.Post, photoUri,ref user.device);
            request.Content = imageContent;
            request.Headers.Add("X-Entity-Type", "image/jpeg");     //????
            request.Headers.Add("Offset", "0");
            request.Headers.Add("X-Instagram-Rupload-Params", photoUploadParams);
            request.Headers.Add("X-Entity-Name", photoEntityName);
            request.Headers.Add("X-Entity-Length", imageBytes.Length.ToString());
            request.Headers.Add("X_FB_PHOTO_WATERFALL_ID", Guid.NewGuid().ToString());
            var response = _httpRequestProcessor.SendAsync(request, user.httpClient);
            var json = response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                return Result.Success(uploadId);
            else
                return Result.Fail<string>("NO UPLOAD ID");
        }
        public IResult<string> UploadSingleVideo(ref Session user, InstaVideoUpload video, bool album = true)
        {
            var uploadId = ApiRequestMessageF.GenerateRandomUploadId();
            var videoHashCode = Path.GetFileName(video.Video.Uri ?? $"C:\\{13.GenerateRandomString()}.mp4").GetHashCode();
            var waterfallId = Guid.NewGuid().ToString();
            var videoEntityName = $"{uploadId}_0_{videoHashCode}";
            var videoUri = UriCreator.GetStoryUploadVideoUri(uploadId, videoHashCode);
            var retryContext = HelperProcessor.GetRetryContext();
            HttpRequestMessage request = null;
            string videoUploadParams = null;
            string json = null;

            var videoUploadParamsObj = new JObject
                {
                    {"upload_media_height", "0"},
                    {"upload_media_width", "0"},
                    {"upload_media_duration_ms", "0"},
                    {"upload_id", uploadId},
                    {"retry_context", retryContext},
                    {"media_type", "2"},
                    {"xsharing_user_ids", "[]"}
                };
            if (album)
            {
                videoUploadParamsObj.Add("is_sidecar", "1");
            }

            videoUploadParams = JsonConvert.SerializeObject(videoUploadParamsObj);
            request = _httpHelper.GetDefaultRequest(HttpMethod.Get, videoUri,ref user.device);
            request.Headers.Add("X_FB_VIDEO_WATERFALL_ID", waterfallId);
            request.Headers.Add("X-Instagram-Rupload-Params", videoUploadParams);
            var response = _httpRequestProcessor.SendAsync(request, user.httpClient);
            json = response.Content.ReadAsStringAsync().Result;
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return Result.UnExpectedResponse<string>(response, json);
            }

            var videoBytes = video.Video.VideoBytes ?? File.ReadAllBytes(video.Video.Uri);

            var videoContent = new ByteArrayContent(videoBytes);
            request = _httpHelper.GetDefaultRequest(HttpMethod.Post, videoUri,ref user.device);
            request.Content = videoContent;
            var vidExt = Path.GetExtension(video.Video.Uri ?? $"C:\\{13.GenerateRandomString()}.mp4").Replace(".", "").ToLower();
            if (vidExt == "mov")
            {
                request.Headers.Add("X-Entity-Type", "video/quicktime");
            }
            else
            {
                request.Headers.Add("X-Entity-Type", "video/mp4");
            }
            request.Headers.Add("Offset", "0");
            request.Headers.Add("X-Instagram-Rupload-Params", videoUploadParams);
            request.Headers.Add("X-Entity-Name", videoEntityName);
            request.Headers.Add("X-Entity-Length", videoBytes.Length.ToString());
            request.Headers.Add("X_FB_VIDEO_WATERFALL_ID", waterfallId);
            response = _httpRequestProcessor.SendAsync(request, user.httpClient);
            json = response.Content.ReadAsStringAsync().Result;
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return Result.UnExpectedResponse<string>(response, json);
            }
            return Result.Success(uploadId);
        }
        private IResult<InstaMedia> ConfigureAlbum(ref Session session, Dictionary<string, InstaAlbumUpload> album, string caption, InstaLocationShort location)
        {
            try
            {
                var instaUri = UriCreator.GetMediaAlbumConfigureUri();
                var clientSidecarId = ApiRequestMessageF.GenerateUploadId();
                var childrenArray = new JArray();

                foreach (var al in album)
                {
                    if (al.Value.IsImage)
                    {
                        childrenArray.Add(GetImageConfigure(ref session, al.Key, al.Value.ImageToUpload));
                    }
                    else if (al.Value.IsVideo)
                    {
                        childrenArray.Add(GetVideoConfigure(ref session, al.Key, al.Value.VideoToUpload));
                    }
                }

                var data = new JObject
                {
                    {"_uuid", session.device.DeviceGuid.ToString()},
                    {"_uid", session.User.LoggedInUser.Pk.ToString()},
                    {"_csrftoken", session.User.CsrfToken},
                    {"caption", caption},
                    {"client_sidecar_id", clientSidecarId},
                    {"upload_id", clientSidecarId},
                    {"timezone_offset", InstaApiConstants.TIMEZONE_OFFSET.ToString()},
                    {"source_type", "4"},
                    {"device_id", session.device.DeviceId},
                    {"creation_logger_session_id", Guid.NewGuid().ToString()},
                    {
                        "device", new JObject
                        {
                            {"manufacturer", session.device.HardwareManufacturer},
                            {"model", session.device.DeviceModelIdentifier},
                            {"android_release", session.device.AndroidVer.VersionNumber},
                            {"android_version", session.device.AndroidVer.APILevel}
                        }
                    },
                    {"children_metadata", childrenArray},
                };
                if (location != null)
                {
                    data.Add("location", location.GetJson());
                    data.Add("date_time_digitalized", DateTime.Now.ToString("yyyy:dd:MM+h:mm:ss"));
                }
                var request = _httpHelper.GetSignedRequest(HttpMethod.Post, instaUri, session.device, data);
                var response = _httpRequestProcessor.SendAsync(request, session.httpClient);
                var json = response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    IResult<InstaMedia> result = Result.UnExpectedResponse<InstaMedia>(response, json.Result);
                    result.unexceptedResponse = true;
                    return result;
                }
                var mediaResponse = JsonConvert.DeserializeObject<InstaMediaAlbumResponse>(json.Result);
                var converter = ConvertersFabric.Instance.GetSingleMediaFromAlbumConverter(mediaResponse);
                var obj = converter.Convert();
                if (obj.Caption == null && !string.IsNullOrEmpty(caption))
                {
                    var editedMedia = EditMedia(ref session, obj.InstaIdentifier, caption, location);
                    if (editedMedia.Succeeded)
                    {
                        return Result.Success(editedMedia.Value);
                    }
                }
                return Result.Success(obj);
            }
            catch (HttpRequestException httpException)
            {
                IResult<InstaMedia> result = Result.Fail(httpException, 
                default(InstaMedia), ResponseType.NetworkProblem);
                result.unexceptedResponse = true;
                return result;
            }
            catch (Exception exception)
            {
                IResult<InstaMedia> result = Result.Fail<InstaMedia>(exception);
                result.unexceptedResponse = true;
                return result;
            }
        }
        /// <summary>
        ///     Upload video with progress [Supports user tags]
        /// </summary>
        /// <param name="progress">Progress action</param>
        /// <param name="video">Video and thumbnail to upload</param>
        /// <param name="caption">Caption</param>
        /// <param name="location">Location => Optional (get it from <seealso cref="LocationProcessor.SearchLocationAsync"/></param>
        public IResult<InstaMedia> UploadVideo(ref Session user, InstaVideoUpload video, string caption, InstaLocationShort location = null)
        {
            try
            {
                if (video?.UserTags?.Count > 0)
                {
                    foreach (var t in video.UserTags)
                    {
                        if (t.Pk <= 0)
                        {
                            try
                            {
                                bool tried = false;
                                TryLabel:
                                var u = userProcessor.GetUser(ref user, t.Username);
                                if (!u.Succeeded)
                                {
                                    if (!tried)
                                    {
                                        tried = true;
                                        goto TryLabel;
                                    }
                                }
                                else
                                {
                                    t.Pk = u.Value.Pk;
                                }
                            }
                            catch { }
                        }
                    }
                }
                var uploadVideo = UploadSingleVideo(ref user, video, false);
                if (!uploadVideo.Succeeded)
                {
                    return Result.Fail<InstaMedia>(uploadVideo.Info.Message);
                }
                var uploadPhoto = UploadSinglePhoto(ref user, video.VideoThumbnail.ConvertToImageUpload(), uploadVideo.Value, false);
                if (uploadPhoto.Succeeded)
                {
                    return ConfigureVideo(ref user, video, uploadVideo.Value, caption, location);
                }
                return Result.Fail<InstaMedia>(uploadPhoto.Value);
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaMedia), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail<InstaMedia>(exception);
            }
        }
        /// <summary>
        ///     Get media by its id asynchronously
        /// </summary>
        /// <param name="mediaId">Media id (<see cref="InstaMedia.InstaIdentifier>"/>)</param>
        /// <returns>
        ///     <see cref="InstaMedia" />
        /// </returns>
        public IResult<InstaMedia> GetMediaById(ref Session user, string mediaId)
        {
            try
            {
                var mediaUri = UriCreator.GetMediaUri(mediaId);
                var request = _httpHelper.GetDefaultRequest(HttpMethod.Get, mediaUri, ref user.device);
                var response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                var json = response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return Result.UnExpectedResponse<InstaMedia>(response, json.Result);
                }
                var mediaResponse = JsonConvert.DeserializeObject<InstaMediaListResponse>(json.Result, new InstaMediaListDataConverter());
                if (mediaResponse.Medias?.Count > 1)
                {
                    var errorMessage = $"Got wrong media count for request with media id={mediaId}";
                    return Result.Fail<InstaMedia>(errorMessage);
                }

                var converter =
                    ConvertersFabric.Instance.GetSingleMediaConverter(mediaResponse.Medias.FirstOrDefault());
                return Result.Success(converter.Convert());
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaMedia), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail<InstaMedia>(exception);
            }
        }
        /// <summary>
        ///     Edit the caption/location of the media (photo/video/album)
        /// </summary>
        /// <param name="mediaId">The media ID</param>
        /// <param name="caption">The new caption</param>
        /// <param name="location">Location => Optional (get it from <seealso cref="LocationProcessor.SearchLocationAsync"/></param>
        /// <param name="userTags">User tags => Optional</param>
        /// <returns>Return true if everything is ok</returns>
        public IResult<InstaMedia> EditMedia(ref Session user, string mediaId, string caption, InstaLocationShort location = null, InstaUserTagUpload[] userTags = null)
        {
            try
            {
                var editMediaUri = UriCreator.GetEditMediaUri(mediaId);

                var currentMedia = GetMediaById(ref user, mediaId);

                var data = new JObject
                {
                    {"_uuid", user.device.DeviceGuid.ToString()},
                    {"_uid", user.User.LoggedInUser.Pk},
                    {"_csrftoken", user.User.CsrfToken},
                    {"caption_text", caption ?? string.Empty}
                };
                if (location != null)
                {
                    data.Add("location", location.GetJson());
                }

                var removeArr = new JArray();
                if (currentMedia.Succeeded)
                {
                    if (currentMedia.Value?.UserTags?.Count > 0)
                    {
                        foreach (var usert in currentMedia.Value.UserTags)
                        {
                            removeArr.Add(usert.User.Pk.ToString());
                        }
                    }
                }
                if (userTags?.Length > 0)
                {
                    var tagArr = new JArray();

                    foreach (var tag in userTags)
                    {
                        try
                        {
                            bool tried = false;
                        TryLabel:
                            var u = userProcessor.GetUser(ref user, tag.Username);
                            if (!u.Succeeded)
                            {
                                if (!tried)
                                {
                                    tried = true;
                                    goto TryLabel;
                                }
                            }
                            else
                            {
                                var position = new JArray(tag.X, tag.Y);
                                var singleTag = new JObject
                                {
                                    {"user_id", u.Value.Pk},
                                    {"position", position}
                                };
                                tagArr.Add(singleTag);
                            }

                        }
                        catch { }
                    }

                    //_instaApi.SetRequestDelay(currentDelay);
                    var root = new JObject
                    {
                        {"in",  tagArr}
                    };
                    if (removeArr.Any())
                        root.Add("removed", removeArr);

                    data.Add("usertags", root.ToString(Formatting.None));
                }
                else
                {
                    if (removeArr.Any())
                    {
                        var root = new JObject
                        {
                            {"removed", removeArr}
                        };
                        data.Add("usertags", root.ToString(Formatting.None));
                    }
                }
                var request = _httpHelper.GetSignedRequest(HttpMethod.Post, editMediaUri, user.device, data);
                var response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                var json = response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var mediaResponse = JsonConvert.DeserializeObject<InstaMediaItemResponse>(json.Result,
                        new InstaMediaDataConverter());
                    var converter = ConvertersFabric.Instance.GetSingleMediaConverter(mediaResponse);
                    return Result.Success(converter.Convert());
                }
                var error = JsonConvert.DeserializeObject<BadStatusResponse>(json.Result);
                IResult<InstaMedia> result = Result.Fail(error.Message, (InstaMedia)null);
                return result;
            }
            catch (HttpRequestException httpException)
            {
                IResult<InstaMedia> result = Result.Fail(httpException, default(InstaMedia), ResponseType.NetworkProblem);
                result.unexceptedResponse = true;
                return result;
            }
            catch (Exception exception)
            {
                IResult<InstaMedia> result = Result.Fail<InstaMedia>(exception);
                result.unexceptedResponse = true;
                return result;
            }
        }
        private IResult<InstaMedia> ConfigureAlbum(ref Session user, Dictionary<string, InstaImageUpload> imagesUploadIds, Dictionary<string, InstaVideoUpload> videos, string caption, InstaLocationShort location)
        {
            try
            {
                var instaUri = UriCreator.GetMediaAlbumConfigureUri();
                var clientSidecarId = ApiRequestMessageF.GenerateUploadId();
                var childrenArray = new JArray();
                if (imagesUploadIds != null && imagesUploadIds.Any())
                {
                    foreach (var img in imagesUploadIds)
                    {
                        childrenArray.Add(GetImageConfigure(ref user, img.Key, img.Value));
                    }
                }
                if (videos != null && videos.Any())
                {
                    foreach (var id in videos)
                    {
                        childrenArray.Add(GetVideoConfigure(ref user, id.Key, id.Value));
                    }
                }
                var data = new JObject
                {
                    {"_uuid", user.device.DeviceGuid.ToString()},
                    {"_uid", user.User.LoggedInUser.Pk},
                    {"_csrftoken", user.User.CsrfToken},
                    {"caption", caption},
                    {"client_sidecar_id", clientSidecarId},
                    {"upload_id", clientSidecarId},
                    {
                        "device", new JObject
                        {
                            {"manufacturer", user.device.HardwareManufacturer},
                            {"model", user.device.DeviceModelIdentifier},
                            {"android_release", user.device.AndroidVer.VersionNumber},
                            {"android_version", user.device.AndroidVer.APILevel}
                        }
                    },
                    {"children_metadata", childrenArray},
                };
                if (location != null)
                {
                    data.Add("location", location.GetJson());
                    data.Add("date_time_digitalized", DateTime.Now.ToString("yyyy:dd:MM+h:mm:ss"));
                }
                var request = _httpHelper.GetSignedRequest(HttpMethod.Post, instaUri, user.device, data);
                var response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                var json = response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return Result.UnExpectedResponse<InstaMedia>(response, json.Result);
                }
                var mediaResponse = JsonConvert.DeserializeObject<InstaMediaAlbumResponse>(json.Result);
                var converter = ConvertersFabric.Instance.GetSingleMediaFromAlbumConverter(mediaResponse);
                var obj = converter.Convert();
                if (obj.Caption == null && !string.IsNullOrEmpty(caption))
                {
                    var editedMedia = EditMedia(ref user, obj.InstaIdentifier, caption, location);
                    if (editedMedia.Succeeded)
                    {
                        return Result.Success(editedMedia.Value);
                    }
                }
                return Result.Success(obj);
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaMedia), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail<InstaMedia>(exception);
            }
        }

        private IResult<InstaMedia> ConfigureVideo(ref Session user, InstaVideoUpload video, string uploadId, string caption, InstaLocationShort location)
        {
            try
            {
                var instaUri = UriCreator.GetMediaConfigureUri(true);
                var data = new JObject
                {
                    {"caption", caption ?? string.Empty},
                    {"upload_id", uploadId},
                    {"source_type", "4"},
                    {"camera_position", "unknown"},
                    {"creation_logger_session_id", Guid.NewGuid().ToString()},
                    {"timezone_offset", InstaApiConstants.TIMEZONE_OFFSET.ToString()},
                    {"date_time_original", DateTime.Now.ToString("yyyy-dd-MMTh:mm:ss-0fffZ")},
                    {
                        "extra", new JObject
                        {
                            {"source_width", 0},
                            {"source_height", 0}
                        }
                    },
                    {
                        "clips", new JArray{
                            new JObject
                            {
                                {"length", 0},
                                {"creation_date", DateTime.Now.ToString("yyyy-dd-MMTh:mm:ss-0fff")},
                                {"source_type", "3"},
                                {"camera_position", "back"}
                            }
                        }
                    },
                    {"poster_frame_index", 0},
                    {"audio_muted", false},
                    {"filter_type", "0"},
                    {"video_result", ""},
                    {"_csrftoken", user.User.CsrfToken},
                    {"_uuid", user.device.DeviceGuid.ToString()},
                    {"_uid", user.User.LoggedInUser.UserName}
                };
                if (location != null)
                {
                    data.Add("location", location.GetJson());
                    data.Add("date_time_digitalized", DateTime.Now.ToString("yyyy:dd:MM+h:mm:ss"));
                }
                if (video.UserTags?.Count > 0)
                {
                    var tagArr = new JArray();
                    foreach (var tag in video.UserTags)
                    {
                        if (tag.Pk != -1)
                        {
                            var position = new JArray(0.0, 0.0);
                            var singleTag = new JObject
                            {
                                {"user_id", tag.Pk},
                                {"position", position}
                            };
                            tagArr.Add(singleTag);
                        }
                    }

                    var root = new JObject
                    {
                        {"in",  tagArr}
                    };
                    data.Add("usertags", root.ToString(Formatting.None));
                }
                var request = _httpHelper.GetSignedRequest(HttpMethod.Post, UriCreator.GetMediaUploadFinishUri(), user.device, data);
                request.Headers.Host = "i.instagram.com";
                var response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                var json = response.Content.ReadAsStringAsync();

                request = _httpHelper.GetSignedRequest(HttpMethod.Post, instaUri, user.device, data);
                request.Headers.Host = "i.instagram.com";
                response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                json = response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    return Result.UnExpectedResponse<InstaMedia>(response, json.Result);
                }
                var mediaResponse = JsonConvert.DeserializeObject<InstaMediaItemResponse>(json.Result, new InstaMediaDataConverter());
                var converter = ConvertersFabric.Instance.GetSingleMediaConverter(mediaResponse);
                var obj = converter.Convert();
                if (obj.Caption == null && !string.IsNullOrEmpty(caption))
                {
                    var editedMedia = EditMedia(ref user, obj.InstaIdentifier, caption, location);
                    if (editedMedia.Succeeded)
                    {
                        return Result.Success(editedMedia.Value);
                    }
                }
                return Result.Success(obj);
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaMedia), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail<InstaMedia>(exception);
            }
        }
        private IResult<bool> LikeUnlikeArchiveUnArchiveMediaInternal(ref Session user, string mediaId, Uri instaUri)
        {
            try
            {
                var fields = new Dictionary<string, string>
                {
                    {"_uuid", user.device.DeviceGuid.ToString()},
                    {"_uid", user.User.LoggedInUser.Pk.ToString()},
                    {"_csrftoken", user.User.CsrfToken},
                    {"media_id", mediaId},
                    {"radio_type", "wifi-none"}
                };
                var request = _httpHelper.GetSignedRequest(HttpMethod.Post, instaUri, user.device, fields);
                var response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                var json = response.Content.ReadAsStringAsync();
                if ( response.StatusCode == HttpStatusCode.OK)
                {
                    return Result.Success(true);
                }
                else
                {
                    IResult<bool> result= Result.UnExpectedResponse<bool>(response, json.Result);
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
                return Result.Fail<bool>(exception);
            }
        }
        JObject GetImageConfigure(ref Session user, string uploadId, InstaImageUpload image)
        {
            var imgData = new JObject
            {
                {"timezone_offset", InstaApiConstants.TIMEZONE_OFFSET.ToString()},
                {"source_type", "4"},
                {"upload_id", uploadId},
                {"caption", ""},
                {
                    "extra", JsonConvert.SerializeObject(new JObject
                    {
                        {"source_width", 0},
                        {"source_height", 0}
                    })
                },
                {
                    "device", JsonConvert.SerializeObject(new JObject{
                        {"manufacturer", user.device.HardwareManufacturer},
                        {"model", user.device.DeviceModelIdentifier},
                        {"android_release", user.device.AndroidVer.VersionNumber},
                        {"android_version", user.device.AndroidVer.APILevel}
                    })
                }
            };
            if (image.UserTags?.Count > 0)
            {
                var tagArr = new JArray();
                foreach (var tag in image.UserTags)
                {
                    if (tag.Pk != -1)
                    {
                        var position = new JArray(tag.X, tag.Y);
                        var singleTag = new JObject
                                    {
                                        {"user_id", tag.Pk},
                                        {"position", position}
                                    };
                        tagArr.Add(singleTag);
                    }
                }

                var root = new JObject
                {
                    {"in",  tagArr}
                };
                imgData.Add("usertags", root.ToString(Formatting.None));
            }
            return imgData;
        }

        JObject GetVideoConfigure(ref Session user, string uploadId, InstaVideoUpload video)
        {
            var vidData = new JObject
            {
                {"timezone_offset", InstaApiConstants.TIMEZONE_OFFSET.ToString()},
                {"caption", ""},
                {"upload_id", uploadId},
                {"date_time_original", DateTime.Now.ToString("yyyy-dd-MMTh:mm:ss-0fffZ")},
                {"source_type", "4"},
                {
                    "extra", JsonConvert.SerializeObject(new JObject
                    {
                        {"source_width", 0},
                        {"source_height", 0}
                    })
                },
                {
                    "clips", JsonConvert.SerializeObject(new JArray{
                        new JObject
                        {
                            {"length", video.Video.Length},
                            {"source_type", "4"},
                        }
                    })
                },
                {
                    "device", JsonConvert.SerializeObject(new JObject{
                        {"manufacturer", user.device.HardwareManufacturer},
                        {"model", user.device.DeviceModelIdentifier},
                        {"android_release", user.device.AndroidVer.VersionNumber},
                        {"android_version", user.device.AndroidVer.APILevel}
                    })
                },
                {"length", video.Video.Length.ToString()},
                {"poster_frame_index", "0"},
                {"audio_muted", "false"},
                {"filter_type", "0"},
                {"video_result", ""},
            };
            if (video.UserTags?.Count > 0)
            {
                var tagArr = new JArray();
                foreach (var tag in video.UserTags)
                {
                    if (tag.Pk != -1)
                    {
                        var position = new JArray(0.0, 0.0);
                        var singleTag = new JObject
                        {
                            {"user_id", tag.Pk},
                            {"position", position}
                        };
                        tagArr.Add(singleTag);
                    }
                }

                var root = new JObject
                {
                    {"in",  tagArr}
                };
                vidData.Add("usertags", root.ToString(Formatting.None));
            }
            return vidData;
        }
    }
}