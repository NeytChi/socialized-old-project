using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using database.context;

namespace socialized
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public readonly string allowSpecificOrigins = "cors-origin";
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var serverConfig = Program.serverConfiguration();
            services.AddCors(options =>
            {
                var origins = serverConfig.GetSection("allow_origin")
                .GetChildren()
                .Select(x => x.Value)
                .ToArray();
                options.AddPolicy(allowSpecificOrigins,
                builder =>
                {
                    builder.WithOrigins(origins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
                });
            });
            AuthOptions authOptions = new AuthOptions();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = AuthOptions.ISSUER,
                        ValidateAudience = true,
                        ValidAudience = AuthOptions.AUDIENCE,
                        ValidateLifetime = true,
                        IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
                        ValidateIssuerSigningKey = true,
                    };
                });
            
            services.AddDbContext<Context>(options
                => options.UseMySql(Context.databaseConnection()));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            
            services.AddMvc(
                options => {
                    options.SslPort = serverConfig.GetValue<int>("port_https");
                    options.Filters.Add(new RequireHttpsAttribute());
                }
            );

            services.AddAntiforgery(
                options => {
                    options.Cookie.Name = "_af";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.HeaderName = "X-XSRF-TOKEN";
                }
            );
            services.AddHttpsRedirection(options => {
                options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
                options.HttpsPort = serverConfig.GetValue<int>("port_https");
            });
          
            services.Configure<FormOptions>(x => {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // In case of multipart
            });
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            if (Program.requestView)
            {
                app.Use(next => context => { 
                        context.Request.EnableRewind(); 
                        return next(context); 
                    });
                app.UseMiddleware<RequestWatchingSteamMiddleware>();
            }
            
            // app.UseDefaultFiles();
            // app.UseStaticFiles();
            
            app.UseCors(allowSpecificOrigins);
            
            app.UseAuthentication();
            
            app.UseHttpsRedirection();
            
            app.UseMvc();
        }
    }
}