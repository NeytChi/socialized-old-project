using System;
using Managment;
using database.context;
using Models.AdminPanel;
using Models.Common;

using Serilog;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace socialized
{
    public class Program
    {
        public static bool requestView = false;  
        public static IConfigurationRoot serverConfig;  
        public static void Main(string[] args)
        {
            if (!InterfaceArguments(args))
                return;
            createDatabase();
            var certificateConfig = certificateConfiguration();
            string certificateFileName = certificateConfig.GetSection("certificateSettings").GetValue<string>("filename");
            string certificatePassword = certificateConfig.GetSection("certificateSettings").GetValue<string>("password");
            var certificate = new X509Certificate2(certificateFileName, certificatePassword);

            serverConfig = serverConfiguration();

            string IP = serverConfig.GetValue<string>("ip");
            int portHttp = serverConfig.GetValue<int>("port_http");
            int portHttps = serverConfig.GetValue<int>("port_https");

            WebHost.CreateDefaultBuilder(args)
                .UseKestrel(options 
                =>  {
                        options.AddServerHeader = false;
                        options.Listen(IPAddress.Parse(IP), portHttp);
                        options.Listen(IPAddress.Parse(IP), portHttps, listenOptions 
                        => {
                            listenOptions.UseHttps(certificate);
                        });
                    })
                .UseConfiguration(certificateConfig)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseUrls("http://" + IP + ":" + portHttp + "/",
                   "https://" + IP + ":" + portHttps + "/")
                .Build()
                .Run();
        }
        public static bool InterfaceArguments(string[] arguments)
        {
            if (arguments != null) {
                for (int i = 0; i < arguments.Length; i++) {
                    switch (arguments[i]) {
                        case "-u": 
                        case "--culture": 
                            SetCulture();
                            return false;
                        case "-cts": 
                        case "--countries": 
                            SetCountries();
                            return false;
                        case "-h": 
                        case "--help": 
                            HelpMenu();
                            return false;
                        case "-c": 
                        case "--clean": 
                            deleteDatabase();
                            return false;
                        case "-v":
                        case "--vision":
                            requestView = true;
                            break;
                        case "-a":
                        case "--admin":
                            if (arguments.Length >= i + 4)
                                addAdmin(arguments[i + 1], arguments[i + 2], arguments[i + 3], arguments[i + 4]);
                            else
                                HelpMenu();
                            return false;
                        default: break;
                    }
                }
            }
            return true;
        }
        public static void HelpMenu()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("[-h OR --help] - help menu;");
            Console.WriteLine("[-a OR --admin] [admin_email] [admin_lastname] [admin_firstname] [admin_password] - add new admin to server;");
            Console.WriteLine("[-c OR --clean] - clean 'socialized' database;");
            Console.WriteLine("[-v OR --vision] - option to see input & output request and responce;");
        }
        public static IConfigurationRoot certificateConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .AddJsonFile("certificate.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"certificate.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", 
                    optional: true, reloadOnChange: true)
                .Build();
        }
        public static IConfigurationRoot serverConfiguration()
        {
            if (serverConfig == null) {
                serverConfig = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .AddJsonFile("server.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"server.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", 
                    optional: true, reloadOnChange: true)
                .Build();
            }
            return serverConfig;
        }
        public static void createDatabase()
        {
            Context context = new Context(false);
            context.Database.EnsureCreated();
        }
        public static void deleteDatabase()
        {
            CreateBackUpDatabase();
            using (Context context = new Context(false)) {
                context.Database.EnsureDeleted();
                Console.WriteLine("Database 'socialized' was deleted.");
            }
        }
        public static void CreateBackUpDatabase()
        {
            string dump; byte[] bytes;
            DateTimeOffset offset = DateTimeOffset.UtcNow;
            var databaseConfiguration = Context.databaseConfiguration();
            string databaseName = databaseConfiguration.GetValue<string>("Database");
            string user = databaseConfiguration.GetValue<string>("User");
            string password = databaseConfiguration.GetValue<string>("Password");
            string backupPath = Directory.GetCurrentDirectory() + "/backups/";
            
            Directory.CreateDirectory(backupPath);
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.FileName = "mysqldump";
            process.StartInfo.Arguments = "-u" + user + " -p" + password + " -f " + databaseName;
            process.Start();
            dump = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            using (FileStream stream = File.Create(backupPath + offset.Day + "-" + offset.Month + ":" 
                + offset.Hour + "-" + offset.Minute + "-" + offset.Second + ".sql")) {
                bytes = Encoding.ASCII.GetBytes(dump);
                stream.Write(bytes, 0, bytes.Length);
            }
            Console.WriteLine("Create a backup for database 'socialized'.");
        }
        public static void addAdmin(string adminEmail, string adminLastName, string adminFirstName, string adminPassword)
        {
            string message = string.Empty;
            Admins admins = new Admins(new LoggerConfiguration()
                .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
                .CreateLogger(), new Context(false));
            AdminCache cache = new AdminCache() {
                admin_email = adminEmail,
                admin_fullname = adminLastName + " " + adminFirstName,
                admin_password = adminPassword
            };
            if (admins.CreateAdmin(cache, ref message) != null) 
                Console.WriteLine("Admin with email -> " + adminEmail + " was created.");
            else
                Console.WriteLine(message);
        }
        public static void SetCulture()
        {
            Context context = new Context(false);
            context.Cultures.RemoveRange(context.Cultures.ToList());
            context.SaveChanges();
            IConfigurationRoot cultureConfig = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .AddJsonFile("culture.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"culture.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", 
                    optional: true, reloadOnChange: true)
                .Build();
            List<Culture> cultures = cultureConfig.GetSection("cultures")
                .GetChildren()
                .Select(x => new Culture() {
                    cultureKey = x.GetValue<string>("cultureKey"),
                    cultureValue = x.GetValue<string>("cultureValue"),
                    cultureName = x.GetValue<string>("cultureName")
                }).ToList();
            context.Cultures.AddRange(cultures);
            context.SaveChanges();
            Console.WriteLine("Set up culture to database.");
        }
        public static void SetCountries()
        {
            List<Country> countries;
            Context context = new Context(false);
            context.countries.RemoveRange(context.countries.ToList());
            context.SaveChanges();
            IConfigurationRoot countriesConfig = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .AddJsonFile("countries.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"countries.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", 
                    optional: true, reloadOnChange: true)
                .Build();
            countries = countriesConfig.GetSection("country")
                .GetChildren()
                .Select(x => new Country() {
                    name = x.GetValue<string>("name"),
                    fullname = x.GetValue<string>("fullname"),
                    english = x.GetValue<string>("english"),
                    alpha2 = x.GetValue<string>("alpha2"),
                    alpha3 = x.GetValue<string>("alpha3"),
                    iso = x.GetValue<string>("iso"),
                    location = x.GetValue<string>("location"),
                    location_precise = x.GetValue<string>("location-precise"),
                }).OrderBy(x => x.name).ToList();
            context.countries.AddRange(countries);
            context.SaveChanges();
            Console.WriteLine("Set up countries to database.");
        }
    }
}
