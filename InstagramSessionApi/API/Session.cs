using System;
using System.Net.Http;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Classes.Android.DeviceInfo;

namespace InstagramApiSharp.API.Builder
{
    public class Session
    {
        public long sessionId;
        public int userId = 0;
        public AndroidDevice device;
        public HttpClient httpClient;
        public HttpClientHandler httpHandler = new HttpClientHandler();
        public ApiRequestMessageData requestMessage;
        public bool IsUserAuthenticated;
        public SessionData User;
        public InstaTwoFactorLoginInfo twoFactorInfo;
        public InstaChallengeLoginInfo challengeinfo;

        public Session()
        {
            this.IsUserAuthenticated = true;
            this.User = new SessionData();
            this.User.LoggedInUser = new InstaUserShort();
            this.device = new AndroidDevice();
            this.requestMessage = new ApiRequestMessageData();     
            this.httpClient = new HttpClient(httpHandler) 
            { 
                BaseAddress = new Uri(InstaApiConstants.INSTAGRAM_URL) 
            };
        }
        public Session(string instagramName, string instagramPassword)
        {
            this.User = new SessionData();
            User.UserName = instagramName;
            User.Password = instagramPassword;
            IsUserAuthenticated = false;
            httpClient = new HttpClient(httpHandler) 
            { 
                BaseAddress = new Uri(InstaApiConstants.INSTAGRAM_URL) 
            };
            device = AndroidDeviceGenerator.GetRandomAndroidDevice();
            requestMessage = new ApiRequestMessageData();
            requestMessage.PhoneId = device.PhoneGuid.ToString();
            requestMessage.Guid = device.DeviceGuid;
            requestMessage.Password = User?.Password;
            requestMessage.Username = User?.UserName;
            requestMessage.DeviceId = ApiRequestMessageF.GenerateDeviceId();
            requestMessage.AdId = device.AdId.ToString();
            if (string.IsNullOrEmpty(requestMessage.Password)) 
            {   
                requestMessage.Password = User?.Password;
            }
            if (string.IsNullOrEmpty(requestMessage.Username)) 
            {
                requestMessage.Username = User?.UserName;
            }
        }
    }
}