using System;
using System.IO;
using System.Net;
using System.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using InstagramApiSharp.Enums;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Helpers;
using System.Collections.Generic;
using InstagramApiSharp.Converters;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Converters.Json;
using InstagramApiSharp.Classes.Android.DeviceInfo;
using InstagramApiSharp.Classes.ResponseWrappers;

namespace InstagramApiSharp.API.Processors
{
    /// <summary>
    ///     Helper processor for other processors
    /// </summary>
    public class HelperProcessor
    {
        #region Properties and constructor
        private readonly HttpRequestProcessor _httpRequestProcessor;
        private UserProcessor userProcessor;
        private MediaProcessor mediaProcessor;
        private readonly HttpHelper _httpHelper;

        public HelperProcessor(UserProcessor userProcessor, MediaProcessor mediaProcessor)
        {
            this._httpRequestProcessor = HttpRequestProcessor.GetInstance();
            this.userProcessor = userProcessor;
            this.mediaProcessor = mediaProcessor;
            _httpHelper = HttpHelper.GetInstance();
        }
        #endregion Properties and constructor

        /// <summary>
        ///     Send video story, direct video, disappearing video
        /// </summary>
        /// <param name="isDirectVideo">Direct video</param>
        /// <param name="isDisappearingVideo">Disappearing video</param>
        public IResult<bool> SendVideoAsync(ref Session _user, Action<InstaUploaderProgress> progress, bool isDirectVideo, bool isDisappearingVideo,string caption, 
            InstaViewMode viewMode, InstaStoryType storyType,  string recipients, string threadId, InstaVideoUpload video, Uri uri = null, InstaStoryUploadOptions uploadOptions = null)
        {
            try
            {
                var uploadId = ApiRequestMessageF.GenerateRandomUploadId();
                var videoHashCode = Path.GetFileName(video.Video.Uri ?? $"C:\\{13.GenerateRandomString()}.mp4").GetHashCode();
                var waterfallId = Guid.NewGuid().ToString();
                var videoEntityName = $"{uploadId}_0_{videoHashCode}";
                var videoUri = UriCreator.GetStoryUploadVideoUri(uploadId, videoHashCode);
                var retryContext = GetRetryContext();
                HttpRequestMessage request = null;
                HttpResponseMessage response = null;
                string videoUploadParams = null;
                Task<string> json = null;
                var videoUploadParamsObj = new JObject();
                if (isDirectVideo)
                {
                    videoUploadParamsObj = new JObject
                    {
                        {"upload_media_height", "0"},
                        {"direct_v2", "1"},
                        {"upload_media_width", "0"},
                        {"upload_media_duration_ms", "0"},
                        {"upload_id", uploadId},
                        {"retry_context", retryContext},
                        {"media_type", "2"}
                    };
                    videoUploadParams = JsonConvert.SerializeObject(videoUploadParamsObj);
                    request = _httpHelper.GetDefaultRequest(HttpMethod.Get, videoUri,ref _user.device);
                    request.Headers.Add("X_FB_VIDEO_WATERFALL_ID", waterfallId);
                    request.Headers.Add("X-Instagram-Rupload-Params", videoUploadParams);
                    response = _httpRequestProcessor.SendAsync(request, _user.httpClient);
                    json = response.Content.ReadAsStringAsync();

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return Result.UnExpectedResponse<bool>(response, json.Result);
                    }
                }
                else
                {
                    videoUploadParamsObj = new JObject
                    {
                        {"_csrftoken", _user.User.CsrfToken},
                        {"_uid", _user.User.LoggedInUser.Pk},
                        {"_uuid", _user.device.DeviceGuid.ToString()},
                        {"media_info", new JObject
                            {
                                    {"capture_mode", "normal"},
                                    {"media_type", 2},
                                    {"caption", caption ?? string.Empty},
                                    {"mentions", new JArray()},
                                    {"hashtags", new JArray()},
                                    {"locations", new JArray()},
                                    {"stickers", new JArray()},
                            }
                        }
                    };
                    request = _httpHelper.GetSignedRequest(HttpMethod.Post, UriCreator.GetStoryMediaInfoUploadUri(), _user.device, videoUploadParamsObj);
                    response = _httpRequestProcessor.SendAsync(request, _user.httpClient);
                    json = response.Content.ReadAsStringAsync();


                    videoUploadParamsObj = new JObject
                    {
                        {"upload_media_height", "0"},
                        {"upload_media_width", "0"},
                        {"upload_media_duration_ms", "0"},
                        {"upload_id", uploadId},
                        {"retry_context", "{\"num_step_auto_retry\":0,\"num_reupload\":0,\"num_step_manual_retry\":0}"},
                        {"media_type", "2"}
                    };
                    if (isDisappearingVideo)
                    {
                        videoUploadParamsObj.Add("for_direct_story", "1");
                    }
                    else
                    {
                        switch (storyType)
                        {
                            case InstaStoryType.SelfStory:
                            default:
                                videoUploadParamsObj.Add("for_album", "1");
                                break;
                            case InstaStoryType.Direct:
                                videoUploadParamsObj.Add("for_direct_story", "1");
                                break;
                            case InstaStoryType.Both:
                                videoUploadParamsObj.Add("for_album", "1");
                                videoUploadParamsObj.Add("for_direct_story", "1");
                                break;
                        }
                    }
                    videoUploadParams = JsonConvert.SerializeObject(videoUploadParamsObj);
                    request = _httpHelper.GetDefaultRequest(HttpMethod.Get, videoUri,ref _user.device);
                    request.Headers.Add("X_FB_VIDEO_WATERFALL_ID", waterfallId);
                    request.Headers.Add("X-Instagram-Rupload-Params", videoUploadParams);
                    response = _httpRequestProcessor.SendAsync(request, _user.httpClient);
                    json = response.Content.ReadAsStringAsync();


                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return Result.UnExpectedResponse<bool>(response, json.Result);
                    }
                }

                // video part
                byte[] videoBytes;
                if (video.Video.VideoBytes == null)
                    videoBytes = File.ReadAllBytes(video.Video.Uri);
                else
                    videoBytes = video.Video.VideoBytes;

                var videoContent = new ByteArrayContent(videoBytes);
                //var progressContent = new ProgressableStreamContent(videoContent, 4096, progress)
                //{
                //    UploaderProgress = upProgress
                //};
                request = _httpHelper.GetDefaultRequest(HttpMethod.Post, videoUri,ref _user.device);
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
                response = _httpRequestProcessor.SendAsync(request, _user.httpClient);
                json = response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return Result.UnExpectedResponse<bool>(response, json.Result);
                }
                if (!isDirectVideo)
                {
                    var photoHashCode = Path.GetFileName(video.VideoThumbnail.Uri ?? $"C:\\{13.GenerateRandomString()}.jpg").GetHashCode();
                    var photoEntityName = $"{uploadId}_0_{photoHashCode}";
                    var photoUri = UriCreator.GetStoryUploadPhotoUri(uploadId, photoHashCode);
                    var photoUploadParamsObj = new JObject
                    {
                        {"retry_context", retryContext},
                        {"media_type", "2"},
                        {"upload_id", uploadId},
                        {"image_compression", "{\"lib_name\":\"moz\",\"lib_version\":\"3.1.m\",\"quality\":\"95\"}"},
                    };

                    var photoUploadParams = JsonConvert.SerializeObject(photoUploadParamsObj);
                    byte[] imageBytes;
                    if (video.VideoThumbnail.ImageBytes == null)
                    {
                        imageBytes = File.ReadAllBytes(video.VideoThumbnail.Uri);
                    }
                    else
                    {
                        imageBytes = video.VideoThumbnail.ImageBytes;
                    }
                    var imageContent = new ByteArrayContent(imageBytes);
                    imageContent.Headers.Add("Content-Transfer-Encoding", "binary");
                    imageContent.Headers.Add("Content-Type", "application/octet-stream");
                    request = _httpHelper.GetDefaultRequest(HttpMethod.Post, photoUri,ref _user.device);
                    request.Content = imageContent;
                    request.Headers.Add("X-Entity-Type", "image/jpeg");
                    request.Headers.Add("Offset", "0");
                    request.Headers.Add("X-Instagram-Rupload-Params", photoUploadParams);
                    request.Headers.Add("X-Entity-Name", photoEntityName);
                    request.Headers.Add("X-Entity-Length", imageBytes.Length.ToString());
                    request.Headers.Add("X_FB_PHOTO_WATERFALL_ID", waterfallId);
                    response = _httpRequestProcessor.SendAsync(request, _user.httpClient);
                    json = response.Content.ReadAsStringAsync();
                }
                return ConfigureVideo(ref _user, uploadId, isDirectVideo, isDisappearingVideo,caption, viewMode,storyType, recipients, threadId, uri, uploadOptions);
            }
            catch (Exception exception)
            {
                return Result.Fail<bool>(exception);
            }
        }

        private IResult<bool> ConfigureVideo(ref Session session, string uploadId, bool isDirectVideo, bool isDisappearingVideo, string caption,
            InstaViewMode viewMode, InstaStoryType storyType, string recipients, string threadId, Uri uri, InstaStoryUploadOptions uploadOptions = null)
        {
            try
            {
                var instaUri = UriCreator.GetDirectConfigVideoUri();
                var retryContext = GetRetryContext();
                var clientContext = Guid.NewGuid().ToString();
                
                if (isDirectVideo)
                {
                    var data = new Dictionary<string, string>
                    {
                         {"action","send_item"},
                         {"client_context",clientContext},
                         {"_csrftoken",session.User.CsrfToken},
                         {"video_result",""},
                         {"_uuid", session.device.DeviceGuid.ToString()},
                         {"upload_id",uploadId}
                    };
                    if (!string.IsNullOrEmpty(recipients))
                    {
                        data.Add("recipient_users", $"[[{recipients}]]");
                    }
                    else
                    {
                        data.Add("thread_ids", $"[{threadId}]");
                    }
                    instaUri = UriCreator.GetDirectConfigVideoUri();
                    var request = _httpHelper.GetDefaultRequest(HttpMethod.Post, instaUri, ref session.device);
                    request.Content = new FormUrlEncodedContent(data);
                    request.Headers.Add("retry_context", retryContext);
                    var response = _httpRequestProcessor.SendAsync(request, session.httpClient);
                    var json = response.Content.ReadAsStringAsync();

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return Result.UnExpectedResponse<bool>(response, json.Result);
                    }
                    var obj = JsonConvert.DeserializeObject<InstaDefault>(json.Result);
                    return obj.Status.ToLower() == "ok" ? Result.Success(true) : Result.UnExpectedResponse<bool>(response, json.Result);
                }
                else
                {
                    var rnd = new Random();
                    var data = new JObject
                    {
                        {"filter_type", "0"},
                        {"timezone_offset", "16200"},
                        {"_csrftoken", session.User.CsrfToken},
                        {"client_shared_at", (DateTime.UtcNow.ToUnixTime() - rnd.Next(25,55)).ToString()},
                        {"story_media_creation_date", (DateTime.UtcNow.ToUnixTime() - rnd.Next(50,70)).ToString()},
                        {"media_folder", "Camera"},
                        {"source_type", "4"},
                        {"video_result", ""},
                        {"_uid", session.User.LoggedInUser.Pk.ToString()},
                        {"_uuid", session.device.DeviceGuid.ToString()},
                        {"caption", caption ?? string.Empty},
                        {"date_time_original", DateTime.Now.ToString("yyyy-dd-MMTh:mm:ss-0fffZ")},
                        {"capture_type", "normal"},
                        {"mas_opt_in", "NOT_PROMPTED"},
                        {"upload_id", uploadId},
                        {"client_timestamp", DateTime.UtcNow.ToUnixTime()},
                        {
                            "device", new JObject{
                                {"manufacturer", session.device.HardwareManufacturer},
                                {"model", session.device.DeviceModelIdentifier},
                                {"android_release", session.device.AndroidVer.VersionNumber},
                                {"android_version", session.device.AndroidVer.APILevel}
                            }
                        },
                        {"length", 0},
                        {
                            "extra", new JObject
                            {
                                {"source_width", 0},
                                {"source_height", 0}
                            }
                        },
                        {"audio_muted", false},
                        {"poster_frame_index", 0},
                    };
                    if (isDisappearingVideo)
                    {
                        data.Add("view_mode", viewMode.ToString().ToLower());
                        data.Add("configure_mode", "2");
                        data.Add("recipient_users", "[]");
                        data.Add("thread_ids", $"[{threadId}]");
                    }
                    else
                    {
                        switch (storyType)
                        {
                            case InstaStoryType.SelfStory:
                            default:
                                data.Add("configure_mode", "1");
                                break;
                            case InstaStoryType.Direct:
                                data.Add("configure_mode", "2");
                                data.Add("view_mode", "replayable");
                                data.Add("recipient_users", "[]");
                                data.Add("thread_ids", $"[{threadId}]");
                                break;
                            case InstaStoryType.Both:
                                data.Add("configure_mode", "3");
                                data.Add("view_mode", "replayable");
                                data.Add("recipient_users", "[]");
                                data.Add("thread_ids", $"[{threadId}]");
                                break;
                        }

                        if (uri != null)
                        {
                            var webUri = new JArray
                            {
                                new JObject
                                {
                                    {"webUri", uri.ToString()}
                                }
                            };
                            var storyCta = new JArray
                            {
                                new JObject
                                {
                                    {"links",  webUri}
                                }
                            };
                            data.Add("story_cta", storyCta.ToString(Formatting.None));
                        }

                        if (uploadOptions != null)
                        {
                            if (uploadOptions.Hashtags?.Count > 0)
                            {
                                var hashtagArr = new JArray();
                                foreach (var item in uploadOptions.Hashtags)
                                    hashtagArr.Add(item.ConvertToJson());

                                data.Add("story_hashtags", hashtagArr.ToString(Formatting.None));
                            }

                            if (uploadOptions.Locations?.Count > 0)
                            {
                                var locationArr = new JArray();
                                foreach (var item in uploadOptions.Locations)
                                    locationArr.Add(item.ConvertToJson());

                                data.Add("story_locations", locationArr.ToString(Formatting.None));
                            }
                            if (uploadOptions.Slider != null)
                            {
                                var sliderArr = new JArray
                                {
                                    uploadOptions.Slider.ConvertToJson()
                                };

                                data.Add("story_sliders", sliderArr.ToString(Formatting.None));
                                if (uploadOptions.Slider.IsSticker)
                                    data.Add("story_sticker_ids", $"emoji_slider_{uploadOptions.Slider.Emoji}");
                            }
                            else
                            {
                                if (uploadOptions.Polls?.Count > 0)
                                {
                                    var pollArr = new JArray();
                                    foreach (var item in uploadOptions.Polls)
                                        pollArr.Add(item.ConvertToJson());

                                    data.Add("story_polls", pollArr.ToString(Formatting.None));
                                }
                                if (uploadOptions.Questions?.Count > 0)
                                {
                                    var questionArr = new JArray();
                                    foreach (var item in uploadOptions.Questions)
                                        questionArr.Add(item.ConvertToJson());

                                    data.Add("story_questions", questionArr.ToString(Formatting.None));
                                }
                            }
                            if (uploadOptions.Countdown != null)
                            {
                                var countdownArr = new JArray
                                {
                                    uploadOptions.Countdown.ConvertToJson()
                                };

                                data.Add("story_countdowns", countdownArr.ToString(Formatting.None));
                                data.Add("story_sticker_ids", "countdown_sticker_time");
                            }
                        }
                    }
                    instaUri = UriCreator.GetVideoStoryConfigureUri(true);
                    var request = _httpHelper.GetSignedRequest(HttpMethod.Post, instaUri, session.device, data);
                 
                    request.Headers.Add("retry_context", retryContext);
                    var response = _httpRequestProcessor.SendAsync(request, session.httpClient);
                    var json = response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var mediaResponse = JsonConvert.DeserializeObject<InstaDefault>(json.Result);
                        return mediaResponse.Status.ToLower() == "ok" ? 
                        Result.Success(true) : 
                        Result.UnExpectedResponse<bool>(response, json.Result);
                    }
                    return Result.UnExpectedResponse<bool>(response, json.Result);
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



        public IResult<bool> SendPhotoAsync(ref Session session, bool isDirectPhoto, bool isDisappearingPhoto, string caption, InstaViewMode viewMode, InstaStoryType storyType, string recipients, string threadId, InstaImage image)
        {
            try
            {
                var uploadId = ApiRequestMessageF.GenerateRandomUploadId();
                var photoHashCode = Path.GetFileName(image.Uri ?? $"C:\\{13.GenerateRandomString()}.jpg").GetHashCode();
                var photoEntityName = $"{uploadId}_0_{photoHashCode}";
                var photoUri = UriCreator.GetStoryUploadPhotoUri(uploadId, photoHashCode);
                var waterfallId = Guid.NewGuid().ToString();
                var retryContext = GetRetryContext();
                HttpRequestMessage request = null;
                HttpResponseMessage response = null;
                Task<string> json = null;
                var photoUploadParamsObj = new JObject
                {
                    {"retry_context", retryContext},
                    {"media_type", "1"},
                    {"upload_id", uploadId},
                    {"image_compression", "{\"lib_name\":\"moz\",\"lib_version\":\"3.1.m\",\"quality\":\"95\"}"},
                };
                var uploadParamsObj = new JObject
                {
                    {"_csrftoken", session.User.CsrfToken},
                    {"_uid", session.User.LoggedInUser.Pk},
                    {"_uuid", session.device.DeviceGuid.ToString()},
                    {"media_info", new JObject
                        {
                                {"capture_mode", "normal"},
                                {"media_type", 1},
                                {"caption", caption ?? string.Empty},
                                {"mentions", new JArray()},
                                {"hashtags", new JArray()},
                                {"locations", new JArray()},
                                {"stickers", new JArray()},
                        }
                    }
                };
                request = _httpHelper.GetSignedRequest(HttpMethod.Post, UriCreator
                .GetStoryMediaInfoUploadUri(), session.device, uploadParamsObj);
                response = _httpRequestProcessor.SendAsync(request, session.httpClient);
                json = response.Content.ReadAsStringAsync();
                var uploadParams = JsonConvert.SerializeObject(photoUploadParamsObj);
                request = _httpHelper.GetDefaultRequest(HttpMethod.Get, photoUri,ref session.device);
                request.Headers.Add("X_FB_PHOTO_WATERFALL_ID", waterfallId);
                request.Headers.Add("X-Instagram-Rupload-Params", uploadParams);
                response = _httpRequestProcessor.SendAsync(request, session.httpClient);
                json = response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return Result.UnExpectedResponse<bool>(response, json.Result);
                }

                var photoUploadParams = JsonConvert.SerializeObject(photoUploadParamsObj);
                byte[] imageBytes;
                imageBytes = image.ImageBytes ?? File.ReadAllBytes(image.Uri);
                var imageContent = new ByteArrayContent(imageBytes);
                imageContent.Headers.Add("Content-Transfer-Encoding", "binary");
                imageContent.Headers.Add("Content-Type", "application/octet-stream");
                request = _httpHelper.GetDefaultRequest(HttpMethod.Post, photoUri,ref session.device);
                request.Content = imageContent;
                request.Headers.Add("X-Entity-Type", "image/jpeg");
                request.Headers.Add("Offset", "0");
                request.Headers.Add("X-Instagram-Rupload-Params", photoUploadParams);
                request.Headers.Add("X-Entity-Name", photoEntityName);
                request.Headers.Add("X-Entity-Length", imageBytes.Length.ToString());
                request.Headers.Add("X_FB_PHOTO_WATERFALL_ID", waterfallId);
                response = _httpRequestProcessor.SendAsync(request, session.httpClient);
                json = response.Content.ReadAsStringAsync();
                
                return ConfigurePhoto(ref session, uploadId, isDirectPhoto, isDisappearingPhoto, caption, viewMode, storyType, recipients, threadId);
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

        private IResult<bool> ConfigurePhoto(ref Session _user, string uploadId, bool isDirectPhoto, bool isDisappearingPhoto, string caption, InstaViewMode viewMode, InstaStoryType storyType, string recipients, string threadId)
        {
            try
            {

                var instaUri = UriCreator.GetDirectConfigVideoUri();
                var retryContext = GetRetryContext();
                var clientContext = Guid.NewGuid().ToString();
                {

                    var rnd = new Random();
                    var data = new JObject
                    {
                        {"timezone_offset", "16200"},
                        {"_csrftoken", _user.User.CsrfToken},
                        {"client_shared_at", (DateTime.UtcNow.ToUnixTime() - rnd.Next(25,55)).ToString()},
                        {"story_media_creation_date", (DateTime.UtcNow.ToUnixTime() - rnd.Next(50,70)).ToString()},
                        {"media_folder", "Camera"},
                        {"source_type", "3"},
                        {"video_result", ""},
                        {"_uid", _user.User.LoggedInUser.Pk.ToString()},
                        {"_uuid", _user.device.DeviceGuid.ToString()},
                        {"caption", caption ?? string.Empty},
                        {"date_time_original", DateTime.Now.ToString("yyyy-dd-MMTh:mm:ss-0fffZ")},
                        {"capture_type", "normal"},
                        {"mas_opt_in", "NOT_PROMPTED"},
                        {"upload_id", uploadId},
                        {"client_timestamp", DateTime.UtcNow.ToUnixTime()},
                        {
                            "device", new JObject{
                                {"manufacturer", _user.device.HardwareManufacturer},
                                {"model", _user.device.DeviceModelIdentifier},
                                {"android_release", _user.device.AndroidVer.VersionNumber},
                                {"android_version", _user.device.AndroidVer.APILevel}
                            }
                        },
                        {
                            "extra", new JObject
                            {
                                {"source_width", 0},
                                {"source_height", 0}
                            }
                        }
                    };
                    if (isDisappearingPhoto)
                    {
                        data.Add("view_mode", viewMode.ToString().ToLower());
                        data.Add("configure_mode", "2");
                        data.Add("recipient_users", "[]");
                        data.Add("thread_ids", $"[{threadId}]");
                    }
                    else
                    {
                        switch (storyType)
                        {
                            case InstaStoryType.SelfStory:
                            default:
                                data.Add("configure_mode", "1");
                                break;
                            case InstaStoryType.Direct:
                                data.Add("configure_mode", "2");
                                data.Add("view_mode", "replayable");
                                data.Add("recipient_users", "[]");
                                data.Add("thread_ids", $"[{threadId}]");
                                break;
                            case InstaStoryType.Both:
                                data.Add("configure_mode", "3");
                                data.Add("view_mode", "replayable");
                                data.Add("recipient_users", "[]");
                                data.Add("thread_ids", $"[{threadId}]");
                                break;
                        }
                    }
                    instaUri = UriCreator.GetVideoStoryConfigureUri(false);
                    var request = _httpHelper.GetSignedRequest(HttpMethod.Post, instaUri, _user.device, data);

                    request.Headers.Add("retry_context", retryContext);
                    var response = _httpRequestProcessor.SendAsync(request, _user.httpClient);
                    var json = response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var mediaResponse = JsonConvert.DeserializeObject<InstaDefault>(json.Result);

                        return mediaResponse.Status.ToLower() == "ok" 
                        ? Result.Success(true) : 
                        Result.UnExpectedResponse<bool>(response, json.Result);
                    }
                    return Result.UnExpectedResponse<bool>(response, json.Result);
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


        public IResult<InstaMedia> SendMediaPhotoAsync(ref Session session, InstaImageUpload image, 
        string caption, InstaLocationShort location, bool configureAsNameTag = false)
        {
            try
            {

                if (image.UserTags != null && image.UserTags.Any())
                {
                    foreach (var t in image.UserTags)
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

                var uploadId = ApiRequestMessageF.GenerateRandomUploadId();
                var photoHashCode = Path.GetFileName(image.Uri ?? $"C:\\{13.GenerateRandomString()}.jpg").GetHashCode();
                var photoEntityName = $"{uploadId}_0_{photoHashCode}";
                var photoUri = UriCreator.GetStoryUploadPhotoUri(uploadId, photoHashCode);
                var waterfallId = Guid.NewGuid().ToString();
                var retryContext = GetRetryContext();
                HttpRequestMessage request = null;
                HttpResponseMessage response = null;
                Task<string> json = null;
                var photoUploadParamsObj = new JObject
                {
                    {"retry_context", retryContext},
                    {"media_type", "1"},
                    {"upload_id", uploadId},
                    {"image_compression", "{\"lib_name\":\"moz\",\"lib_version\":\"3.1.m\",\"quality\":\"95\"}"},
                };

                var uploadParams = JsonConvert.SerializeObject(photoUploadParamsObj);
                request = _httpHelper.GetDefaultRequest(HttpMethod.Get, photoUri,ref session.device);
                request.Headers.Add("X_FB_PHOTO_WATERFALL_ID", waterfallId);
                request.Headers.Add("X-Instagram-Rupload-Params", uploadParams);
                response = _httpRequestProcessor.SendAsync(request, session.httpClient);
                json = response.Content.ReadAsStringAsync();


                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return Result.UnExpectedResponse<InstaMedia>(response, json.Result);
                }
                var photoUploadParams = JsonConvert.SerializeObject(photoUploadParamsObj);
                var imageBytes = image.ImageBytes ?? File.ReadAllBytes(image.Uri);
                var imageContent = new ByteArrayContent(imageBytes);
                imageContent.Headers.Add("Content-Transfer-Encoding", "binary");
                imageContent.Headers.Add("Content-Type", "application/octet-stream");
                request = _httpHelper.GetDefaultRequest(HttpMethod.Post, photoUri,ref session.device);
                request.Content = imageContent;
                request.Headers.Add("X-Entity-Type", "image/jpeg");
                request.Headers.Add("Offset", "0");
                request.Headers.Add("X-Instagram-Rupload-Params", photoUploadParams);
                request.Headers.Add("X-Entity-Name", photoEntityName);
                request.Headers.Add("X-Entity-Length", imageBytes.Length.ToString());
                request.Headers.Add("X_FB_PHOTO_WATERFALL_ID", waterfallId);
                response = _httpRequestProcessor.SendAsync(request, session.httpClient);
                json = response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    if (configureAsNameTag)
                    {
                        return ConfigureMediaPhotoAsNametagAsync(ref session, uploadId);
                    }
                    return ConfigureMediaPhotoAsync(ref session, uploadId, caption, location, image.UserTags);
                }

                return Result.UnExpectedResponse<InstaMedia>(response, json.Result);
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
        private IResult<InstaMedia> ConfigureMediaPhotoAsync(ref Session _user, string uploadId, 
        string caption, InstaLocationShort location, List<InstaUserTagUpload> userTags = null)
        {
            try
            {
                var instaUri = UriCreator.GetMediaConfigureUri();
                var retryContext = GetRetryContext();
                var rnd = new Random();
                var data = new JObject
                {
                    {"date_time_digitalized", DateTime.UtcNow.ToString("yyyy:MM:dd+hh:mm:ss")},
                    {"date_time_original", DateTime.UtcNow.ToString("yyyy:MM:dd+hh:mm:ss")},
                    {"is_suggested_venue", "false"},
                    {"timezone_offset", InstaApiConstants.TIMEZONE_OFFSET.ToString()},
                    {"_csrftoken", _user.User.CsrfToken},
                    {"media_folder", "Camera"},
                    {"source_type", "3"},
                    {"_uid", _user.User.LoggedInUser.Pk.ToString()},
                    {"_uuid", _user.device.DeviceGuid.ToString()},
                    {"caption", caption ?? string.Empty},
                    {"upload_id", uploadId},
                    {
                        "device", new JObject{
                            {"manufacturer", _user.device.HardwareManufacturer},
                            {"model", _user.device.DeviceModelIdentifier},
                            {"android_release", _user.device.AndroidVer.VersionNumber},
                            {"android_version", _user.device.AndroidVer.APILevel}
                        }
                    },
                    {
                        "extra", new JObject
                        {
                            {"source_width", 0},
                            {"source_height", 0}
                        }
                    }
                };
                if (location != null)
                {
                    data.Add("location", location.GetJson());
                }
                if (userTags != null && userTags.Any())
                {
                    var tagArr = new JArray();
                    foreach (var tag in userTags)
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
                    data.Add("usertags", root.ToString(Formatting.None));
                }
                var request = _httpHelper.GetSignedRequest(HttpMethod.Post, instaUri, _user.device, data);
                request.Headers.Add("retry_context", retryContext);
                var response = _httpRequestProcessor.SendAsync(request, _user.httpClient);
                var json = response.Content.ReadAsStringAsync();
                
                var mediaResponse =
                     JsonConvert.DeserializeObject<InstaMediaItemResponse>(json.Result, new InstaMediaDataConverter());
                var converter = ConvertersFabric.Instance.GetSingleMediaConverter(mediaResponse);
                var obj = converter.Convert();
                if (obj.Caption == null && !string.IsNullOrEmpty(caption))
                {
                    var editedMedia = mediaProcessor.EditMedia( ref _user, obj.InstaIdentifier, caption, location);
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

        private IResult<InstaMedia> ConfigureMediaPhotoAsNametagAsync(ref Session session, string uploadId)
        {
            try
            {
                var instaUri = UriCreator.GetMediaNametagConfigureUri();
                var retryContext = GetRetryContext();
                var data = new JObject
                {
                    {"upload_id", uploadId},
                    {"_csrftoken", session.User.CsrfToken},
                    {"_uid", session.User.LoggedInUser.Pk.ToString()},
                    {"_uuid", session.device.DeviceGuid.ToString()}
                };
                var request = _httpHelper.GetSignedRequest(HttpMethod.Post, instaUri, session.device, data);
                request.Headers.Add("retry_context", retryContext);
                var response = _httpRequestProcessor.SendAsync(request, session.httpClient);
                var json = response.Content.ReadAsStringAsync();

                var mediaResponse = JsonConvert.DeserializeObject<InstaMediaItemResponse>
                (json.Result, new InstaMediaDataConverter());
                var converter = ConvertersFabric.Instance.GetSingleMediaConverter(mediaResponse);
                var obj = converter.Convert();
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
        public static string GetRetryContext()
        {
            return new JObject
                {
                    {"num_step_auto_retry", 0},
                    {"num_reupload", 0},
                    {"num_step_manual_retry", 0}
                }.ToString(Formatting.None);
        }
    }
}
