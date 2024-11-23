using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using iText.Layout;
using iText.Layout.Element;
using iText.Signatures;
using iText.Forms;
using iText.Kernel.Pdf;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.IO.Font.Constants;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Colors;
using iText.IO.Image;
using iText.Kernel.Pdf.Xobject;

namespace test_pdf
{
    class Program
    {
        public static string IN = Directory.GetCurrentDirectory() + "/" + "statistics.pdf";
        public static string OUT = Directory.GetCurrentDirectory() + "/" + "output.pdf";
        
        public static void Main(String[] args) 
        {
            FileInfo file = new FileInfo(IN);
            PdfReader reader = new PdfReader(file);
            PdfWriter writer = new PdfWriter(OUT);
            
            PdfDocument template = new PdfDocument(reader, writer);
            
            PdfAcroForm fields = PdfAcroForm.GetAcroForm(template, true);
            PdfPage page = template.GetPage(1);
            PdfCanvas canvas = new PdfCanvas(page);
            
            Rectangle rectangle = page.GetPageSize();
            PageSize ps = new PageSize(rectangle);
        
            TestFirstSection(canvas, ps);
            TestSecondSection(canvas, ps);
            TestThirdSection(canvas, ps);
            TestFouthSection(canvas, ps);
            TestFifthSection(canvas, ps);
            TestSixthSection(canvas, ps);
            TestSeventhSection(canvas, ps);
            TestEighthSection(template, ps);
            template.Close();
        }
        public static void TestFirstSection(PdfCanvas canvas, PageSize ps)
        {
            canvas.ConcatMatrix(1, 0, 0, 1, 0, ps.GetHeight());
            List<string> valuesFirstBlocks = new List<string>();
            valuesFirstBlocks.Add("256");
            valuesFirstBlocks.Add("3");
            valuesFirstBlocks.Add("2.2K");
            valuesFirstBlocks.Add("17.06%");
            SetFirstBlocks(canvas, ps, valuesFirstBlocks, 105, -130);
            canvas.ConcatMatrix(1, 0, 0, 1, 0, -ps.GetHeight());
        }
        public static void SetFirstBlocks(PdfCanvas canvas, PageSize ps, List<string> values, int xInit, int yInit)
        {
            int xStep = 127;

            foreach(string value in values) {
                SetTextUnit(ref canvas, ps, value, xInit, yInit);
                xInit += xStep;
            }
            canvas.EndText();
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static void TestFouthSection(PdfCanvas canvas, PageSize ps)
        {
            int yPosition = 528;
            int[] values = { 1500, 1770, 600, 2400, 2200, 2100};
            SetGraphicMetrics(canvas, ps, values, DateTime.Now.AddDays(-6), 70, ps.GetHeight() - yPosition);
            SetScatterPlot(canvas, ps, values, 100, ps.GetHeight() - yPosition);
            
            int[] valuesSecond = { 1100, 213, 360, 560, 800, 1300 }; 
            SetGraphicMetrics(canvas, ps, valuesSecond, DateTime.Now.AddDays(-6), 465, ps.GetHeight() - yPosition);
            SetScatterPlot(canvas, ps, valuesSecond, 495, ps.GetHeight() - yPosition);
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static void TestFifthSection(PdfCanvas canvas, PageSize ps)
        {
            int yPosition = 845;  
            int xPosition = 110;
            canvas.ConcatMatrix(1, 0, 0, 1, xPosition, yPosition);
            List<string> valuesFirstBlocks = new List<string>();
            valuesFirstBlocks.Add("14.04%");
            valuesFirstBlocks.Add("25");
            SetFirstBlocks(canvas, ps, valuesFirstBlocks, 0, 0);
            canvas.ConcatMatrix(1, 0, 0, 1, -xPosition, -yPosition);
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static void TestSixthSection(PdfCanvas canvas, PageSize ps)
        {
            int xPosition = 110;
            int yPosition = 721;
            canvas.ConcatMatrix(1, 0, 0, 1, xPosition, yPosition);
            List<string> valuesFirstBlocks = new List<string>();
            valuesFirstBlocks.Add("1205");
            valuesFirstBlocks.Add("46");
            valuesFirstBlocks.Add("41");
            valuesFirstBlocks.Add("15");
            SetFirstBlocks(canvas, ps, valuesFirstBlocks, 0, 0);
            canvas.ConcatMatrix(1, 0, 0, 1, -xPosition, -(yPosition));
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static string urlImage1 = "https://instagram.fdnk3-2.fna.fbcdn.net/v/t51.2885-15/e35/82493363_164036438272440_1309498389338642742_n.jpg?_nc_ht=instagram.fdnk3-2.fna.fbcdn.net&_nc_cat=100&_nc_ohc=sobwYkrkqiAAX9vJ3R1&oh=16d811241f7f9dd6f74e54b2cf67a40c&oe=5E8B446C";
        public static string urlImage2 = "https://instagram.fdnk3-2.fna.fbcdn.net/v/t51.2885-15/e35/80006578_246580999642046_8726402782685908086_n.jpg?_nc_ht=instagram.fdnk3-2.fna.fbcdn.net&_nc_cat=102&_nc_ohc=BzLLak6nr5IAX9hi2Gd&oh=301dcec418ba56792d4dd80915e84593&oe=5E8B758D";
        public static string urlImage3 = "https://instagram.fdnk3-2.fna.fbcdn.net/v/t51.2885-15/e35/81322010_607760513371959_3188899754587643645_n.jpg?_nc_ht=instagram.fdnk3-2.fna.fbcdn.net&_nc_cat=103&_nc_ohc=AT3l6kZXsloAX9oysX8&oh=5c202be2d656dfbb008a937ee1bf898e&oe=5E8C63FD";
        public static string urlImage4 = "https://instagram.fdnk3-1.fna.fbcdn.net/v/t51.2885-15/e35/80410324_458771888123062_6640639507966742584_n.jpg?_nc_ht=instagram.fdnk3-1.fna.fbcdn.net&_nc_cat=110&_nc_ohc=K7hLuQB3kbgAX-bQP1W&oh=5106e0d3f1d88b5ed778e4668cca2404&oe=5E88F2BF";
        public static void TestEighthSection(PdfDocument pdf, PageSize ps)
        {
            // int blockWigth = 180;
            // int blockHeight = 140;
            // var document = new Document(pdf);
            // var igImage = new Image(ImageDataFactory.Create(new Uri(urlImage)));
            // float imageWigth = igImage.GetImageWidth();
            // float imageHeight = igImage.GetImageWidth();
            
            int imageX = -30;
            int blockX = 184;

            List<string> images = new List<string>();
            images.Add(urlImage1);
            images.Add(urlImage2);
            images.Add(urlImage3);
            images.Add(urlImage4);

            Document document = new Document(pdf);

            for (int i = 0; i < 4; i++) {
                
                Image image = new Image(ImageDataFactory.Create(new Uri(images[i])));
                image.SetWidth(image.GetImageWidth() / 5); // / 5
                image.SetHeight(image.GetImageHeight() / 5); // / 5
                image.SetFixedPosition(imageX + (blockX * i), -1060 + (i * 140));

                Rectangle rectangle = new Rectangle(blockX * i, -1060 + (i * 140), blockX - 4, 140);// y = -1060
                PdfFormXObject template = new PdfFormXObject(rectangle);
                Canvas canvas = new Canvas(template, pdf);
                canvas.Add(image);
                Image croppedImage = new Image(template);
                document.Add(croppedImage);
            }
            // document.close();



            // while (imageHeight > blockHeight) {

            // }
            // Console.WriteLine(igImage.GetImageWidth());
            // Console.WriteLine(igImage.GetImageHeight());
            
            // igImage.ScaleAbsolute(180, 140);
            // // igImage.SetWidth(400); //.SetAbsolutePosition(12, 300);
            // // igImage.SetHeight(400);            
            // igImage.SetFixedPosition(55, 345);
            // // canvas.AddImage(igImage, 1,0,0,1, xPosition, yPosition);
            // document.Add(igImage);
            // // canvas.AddImage()
            // // BlocksOnlineFollowers(canvas, ps, values, xPosition, yPosition);
            // // canvas.AddImage(igImage, 1, 0, 0, 1, 105, 105);
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public struct OnlineFollowers 
        {
            public int value;
            public DateTime date;
        } 
        
        public static void TestSeventhSection(PdfCanvas canvas, PageSize ps)
        {
            int xPosition = 78;
            int yPosition = 530;
            DateTime date = DateTime.Now.AddDays(-7);
            List<OnlineFollowers> values = new List<OnlineFollowers>(); 
            
            for (int i = 0; i < 7; i++) {
                for (int j = 0; j < 24; j++) {
                    OnlineFollowers online = new OnlineFollowers();
                    online.value = i * 10;
                    online.date = date.AddHours(j);
                    values.Add(online);
                }
            }
            BlocksOnlineFollowers(canvas, ps, values, xPosition, yPosition);
            YMetricsOnlineFollowers(canvas, ps, xPosition - 50, yPosition - 212);
            XMetricsOnlineFollowers(canvas, ps, xPosition - 38, yPosition - 270);
        }
        public static void YMetricsOnlineFollowers(PdfCanvas canvas, PageSize ps, int xPosition, int yPosition)
        {
            int yStep = 17;
            int y = yPosition;

            canvas.ConcatMatrix(1, 0, 0, 1, xPosition, yPosition);
            List<string> days = new List<string>();
            days.Add("Mn.");days.Add("Ts.");days.Add("Wn.");days.Add("Th.");days.Add("Fr.");days.Add("St.");days.Add("Sn.");
            // days.Add("Пн.");days.Add("Вт.");days.Add("Ср.");days.Add("Чт.");days.Add("Пт.");days.Add("Сб.");days.Add("Вс.");
            
            for (int i = 0; i < days.Count; i++) {
                canvas.BeginText().SetFontAndSize(PdfFontFactory.CreateFont(StandardFonts.HELVETICA), 8)
                    .MoveText(xPosition, y);
                canvas.NewlineShowText(days[i]);
                y -= yStep;
            }
            canvas.EndText();
            canvas.ConcatMatrix(1, 0, 0, 1, -xPosition, -yPosition);
        }
        public static void XMetricsOnlineFollowers(PdfCanvas canvas, PageSize ps, int xPosition, int yPosition)
        {
            int x = xPosition;
            int xStep = 30;

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
        
        public static void BlocksOnlineFollowers(PdfCanvas canvas, PageSize ps, List<OnlineFollowers> values, int xPosition, int yPosition)
        {
            double startX = 0;
            double startY = 0;
            int xBlock = 27;
            int yBlock = 14;

            int xStep = xBlock + 3;
            int yStep = -(yBlock + 3);
            

            Color greenColor = new DeviceCmyk(82, 0, 98, 1);

            canvas.SaveState();
            canvas.ConcatMatrix(1, 0, 0, 1, xPosition, yPosition);
            canvas.SetLineJoinStyle(PdfCanvasConstants.LineJoinStyle.ROUND);

            for (int i = 0; i < values.Count / 24; i++) {
                for (int j = i * 24; j < (i + 1) * 24; j++) {
                    canvas.Rectangle(new Rectangle((float)startX, 
                        (float)startY, xBlock, yBlock));
                    canvas.SetFillColor(greenColor);
                    canvas.Fill();
                    startX += xStep;
                }
                startX -= xStep * 24;
                startY -= yStep; 
            }
            canvas.ConcatMatrix(1, 0, 0, 1, -xPosition, -(yPosition));            
            canvas.RestoreState();
            canvas.EndText();
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static void TestThirdSection(PdfCanvas canvas, PageSize ps)
        {
            int[] valuesPosts = { 12, 6, 0 };
            int[] valuesLikes = { 300, 900, 600 };
            int[] valuesComments = { 30, 90, 150 };
            canvas.ConcatMatrix(1, 0, 0, 1, 70, ps.GetHeight() - 368);
            SetGraphicMetricsY(canvas, ps, -45, valuesPosts);
            canvas.ConcatMatrix(1, 0, 0, 1, -(70), -(ps.GetHeight() - 368));
            
            canvas.ConcatMatrix(1, 0, 0, 1, 335, ps.GetHeight() - 368);
            SetGraphicMetricsY(canvas, ps, -45, valuesLikes);
            canvas.ConcatMatrix(1, 0, 0, 1, -(335), -(ps.GetHeight() - 368));
            
            canvas.ConcatMatrix(1, 0, 0, 1, 595, ps.GetHeight() - 368);
            SetGraphicMetricsY(canvas, ps, -45, valuesComments);
            canvas.ConcatMatrix(1, 0, 0, 1, -(595), -(ps.GetHeight() - 368));
            
            DrawGraphicThirdSection(canvas, ps, valuesPosts, 150, ps.GetHeight() - 365);
            DrawGraphicThirdSection(canvas, ps, valuesLikes, 415, ps.GetHeight() - 365);
            DrawGraphicThirdSection(canvas, ps, valuesComments, 675, ps.GetHeight() - 365);
            
            // SetScatterPlot(canvas, ps, values, 100, ps.GetHeight() - 190);
            
            // int[] valuesSecond = { 100, 123, 360, 189, 233, 288 }; 
            // SetGraphicMetrics(canvas, ps, valuesSecond, DateTime.Now.AddDays(-6), 465, ps.GetHeight() - 195);
            // SetScatterPlot(canvas, ps, valuesSecond, 495, ps.GetHeight() - 190);
        }
        public static void DrawGraphicThirdSection(PdfCanvas canvas, PageSize ps, int[] values, double x, double y)
        {
            double startX = 0;
            double startY = 0;
            int stepY = -46;
            int levelY = 0;
            Color greenColor = new DeviceCmyk(21, 0, 25, 7);
            Color blueColor = new DeviceCmyk(100, 95, 0, 13);

            canvas.SaveState();
            canvas.ConcatMatrix(1, 0, 0, 1, x, y);
            canvas.SetLineJoinStyle(PdfCanvasConstants.LineJoinStyle.ROUND);

            for (int i = 0; i < values.Length - 1; i++) {
                levelY = CountValueLevel(values, values[i]);
                startY = levelY * stepY;

                canvas.Rectangle(new Rectangle((float)startX, 
                    (float)startY, 30, 2));
                canvas.SetFillColor(blueColor);
                canvas.Fill();
                
                // canvas.MoveTo(startX, startY)
                //     .LineTo(startX + 30, startY)
                //     .Stroke();

                canvas.Rectangle(new Rectangle((float)startX, 
                    (float)startY, 30, (float) ((values.Length - levelY - 1) * stepY)));
                canvas.SetFillColor(greenColor);
                canvas.Fill();
                startX += 40;

            }
            canvas.ConcatMatrix(1, 0, 0, 1, -x, -y);
            canvas.RestoreState();
            canvas.EndText();
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static void TestSecondSection(PdfCanvas canvas, PageSize ps)
        {
            int[] values = { 256, 159, 0, 233, 140, 242 };
            SetGraphicMetrics(canvas, ps, values, DateTime.Now.AddDays(-6), 70, ps.GetHeight() - 195);
            SetScatterPlot(canvas, ps, values, 100, ps.GetHeight() - 190);
            
            int[] valuesSecond = { 100, 123, 360, 189, 233, 288 }; 
            SetGraphicMetrics(canvas, ps, valuesSecond, DateTime.Now.AddDays(-6), 465, ps.GetHeight() - 195);
            SetScatterPlot(canvas, ps, valuesSecond, 495, ps.GetHeight() - 190);
        }
        /// <function>
        /// Default size of plot's step x = 60;
        /// </function>
        public static void SetScatterPlot(PdfCanvas canvas, PageSize ps, int[] values, double x, double y)
        {
            double startX = 0;
            double startY = 0;
            int stepY = -18;

            canvas.SaveState();
            canvas.ConcatMatrix(1, 0, 0, 1, x, y);
            canvas.SetLineJoinStyle(PdfCanvasConstants.LineJoinStyle.ROUND);

            for (int i = 0; i < 5; i ++) {
                startY = CountValueLevel(values, values[i]) * stepY;
                canvas.MoveTo(startX, startY)
                    .LineTo(startX + 60, CountValueLevel(values, values[i + 1]) * stepY)
                    .Stroke();
                startX += 60;
            }
            canvas.ConcatMatrix(1, 0, 0, 1, -x, -y);
            canvas.RestoreState();
        }
        public static int CountValueLevel(int[] values, int value)
        {
            values = values.OrderByDescending(v => v).ToArray();
            for (int i = 0; i < values.Length; i++) {
                if (values[i] == value) 
                    return i;
            }
            return 0;
        }
        public static void SetGraphicMetrics(PdfCanvas canvas, PageSize ps, int[] values, DateTime firstDay, double x, double y)
        {
            canvas.ConcatMatrix(1, 0, 0, 1, x, y);
            SetGraphicMetricsY(canvas, ps, -18, values);
            canvas.ConcatMatrix(1, 0, 0, 1, 40, -100);
            SetGraphicMetricsX(canvas, ps, firstDay);
            canvas.ConcatMatrix(1, 0, 0, 1, -(x + 40), -(y - 100));
        }
        public static void SetGraphicMetricsY(PdfCanvas canvas, PageSize ps, double yStep, int[] values)
        {
            double xPosition = 0;
            double yPosition = 0;
            values = values.OrderByDescending(v => v).ToArray();
                
            for (int i = 0; i < values.Length; i++) {
                canvas.BeginText().SetFontAndSize(PdfFontFactory.CreateFont(StandardFonts.HELVETICA), 8)
                    .MoveText((xPosition - (values[i].ToString().Length / 2 * 4)), yPosition);
                canvas.NewlineShowText(values[i].ToString());
                yPosition += yStep;
            }
            canvas.EndText();
        }
        public static void SetGraphicMetricsX(PdfCanvas canvas, PageSize ps, DateTime firstDay)
        {
            int xPosition = 0;
            int yPosition = 0;
            
            for (int i = 0; i < 6; i++) {
                canvas.BeginText().SetFontAndSize(PdfFontFactory.CreateFont(StandardFonts.HELVETICA), 8)
                    .MoveText((xPosition - (firstDay.ToShortDateString().Length / 2 * 6)), yPosition);
                canvas.NewlineShowText(firstDay.ToShortDateString());
                firstDay = firstDay.AddDays(1);
                xPosition += 60;
            }
        }
        
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static void TestBlocks(PdfCanvas canvas, PageSize ps)
        {
            canvas.ConcatMatrix(1, 0, 0, 1, 0, ps.GetHeight());
            List<string> valuesFirstBlocks = new List<string>();
            valuesFirstBlocks.Add("256");
            valuesFirstBlocks.Add("3");
            valuesFirstBlocks.Add("2.2K");
            valuesFirstBlocks.Add("17.06%");
            SetFirstBlocks(canvas, ps, valuesFirstBlocks, 105, -130);
            canvas.ConcatMatrix(1, 0, 0, 1, 0, -ps.GetHeight());
        }
        public static void SetSecondBlocks(ref PdfCanvas canvas, PageSize ps)
        {
            
            canvas.ConcatMatrix(1, 0, 0, 1, 0, ps.GetHeight());
            // SetTextUnit(ref canvas, ps, "1", 110, -730);    // отсчёт с одного символа 110, -730;
            // SetTextUnit(ref canvas, ps, "1",  240, -730);   // отсчёт с одного символа 240, -730;
            
            // SetTextUnit(ref canvas, ps, "1", 110, -855);    // отсчёт с одного символа 110, -855;
            // SetTextUnit(ref canvas, ps, "1",  240, -855);   // отсчёт с одного символа 240, -855;
            // SetTextUnit(ref canvas, ps, "1", 367, -855);    // отсчёт с одного символа 367, -855;
            // SetTextUnit(ref canvas, ps, "1", 494, -855);    // отсчёт с одного символа 494, -855;
            SetTextUnit(ref canvas, ps, "14.06%", 110, -730);
            SetTextUnit(ref canvas, ps, "25",  240, -730);
            
            SetTextUnit(ref canvas, ps, "1205", 110, -855);
            SetTextUnit(ref canvas, ps, "46",  240, -855);
            SetTextUnit(ref canvas, ps, "41", 367, -855);
            SetTextUnit(ref canvas, ps, "15", 494, -855);
        }
        public static void SetSecondGraphicMetrics(ref PdfCanvas canvas, PageSize ps)
        {
            int xGraphic = 465;
            int yGraphic = -193;
            int numberGraphic = 259;
            for (int i = 0; i < 6; i++) {
                SetGraphicText(ref canvas, ps, numberGraphic.ToString(), xGraphic, yGraphic);
                numberGraphic -= 3;
                yGraphic += -18;
            }
        }
        public static void SetTextUnit(ref PdfCanvas canvas, PageSize ps, string number, double x, double y)
        {
            canvas.BeginText().SetFontAndSize(PdfFontFactory.CreateFont(StandardFonts.HELVETICA), 16)
                .MoveText((x - (number.Length / 2 * 6)), y);
            canvas.NewlineShowText(number.ToString());
        }
        public static void SetGraphicText(ref PdfCanvas canvas, PageSize ps, string number, double x, double y)
        {
            canvas.BeginText().SetFontAndSize(PdfFontFactory.CreateFont(StandardFonts.HELVETICA), 8)
                .MoveText((x - (number.Length / 2 * 6)), y);
            canvas.NewlineShowText(number.ToString());
        }
        public void SetText(PdfCanvas canvas, PageSize ps)
        {
            IList<String> text = new List<String>();
            text.Add("         Episode V         ");
            text.Add("  THE EMPIRE STRIKES BACK  ");
            text.Add("It is a dark time for the");
            text.Add("Rebellion. Although the Death");
            text.Add("Star has been destroyed,");
            text.Add("Imperial troops have driven the");
            text.Add("Rebel forces from their hidden");
            text.Add("base and pursued them across");
            text.Add("the galaxy.");
            text.Add("Evading the dreaded Imperial");
            text.Add("Starfleet, a group of freedom");
            text.Add("fighters led by Luke Skywalker");
            text.Add("has established a new secret");
            text.Add("base on the remote ice world");
            text.Add("of Hoth...");
            //Replace the origin of the coordinate system to the top left corner
            canvas.ConcatMatrix(1, 0, 0, 1, 0, ps.GetHeight());
            canvas.BeginText().SetFontAndSize(PdfFontFactory.CreateFont(StandardFonts.COURIER_BOLD), 14).SetLeading(14 * 1.2f).MoveText(70, -40);
            foreach (String s in text) {
                //Add text and move to the next line
                canvas.NewlineShowText(s);
            }
            canvas.EndText();
        }
        public static void SetGraphic(ref PdfCanvas canvas, PageSize ps)
        {
            canvas.SaveState();
            
            // Change the page's coordinate system so that 0,0 is at the center
            canvas.ConcatMatrix(1, 0, 0, 1, ps.GetWidth() / 2, ps.GetHeight() / 2);
            
            // When joining lines we want them to use a rounded corner
            canvas.SetLineJoinStyle(PdfCanvasConstants.LineJoinStyle.ROUND);
            
            // Draw X axis
            canvas.MoveTo(-(ps.GetWidth() / 2 - 15), 0)
                    .LineTo(ps.GetWidth() / 2 - 15, 0)
                    .Stroke();
            
            //Draw Y axis
            canvas.MoveTo(0, -(ps.GetHeight() / 2 - 15))
                    .LineTo(0, ps.GetHeight() / 2 - 15)
                    .Stroke();
            
            //Draw X axis arrow
            canvas.MoveTo(ps.GetWidth() / 2 - 25, -10)
                    .LineTo(ps.GetWidth() / 2 - 15, 0)
                    .LineTo(ps.GetWidth() / 2 - 25, 10)
                    .Stroke();
            
            //Draw Y axis arrow
            canvas.MoveTo(-10, ps.GetHeight() / 2 - 25)
                    .LineTo(0, ps.GetHeight() / 2 - 15)
                    .LineTo(10, ps.GetHeight() / 2 - 25)
                    .Stroke();
            
            //Draw X serif
            for (int i = -((int)ps.GetWidth() / 2 - 61); i < ((int)ps.GetWidth() / 2 - 60); i += 40)
                canvas.MoveTo(i, 5).LineTo(i, -5);
            //Draw Y serif
            for (int j = -((int)ps.GetHeight() / 2 - 57); j < ((int)ps.GetHeight() / 2 - 56); j += 40)
                canvas.MoveTo(5, j).LineTo(-5, j);
            canvas.Stroke();
            
            //"Restore" our "backup" which resets any changes that the above made
            canvas.RestoreState();
        }
    }
}
