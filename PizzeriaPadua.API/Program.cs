using PizzeriaPadua.API.Data;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PizzeriaPadua.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var host = BuildWebHost(args);

                using (var scope = host.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<PizzeriaPaduaContext>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<PizzeriaPaduaContextSeed>>();

                    for (int i = 0; i < 10; i++)
                    {
                        try
                        {
                            context.Database.EnsureCreated();
                            var seeder = new PizzeriaPaduaContextSeed();
                            seeder.Seed(context, logger).Wait();
                            logger.LogInformation("Database migrated and seeded successfully");
                            break;
                        }
                        catch (Exception ex) when (i < 9)
                        {
                            logger.LogWarning(ex, "Database unavailable, retrying in 3s... ({I}/10)", i + 1);
                            Thread.Sleep(3000);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Database unavailable. Starting without database.");
                        }
                    }
                }

                host.Run();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[FATAL] Startup error: {ex.GetType().FullName}: {ex.Message}");
                Console.Error.WriteLine($"[FATAL] StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine($"[FATAL] Inner: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls($"http://0.0.0.0:{port}")
                .Build();
        }
    }
}
