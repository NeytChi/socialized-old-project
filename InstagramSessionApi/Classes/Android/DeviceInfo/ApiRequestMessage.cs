using System;
using Newtonsoft.Json;
using System.Diagnostics;
using InstagramApiSharp.API;
using InstagramApiSharp.Helpers;
using InstagramApiSharp.API.Versions;

namespace InstagramApiSharp.Classes.Android.DeviceInfo
{
    public partial class ApiRequestChallengeMessage : ApiRequestMessageData
    {
        [JsonProperty("_csrftoken")]
        public string CsrtToken { get; set; }
    }
    public partial class ApiRequestMessageData
    {
        [JsonProperty("phone_id")]
        public string PhoneId { get; set; }
        [JsonProperty("username")]
        public string Username { get; set; }
        [JsonProperty("adid")]
        public string AdId { get; set; }
        [JsonProperty("guid")]
        public string guid;
        public Guid Guid { get; set; }
        [JsonProperty("device_id")]
        public string DeviceId { get; set; }
        [JsonProperty("_uuid")]
        public string Uuid => Guid.ToString();
        [JsonProperty("google_tokens")]
        public string GoogleTokens { get; set; } = "[]";
        [JsonProperty("password")]
        public string Password { get; set; }
        [JsonProperty("login_attempt_count")]
        public string LoginAttemptCount { get; set; } = "1";
    }
    public static class ApiRequestMessageF
    {
        static readonly Random Rnd = new Random();

        public static string GetMessageString(ApiRequestMessageData messageData)
        {
            var json = JsonConvert.SerializeObject(messageData);
            Debug.WriteLine(json);
            return json;
        }
        public static string GetChallengeMessageString(string csrfToken, ref ApiRequestMessageData messageData)
        {
            var api = new ApiRequestChallengeMessage
            {
                CsrtToken = csrfToken,
                DeviceId = messageData.DeviceId,
                Guid = messageData.Guid,
                LoginAttemptCount = "1",
                Password = messageData.Password,
                PhoneId = messageData.PhoneId,
                Username = messageData.Username,
                AdId = messageData.AdId
            };
            var json = JsonConvert.SerializeObject(api);
            return json;
        }
        public static string GetMessageStringForChallengeVerificationCodeSend(ref ApiRequestMessageData messageData, int Choice = 1)
        {
            return JsonConvert.SerializeObject(new { choice = Choice.ToString(), _csrftoken = "ReplaceCSRF", messageData.Guid, messageData.DeviceId });
        }
        public static string GetChallengeVerificationCodeSend(ref ApiRequestMessageData messageData, string verify)
        {
            return JsonConvert.SerializeObject(new { security_code = verify, _csrftoken = "ReplaceCSRF", messageData.Guid, messageData.DeviceId });
        }
        public static string GenerateSignature(ref ApiRequestMessageData messageData, InstaApiVersion apiVersion, string signatureKey, out string deviceid)
        {
            if (string.IsNullOrEmpty(signatureKey))
                signatureKey = apiVersion.SignatureKey;
            var res = CryptoHelper.CalculateHash(signatureKey, JsonConvert.SerializeObject(messageData));
            deviceid = messageData.DeviceId;
            return res;
        }
        public static string GenerateChallengeSignature(ref ApiRequestMessageData messageData, InstaApiVersion apiVersion, string signatureKey,string csrfToken, out string deviceid)
        {
            if (string.IsNullOrEmpty(signatureKey))
                signatureKey = apiVersion.SignatureKey;
            var api = new ApiRequestChallengeMessage
            {
                CsrtToken = csrfToken,
                DeviceId = messageData.DeviceId,
                Guid = messageData.Guid,
                LoginAttemptCount = "1",
                Password = messageData.Password,
                PhoneId = messageData.PhoneId,
                Username = messageData.Username,
                AdId = messageData.AdId
            };
            var res = CryptoHelper.CalculateHash(signatureKey,
                JsonConvert.SerializeObject(api));
            deviceid = messageData.DeviceId;
            return res;
        }
        public static bool IsEmpty(ref ApiRequestMessageData messageData)
        {
            if (string.IsNullOrEmpty(messageData.PhoneId)) return true;
            if (string.IsNullOrEmpty(messageData.DeviceId)) return true;
            if (Guid.Empty == messageData.Guid) return true;
            return false;
        }

        public static string GenerateDeviceId()
        {
            return GenerateDeviceIdFromGuid(Guid.NewGuid());
        }

        public static string GenerateUploadId()
        {
            return new Random().Next(1000000000, Int32.MaxValue).ToString();
        }
        public static string GenerateRandomUploadId()
        {
            return DateTime.UtcNow.ToUnixTimeMiliSeconds().ToString();
        }
        public static ApiRequestMessageData FromDevice(AndroidDevice device)
        {
            var requestMessage = new ApiRequestMessageData
            {
                PhoneId = device.PhoneGuid.ToString(),
                Guid = device.DeviceGuid,
                DeviceId = device.DeviceId
            };
            return requestMessage;
        }

        public static string GenerateDeviceIdFromGuid(Guid guid)
        {
            var hashedGuid = CryptoHelper.CalculateMd5(guid.ToString());
            return $"android-{hashedGuid.Substring(0, 16)}";
        }
    }
}