using Serilog;
using System.Linq;
using Serilog.Core;
using database.context;
using Models.GettingSubscribes;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;

namespace ngettingsubscribers
{
    /// <summary>
    /// This class provide functional for receiving Instagram media for task 'Getting Subscribes'.
    /// </summary>
    public class ReceiverMediaGS
    {
        Logger log = new LoggerConfiguration()
            .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        
        public ReceiverUnitsGS receiverUnits;
        public ReceiverMediaGS()
        {
            receiverUnits = ReceiverUnitsGS.GetInstance();
        }
        public InstaMediaList RemoveExtraMedia(InstaMediaList medias, int likeCount, int savedMedia)
        {
            while(savedMedia + medias.Count > likeCount)
                medias.Remove(medias[medias.Count - 1]);
            return medias;
        }
        public void SaveMedias(Context context, InstaMediaList medias, long unitId)
        {
            int queue = 1;
            foreach (InstaMedia media in medias) {
                SaveMedia(context, media.Pk, unitId, queue);
                ++queue;
            }
        }
        public void SaveMedia(Context context, string mediaPk, long unitId, int mediaQueue)
        {
            MediaGS media = new MediaGS()
            {
                unitId = unitId,
                mediaPk = mediaPk,
                mediaQueue = mediaQueue,
                mediaHandled = false,
                handledAt = null
            };
            context.Medias.Add(media);
            context.SaveChanges();
        }
        public InstaMediaList RemoveExcessMedia(Context context, InstaMediaList medias, long unitId)
        {
            for (int i = medias.Count - 1; i >= 0; i--) {
                if (context.Medias.Any(m
                => m.unitId == unitId
                && m.mediaPk == medias[i].Pk))
                    medias.Remove(medias[i]);
            }
            log.Information("Sort from excess media.");
            return medias;
        }
        public MediaGS GetNonHandledMedia(Context context, long unitId)
        {
            return context.Medias.Where(m
                => m.unitId == unitId
                && m.mediaHandled == false).FirstOrDefault();
        }
        public MediaGS GetHandledMedia(Context context, long unitId)
        {
            return context.Medias.Where(m 
                => m.unitId == unitId
                && m.mediaHandled == true).FirstOrDefault();
        }
        public MediaGS GetMediaGS(Context context, UnitGS unit, ref Session session, int likeCount)
        {
            MediaGS media = GetNonHandledMedia(context, unit.unitId);
            if (media != null)
                return media;
            media = GetHandledMedia(context, unit.unitId);
            if (media == null)
                if (GetSaveUserMedia(context, ref session, unit, likeCount))
                    return GetNonHandledMedia(context, unit.unitId);
            return null;
        }
        public bool GetSaveUserMedia(Context context, ref Session session, UnitGS unit, int likeCount)
        {
            int pagination, savedMedia;

            pagination = 0;
            savedMedia = 0;
            while (likeCount > savedMedia) {
                var medias = receiverUnits.GetUserMedia(ref session, unit.username, pagination);
                if (medias != null) {
                    if (medias.Count > 0) {
                        medias = RemoveExcessMedia(context, medias, unit.unitId);
                        medias = RemoveExtraMedia(medias, likeCount, savedMedia);
                        if (medias.Count == 0)
                            break;
                        SaveMedias(context, medias, unit.unitId);
                        savedMedia += medias.Count;
                    }
                    else {
                        log.Information("User doesn't have any media, id -> " + session.sessionId);
                        break;
                    }
                }
                else
                    break;
                ++pagination;
            }
            MediaGS media = GetNonHandledMedia(context, unit.unitId);
            return media == null ? false : true;
        }
    }
}