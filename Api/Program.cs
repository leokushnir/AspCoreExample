using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Middleware.Serilog;
using Serilog;

namespace Api
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                CreateWebHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Host terminated unexpectedly");
                Console.Write(ex.ToString());
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateWebHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseSerilog((context, loggerConfig) =>
                {
                    loggerConfig.WithSimpleConfiguration("Api", context.Configuration);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                    webBuilder.UseStartup<Startup>());
        }
    }
}