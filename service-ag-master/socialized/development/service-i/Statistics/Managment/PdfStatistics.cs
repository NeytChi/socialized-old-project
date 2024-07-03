using System;
using Serilog;
using Serilog.Core;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using socialized;
using database.context;
using Models.Statistics;

using iText.Forms;
using iText.Signatures;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Image;
using iText.IO.Font.Constants;
using iText.Kernel.Pdf;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Colors;
using iText.Kernel.Pdf.Xobject;
using iText.Kernel.Pdf.Canvas;


namespace InstagramService.Statistics
{
    public class PdfStatistics
    {
        public string savingPath;
        public string templatePath;
        public string awsUrl;
        public Logger log;
        public GetterStatistics getter;
        public PdfStatistics(Logger log, GetterStatistics getter)
        {
            this.log = log;
            var config = Program.serverConfiguration();
            this.awsUrl = config.GetValue<string>("aws_host_url");
            this.savingPath = config.GetValue<string>("pdf_saving_path");
            this.templatePath = config.GetValue<string>("pdf_template_path");
            this.getter = getter;
        }
        public PdfDocument CreateNewPdf(string nameFile) 
        {
            FileInfo file = new FileInfo(templatePath);
            PdfReader reader = new PdfReader(file);
            PdfWriter writer = new PdfWriter(savingPath + nameFile + ".pdf");
            log.Information("Create new pdf instance with name -> " + nameFile);
            return new PdfDocument(reader, writer);
        }
        public void FillingData(string pdfName, BusinessAccount account, DateTime from, DateTime to)
        {
            PdfDocument document = CreateNewPdf(pdfName);
            PdfAcroForm acroForm = PdfAcroForm.GetAcroForm(document, true);
            PdfPage page = document.GetPage(1);
            PdfCanvas canvas = new PdfCanvas(page);
            PageSize size = new PageSize(page.GetPageSize());
            float height = size.GetHeight();
            DateTime lastFrom = from.AddTicks(-(to - from).Ticks);

            List<DayStatistics> days = getter.GetDayStatistics(account.businessId, from, to);
            List<PostStatistics> posts = getter.GetPostStatistics(account.businessId, from, to);
            List<StoryStatistics> stories = getter.GetStoryStatistics(account.businessId, from, to);
            List<OnlineFollowers> followersOnline = getter.GetOnlineFollowers(account.businessId, from);
            List<PostStatistics> last = getter.GetPostStatistics(account.businessId, lastFrom, from);
            List<dynamic> growingFollowers = getter.GrowFollowers(days, account.followersCount);
            
            Generals(canvas, height, account.followersCount,
                days.Sum(x => x.followerCount),
                days.Sum(x => x.profileViews),
                posts.Sum(x => x.engagement)
            );
            FollowersEngagements(canvas, height, growingFollowers, posts);
            CompareColumn(canvas, height, last, posts);
            LikesComments(canvas, height, posts);
            PostSection(canvas, posts.Sum(x => x.engagement), posts.Sum(x => x.saved));
            ActivityProfile(canvas, days);
            OnlineFollowersBlocks(canvas, followersOnline, account.followersCount, from);
            PicturePosts(document, posts);
            PictureStories(document, stories);

            document.Close();
            log.Information("Fill pdf by analytics-statistics data.");
        }
        public void Generals(PdfCanvas canvas, float height, 
            long followersCount, long followersGrow, long profileViews, long engagementCount)
        {
            List<string> values = new List<string>();
            values.Add(ConvertToBlock(followersCount));
            values.Add(ConvertToBlock(followersGrow));
            values.Add(ConvertToBlock(profileViews));
            values.Add(ConvertToBlock(engagementCount) + "%");
            SetBlocksMetrics(canvas, values, 105, height - 130);
        }
        public void FollowersEngagements(PdfCanvas canvas, float height, List<dynamic> growFollowers, List<PostStatistics> posts)
        {
            Dictionary<DateTime, long> values = GetControllPoints(
                growFollowers.OrderBy(x => x.end_time).Select(x => (DateTime)x.end_time).ToArray(),
                growFollowers.OrderBy(x => x.end_time).Select(x => (long)x.value).ToArray(),
                6.0);
            
            Dictionary<DateTime, long> engagements = GetControllPoints(
                posts.OrderBy(x => x.timestamp).Select(x => x.timestamp).ToArray(),
                posts.OrderBy(x => x.timestamp).Select(x => x.engagement).ToArray(), 
                6.0);
            
            SetGraphicMetrics(canvas, values, 70, height - 195);
            SetScatterPlot(canvas, values.Values.ToList(), 100, height - 190);

            SetGraphicMetrics(canvas, engagements, 465, height - 195);
            SetScatterPlot(canvas, engagements.Values.ToList(), 495, height - 190);
        }
        public void CompareColumn(PdfCanvas canvas, float height, List<PostStatistics> last, List<PostStatistics> current)
        {
            List<Color> colors = new List<Color>();
            List<long> posts = new List<long>(), likes = new List<long>(), comments = new List<long>();
            posts.Add(last.Count);
            posts.Add(current.Count);
            posts.Add(0);
            likes.Add(last.Sum(x => x.likeCount));
            likes.Add(current.Sum(x => x.likeCount));
            likes.Add(0);
            comments.Add(last.Sum(x => x.commentsCount));
            comments.Add(current.Sum(x => x.commentsCount));
            comments.Add(0);

            colors.Add(new DeviceCmyk(0, 2, 2, 82));    // grey
            colors.Add(new DeviceCmyk(0, 0, 0, 29));    // white grey
            colors.Add(new DeviceCmyk(98, 67, 0, 83));  // white blue
            colors.Add(new DeviceCmyk(38, 26, 0, 11));  // blue
            
            canvas.ConcatMatrix(1, 0, 0, 1, 70, height - 368);
            SetGraphicMetricsY(canvas, -45, 3, posts.ToList());
            canvas.ConcatMatrix(1, 0, 0, 1, -(70), -(height - 368));
            
            canvas.ConcatMatrix(1, 0, 0, 1, 335, height - 368);
            SetGraphicMetricsY(canvas, -45, 3, likes.ToList());
            canvas.ConcatMatrix(1, 0, 0, 1, -(335), -(height - 368));
            
            canvas.ConcatMatrix(1, 0, 0, 1, 595, height - 368);
            SetGraphicMetricsY(canvas, -45, 3, comments.ToList());
            canvas.ConcatMatrix(1, 0, 0, 1, -(595), -(height - 368));
            
            DrawCompareColumn(canvas, colors, posts, 150, height - 365);

            colors[2] = new DeviceCmyk(0, 96, 9, 58);                       // white pink
            colors[3] = new DeviceCmyk(0, 26, 3, 4);                        // pink
            
            DrawCompareColumn(canvas, colors, likes, 415, height - 365);

            colors[2] = new DeviceCmyk(80, 0, 100, 77);                     // white green
            colors[3] = new DeviceCmyk(40, 0, 48, 31);                      // green
            
            DrawCompareColumn(canvas, colors, comments, 675, height - 365);
        }
        public void LikesComments(PdfCanvas canvas, float height, List<PostStatistics> posts)
        {
            int yPosition = 528;
            Dictionary<DateTime, long> likes = GetControllPoints(posts.Select(p => p.timestamp).ToArray(),
                posts.Select(p => p.likeCount).ToArray(), 6.0);
            
            Dictionary<DateTime, long> comments = GetControllPoints(posts.Select(p => p.timestamp).ToArray(),
                posts.Select(p => (long)p.commentsCount).ToArray(), 6.0);
            
            SetGraphicMetrics(canvas, likes, 70, height - yPosition);
            SetScatterPlot(canvas, likes.Values.ToList(), 100, height - yPosition);
            
            SetGraphicMetrics(canvas, comments, 465, height - yPosition);
            SetScatterPlot(canvas, comments.Values.ToList(), 495, height - yPosition);
        }
        public void PostSection(PdfCanvas canvas, long postEngagement, long savedPosts)
        {
            int y = 845;  
            int x = 110;
            List<string> valuesFirstBlocks = new List<string>();

            canvas.ConcatMatrix(1, 0, 0, 1, x, y);
            valuesFirstBlocks.Add(ConvertToBlock(postEngagement) + "%");
            valuesFirstBlocks.Add(ConvertToBlock(savedPosts));
            SetBlocksMetrics(canvas, valuesFirstBlocks, 0, 0);
            canvas.ConcatMatrix(1, 0, 0, 1, -x, -y);
        }
        public void ActivityProfile(PdfCanvas canvas, List<DayStatistics> days)
        {
            int xPosition = 110, yPosition = 721;
            List<string> valuesFirstBlocks = new List<string>();
            valuesFirstBlocks.Add(ConvertToBlock(days.Sum(x => x.profileViews)));
            valuesFirstBlocks.Add(ConvertToBlock(days.Sum(x => x.websiteClicks)));
            valuesFirstBlocks.Add(ConvertToBlock(days.Sum(x => x.emailContacts)));
            valuesFirstBlocks.Add(ConvertToBlock(days.Sum(x => x.getDirectionsClicks)));
            SetBlocksMetrics(canvas, valuesFirstBlocks, xPosition, yPosition);
        }
        public void OnlineFollowersBlocks(PdfCanvas canvas, List<OnlineFollowers> onlineFollowers, long followersCount, DateTime startingAt)
        {
            int x = 78, y = 530;
            
            int[,] onlineValues = GetDoubleMassiveOnlineFollowers(onlineFollowers, startingAt);
            ChangeOnPercentOnlineFollowers(ref onlineValues, followersCount);
            List<DayOfWeek> daysNumbers = GetDaysOfWeek(startingAt);
            List<string> daysOfWeekNames = SetNameDaysOfWeek(daysNumbers, 0);
            
            YMetricsOnlineFollowers(canvas, x - 50, y - 212, daysOfWeekNames);
            XMetricsOnlineFollowers(canvas, x - 38, y - 270);

            BlocksOnlineFollowers(canvas, onlineValues, x, y + 102);
        }
        public void PicturePosts(PdfDocument pdf, List<PostStatistics> posts)
        {
            int imageX = -30, blockX = 184, index = 2;
            float imageHeight = 0, imageWidth = 0;
            Document document = new Document(pdf);

            for (int i = 0; i < posts.Count && i < 4; i++) {       
                
                Image image = new Image(ImageDataFactory.Create(new Uri(posts[i].postUrl)));
                imageWidth = image.GetImageWidth(); 
                imageHeight = image.GetImageHeight(); 
                while (imageWidth > 320 || imageHeight > 280) {
                    imageWidth = image.GetImageWidth() / index; 
                    imageHeight = image.GetImageHeight() / index; 
                    ++index;
                }
                index = 2;
                image.SetWidth(imageWidth);
                image.SetHeight(imageHeight);
                
                image.SetFixedPosition(imageX + (blockX * i), -1060 + (i * 140));

                Rectangle rectangle = new Rectangle(blockX * i, -1060 + (i * 140), blockX - 4, 140);
                PdfFormXObject template = new PdfFormXObject(rectangle);
                Canvas canvas = new Canvas(template, pdf);
                canvas.Add(image);
                Image croppedImage = new Image(template);
                document.Add(croppedImage);
            }
        }
        public void PictureStories(PdfDocument pdf, List<StoryStatistics> stories)
        {
            int imageX = -30, blockX = 184;
            Document document = new Document(pdf);

            for (int i = 0; i < stories.Count && i < 4; i++) {       
                
                Image image = new Image(ImageDataFactory.Create(new Uri(stories[i].storyUrl)));
                image.SetWidth(image.GetImageWidth() / 5);
                image.SetHeight(image.GetImageHeight() / 5);
                image.SetFixedPosition(imageX + (blockX * i), -1235 + (i * 140));

                Rectangle rectangle = new Rectangle(blockX * i, -1235 + (i * 140), blockX - 4, 140);
                PdfFormXObject template = new PdfFormXObject(rectangle);
                Canvas canvas = new Canvas(template, pdf);
                canvas.Add(image);
                Image croppedImage = new Image(template);
                document.Add(croppedImage);
            }
        }
        public void SetBlocksMetrics(PdfCanvas canvas, List<string> values, float xPosition, float yPosition)
        {
            int xStep = 127, xInit = 0, yInit = 0;
            canvas.ConcatMatrix(1, 0, 0, 1, xPosition, yPosition);
            foreach(string value in values) {
                canvas.BeginText().SetFontAndSize(PdfFontFactory.CreateFont(StandardFonts.HELVETICA), 16)
                    .MoveText((xInit - (value.Length / 2 * 6)), yInit);
                canvas.NewlineShowText(value);
                xInit += xStep;
            }
            canvas.EndText();
            canvas.ConcatMatrix(1, 0, 0, 1, -xPosition, -yPosition);
        }
        public int[,] GetDoubleMassiveOnlineFollowers(List<OnlineFollowers> onlineFollowers, DateTime startingAt)
        {
            long value;
            int[,] values = new int[7, 24];
            DateTime start;

            if (onlineFollowers.Count != 0) {
                start = new DateTime(startingAt.Year, startingAt.Month, startingAt.Day, 0, 0 ,0);

                for (int i = 0; i < 7; i++) {
                    for (int j = 0; j < 24; j++) {
                        value = onlineFollowers.Where(x 
                            => x.endTime.Year == start.Year 
                            && x.endTime.Month == start.Month 
                            && x.endTime.Day == start.Day 
                            && x.endTime.Hour == start.Hour)
                            .Select(x => x.value).FirstOrDefault();
                        values[i, j] = (int)value;
                        start = start.AddHours(1);
                        value = 0;
                    }
                }
            }
            return values;
        }
        public void ChangeOnPercentOnlineFollowers(ref int[,] onlineValues, long followerCount)
        {
            for (int i = 0; i < 7; i++) {
                for (int j = 0; j < 24; j++)
                    onlineValues[i, j] = getter.GetColorOnlineFollowers(followerCount, onlineValues[i, j]);
            }
        }
        public List<DayOfWeek> GetDaysOfWeek(DateTime startingAt)
        {
            List<DayOfWeek> dayOfWeekNumber = new List<DayOfWeek>();
            for (int i = 0; i < 7; i++) {
                if (!dayOfWeekNumber.Contains(startingAt.DayOfWeek))
                    dayOfWeekNumber.Add(startingAt.DayOfWeek);
                startingAt = startingAt.AddDays(1);
            }
            return dayOfWeekNumber;
        }
        public List<string> SetNameDaysOfWeek(List<DayOfWeek> dayOfWeekNumber, sbyte language)
        {
            List<string> dayOfWeek = new List<string>();
            for (int i = 0; i < dayOfWeekNumber.Count; i++) {
                switch (language) {
                    case 0: dayOfWeek.Add(GetEnglishDayOfWeek(dayOfWeekNumber[i]));
                        break;
                    case 1: dayOfWeek.Add(GetRussianDayOfWeek(dayOfWeekNumber[i]));
                        break;
                    default: break;
                }
            }
            return dayOfWeek;
        }
        public string GetEnglishDayOfWeek(DayOfWeek day)
        {
            switch(day) {
                case DayOfWeek.Sunday: return "Sn.";
                case DayOfWeek.Monday: return "Mn.";
                case DayOfWeek.Tuesday: return "Ts.";
                case DayOfWeek.Wednesday: return "Wn.";
                case DayOfWeek.Thursday: return "Th.";
                case DayOfWeek.Friday: return "Fr.";
                case DayOfWeek.Saturday: return "St.";
                default: return "";
            }
        }
        public string GetRussianDayOfWeek(DayOfWeek day)
        {
            switch(day) {
                case DayOfWeek.Sunday: return "Вс.";
                case DayOfWeek.Monday: return "Пн.";
                case DayOfWeek.Tuesday: return "Вт.";
                case DayOfWeek.Wednesday: return "Ср.";
                case DayOfWeek.Thursday: return "Чт.";
                case DayOfWeek.Friday: return "Пт.";
                case DayOfWeek.Saturday: return "Сб.";
                default: return "";
            }
        }
        public void YMetricsOnlineFollowers(PdfCanvas canvas, int xPosition, int yPosition, List<string> days)
        {
            int yStep = 17, y = yPosition;
            
            canvas.ConcatMatrix(1, 0, 0, 1, xPosition, yPosition);
            for (int i = 0; i < days.Count; i++) {
                canvas.BeginText().SetFontAndSize(PdfFontFactory.CreateFont(StandardFonts.HELVETICA), 8)
                    .MoveText(xPosition, y);
                canvas.NewlineShowText(days[i]);
                y -= yStep;
            }
            canvas.EndText();
            canvas.ConcatMatrix(1, 0, 0, 1, -xPosition, -yPosition);
        }
        public void XMetricsOnlineFollowers(PdfCanvas canvas, int xPosition, int yPosition)
        {
            int x = xPosition, xStep = 30;

            canvas.ConcatMatrix(1, 0, 0, 1, xPosition, yPosition);
            List<string> hours = new List<string>();
            for (int i = 0; i < 24; i++) {
                if (i < 10)
                    hours.Add("0" + i + ":00");
                else
                    hours.Add(i + ":00");
            }
            for (int i = 0; i < hours.Count; i++) {
                canvas.BeginText().SetFontAndSize(PdfFontFactory.CreateFont(StandardFonts.HELVETICA), 8)
                    .MoveText(x, yPosition);
                canvas.NewlineShowText(hours[i]);
                x += xStep;
            }
            canvas.EndText();
            canvas.ConcatMatrix(1, 0, 0, 1, -xPosition, -yPosition);
        }
        public void BlocksOnlineFollowers(PdfCanvas canvas, int[,] values, int xPosition, int yPosition)
        {
            double startX = 0, startY = 0;
            int xBlock = 27, yBlock = 14, xStep = xBlock + 3, yStep = -(yBlock + 3);

            canvas.SaveState();
            canvas.ConcatMatrix(1, 0, 0, 1, xPosition, yPosition);
            canvas.SetLineJoinStyle(PdfCanvasConstants.LineJoinStyle.ROUND);
            for (int i = 0; i < 7; i++) {
                for (int j = 0; j < 24; j++) {
                    canvas.Rectangle(new Rectangle((float)startX,(float)startY, xBlock, yBlock));
                    canvas.SetFillColor(GetOnlineFollowersColor(values[i, j]));
                    canvas.Fill();
                    startX += xStep;
                }
                startX -= xStep * 24;
                startY += yStep;
            }
            canvas.ConcatMatrix(1, 0, 0, 1, -xPosition, -(yPosition));            
            canvas.RestoreState();
            canvas.EndText();
        }
        public Color GetOnlineFollowersColor(int color)
        {
            switch(color){
                case 1: return new DeviceCmyk(26, 0, 36, 1);
                case 2: return new DeviceCmyk(35, 0, 47, 15);
                case 3: return new DeviceCmyk(43, 0, 60, 31);
                case 4: return new DeviceCmyk(53, 0, 73, 48);
                case 5: return new DeviceCmyk(60, 0, 84, 66);
                case 6: return new DeviceCmyk(70, 0, 100, 83);
                default: return new DeviceCmyk(26, 0, 36, 1);
            }
        }
        public void DrawCompareColumn(PdfCanvas canvas, List<Color> colors, List<long> values, double x, double y)
        {
            double startX = 0, startY = 0;
            int stepY = -46, levelY = 0;
            int colorsIndex = 0;            

            canvas.SaveState();
            canvas.ConcatMatrix(1, 0, 0, 1, x, y);
            canvas.SetLineJoinStyle(PdfCanvasConstants.LineJoinStyle.ROUND);

            for (int i = 0; i < values.Count - 1; i++) {
                levelY = CountValueLevel(values, values[i]);
                startY = levelY * stepY;

                canvas.Rectangle(new Rectangle((float)startX, 
                    (float)startY, 30, 2));
                canvas.SetFillColor(colors[colorsIndex++]);
                canvas.Fill();
                
                canvas.Rectangle(new Rectangle((float)startX, 
                    (float)startY, 30, (float) ((values.Count - levelY - 1) * stepY)));
                canvas.SetFillColor(colors[colorsIndex++]);
                canvas.Fill();
                startX += 40;
            }
            canvas.ConcatMatrix(1, 0, 0, 1, -x, -y);
            canvas.RestoreState();
            canvas.EndText();
        }
        public Dictionary<DateTime, long> GetControllPoints(DateTime[] days, long[] values, double pointCount)
        {
            int part, count = days.Length, step = 0;
            Dictionary<DateTime, long> points = new Dictionary<DateTime, long>();

            if (((float)count / pointCount) > 1.0) {
                part = count / (int)pointCount;
                for (int i = 0; i < (int)pointCount - 1; i++, step += part)
                    points.Add(days[step], values[step]);
                points.Add(days[days.Length - 1], values[values.Length - 1]);
                return points;
            }
            for (int i = 0; i < days.Length; i++)
                points.Add(days[i], values[i]);
            return points;
        }
        public long[] GetControllPoints(long[] values, double pointCount)
        {
            int part, count = values.Length, step = 0;
            long[] controllPoints = new long[(int)pointCount];

            if (((float)count / pointCount) > 1.0) {
                part = count / (int)pointCount;
                for (int i = 0; i < (int)pointCount - 1; i++, step += part)
                    controllPoints[i] = values[step];
                controllPoints[controllPoints.Length - 1] = values[values.Length - 1];
                return controllPoints;
            }
            for (int i = 0; i < pointCount - values.Length; i++)
                controllPoints[i] = 0;
            for (int i = (int)pointCount - values.Length; i < values.Length; i++)
                controllPoints[i] = values[i];
            return controllPoints;
        }
        /// <function>
        /// Default size of plot's step x = 60;
        /// </function>
        public void SetScatterPlot(PdfCanvas canvas, List<long> values, double x, double y)
        {
            double startX = 0, startY = 0;
            int step = -108, stepY;

            canvas.SaveState();
            canvas.ConcatMatrix(1, 0, 0, 1, x, y);
            canvas.SetLineJoinStyle(PdfCanvasConstants.LineJoinStyle.ROUND);

            for (int i = 0; i < values.Count - 1; i++) {
                stepY = step / values.Count;
                startY = CountValueLevel(values, values[i]) * stepY;
                canvas.MoveTo(startX, startY)
                    .LineTo(startX + 360 / values.Count, CountValueLevel(values, values[i + 1]) * stepY)
                    .Stroke();
                startX += 360 / values.Count;
            }
            canvas.ConcatMatrix(1, 0, 0, 1, -x, -y);
            canvas.RestoreState();
        }
        public void SetGraphicMetrics(PdfCanvas canvas, Dictionary<DateTime, long> values, double x, double y)
        {
            canvas.ConcatMatrix(1, 0, 0, 1, x, y);
            List<long> digitValues = RemoveExistValues(values.Values.ToList());
            SetGraphicMetricsY(canvas, -18, 6, digitValues);
            canvas.ConcatMatrix(1, 0, 0, 1, 40, -100);
            SetGraphicMetricsX(canvas, values.Keys.ToArray());
            canvas.ConcatMatrix(1, 0, 0, 1, -(x + 40), -(y - 100));
        }
        public List<long> RemoveExistValues(List<long> values)
        {
            List<long> unique = new List<long>();
            foreach (long value in values) {
                if (!unique.Contains(value))
                    unique.Add(value);
            }
            return unique;
        }
        public void SetGraphicMetricsY(PdfCanvas canvas, double yStep, sbyte stepCount, List<long> values)
        {
            double xPosition = 0, yPosition = 0, yRegion = yStep * stepCount;

            values = values.OrderByDescending(v => v).ToList();
            for (int i = 0; i < values.Count; i++) {
                if (values[i] == 0 && i != values.Count - 1) {}
                else {
                    canvas.BeginText().SetFontAndSize(PdfFontFactory.CreateFont(StandardFonts.HELVETICA), 8)
                        .MoveText((xPosition - (values[i].ToString().Length / 2 * 4)), yPosition);
                    canvas.NewlineShowText(values[i].ToString());
                }
                yPosition += yRegion / values.Count;
            }
            canvas.EndText();
        }
        public void SetGraphicMetricsX(PdfCanvas canvas, DateTime[] days)
        {
            int xPosition = 0, yPosition = 0;
            string line;

            for (int i = 0; i < days.Length; i++) {
                line = days[i].Month >= 10 ? days[i].Month.ToString() : "0" + days[i].Month.ToString();
                line += ".";
                line += days[i].Day >= 10 ? days[i].Day.ToString() : "0" + days[i].Day.ToString();
                line += ".";
                line += days[i].Year.ToString().Substring(2, 2);
                canvas.BeginText().SetFontAndSize(PdfFontFactory.CreateFont(StandardFonts.HELVETICA), 8)
                    .MoveText((xPosition - (line.Length / 2 * 6)), yPosition);
                canvas.NewlineShowText(line);
                xPosition += 360 / days.Length;
            }
        }
        public int CountValueLevel(List<long> values, long value)
        {
            if (value == 0)
                return values.Count - 1;
            values = values.OrderByDescending(v => v).ToList();
            for (int i = 0; i < values.Count; i++) {
                if (values[i] == value) 
                    return i;
            }
            return 0;
        }
        public string ConvertToBlock(long value)
        {
            if (value > 999 && value < 100000)
                return (value / 1000).ToString() + "." + (value % 1000 / 100).ToString() + "K";
            else if (value > 99999 && value < 1000000)
                return (value / 1000).ToString() + "K";
            else if (value > 999999)
                return (value / 1000000).ToString() + "." + 
                    (value % 1000000 / 100000).ToString() + "M";
            return value.ToString();
        }
    }
}