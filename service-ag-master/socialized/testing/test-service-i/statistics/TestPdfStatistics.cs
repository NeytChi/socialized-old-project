using System;
using System.Collections.Generic;
using Serilog;
using NUnit;
using NUnit.Framework;

using database.context;
using Models.Statistics;
using InstagramService.Statistics;

namespace Testing.Service
{
    [TestFixture]
    public class TestPdfStatistics
    {
        public Context context;
        public PdfStatistics pdf;
        public TestPdfStatistics()
        {
            this.context = TestMockingContext.GetContext();
            GetterStatistics getter = new GetterStatistics(context);
            this.pdf = new PdfStatistics(new LoggerConfiguration().CreateLogger(), getter);
        }
        [Test]
        public void GeneratePDF()
        {
            
        }   

        [Test]
        public void GetControllPoints_Littel_Mass()
        {
            int count = 3;
            DateTime[] days = new DateTime[count];
            long[] values = new long[count];
            for (int i = 0; i < count; i++ ) {
                days[i] = DateTime.Now; values[i] = i;
            }
            Assert.AreEqual(pdf.GetControllPoints(days, values, (double) count).Count, count);
        }   
        [Test]
        public void GetControllPoints_Even_Mass()
        {
            int count = 6;
            DateTime[] days = new DateTime[count];
            long[] values = new long[count];
            for (int i = 0; i < count; i++ ) {
                days[i] = DateTime.Now; values[i] = i;
            }
            Assert.AreEqual(pdf.GetControllPoints(days, values, count).Count, count);
        }   
        [Test]
        public void GetControllPoints_Big_Mass()
        {
            int count = 16;
            DateTime[] days = new DateTime[count];
            long[] values = new long[count];
            for (int i = 0; i < count; i++ ) {
                days[i] = DateTime.Now; values[i] = i;
            }
            Assert.AreEqual(pdf.GetControllPoints(days, values, 6).Count, 6);
        }  
        [Test]
        public void GetControllPoints_More_Big_Mass()
        {
            int count = 32;
            DateTime[] days = new DateTime[count];
            long[] values = new long[count];
            for (int i = 0; i < count; i++ ) {
                days[i] = DateTime.Now; values[i] = i;
            }
            Assert.AreEqual(pdf.GetControllPoints(days, values, 6).Count, 6);
        }  
        [Test]
        public void GetControllPoints_Exception_Big_Mass()
        {
            int count = 8;
            DateTime[] days = new DateTime[count];
            long[] values = new long[count];
            for (int i = 0; i < count; i++ ) {
                days[i] = DateTime.Now; values[i] = i;
            }
            Assert.AreEqual(pdf.GetControllPoints(days, values, 6).Count, 6);
        }  
        [Test]
        public void ConvertToBlock()
        {
            Assert.AreEqual(pdf.ConvertToBlock(0), "0");
            Assert.AreEqual(pdf.ConvertToBlock(1), "1");
            Assert.AreEqual(pdf.ConvertToBlock(12), "12");
            Assert.AreEqual(pdf.ConvertToBlock(123), "123");
            Assert.AreEqual(pdf.ConvertToBlock(1222), "1.2K");
            Assert.AreEqual(pdf.ConvertToBlock(12223), "12.2K");
            Assert.AreEqual(pdf.ConvertToBlock(122233), "122K");
            Assert.AreEqual(pdf.ConvertToBlock(1222333), "1.2M");
        }
        [Test]
        public void GetDoubleMassiveOnlineFollowers()
        {
            OnlineFollowers followers = new OnlineFollowers();
            followers.value = 25;
            followers.endTime = DateTime.Now;
            List<OnlineFollowers> online = new List<OnlineFollowers>();
            for (int i = 0; i < 24 * 7; i++, followers.endTime.AddHours(1))
                online.Add(followers);
            var result = pdf.GetDoubleMassiveOnlineFollowers(online, DateTime.Now);
            Assert.AreEqual(result.Length, 7 * 24);
        }
        [Test]
        public void GetDoubleMassiveOnlineFollowers_With_Empty_List()
        {
            List<OnlineFollowers> online = new List<OnlineFollowers>();
            var result = pdf.GetDoubleMassiveOnlineFollowers(online, DateTime.Now);
            Assert.AreEqual(result.Length, 7 * 24);
        }
        [Test]
        public void GetDoubleMassiveOnlineFollowers_With_Missings()
        {
            long value = 25;
            DateTime endTime = DateTime.Now;
            List<OnlineFollowers> online = new List<OnlineFollowers>();
            for (int i = 0; i < 24 * 6; i++, endTime = endTime.AddHours(1))
                online.Add(new OnlineFollowers() { endTime = endTime.AddHours(1), value = value + i });
            var result = pdf.GetDoubleMassiveOnlineFollowers(online, endTime);
            Assert.AreEqual(result.Length, 7 * 24);
        }
        [Test]
        public void GetDayOfWeek()
        {
            DateTime endTime = DateTime.Now;
            Assert.AreEqual(pdf.GetDaysOfWeek(endTime).Count, 7);
        }
        [Test]
        public void GetDayOfWeek_With_Missing_Days()
        {
            DateTime endTime = DateTime.Now;
            Assert.AreEqual(pdf.GetDaysOfWeek(endTime).Count, 7);
        }
        [Test]
        public void ChangeOnPercentOnlineFollowers()
        {
            int [,] values = new int[7, 24];
            for (int i = 0; i < 7; i++) {
                for (int j = 0; j < 24; j++)
                    values[i,j] = (i + 1) * (j + 1);
            }
            pdf.ChangeOnPercentOnlineFollowers(ref values, 400);
        }
        [Test]
        public void CountValueLevel()
        {
            List<long> values = new List<long>();
            for (int i = 0; i < 3; i++)
                values.Add(i);
            for (int i = 2, j = 0; i >= 0; i--, j++)
                Assert.AreEqual(pdf.CountValueLevel(values, i), j);
        }
        [Test]
        public void CountValueLevel_Zero_Value()
        {
            List<long> values = new List<long>();
            for (int i = 0; i < 3; i++)
                values.Add(i);
            Assert.AreEqual(pdf.CountValueLevel(values, 0), values.Count - 1);
        }
        [Test]
        public void CountValueLevel_Non_Exist_Values()
        {
            List<long> values = new List<long>();
            for (int i = 0; i < 3; i++)
                values.Add(i);
            Assert.AreEqual(pdf.CountValueLevel(values, 4), 0);
        }
    }
}