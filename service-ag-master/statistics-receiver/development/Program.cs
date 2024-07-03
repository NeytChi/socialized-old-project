using System;
using Serilog;
using Serilog.Core;
using InstagramService.Statistics;

namespace statistics_receiver
{
    public class Program
    {
        public static Logger log = new LoggerConfiguration()
            .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
		
        public static void Main(string[] args)
        {
            int businessId = 0, gettingDays = -1;
            for (int i = 0; i < args.Length; i++) {
                if (args[i].Equals("-s"))
                    businessId = Int32.Parse(args[i + 1]);
                if (args[i].Equals("-d"))
                    gettingDays = Int32.Parse(args[i + 1]);
            }
            if (businessId == 0) {
                TimerReceiver timerReceiver = new TimerReceiver(log);
            }
            else {
                Receiver receiver = new Receiver(log);
                receiver.Start(businessId, gettingDays);
            }
        }
    }
}