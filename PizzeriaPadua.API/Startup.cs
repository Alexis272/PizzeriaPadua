using PizzeriaPadua.API.Data;
using PizzeriaPadua.API.Middleware;
using PizzeriaPadua.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;

namespace PizzeriaPadua.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private string ConvertToNpgsqlConnectionString(string postgresUrl)
        {
            var uri = new Uri(postgresUrl);
            var userInfo = uri.UserInfo.Split(':');
            var host = uri.Host;
            var port = uri.Port;
            var database = uri.AbsolutePath.TrimStart('/');
            var username = userInfo[0];
            var password = userInfo.Length > 1 ? userInfo[1] : "";

            return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<PizzeriaPaduaSettings>(Configuration);

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                });

            var connectionString = Configuration.GetValue<string>("DATABASE_URL");
            if (string.IsNullOrEmpty(connectionString))
            {
                var dbPath = Configuration.GetValue<string>("DatabasePath") ?? Path.Combine(Directory.GetCurrentDirectory(), "pizzeriapadua.db");
                services.AddDbContext<PizzeriaPaduaContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));
            }
            else
            {
                var npgsqlConnectionString = ConvertToNpgsqlConnectionString(connectionString);
                services.AddDbContext<PizzeriaPaduaContext>(options =>
                    options.UseNpgsql(npgsqlConnectionString));
            }

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            services.AddSingleton<LicensingService>();
            services.AddSingleton<EventBroadcaster>();
            services.AddSingleton<VerificationCodeStore>();
            services.AddSingleton<UsernameValidationService>();
            services.AddSingleton<UserSessionStore>();
            services.AddSingleton<AdminAuthService>();
            services.Configure<SendGridSettings>(Configuration.GetSection("SendGridSettings"));
            services.Configure<FacebookSettings>(Configuration.GetSection("FacebookSettings"));
            services.AddHttpClient<IEmailService, EmailService>();
            services.AddHttpClient<IFacebookAuthService, FacebookAuthService>();
            services.AddHttpClient<ISmsService, VonageSmsService>(client =>
            {
                client.BaseAddress = new Uri("https://rest.nexmo.com/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddHttpClient<MercadoPagoService>(client =>
            {
                client.BaseAddress = new Uri("https://api.mercadopago.com/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddHealthChecks();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            using (var scope = app.ApplicationServices.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PizzeriaPaduaContext>();
                db.Database.EnsureCreated();
            }

            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value ?? "";
                if (path == "/")
                {
                    context.Response.Redirect("/PizzeriaPadua/");
                    return;
                }
                await next();
            });

            app.UsePathBase("/PizzeriaPadua");

            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["X-Frame-Options"] = "DENY";
                context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
                await next();
            });

            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value;
                if (path != null && (path.StartsWith("/gestion") || path.Equals("/admin.html")))
                {
                    if (context.Request.Cookies["auth"] != "1" && !path.Equals("/admin.html"))
                    {
                        context.Response.Redirect("/PizzeriaPadua/admin.html");
                        return;
                    }
                }
                await next();
            });

            app.UseCors();
            app.UseLicenseMiddleware();
            app.UseRouting();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/hc");

                endpoints.MapGet("/__test", async context =>
                {
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync($"METHOD={context.Request.Method} PATH={context.Request.Path}");
                });

                endpoints.MapGet("/debug/ping", async context =>
                {
                    await context.Response.WriteAsync("pong");
                });
            });
        }
    }
}
