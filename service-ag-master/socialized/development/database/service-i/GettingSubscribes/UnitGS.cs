using System.Collections.Generic;

namespace Models.GettingSubscribes
{
    /// <summary>
    /// This class provide data of Instagram users and their comment by optional.
    /// This class is required for handling by one of ModeGS class.
    /// </summary>
    public partial class UnitGS
    {
        public long unitId { get; set; }
        public long dataId { get; set; }
        public long userPk { get; set; }
        public bool userIsPrivate { get; set; }
        public string username { get; set; }
        public string commentPk { get; set; }
        public long createdAt { get; set; }    
        public bool unitHandled { get; set; }    
        public long? handledAt { get; set; }    
        public bool handleAgain { get; set; }    
        public virtual TaskData Data { get; set; }
        public virtual ICollection<MediaGS> medias { get; set; }
    }
} 