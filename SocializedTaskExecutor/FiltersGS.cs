using System;
using Serilog;
using System.Linq;
using Serilog.Core;
using InstagramService;
using InstagramApiSharp.API;
using Models.GettingSubscribes;
using InstagramApiSharp.Classes;
using System.Collections.Generic;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;

namespace ngettingsubscribers
{
    public class FiltersGS
    {
        public Logger log = new LoggerConfiguration()
            .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        public SessionStateHandler stateHandler;
        public InstagramApi api = InstagramApi.GetInstance();
        public ReceiverUnitsGS receiver = ReceiverUnitsGS.GetInstance();
        public FiltersGS(SessionStateHandler stateHandler)
        {
            this.stateHandler = stateHandler;
        }
        private char[] english = 
        {
            'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'E', 'e', 'F', 'f', 'G', 'g', 'H', 'h',
            'I', 'i', 'J', 'j', 'K', 'k', 'L', 'l', 'M', 'm', 'N', 'n', 'O', 'o', 'P', 'p',
            'Q', 'q', 'R', 'r', 'S', 's', 'T', 't', 'U', 'u', 'V', 'v', 'W', 'w', 'X', 'x',
            'Y', 'y', 'Z', 'z'
        };
        private char[] ukrainian = 
        {
            'А', 'а', 'Б', 'б', 'В', 'в', 'Г', 'г', 'Ґ', 'ґ', 'Д','д', 'Е', 'е',
            'Є', 'є', 'Ж', 'ж', 'З', 'з', 'И', 'и', 'І', 'і', 'Ї', 'ї', 'Й', 'й',
            'К', 'к', 'Л', 'л', 'М', 'м', 'Н', 'н', 'О', 'о', 'П', 'п', 'Р', 'р',
            'С', 'с', 'Т', 'т', 'У', 'у', 'Ф', 'ф', 'Х', 'х', 'Ц', 'ц', 	
            'Ч', 'ч', 'Ш', 'ш', 'Щ', 'щ', 'Ь', 'ь', 'Ю', 'ю', 'Я', 'я',    
        };
        private char[] russian = 
        {
            'А', 'а', 'Б', 'б', 'В', 'в', 'Г', 'г', 'Д', 'д', 'Е', 'е', 'Ё', 'ё',
            'Ж', 'ж', 'З', 'з', 'И', 'и', 'Й', 'й', 'К', 'к', 'Л', 'л', 'М', 'м',
            'Н', 'н', 'О', 'о', 'П', 'п', 'Р', 'р', 'С', 'с', 'Т', 'т', 'У', 'у',
            'Ф', 'ф', 'Х', 'х', 'Ц', 'ц', 'Ч', 'ч', 'Ш', 'ш', 'Щ', 'щ', 'Ь', 'ь',
            'Ы', 'ы', 'Ъ', 'ъ', 'Э', 'э',  'Ю', 'ю','Я', 'я',
        };
        private char[] arabian = 
        {
            'خ' ,	'ح', 	'ج' ,	'ث' ,	'ت' ,	'ب', 	'ا',
            'ص' ,	'ش' ,	'س', 	'ز' ,	'ر' ,	'ذ' ,	'د', 
            'ق' ,	'ف' ,	'غ' ,	'ع', 	'ظ' ,	'ط' ,	'ض',
            'ي', 	'و', 	'ه', 	'ن', 	'م', 	'ل', 	'ك' 
        };
        /// <summary>
        /// Check user by specific filters.
        /// </summary>
        /// <param name="task">Current task</param>
        public bool CheckByAllFilters(ref Session session, TaskFilter taskFilter, long userPk)
        {
            if (taskFilter != null) {
                InstaFullUserInfo userInfo = GetFullUserInfo(ref session, userPk);
                if (userInfo != null)
                    if (CheckRanges(taskFilter, userInfo.UserDetail.FollowerCount, 
                    userInfo.UserDetail.FollowingCount))
                        if (CheckDescription(taskFilter, userInfo))
                            if (CheckMedias(ref session, taskFilter, userInfo))
                                return true;
            }
            else
                return true;
            return false;
        } 
        public InstaFullUserInfo GetFullUserInfo(ref Session session, long userPk)
        {
            IResult<InstaFullUserInfo> result = api.users.GetFullUserInfo(ref session, userPk);
            if (result.Succeeded)
                return result.Value;
            else 
            { 
                if (result.unexceptedResponse)
                    stateHandler.HandleState(result.Info.ResponseType, session);
                log.Warning("Can't get user details."); 
            }
            return null;
        }
        private bool CheckRanges(TaskFilter filter, long followerCount, long followingCount)
        {
            if (filter != null)
                if (RangeSubscribersFrom(filter.range_subscribers_from, followerCount))
                    if (RangeSubscribersTo(filter.range_subscribers_to, followerCount))
                        if (RangeFollowingFrom(filter.range_following_from, followingCount))
                            if (RangeFollowingTo(filter.range_following_to, followingCount))
                                return true;
            return false;
        }
        private bool CheckDescription(TaskFilter filter, InstaFullUserInfo userDetails)
        {
            if (WithProfileUrl(filter.with_profile_url, userDetails.UserDetail.Biography))
                if (Languages(filter, userDetails.UserDetail.Biography))
                    if (WordsInDescription(filter, userDetails.UserDetail.Biography))
                        if (NoWordsInDescription(filter, userDetails.UserDetail.Biography))
                            return true;
            return false;
        }
        private bool CheckMedias(ref Session session, TaskFilter filter, InstaFullUserInfo userDetails)
        {
            if (PublicationCount(filter.publication_count, userDetails.UserDetail.MediaCount))
                if (LatestPublicationNoYonger(ref session, filter.latest_publication_no_younger, userDetails))
                    if (WithoutProfilePhoto(filter.without_profile_photo, userDetails.UserDetail.ProfilePicId))
                        return true;
            return false;
        }
        private bool Languages(TaskFilter filter, string biography)
        {
            if (LanguageEnglish(filter.english, biography))
                if (LanguageUkrainian(filter.ukrainian, biography))
                    if (LanguageArabian(filter.arabian, biography))
                        if (LanguageRussian(filter.russian, biography))
                            return true;
            return false;
        }
        public bool RangeSubscribersFrom(int value, long followerCount)
        {
            if (value <= followerCount)
                return true;
            else  { 
                log.Information("Following range from->" + value + " subscribers count->" + followerCount + "."); 
                return false;
            } 
        }
        public bool RangeSubscribersTo(int value, long followerCount)
        {
            if (value >= followerCount)
                return true;
            else { 
                log.Information("Subscribers range to->" + value + " subscribers count->" + followerCount + "."); 
                return false;
            }     
        }
        public bool RangeFollowingFrom(int value, long followingCount)
        {
            if (followingCount >= value)
                return true;
            else { 
                log.Information("Subscribers range from->" + value + " subscribers count->" + followingCount + "."); 
                return false;                
            }
        }
        public bool RangeFollowingTo(int value, long followingCount)
        {
            if (followingCount <= value)
                return true;
            else { 
                log.Information("Following range to->" + value + " subscribers count->" + followingCount + "."); 
                return false;
            }
        }
        public bool PublicationCount(long publicationCount, long mediaCount)
        {
            if (mediaCount >= publicationCount)
                return true;
            else { 
                log.Information("Publication count -> " + publicationCount + " user's media count->" + mediaCount + "."); 
                return false;
            }
        }
        public bool WithoutProfilePhoto(bool withProfilePhoto, string profilePicId)
        {
            if (withProfilePhoto) {
                if (profilePicId == null) {
                    log.Information("User doesn't have profile photo.");
                    return false;
                }
            }
            return true;
        }
        public bool LatestPublicationNoYonger(ref Session session, int publicationNoYonger, InstaFullUserInfo userInfo)
        {
            if (userInfo.UserDetail.MediaCount >= 1) {   
                InstaMediaList medias = receiver.GetUserMedia(ref session, userInfo.UserDetail.Username, 0);
                if (medias != null) {
                    DateTime lastPublication = DateTime.Now.AddDays(-publicationNoYonger);
                    if (medias[0].TakenAt > lastPublication)
                        return true;
                    else 
                        log.Information("Last publication date < current date, created at->" + medias[0].TakenAt + "."); 
                }
            }
            else 
                log.Information("User's account doesn't have publications."); 
            return false;
        }
        public bool WithProfileUrl(bool withProfileUrl, string biography)
        {
            if (withProfileUrl) {
                if (biography.Contains('@'))
                    return true;
                else {
                    log.Information("Biography doesn't contain profile url.");
                    return false;
                }
            }
            else
                return true;
        }
        public bool LanguageEnglish(bool English, string biography)
        {
            if (English) {
                foreach(char ch in english) {
                    if (biography.Contains(ch))
                        return true;
                }
                log.Information("Biography doesn't contain English.");
                return false;
            }
            return true;
        }
        public bool LanguageUkrainian(bool Ukrainian, string biography)
        {
            if (Ukrainian) {
                foreach(char ch in ukrainian) {
                    if (biography.Contains(ch))
                        return true;
                }
                log.Information("Biography doesn't contain Ukrainian.");
                return false;
            }
            return true;
        }
        public bool LanguageArabian(bool Arabian, string biography)
        {
            if (Arabian) {
                foreach(char ch in arabian) {
                    if (biography.Contains(ch))
                        return true;
                }
                log.Information("Biography doesn't contain Arabian.");
                return false;
            }
            return true;
        }
        public bool LanguageRussian(bool Russian, string biography)
        {
            if (Russian) {
                foreach(char ch in russian) {
                    if (biography.Contains(ch))
                        return true;
                }
                log.Information("Biography doesn't contain Russian.");
                return false;
            }
            return true;
        }
        public bool WordsInDescription(TaskFilter filter, string biography)
        {
            if (filter != null) {
                List<FilterWord> words = filter.words.Where(w 
                    => w.wordUse == true).ToList();
                foreach(FilterWord word in words) {
                    if (!biography.Contains(word.wordValue)) {
                        log.Information("User's biography doesn't contains word ->" + word.wordValue);
                        return false;
                    }
                }
            }
            return true;
        }
        public bool NoWordsInDescription(TaskFilter filter, string biography)
        {
            if (filter != null) {
                List<FilterWord> words = filter.words.Where(w => w.wordUse == false).ToList();
                foreach(FilterWord word in words) {
                    if (biography.Contains(word.wordValue)) {
                        log.Information("User's biography contains word -> " + word.wordValue);
                        return false;
                    }
                }
            }
            return true;
        }
    }
} 