using System.Collections.Generic;

namespace InstagramApiSharp.Classes.Models
{
    public class InstaUserShortFriendshipList : List<InstaUserShortFriendship> { }
    public class InstaUserShortFriendship : InstaUserShort
    {
        public InstaFriendshipShortStatus FriendshipStatus { get; set; }
    }
}
