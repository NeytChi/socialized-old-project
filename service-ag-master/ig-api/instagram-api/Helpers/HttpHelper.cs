using System;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using InstagramApiSharp.API;
using System.Collections.Generic;
using InstagramApiSharp.API.Versions;
using InstagramApiSharp.Classes.Android.DeviceInfo;

namespace InstagramApiSharp.Helpers
{
    public class HttpHelper
    {
        public InstaApiVersion apiVersion;
        private HttpHelper(InstaApiVersion apiVersionType)
        {
            apiVersion = apiVersionType;
        }
        public static HttpHelper instance;
        public static HttpHelper GetInstance(InstaApiVersion apiVersionType)
        {
            if (instance == null)
            {
                instance = new HttpHelper(apiVersionType);
            }
            return instance;
        }
        public static HttpHelper GetInstance()
        {
            return instance;
        }
        public HttpRequestMessage GetDefaultRequest(HttpMethod method, Uri uri, ref AndroidDevice deviceInfo)
        {
            string userAgent = deviceInfo.GenerateUserAgent(apiVersion);
            HttpRequestMessage request = new HttpRequestMessage(method, uri);
            request.Headers.Add(InstaApiConstants.HEADER_ACCEPT_LANGUAGE, InstaApiConstants.ACCEPT_LANGUAGE);
            request.Headers.Add(InstaApiConstants.HEADER_IG_CAPABILITIES, apiVersion.Capabilities);
            request.Headers.Add(InstaApiConstants.HEADER_IG_CONNECTION_TYPE, InstaApiConstants.IG_CONNECTION_TYPE);
            request.Headers.Add(InstaApiConstants.HEADER_USER_AGENT, userAgent);
            request.Headers.Add(InstaApiConstants.HEADER_IG_APP_ID, InstaApiConstants.IG_APP_ID);
            request.Properties.Add(new KeyValuePair<string, object>(InstaApiConstants.HEADER_XGOOGLE_AD_IDE, deviceInfo.GoogleAdId.ToString()));
            return request;
        }
        public HttpRequestMessage GetDefaultRequest(HttpMethod method, Uri uri, AndroidDevice deviceInfo, Dictionary<string, string> data)
        {
            var request = GetDefaultRequest(HttpMethod.Post, uri,ref deviceInfo);
            request.Content = new FormUrlEncodedContent(data);
            return request;
        }
        /// <summary>
        /// This is only for https://instagram.com site
        /// </summary>
        public HttpRequestMessage GetWebRequest(HttpMethod method, Uri uri, AndroidDevice deviceInfo)
        {
            var request = GetDefaultRequest(HttpMethod.Get, uri,ref deviceInfo);
            request.Headers.Remove(InstaApiConstants.HEADER_USER_AGENT);
            request.Headers.Add(InstaApiConstants.HEADER_USER_AGENT, InstaApiConstants.WEB_USER_AGENT);
            return request;
        }
        public HttpRequestMessage GetSignedRequest(HttpMethod method,
            Uri uri,
            AndroidDevice deviceInfo,
            Dictionary<string, string> data)
        {
            var hash = CryptoHelper.CalculateHash(apiVersion.SignatureKey,
                JsonConvert.SerializeObject(data));
            var payload = JsonConvert.SerializeObject(data);
            var signature = $"{hash}.{payload}";

            var fields = new Dictionary<string, string>
            {
                {InstaApiConstants.HEADER_IG_SIGNATURE, signature},
                {InstaApiConstants.HEADER_IG_SIGNATURE_KEY_VERSION, InstaApiConstants.IG_SIGNATURE_KEY_VERSION}
            };
            var request = GetDefaultRequest(HttpMethod.Post, uri, ref deviceInfo);
            request.Content = new FormUrlEncodedContent(fields);
            request.Properties.Add(InstaApiConstants.HEADER_IG_SIGNATURE, signature);
            request.Properties.Add(InstaApiConstants.HEADER_IG_SIGNATURE_KEY_VERSION,
                InstaApiConstants.IG_SIGNATURE_KEY_VERSION);
            return request;
        }


        public HttpRequestMessage GetSignedRequest(HttpMethod method,
            Uri uri,
            AndroidDevice deviceInfo,
            Dictionary<string, int> data)
        {
            var hash = CryptoHelper.CalculateHash(apiVersion.SignatureKey,
                JsonConvert.SerializeObject(data));
            var payload = JsonConvert.SerializeObject(data);
            var signature = $"{hash}.{payload}";

            var fields = new Dictionary<string, string>
            {
                {InstaApiConstants.HEADER_IG_SIGNATURE, signature},
                {InstaApiConstants.HEADER_IG_SIGNATURE_KEY_VERSION, InstaApiConstants.IG_SIGNATURE_KEY_VERSION}
            };
            var request = GetDefaultRequest(HttpMethod.Post, uri,ref deviceInfo);
            request.Content = new FormUrlEncodedContent(fields);
            request.Properties.Add(InstaApiConstants.HEADER_IG_SIGNATURE, signature);
            request.Properties.Add(InstaApiConstants.HEADER_IG_SIGNATURE_KEY_VERSION,
                InstaApiConstants.IG_SIGNATURE_KEY_VERSION);
            return request;
        }



        public HttpRequestMessage GetSignedRequest(HttpMethod method,
            Uri uri,
            AndroidDevice deviceInfo,
            Dictionary<string, object> data)
        {
            var hash = CryptoHelper.CalculateHash(apiVersion.SignatureKey,
                JsonConvert.SerializeObject(data));
            var payload = JsonConvert.SerializeObject(data);
            var signature = $"{hash}.{payload}";

            var fields = new Dictionary<string, string>
            {
                {InstaApiConstants.HEADER_IG_SIGNATURE, signature},
                {InstaApiConstants.HEADER_IG_SIGNATURE_KEY_VERSION, InstaApiConstants.IG_SIGNATURE_KEY_VERSION}
            };
            var request = GetDefaultRequest(HttpMethod.Post, uri,ref deviceInfo);
            request.Content = new FormUrlEncodedContent(fields);
            request.Properties.Add(InstaApiConstants.HEADER_IG_SIGNATURE, signature);
            request.Properties.Add(InstaApiConstants.HEADER_IG_SIGNATURE_KEY_VERSION,
                InstaApiConstants.IG_SIGNATURE_KEY_VERSION);
            return request;
        }

        public HttpRequestMessage GetSignedRequest(HttpMethod method,
            Uri uri,
            AndroidDevice deviceInfo,
            JObject data)
        {
            var hash = CryptoHelper.CalculateHash(apiVersion.SignatureKey,
                data.ToString(Formatting.None));
            var payload = data.ToString(Formatting.None);
            var signature = $"{hash}.{payload}";
            var fields = new Dictionary<string, string>
            {
                {InstaApiConstants.HEADER_IG_SIGNATURE, signature},
                {InstaApiConstants.HEADER_IG_SIGNATURE_KEY_VERSION, InstaApiConstants.IG_SIGNATURE_KEY_VERSION}
            };
            var request = GetDefaultRequest(HttpMethod.Post, uri,ref deviceInfo);
            request.Content = new FormUrlEncodedContent(fields);
            request.Properties.Add(InstaApiConstants.HEADER_IG_SIGNATURE, signature);
            request.Properties.Add(InstaApiConstants.HEADER_IG_SIGNATURE_KEY_VERSION,
                InstaApiConstants.IG_SIGNATURE_KEY_VERSION);
            return request;
        }

        public string GetSignature(JObject data)
        {
            var hash = CryptoHelper.CalculateHash(apiVersion.SignatureKey, data.ToString(Formatting.None));
            var payload = data.ToString(Formatting.None);
            var signature = $"{hash}.{payload}";
            return signature;
        }
    }
}