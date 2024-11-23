using System;

namespace InstagramApiSharp.Classes.Android.DeviceInfo
{
    [Serializable]
    public class AndroidDevice
    {
        public AndroidDevice()
        {
        //    this.AndroidVer = AndroidVersion.GetRandomAndriodVersion();
        }
        //public long session_device_id { get; set; }
        //public long session_id { get; set; }
        public Guid PhoneGuid { get; set; }
        public Guid DeviceGuid { get; set; }
        public Guid GoogleAdId { get; set; } = Guid.NewGuid();
        public Guid RankToken { get; set; } = Guid.NewGuid();
        public Guid AdId { get; set; } = Guid.NewGuid();
        //public string googleAdId { get; set; }
        //public string phoneGuid { get; set; }
        //public string deviceGuid { get; set; }
        //public string rankToken { get; set; }
        //public string adId { get; set; }
        public string AndroidBoardName { get; set; }
        public string AndroidBootloader { get; set; }
        public string DeviceBrand { get; set; }
        public string DeviceId { get; set; }
        public string DeviceModel { get; set; }
        public string DeviceModelBoot { get; set; }
        public string DeviceModelIdentifier { get; set; }
        public string FirmwareBrand { get; set; }
        public string FirmwareFingerprint { get; set; }
        public string FirmwareTags { get; set; }
        public string FirmwareType { get; set; }
        public string HardwareManufacturer { get; set; }
        public string HardwareModel { get; set; }
        public string Resolution { get; set; } = "1080x1812";
        public string Dpi { get; set; } = "480dpi";
        public string Codename { get; set; }
        public string VersionNumber { get; set; }
        public string APILevel { get; set; }
        public AndroidVersion AndroidVer { get; set; }
        public virtual SessionData session { get; set; }
    }
}