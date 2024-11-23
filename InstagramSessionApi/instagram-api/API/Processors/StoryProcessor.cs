using System;
using System.IO;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading;
using Newtonsoft.Json.Linq;
using InstagramApiSharp.Enums;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Helpers;
using InstagramApiSharp.Converters;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Classes.ResponseWrappers;
using InstagramApiSharp.Classes.Android.DeviceInfo;

namespace InstagramApiSharp.API.Processors
{
    /// <summary>
    ///     Story api functions.
    /// </summary>
    public class StoryProcessor
    {
        private readonly HttpHelper _httpHelper;
        private readonly HttpRequestProcessor _httpRequestProcessor;
        private readonly UserProcessor userProcessor;

        public StoryProcessor(UserProcessor userProcessor)
        {
            _httpRequestProcessor = HttpRequestProcessor.GetInstance();
            _httpHelper = HttpHelper.GetInstance();
            this.userProcessor = userProcessor;
        }
        /// <summary>
        ///     Get user story reel feed. Contains user info last story including all story items.
        /// </summary>
        /// <param name="userId">User identifier (PK)</param>
        public IResult<InstaReelFeed> GetUserStoryFeed(ref Session user, long userId)
        {
            var feed = new InstaReelFeed();
            try
            {
                var userFeedUri = UriCreator.GetUserReelFeedUri(userId);
                var request = _httpHelper.GetDefaultRequest(HttpMethod.Get, userFeedUri,ref user.device);
                var response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                var json = response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    IResult<InstaReelFeed> result = Result.UnExpectedResponse<InstaReelFeed>(response, json.Result);
                    result.unexceptedResponse = true;
                    return result;
                }
                var feedResponse = JsonConvert.DeserializeObject<InstaReelFeedResponse>(json.Result);
                feed = ConvertersFabric.Instance.GetReelFeedConverter(feedResponse).Convert();
                return Result.Success(feed);
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaReelFeed), ResponseType.NetworkProblem);
            }
            catch (Exception exception)
            {
                return Result.Fail(exception, feed);
            }
        }
        /// <summary>
        ///     Seen story
        /// </summary>
        /// <param name="storyMediaId">Story media identifier</param>
        /// <param name="takenAtUnix">Taken at unix</param>
        public IResult<bool> MarkStoryAsSeen(ref Session user, string storyMediaId, long takenAtUnix)
        {
            try
            {
                var instaUri = UriCreator.GetSeenMediaStoryUri();
                var storyId = $"{storyMediaId}_{storyMediaId.Split('_')[1]}";
                var dateTimeUnix = DateTime.UtcNow.ToUnixTime();
                var reel = new JObject
                {
                    { storyId, new JArray($"{takenAtUnix}_{dateTimeUnix}") }
                };
                var data = new JObject
                {
                    {"_csrftoken", user.User.CsrfToken},
                    {"_uid", user.User.LoggedInUser.Pk.ToString()},
                    {"_uuid", user.device.DeviceGuid.ToString()},
                    {"container_module", "feed_timeline"},
                    {"live_vods_skipped", new JObject()},
                    {"nuxes_skipped", new JObject()},
                    {"nuxes", new JObject()},
                    {"reels", reel},
                    {"live_vods", new JObject()},
                    {"reel_media_skipped", new JObject()}
                };
                var request =
                    _httpHelper.GetSignedRequest(HttpMethod.Post, instaUri, user.device, data);
                var response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                var json = response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    IResult<bool> result = Result.UnExpectedResponse<bool>(response, json.Result);
                    result.unexceptedResponse = true;
                    return result;
                }
                var obj = JsonConvert.DeserializeObject<InstaDefault>(json.Result);
                return obj.Status.ToLower() == "ok" ? 
                Result.Success(true) : 
                Result.UnExpectedResponse<bool>(response, json.Result);
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
        /// <summary>
        ///     Upload story photo
        /// </summary>
        /// <param name="image">Photo to upload</param>
        /// <param name="caption">Caption</param>
        /// param name="uploadOptions">Upload options => Optional</param>
        public IResult<InstaStoryMedia> UploadStoryPhoto(ref Session user, InstaImage image, string caption,
            InstaStoryUploadOptions uploadOptions = null)
        {
            return UploadStoryPhoto(ref user, image, caption, null, uploadOptions);
        }
        /// <summary>
        ///     Upload story photo with adding link address (with progress)
        ///     <para>Note: this function only works with verified account or you have more than 10k followers.</para>
        /// </summary>
        /// <param name="progress">Progress action</param>
        /// <param name="image">Photo to upload</param>
        /// <param name="caption">Caption</param>
        /// <param name="uri">Uri to add</param>
        /// <param name="uploadOptions">Upload options => Optional</param>
        public IResult<InstaStoryMedia> UploadStoryPhoto(ref Session user,
            InstaImage image, string caption, Uri uri, InstaStoryUploadOptions uploadOptions = null)
        {
            try
            {
                if (uploadOptions?.Mentions?.Count > 0)
                {
                    foreach (var t in uploadOptions.Mentions)
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
                                t.Pk = u.Value.Pk;
                        }
                        catch { }
                    }
                }
                if(uploadOptions?.Questions?.Count > 0)
                {
                    try
                    {
                        bool tried = false;
                        var profilePicture = string.Empty;
                    TryToGetMyUser:
                        // get latest profile picture
                        var myUser = userProcessor.GetUser(ref user, user.User.UserName.ToLower());
                        if (!myUser.Succeeded)
                        {
                            if (!tried)
                            {
                                tried = true;
                                goto TryToGetMyUser;
                            }
                            else
                                profilePicture = user.User.LoggedInUser.ProfilePicture;
                        }
                        else
                            profilePicture = myUser.Value.ProfilePicture;


                        foreach (var question in uploadOptions.Questions)
                            question.ProfilePicture = profilePicture;
                    }
                    catch { }
                }

                var uploadId = ApiRequestMessageF.GenerateRandomUploadId();
                var photoHashCode = Path.GetFileName(image.Uri ?? $"C:\\{13.GenerateRandomString()}.jpg").GetHashCode();

                var waterfallId = Guid.NewGuid().ToString();

                var photoEntityName = $"{uploadId}_0_{photoHashCode}";
                var photoUri = UriCreator.GetStoryUploadPhotoUri(uploadId, photoHashCode);

                var videoMediaInfoData = new JObject
                {
                    {"_csrftoken", user.User.CsrfToken},
                    {"_uid", user.User.LoggedInUser.Pk},
                    {"_uuid", user.device.DeviceGuid.ToString()},
                    {"media_info", new JObject
                        {
                              {"capture_mode", "normal"},
                              {"media_type", 1},
                              {"caption", caption},
                              {"mentions", new JArray()},
                              {"hashtags", new JArray()},
                              {"locations", new JArray()},
                              {"stickers", new JArray()},
                        }
                    }
                };
                var request = _httpHelper.GetSignedRequest(HttpMethod.Post, UriCreator.GetStoryMediaInfoUploadUri(), user.device, videoMediaInfoData);
                var response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                var json = response.Content.ReadAsStringAsync();
               
                var photoUploadParamsObj = new JObject
                {
                    {"upload_id", uploadId},
                    {"media_type", "1"},
                    {"retry_context", "{\"num_step_auto_retry\":0,\"num_reupload\":0,\"num_step_manual_retry\":0}"},
                    {"image_compression", "{\"lib_name\":\"moz\",\"lib_version\":\"3.1.m\",\"quality\":\"95\"}"}
                };
                var photoUploadParams = JsonConvert.SerializeObject(photoUploadParamsObj);
                request = _httpHelper.GetDefaultRequest(HttpMethod.Get, photoUri,ref user.device);
                request.Headers.Add("X_FB_PHOTO_WATERFALL_ID", waterfallId);
                request.Headers.Add("X-Instagram-Rupload-Params", photoUploadParams);
                response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                json = response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    IResult<InstaStoryMedia> result = Result.UnExpectedResponse
                    <InstaStoryMedia>(response, json.Result);
                    result.unexceptedResponse = true;
                    return result;
                }

                var imageBytes = image.ImageBytes ?? File.ReadAllBytes(image.Uri);
                var imageContent = new ByteArrayContent(imageBytes);
                imageContent.Headers.Add("Content-Transfer-Encoding", "binary");
                imageContent.Headers.Add("Content-Type", "application/octet-stream");
                request = _httpHelper.GetDefaultRequest(HttpMethod.Post, photoUri,ref user.device);
                request.Content = imageContent;
                request.Headers.Add("X-Entity-Type", "image/jpeg");
                request.Headers.Add("Offset", "0");
                request.Headers.Add("X-Instagram-Rupload-Params", photoUploadParams);
                request.Headers.Add("X-Entity-Name", photoEntityName);
                request.Headers.Add("X-Entity-Length", imageBytes.Length.ToString());
                request.Headers.Add("X_FB_PHOTO_WATERFALL_ID", waterfallId);
                response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                json = response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Thread.Sleep(5000);
                    return ConfigureStoryPhoto(ref user, image, uploadId, caption, uri, uploadOptions);
                }
                IResult<InstaStoryMedia> resultMedia = Result.UnExpectedResponse<InstaStoryMedia>(
                response, json.Result);
                resultMedia.unexceptedResponse = true;
                return resultMedia;
            }
            catch (HttpRequestException httpException)
            {
                IResult<InstaStoryMedia> result = Result.Fail(httpException, default(InstaStoryMedia), ResponseType.NetworkProblem);
                result.unexceptedResponse = true;
                return result;
            }
            catch (Exception exception)
            {
                IResult<InstaStoryMedia> result = Result.Fail<InstaStoryMedia>(exception);
                result.unexceptedResponse = true;
                return result;
            }
        }

        /// <summary>
        ///     Upload story video (to self story)
        /// </summary>
        /// <param name="video">Video to upload</param>
        /// <param name="caption">Caption</param>
        /// <param name="uploadOptions">Upload options => Optional</param>
        public IResult<InstaStoryMedia> UploadStoryVideo(ref Session session, InstaVideoUpload video, string caption, InstaStoryUploadOptions uploadOptions = null)
        {
            return UploadStoryVideo(ref session, video, caption, null, uploadOptions);
        }

        /// <summary>
        ///     Upload story video (to self story) with adding link address (with progress)
        ///     <para>Note: this function only works with verified account or you have more than 10k followers.</para>
        /// </summary>
        /// <param name="progress">Progress action</param>
        /// <param name="video">Video to upload</param>
        /// <param name="caption">Caption</param>
        /// <param name="uri">Uri to add</param>
        /// <param name="uploadOptions">Upload options => Optional</param>
        public IResult<InstaStoryMedia> UploadStoryVideo(ref Session user,
            InstaVideoUpload video, string caption, Uri uri, InstaStoryUploadOptions uploadOptions = null)
        {
            try
            {
                var uploadId = ApiRequestMessageF.GenerateRandomUploadId();
                var videoHashCode = Path.GetFileName(video.Video.Uri ?? $"C:\\{13.GenerateRandomString()}.mp4").GetHashCode();
                var photoHashCode = Path.GetFileName(video.VideoThumbnail.Uri ?? $"C:\\{13.GenerateRandomString()}.jpg").GetHashCode();

                var waterfallId = Guid.NewGuid().ToString();

                var videoEntityName = $"{uploadId}_0_{videoHashCode}";
                var videoUri = UriCreator.GetStoryUploadVideoUri(uploadId, videoHashCode);

                var photoEntityName = $"{uploadId}_0_{photoHashCode}";
                var photoUri = UriCreator.GetStoryUploadPhotoUri(uploadId, photoHashCode);

                var videoMediaInfoData = new JObject
                {
                    {"_csrftoken", user.User.CsrfToken},
                    {"_uid", user.User.LoggedInUser.Pk},
                    {"_uuid", user.device.DeviceGuid.ToString()},
                    {"media_info", new JObject
                        {
                              {"capture_mode", "normal"},
                              {"media_type", 2},
                              {"caption", caption},
                              {"mentions", new JArray()},
                              {"hashtags", new JArray()},
                              {"locations", new JArray()},
                              {"stickers", new JArray()},
                        }
                    }
                };
                var request = _httpHelper.GetSignedRequest(HttpMethod.Post, UriCreator.GetStoryMediaInfoUploadUri(), 
                user.device, videoMediaInfoData);
                var response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                var json = response.Content.ReadAsStringAsync();

                var videoUploadParamsObj = new JObject
                {
                    {"upload_media_height", "0"},
                    {"upload_media_width", "0"},
                    {"upload_media_duration_ms", "46000"},
                    {"upload_id", uploadId},
                    {"for_album", "1"},
                    {"retry_context", "{\"num_step_auto_retry\":0,\"num_reupload\":0,\"num_step_manual_retry\":0}"},
                    {"media_type", "2"},
                };
                var videoUploadParams = JsonConvert.SerializeObject(videoUploadParamsObj);
                request = _httpHelper.GetDefaultRequest(HttpMethod.Get, videoUri,ref user.device);
                request.Headers.Add("X_FB_VIDEO_WATERFALL_ID", waterfallId);
                request.Headers.Add("X-Instagram-Rupload-Params", videoUploadParams);
                response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                json = response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    IResult<InstaStoryMedia> resultStory = Result.UnExpectedResponse<InstaStoryMedia>(
                    response, json.Result);
                    resultStory.unexceptedResponse = true;
                    return resultStory;
                }


                var videoBytes = video.Video.VideoBytes ?? File.ReadAllBytes(video.Video.Uri);
                var videoContent = new ByteArrayContent(videoBytes);
                videoContent.Headers.Add("Content-Transfer-Encoding", "binary");
                videoContent.Headers.Add("Content-Type", "application/octet-stream");
                request = _httpHelper.GetDefaultRequest(HttpMethod.Post, videoUri,ref user.device);
                request.Content = videoContent;
                var vidExt = Path.GetExtension(video.Video.Uri ?? $"C:\\{13.GenerateRandomString()}.mp4").Replace(".", "").ToLower();
                if (vidExt == "mov")
                {
                    request.Headers.Add("X-Entity-Type", "image/quicktime");
                }
                else
                {
                    request.Headers.Add("X-Entity-Type", "image/mp4");
                }
                request.Headers.Add("Offset", "0");
                request.Headers.Add("X-Instagram-Rupload-Params", videoUploadParams);
                request.Headers.Add("X-Entity-Name", videoEntityName);
                request.Headers.Add("X-Entity-Length", videoBytes.Length.ToString());
                request.Headers.Add("X_FB_VIDEO_WATERFALL_ID", waterfallId);
                response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                json = response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    IResult<InstaStoryMedia> resultStory = Result.UnExpectedResponse<InstaStoryMedia>(
                        response, json.Result);
                    resultStory.unexceptedResponse = true;
                    return resultStory;
                }
                var photoUploadParamsObj = new JObject
                {
                    {"retry_context", "{\"num_step_auto_retry\":0,\"num_reupload\":0,\"num_step_manual_retry\":0}"},
                    {"media_type", "2"},
                    {"upload_id", uploadId},
                    {"image_compression", "{\"lib_name\":\"moz\",\"lib_version\":\"3.1.m\",\"quality\":\"95\"}"},
                };
                var photoUploadParams = JsonConvert.SerializeObject(photoUploadParamsObj);
                request = _httpHelper.GetDefaultRequest(HttpMethod.Get, photoUri,ref user.device);
                request.Headers.Add("X_FB_PHOTO_WATERFALL_ID", waterfallId);
                request.Headers.Add("X-Instagram-Rupload-Params", photoUploadParams);
                response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                json = response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    IResult<InstaStoryMedia> resultStory = Result.UnExpectedResponse<InstaStoryMedia>
                    (response, json.Result);
                    resultStory.unexceptedResponse = true;
                    return resultStory;
                }

                var imageBytes = video.VideoThumbnail.ImageBytes ?? File.ReadAllBytes(video.VideoThumbnail.Uri);
                var imageContent = new ByteArrayContent(imageBytes);
                imageContent.Headers.Add("Content-Transfer-Encoding", "binary");
                imageContent.Headers.Add("Content-Type", "application/octet-stream");
                request = _httpHelper.GetDefaultRequest(HttpMethod.Post, photoUri,ref user.device);
                request.Content = imageContent;
                request.Headers.Add("X-Entity-Type", "image/jpeg");
                request.Headers.Add("Offset", "0");
                request.Headers.Add("X-Instagram-Rupload-Params", photoUploadParams);
                request.Headers.Add("X-Entity-Name", photoEntityName);
                request.Headers.Add("X-Entity-Length", imageBytes.Length.ToString());
                request.Headers.Add("X_FB_PHOTO_WATERFALL_ID", waterfallId);
                response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                json = response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Thread.Sleep(30000);
                    return ConfigureStoryVideo(ref user, video, uploadId, caption, uri, uploadOptions);
                }
                IResult<InstaStoryMedia> result = Result.UnExpectedResponse<InstaStoryMedia>(response, json.Result);
                result.unexceptedResponse = true;
                return result;
            }
            catch (HttpRequestException httpException)
            {
                IResult<InstaStoryMedia> media = Result.Fail(httpException, default(InstaStoryMedia), ResponseType.NetworkProblem);
                media.unexceptedResponse = true;
                return media;
            }
            catch (Exception exception)
            {
                IResult<InstaStoryMedia> result = Result.Fail<InstaStoryMedia>(exception);
                result.unexceptedResponse = true;
                return result;
            }
        }
        /// <summary>
        ///     Configure story photo
        /// </summary>
        /// <param name="image">Photo to configure</param>
        /// <param name="uploadId">Upload id</param>
        /// <param name="caption">Caption</param>
        /// <param name="uri">Uri to add</param>
        private IResult<InstaStoryMedia> ConfigureStoryPhoto(ref Session user, InstaImage image, string uploadId,
        string caption, Uri uri, InstaStoryUploadOptions uploadOptions = null)
        {
            try
            {
                var instaUri = UriCreator.GetVideoStoryConfigureUri();// UriCreator.GetStoryConfigureUri();
                var data = new JObject
                {
                    {"_uuid", user.device.DeviceGuid.ToString()},
                    {"_uid", user.User.LoggedInUser.Pk},
                    {"_csrftoken", user.User.CsrfToken},
                    {"source_type", "3"},
                    {"caption", caption},
                    {"upload_id", uploadId},
                    {"edits", new JObject()},
                    {"disable_comments", false},
                    {"configure_mode", 1},
                    {"camera_position", "unknown"}
                };
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
                        {
                            data.Add("story_sticker_ids", $"{uploadOptions.Slider.Emoji}");
                        }
                    }
                    else
                    {
                        if (uploadOptions.Polls?.Count > 0)
                        {
                            var pollArr = new JArray();
                            foreach (var item in uploadOptions.Polls)
                            {
                                pollArr.Add(item.ConvertToJson());
                            }
                            data.Add("story_polls", pollArr.ToString(Formatting.None));
                        }
                        if (uploadOptions.Questions?.Count > 0)
                        {
                            var questionArr = new JArray();
                            foreach (var item in uploadOptions.Questions)
                            {
                                questionArr.Add(item.ConvertToJson());
                            }
                            data.Add("story_questions", questionArr.ToString(Formatting.None));
                        }
                    }
                    if (uploadOptions.MediaStory != null)
                    {
                        var mediaStory = new JArray
                        {
                            uploadOptions.MediaStory.ConvertToJson()
                        };
                        data.Add("attached_media", mediaStory.ToString(Formatting.None));
                    }
                    if (uploadOptions.Mentions?.Count > 0)
                    {
                        var mentionArr = new JArray();
                        foreach (var item in uploadOptions.Mentions)
                        {
                            mentionArr.Add(item.ConvertToJson());
                        }
                        data.Add("reel_mentions", mentionArr.ToString(Formatting.None));
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
                var request = _httpHelper.GetSignedRequest(HttpMethod.Post, instaUri, user.device, data);
                var response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                var json = response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var mediaResponse = JsonConvert.DeserializeObject<InstaStoryMediaResponse>(json.Result);
                    var converter = ConvertersFabric.Instance.GetStoryMediaConverter(mediaResponse);
                    var obj = converter.Convert();
                    
                    return Result.Success(obj);
                }
                IResult<InstaStoryMedia> result = Result.UnExpectedResponse<InstaStoryMedia>(response, json.Result);
                result.unexceptedResponse = true;
                return result;
            }
            catch (HttpRequestException httpException)
            {
                IResult<InstaStoryMedia> result = Result.Fail(httpException, default(InstaStoryMedia), ResponseType.NetworkProblem);
                result.unexceptedResponse = true;
                return result;
            }
            catch (Exception exception)
            {
                IResult<InstaStoryMedia> result = Result.Fail<InstaStoryMedia>(exception);
                result.unexceptedResponse = true;
                return result;
            }
        }
        /// <summary>
        ///     Configure story video
        /// </summary>
        /// <param name="video">Video to configure</param>
        /// <param name="uploadId">Upload id</param>
        /// <param name="caption">Caption</param>
        /// <param name="uri">Uri to add</param>
        private IResult<InstaStoryMedia> ConfigureStoryVideo(ref Session user, InstaVideoUpload video, string uploadId,
            string caption, Uri uri, InstaStoryUploadOptions uploadOptions = null)
        {
            try
            {
                var instaUri = UriCreator.GetVideoStoryConfigureUri(false);
                var rnd = new Random();
                var data = new JObject
                {
                    {"filter_type", "0"},
                    {"timezone_offset", "16200"},
                    {"_csrftoken", user.User.CsrfToken},
                    {"client_shared_at", (long.Parse(ApiRequestMessageF.GenerateUploadId())- rnd.Next(25,55)).ToString()},
                    {"story_media_creation_date", (long.Parse(ApiRequestMessageF.GenerateUploadId())- rnd.Next(50,70)).ToString()},
                    {"media_folder", "Camera"},
                    {"configure_mode", "1"},
                    {"source_type", "4"},
                    {"video_result", ""},
                    {"_uid", user.User.LoggedInUser.Pk.ToString()},
                    {"_uuid", user.device.DeviceGuid.ToString()},
                    {"caption", caption},
                    {"date_time_original", DateTime.Now.ToString("yyyy-dd-MMTh:mm:ss-0fffZ")},
                    {"capture_type", "normal"},
                    {"mas_opt_in", "NOT_PROMPTED"},
                    {"upload_id", uploadId},
                    {"client_timestamp", ApiRequestMessageF.GenerateUploadId()},
                    {
                        "device", new JObject{
                            {"manufacturer", user.device.HardwareManufacturer},
                            {"model", user.device.DeviceModelIdentifier},
                            {"android_release", user.device.AndroidVer.VersionNumber},
                            {"android_version", user.device.AndroidVer.APILevel}
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
                var request = _httpHelper.GetSignedRequest(HttpMethod.Post, instaUri, user.device, data);
                var uploadParamsObj = new JObject
                {
                    {"num_step_auto_retry", 0},
                    {"num_reupload", 0},
                    {"num_step_manual_retry", 0}
                };
                var uploadParams = JsonConvert.SerializeObject(uploadParamsObj);
                request.Headers.Add("retry_context", uploadParams);
                var response = _httpRequestProcessor.SendAsync(request, user.httpClient);
                var json = response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var mediaResponse = JsonConvert.DeserializeObject<InstaStoryMediaResponse>(json.Result);
                    var converter = ConvertersFabric.Instance.GetStoryMediaConverter(mediaResponse);
                    var obj = Result.Success(converter.Convert());
                    return obj;
                }
                IResult<InstaStoryMedia> result = Result.UnExpectedResponse<InstaStoryMedia>(response, json.Result);
                result.unexceptedResponse = true;
                return result;
            }
            catch (HttpRequestException httpException)
            {
                IResult<InstaStoryMedia> result = Result.Fail(httpException, default(InstaStoryMedia), ResponseType.NetworkProblem);
                result.unexceptedResponse = true;
                return result;
            }
            catch (Exception exception)
            {
                IResult<InstaStoryMedia> result = Result.Fail<InstaStoryMedia>(exception);
                result.unexceptedResponse = true;
                return result;
            }
        }
        /// <summary>
        ///     Delete a media story (photo or video)
        /// </summary>
        /// <param name="storyMediaId">Story media id</param>
        /// <param name="sharingType">The type of the media</param>
        /// <returns>Return true if the story media is deleted</returns>
        public IResult<bool> DeleteStory(ref Session user, string storyMediaId, InstaSharingType sharingType = InstaSharingType.Video)
        {
            try
            {
                var deleteMediaUri = UriCreator.GetDeleteStoryMediaUri(storyMediaId, sharingType);

                var data = new JObject
                {
                    {"_uuid", user.device.DeviceGuid.ToString()},
                    {"_uid", user.User.LoggedInUser.Pk},
                    {"_csrftoken", user.User.CsrfToken},
                    {"media_id", storyMediaId}
                };

                var request = _httpHelper.GetSignedRequest(HttpMethod.Post, deleteMediaUri, user.device, data);
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
    }
}