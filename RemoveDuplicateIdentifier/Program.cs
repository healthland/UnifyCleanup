using RemoveDuplicateIdentifier.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using RemoveDuplicateIdentifier.Models;

namespace RemoveDuplicateIdentifier
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder();
            BuildConfig(configurationBuilder);
            var configuration = configurationBuilder.Build();
            string connectionId = string.Empty;
            string action = string.Empty;

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            Log.Logger.Information("Starting application...");
            
            CommandOptions commandLineOptions = new();

            _ = Parser.Default.ParseArguments<CommandOptions>(args)
            .WithParsed(o =>
            {
                commandLineOptions = o;
            });

#if DEBUG
         // debug stuff goes here
            if (string.IsNullOrEmpty(commandLineOptions.Environment))
                commandLineOptions.Environment = "PD";
           
#endif   

            var builder = Host.CreateDefaultBuilder();

            var host = builder.ConfigureServices((context, services) =>
            {
                services.AddSingleton<IRemoveDuplicateIdService, RemoveDuplicateIdService>();
                services.AddHttpClient();
            })
            .UseSerilog()
            .Build();

            CancellationTokenSource source = new CancellationTokenSource();
            var app = host.Services.GetService<IRemoveDuplicateIdService>();

            await app.Execute(commandLineOptions, source.Token);

            source.Cancel();
        }

        static void BuildConfig(IConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "production"}.json", optional: true)
                .AddEnvironmentVariables();
        }
    }
}
