using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration;

using nautoposting;
using database.context;

namespace auto_posting_gen
{
    class Program
    {
        static Semaphore semaphore = new Semaphore(1,1);
        static IConfigurationRoot configuration;
        public static void Main(string[] args)
        {
            Context context = new Context(false);
            AutoPostingService service = new AutoPostingService(true, context);
            semaphore.WaitOne();
            semaphore.WaitOne();
        }
        public static IConfigurationRoot GetConfiguration()
        {
            if (configuration == null) {
                configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .AddJsonFile("configuration.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"configuration.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", 
                    optional: true, reloadOnChange: true)
                .Build();
            }
            return configuration;
        }
    }
}
