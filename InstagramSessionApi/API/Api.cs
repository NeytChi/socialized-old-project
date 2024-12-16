using Serilog;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using InstagramApiSharp.Enums;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Helpers;
using InstagramApiSharp.Converters;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.API.Versions;
using InstagramApiSharp.API.Processors;
using InstagramApiSharp.Classes.ResponseWrappers;
using InstagramApiSharp.Classes.Android.DeviceInfo;

namespace InstagramApiSharp.API
{
    public interface IInstagramApi
    {
        InstaLoginResult Login(Session session, bool isNewLogin = true);
        Session LoadStateDataFromString(string json);
        InstaLoginResult VerifyCodeForChallengeRequire(string verifyCode, Session session);
        IResult<InstaChallengeRequireSMSVerify> VerifyCodeToSMSForChallengeRequire(bool replayChallenge, Session session);
        IResult<InstaChallengeRequireVerifyMethod> GetChallengeRequireVerifyMethod(Session _user);
        string GetStateDataAsString(Session session);
    }
    public class InstagramApi : IInstagramApi
    {
        private InstagramApi()
        {

        }
        private static InstagramApi instance;
        public static InstagramApi CreateInstance(InstaApiVersionType apiVersionType, ILogger logger)
        {
            if (instance == null)
            {
                instance = new InstagramApi(apiVersionType, logger);
            }
            return instance;
        }
        public static InstagramApi GetInstance(ILogger logger)
        {
            if (instance == null)
            {
                instance = new InstagramApi(InstaApiVersionType.Version86, logger);
            }
            return instance;
        }
        public HashtagProcessor hashtag;
        public UserProcessor users;
        public LocationProcessor location;
        public MediaProcessor media;
        public CommentProcessor comment;
        public HelperProcessor helper;
        public StoryProcessor story;
        public WebProcessor web;
        
        public HttpRequestProcessor httpRequestProcessor;

        private InstaApiVersionType _apiVersionType;
        private InstaApiVersion _apiVersion;
        private HttpHelper httpHelper { get; set; }
        
        bool IsCustomDeviceSet = false;

      //  private ICollectionProcessor _collectionProcessor;
      //  private ILiveProcessor _liveProcessor;
      //  private IDiscoverProcessor _discoverProcessor;
      //  private IAccountProcessor _accountProcessor;


       // ITVProcessor _tvProcessor;
       // HelperProcessor _helperProcessor;
       // IShoppingProcessor _shoppingProcessor;
        private InstagramApi(InstaApiVersionType apiVersionType, ILogger logger)
        {
            this._apiVersionType = apiVersionType;
            this._apiVersion = InstaApiVersionList.GetApiVersionList().GetApiVersion(apiVersionType);
            this.httpHelper = HttpHelper.GetInstance(_apiVersion);
            this.httpRequestProcessor = HttpRequestProcessor.GetInstance();
            this.hashtag = new HashtagProcessor(logger);
            this.users = new UserProcessor(logger);
            this.location = new LocationProcessor(logger);
            this.media = new MediaProcessor(this.users);
            this.comment = new CommentProcessor();
            this.helper = new HelperProcessor(this.users, this.media);
            this.story = new StoryProcessor(this.users);
            this.web = new WebProcessor();
        }

        /// <summary>
        ///     Login using given credentials asynchronously
        /// </summary>
        /// <param name="isNewLogin"></param>
        /// <returns>
        ///     Success --> is succeed
        ///     TwoFactorRequired --> requires 2FA login.
        ///     BadPassword --> Password is wrong
        ///     InvalidUser --> User/phone number is wrong
        ///     Exception --> Something wrong happened
        ///     ChallengeRequired --> You need to pass Instagram challenge
        /// </returns>
        public InstaLoginResult Login(Session session, bool isNewLogin = true)
        {
            try
            {
                bool needsRelogin = false;
                ReloginLabel:
                if (isNewLogin)
                {
                    var firstResponse = httpRequestProcessor.GetAsync(session.httpClient.BaseAddress, session.httpClient);
                    var html = firstResponse.Result.Content.ReadAsStringAsync();
                }
                var cookies = session.httpHandler.CookieContainer.GetCookies(session.httpClient.BaseAddress);

                var csrftoken = cookies[InstaApiConstants.CSRFTOKEN]?.Value ?? string.Empty;
                session.User.CsrfToken = csrftoken;
                var instaUri = UriCreator.GetLoginUri();
                var signature = string.Empty;
                var devid = string.Empty;
                if (isNewLogin)
                {
                    signature = $"{ApiRequestMessageF.GenerateSignature(ref session.requestMessage, _apiVersion, _apiVersion.SignatureKey, out devid)}.{ApiRequestMessageF.GetMessageString(session.requestMessage)}";
                }
                else
                {
                    signature = $"{ApiRequestMessageF.GenerateChallengeSignature(ref session.requestMessage, _apiVersion, _apiVersion.SignatureKey, csrftoken, out devid)}.{ApiRequestMessageF.GetChallengeMessageString(csrftoken,ref session.requestMessage)}";
                }
                session.device.DeviceId = devid;
                var fields = new Dictionary<string, string>
                {
                    {InstaApiConstants.HEADER_IG_SIGNATURE, signature},
                    {InstaApiConstants.HEADER_IG_SIGNATURE_KEY_VERSION, InstaApiConstants.IG_SIGNATURE_KEY_VERSION}
                };
                var request = httpHelper.GetDefaultRequest(HttpMethod.Post, instaUri, session.device, fields);
                request.Headers.Add("Host", "i.instagram.com");
                var response = httpRequestProcessor.SendAsync(request, session.httpClient);
                var json = response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var loginFailReason = JsonConvert.DeserializeObject<InstaLoginBaseResponse>(json.Result);
                    if (loginFailReason.InvalidCredentials)
                        return Result.Fail("Invalid Credentials",
                            loginFailReason.ErrorType == "bad_password"
                                ? InstaLoginResult.BadPassword
                                : InstaLoginResult.InvalidUser).Value;
                    if (loginFailReason.TwoFactorRequired)
                    {
                        if (loginFailReason.TwoFactorLoginInfo != null)
                        {
                            session.requestMessage.Username = loginFailReason.TwoFactorLoginInfo.Username;
                        }
                        session.twoFactorInfo = loginFailReason.TwoFactorLoginInfo;
                        //2FA is required!
                        return Result.Fail("Two Factor Authentication is required", InstaLoginResult.TwoFactorRequired).Value;
                    }
                    if (loginFailReason.ErrorType == "checkpoint_challenge_required")
                    {
                        session.challengeinfo = loginFailReason.Challenge;
                        return Result.Fail("Challenge is required", InstaLoginResult.ChallengeRequired).Value;
                    }
                    if (loginFailReason.ErrorType == "rate_limit_error")
                    {
                        return Result.Fail("Please wait a few minutes before you try again.", InstaLoginResult.LimitError).Value;
                    }
                    if (loginFailReason.ErrorType == "inactive user" || loginFailReason.ErrorType == "inactive_user")
                    {
                        return Result.Fail($"{loginFailReason.Message}\r\nHelp url: {loginFailReason.HelpUrl}", InstaLoginResult.InactiveUser).Value;
                    }
                    if (loginFailReason.ErrorType == "checkpoint_logged_out")
                    {
                        if (!needsRelogin)
                        {
                            needsRelogin = true;
                            goto ReloginLabel;
                        }
                        return Result.Fail($"{loginFailReason.ErrorType} {loginFailReason.CheckpointUrl}", InstaLoginResult.CheckpointLoggedOut).Value;
                    }
                    return Result.UnExpectedResponse<InstaLoginResult>(response, json.Result).Value;
                }
                InstaLoginResponse loginInfo = JsonConvert.DeserializeObject<InstaLoginResponse>(json.Result);
                session.User.UserName = loginInfo.User?.UserName;
                session.IsUserAuthenticated = loginInfo.User != null;
                if (loginInfo.User != null)
                {
                    session.requestMessage.Username = loginInfo.User.UserName;
                }
                var converter = ConvertersFabric.Instance.GetUserShortConverter(loginInfo.User);
                session.User.LoggedInUser = converter.Convert();
                session.User.RankToken = $"{session.User.LoggedInUser.Pk}_{session.requestMessage.PhoneId}";
                if (string.IsNullOrEmpty(session.User.CsrfToken))
                {
                    cookies = session.httpHandler.CookieContainer.GetCookies(session.httpClient.BaseAddress);
                    session.User.CsrfToken = cookies[InstaApiConstants.CSRFTOKEN]?.Value ?? string.Empty;
                }
                return InstaLoginResult.Success;
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, InstaLoginResult.Exception, ResponseType.NetworkProblem).Value;
            }
            catch (Exception exception)
            {
                return Result.Fail(exception, InstaLoginResult.Exception).Value;
            }
        }
        /// <summary>
        ///     Get challenge require (checkpoint required) options
        /// </summary>
        public IResult<InstaChallengeRequireVerifyMethod> GetChallengeRequireVerifyMethod(Session _user)
        {
            if (_user.challengeinfo == null)
                return Result.Fail("challenge require info is empty.\r\ntry to call LoginAsync function first.", (InstaChallengeRequireVerifyMethod)null);
            try
            {
                var instaUri = UriCreator.GetChallengeRequireFirstUri(_user.challengeinfo.ApiPath, 
                _user.device.DeviceGuid.ToString(), _user.device.DeviceId);
                var request = httpHelper.GetDefaultRequest(HttpMethod.Get, instaUri,ref _user.device);
                var response = httpRequestProcessor.SendAsync(request, _user.httpClient);
                var json = response.Content.ReadAsStringAsync().Result;
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaChallengeRequireVerifyMethod>(response, json);
                var obj = JsonConvert.DeserializeObject<InstaChallengeRequireVerifyMethod>(json);
                return Result.Success(obj);
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaChallengeRequireVerifyMethod), ResponseType.NetworkProblem);
            }
            catch (Exception ex)
            {
                return Result.Fail(ex, (InstaChallengeRequireVerifyMethod)null);
            }
        }
        /// <summary>
        ///     Request verification code sms for challenge require (checkpoint required)
        /// </summary>
        /// <param name="replayChallenge">true if Instagram should resend verification code to you</param>
        public IResult<InstaChallengeRequireSMSVerify> VerifyCodeToSMSForChallengeRequire(bool replayChallenge,Session _user)
        {
            return RequestVerifyCodeToSMSForChallengeRequire(replayChallenge, _user);
        }

        private IResult<InstaChallengeRequireSMSVerify> RequestVerifyCodeToSMSForChallengeRequire(bool replayChallenge, Session _user, string phoneNumber = null)
        {
            if (_user.challengeinfo == null)
                return Result.Fail("challenge require info is empty.\r\ntry to call LoginAsync function first.", (InstaChallengeRequireSMSVerify)null);

            try
            {
                Uri instaUri;

                if (replayChallenge)
                    instaUri = UriCreator.GetChallengeReplayUri(_user.challengeinfo.ApiPath);
                else
                    instaUri = UriCreator.GetChallengeRequireUri(_user.challengeinfo.ApiPath);

                var data = new JObject
                {
                    {"_csrftoken", _user.User.CsrfToken},
                    {"guid", _user.device.DeviceGuid.ToString()},
                    {"device_id", _user.device.DeviceId},
                };
                if (!string.IsNullOrEmpty(phoneNumber))
                    data.Add("phone_number", phoneNumber);
                else
                    data.Add("choice", "0");

                var request = httpHelper.GetSignedRequest(HttpMethod.Post, instaUri, _user.device, data);
                request.Headers.Add("Host", "i.instagram.com");
                var response = httpRequestProcessor.SendAsync(request, _user.httpClient);
                var json = response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var msg = "";
                    try
                    {
                        var j = JsonConvert.DeserializeObject<InstaChallengeRequireSMSVerify>(json.Result);
                        msg = j.Message;
                    }
                    catch { }
                    return Result.Fail(msg, (InstaChallengeRequireSMSVerify)null);
                }

                var obj = JsonConvert.DeserializeObject<InstaChallengeRequireSMSVerify>(json.Result);
                return Result.Success(obj);
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaChallengeRequireSMSVerify), ResponseType.NetworkProblem);
            }
            catch (Exception ex)
            {
                return Result.Fail(ex, (InstaChallengeRequireSMSVerify)null);
            }
        }
        /// <summary>
        ///     Verify verification code for challenge require (checkpoint required)
        /// </summary>
        /// <param name="verifyCode">Verification code</param>
        public InstaLoginResult VerifyCodeForChallengeRequire(string verifyCode, Session session)
        {
            if (session.challengeinfo == null)
                return Result.Fail("challenge require info is empty.\r\ntry to call Login function first.", InstaLoginResult.Exception).Value;

            try
            {
                var cookies = session.httpHandler.CookieContainer.GetCookies(session.httpClient.BaseAddress);
                var csrftoken = cookies[InstaApiConstants.CSRFTOKEN]?.Value ?? string.Empty;
                session.User.CsrfToken = csrftoken;
                var instaUri = UriCreator.GetChallengeRequireUri(session.challengeinfo.ApiPath);

                var data = new JObject
                {
                    {"security_code", verifyCode},
                    {"_csrftoken", session.User.CsrfToken},
                    {"guid", session.device.DeviceGuid.ToString()},
                    {"device_id", session.device.DeviceId},
                };
                var request = httpHelper.GetSignedRequest(HttpMethod.Post, instaUri, session.device, data);
                request.Headers.Add("Host", "i.instagram.com");
                var response =  httpRequestProcessor.SendAsync(request, session.httpClient);
                var json = response.Content.ReadAsStringAsync().Result;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var msg = "";
                    try
                    {
                        var j = JsonConvert.DeserializeObject<InstaChallengeRequireVerifyCode>(json);
                        msg = j.Message;
                    }
                    catch { }
                    return Result.Fail(msg, InstaLoginResult.Exception).Value;
                }

                var obj = JsonConvert.DeserializeObject<InstaChallengeRequireVerifyCode>(json);
                if (obj != null)
                {
                    if (obj.LoggedInUser != null)
                    {
                        ValidateUserAsync(ref session, obj.LoggedInUser, csrftoken);
                        Task.Delay(3000);
                        return Result.Success(InstaLoginResult.Success).Value;
                    }

                    if (!string.IsNullOrEmpty(obj.Action))
                    {
                        // we should wait at least 15 seconds and then trying to login again
                        Task.Delay(15000);
                        return Login(session, false);
                    }
                }
                return Result.Fail(obj?.Message, InstaLoginResult.Exception).Value;
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaLoginResult), ResponseType.NetworkProblem).Value;
            }
            catch (Exception ex)
            {
                return Result.Fail(ex, InstaLoginResult.Exception).Value;
            }
        }
        /// <summary>
        ///     Get current state info as Memory stream
        /// </summary>
        /// <returns>
        ///     State data
        /// </returns>
        public Stream GetStateDataAsStream(Session session)
        {
            var Cookies =  session.httpHandler.CookieContainer.GetCookies(new Uri(InstaApiConstants.INSTAGRAM_URL));
            var RawCookiesList = new List<Cookie>();
            foreach (Cookie cookie in Cookies)
                RawCookiesList.Add(cookie);
            var state = new StateData
            {
                _twoFactorInfo = session.twoFactorInfo,
                _challengeinfo = session.challengeinfo,
                DeviceInfo = session.device,
                IsAuthenticated = session.IsUserAuthenticated,
                UserSession = session.User,
                Cookies =  session.httpHandler.CookieContainer,
                RawCookies = RawCookiesList,
                InstaApiVersion = _apiVersionType
            };
            return SerializationHelper.SerializeToStream(state);
        }
        /// <summary>
        ///     Get current state info as Json string
        /// </summary>
        /// <returns>
        ///     State data
        /// </returns>
        public string GetStateDataAsString(Session session)
        {

            var Cookies = session.httpHandler.CookieContainer.GetCookies(new Uri(InstaApiConstants.INSTAGRAM_URL));
            var RawCookiesList = new List<Cookie>();
            foreach (Cookie cookie in Cookies)
                RawCookiesList.Add(cookie);

            var state = new StateData
            {
                _twoFactorInfo = session.twoFactorInfo,
                _challengeinfo = session.challengeinfo,
                DeviceInfo = session.device,
                IsAuthenticated = session.IsUserAuthenticated,
                UserSession = session.User,
                Cookies =  session.httpHandler.CookieContainer,
                RawCookies = RawCookiesList,
                InstaApiVersion = _apiVersionType
            };

            // var state = new StateData
            // {
            //     DeviceInfo = session.device,
            //     IsAuthenticated = session.IsUserAuthenticated,
            //     UserSession = new SessionData(),
            //     Cookies = session.httpHandler.CookieContainer,
            //     RawCookies = RawCookiesList,
            //     InstaApiVersion = _apiVersionType
            // };
            // state.UserSession.LoggedInUser = new InstaUserShort();
            // state.UserSession.UserName = session.User.UserName;
            // state.UserSession.UserName = session.User.Password;
            // state.UserSession.RankToken = session.User.RankToken;
            // state.UserSession.CsrfToken = session.User.CsrfToken;
            // state.UserSession.FacebookUserId = session.User.FacebookUserId;
            // state.UserSession.FacebookAccessToken = session.User.FacebookAccessToken;

            return SerializationHelper.SerializeToString(state);
        }
        /// <summary>
        ///     Loads the state data from stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public Session LoadStateDataFromStream(Stream stream)
        {
            var session = new Session();
            StateData data = SerializationHelper.DeserializeFromStream<StateData>(stream);
            if (!IsCustomDeviceSet)
                session.device = data.DeviceInfo;
            session.twoFactorInfo = data._twoFactorInfo;
            session.challengeinfo = data._challengeinfo;
            session.User = data.UserSession;
           
            session.requestMessage.Username = data.UserSession.UserName;
            session.requestMessage.Password = data.UserSession.Password;

            session.requestMessage.DeviceId = data.DeviceInfo.DeviceId;
            session.requestMessage.PhoneId = data.DeviceInfo.PhoneGuid.ToString();
            session.requestMessage.Guid = data.DeviceInfo.DeviceGuid;
            session.requestMessage.AdId = data.DeviceInfo.AdId.ToString();

            foreach (var cookie in data.RawCookies)
                session.httpHandler.CookieContainer.Add(new Uri(InstaApiConstants.INSTAGRAM_URL), cookie);

            if (data.InstaApiVersion == null)
                data.InstaApiVersion = InstaApiVersionType.Version86;
            _apiVersionType = data.InstaApiVersion.Value;
            _apiVersion = InstaApiVersionList.GetApiVersionList().GetApiVersion(_apiVersionType);
            httpHelper = HttpHelper.GetInstance(_apiVersion);

            session.IsUserAuthenticated = data.IsAuthenticated;
            return session;
        }
        /// <summary>
        ///     Set state data from provided json string
        /// </summary>
        public Session LoadStateDataFromString(string json)
        {
            var session = new Session();
            var data = SerializationHelper.DeserializeFromString<StateData>(json);
            if (!IsCustomDeviceSet)
                session.device = data.DeviceInfo;
            session.twoFactorInfo = data._twoFactorInfo;
            session.challengeinfo = data._challengeinfo;
            session.User = data.UserSession;
           
            // if (_user.User == null)
            //     _user.User = new SessionData();
            // _user.User.UserName = data.UserSession.UserName;
            // _user.User.Password = data.UserSession.UserName;
            // _user.User.RankToken = data.UserSession.RankToken;
            // _user.User.CsrfToken = data.UserSession.CsrfToken;
            // _user.User.FacebookUserId = data.UserSession.FacebookUserId;
            // _user.User.FacebookAccessToken = data.UserSession.FacebookAccessToken;

            //Load Stream Edit 
            session.requestMessage.Username = data.UserSession.UserName;
            session.requestMessage.Password = data.UserSession.Password;

            session.requestMessage.DeviceId = data.DeviceInfo.DeviceId;
            session.requestMessage.PhoneId = data.DeviceInfo.PhoneGuid.ToString();
            session.requestMessage.Guid = data.DeviceInfo.DeviceGuid;
            session.requestMessage.AdId = data.DeviceInfo.AdId.ToString();

            foreach (var cookie in data.RawCookies)
                session.httpHandler.CookieContainer.Add(new Uri(InstaApiConstants.INSTAGRAM_URL), cookie);

            if (data.InstaApiVersion == null)
                data.InstaApiVersion = InstaApiVersionType.Version86;
            _apiVersionType = data.InstaApiVersion.Value;
            _apiVersion = InstaApiVersionList.GetApiVersionList().GetApiVersion(_apiVersionType);
            httpHelper = HttpHelper.GetInstance(_apiVersion);

            session.IsUserAuthenticated = data.IsAuthenticated;
            return session;
         }


        /// <summary>
        ///     Set state data from StateData object
        /// </summary>
        /// <param name="stateData"></param>
        public Session LoadStateDataFromObject(StateData stateData)
        {
            var session = new Session();
            if (!IsCustomDeviceSet)
            {
                session.device = stateData.DeviceInfo;
            }
            session.User = stateData.UserSession;
            
            //Load Stream Edit 
            session.requestMessage.Username = stateData.UserSession.UserName;
            session.requestMessage.Password = stateData.UserSession.Password;

            session.requestMessage.DeviceId = stateData.DeviceInfo.DeviceId;
            session.requestMessage.PhoneId = stateData.DeviceInfo.PhoneGuid.ToString();
            session.requestMessage.Guid = stateData.DeviceInfo.DeviceGuid;
            session.requestMessage.AdId = stateData.DeviceInfo.AdId.ToString();

            foreach (var cookie in stateData.RawCookies)
            {
                session.httpHandler.CookieContainer.Add(new Uri(InstaApiConstants.INSTAGRAM_URL), cookie);
            }

            if (stateData.InstaApiVersion == null)
                stateData.InstaApiVersion = InstaApiVersionType.Version86;
            _apiVersionType = stateData.InstaApiVersion.Value;
            _apiVersion = InstaApiVersionList.GetApiVersionList().GetApiVersion(_apiVersionType);
            httpHelper = HttpHelper.GetInstance(_apiVersion);

            session.IsUserAuthenticated = stateData.IsAuthenticated;
            return session;
        }
        public int GetTimezoneOffset() => InstaApiConstants.TIMEZONE_OFFSET;        
        private void ValidateUserAsync(ref Session _user, InstaUserShortResponse user, string csrfToken, bool validateExtra = true, string password = null)
        {
            try
            {
                var converter = ConvertersFabric.Instance.GetUserShortConverter(user);
                _user.User.LoggedInUser = converter.Convert();
                if (password != null)
                {
                    _user.User.Password = password;
                }
                _user.User.UserName = _user.User.UserName;
                if (validateExtra)
                {
                    _user.User.RankToken = $"{_user.User.LoggedInUser.Pk}_{_user.requestMessage.PhoneId}";
                    _user.User.CsrfToken = csrfToken;
                    if (string.IsNullOrEmpty(_user.User.CsrfToken))
                    {
                        var cookies = _user.httpHandler.CookieContainer.GetCookies(_user.httpClient.BaseAddress);
                        _user.User.CsrfToken = cookies[InstaApiConstants.CSRFTOKEN]?.Value ?? string.Empty;
                    }
                    _user.IsUserAuthenticated = true;
                }

            }
            catch { }
        }
    }
}