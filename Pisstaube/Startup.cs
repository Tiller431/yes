﻿using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.IO;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using Pisstaube.CacheDb;
using Pisstaube.Database;
using Pisstaube.Online;
using Pisstaube.Utils;
using Sora.Allocation;
using StatsdClient;

namespace Pisstaube
{
    public class Startup
    {
        // ReSharper disable once UnusedParameter.Local
        public Startup(IConfiguration configuration)
        {
            dataStorage = new NativeStorage("data");
            osuContextFactory = new DatabaseContextFactory(dataStorage);
            cacheContextFactory = new PisstaubeCacheDbContextFactory(dataStorage);
            dbContextFactory = new PisstaubeDbContextFactory();
            
            // copy paste of OsuGameBase.cs
            try
            {
                using (var db = dbContextFactory.GetForWrite(false))
                    db.Context.Database.Migrate();
                
                using (var db = osuContextFactory.GetForWrite(false))
                    db.Context.Migrate();
            }
            catch (Exception e)
            {
                Logger.Error(e.InnerException ?? e, "Migration failed! We'll be starting with a fresh database.", LoggingTarget.Database);
                osuContextFactory.ResetDatabase();
                Logger.Log("Database purged successfully.", LoggingTarget.Database);
                using (var db = osuContextFactory.GetForWrite(false))
                    db.Context.Migrate();
            }
            
            // copy paste of OsuGameBase.cs
            try
            {
                using (var db = cacheContextFactory.GetForWrite(false))
                    db.Context.Migrate();
            }
            catch (Exception e)
            {
                Logger.Error(e.InnerException ?? e, "Migration failed! We'll be starting with a fresh database.", LoggingTarget.Database);
                cacheContextFactory.ResetDatabase();
                Logger.Log("Database purged successfully.", LoggingTarget.Database);
                using (var db = cacheContextFactory.GetForWrite(false))
                    db.Context.Migrate();
            }
        }

        private readonly DatabaseContextFactory osuContextFactory;
        private readonly PisstaubeCacheDbContextFactory cacheContextFactory;
        private readonly PisstaubeDbContextFactory dbContextFactory;
        private readonly NativeStorage dataStorage;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddDbContext<PisstaubeDbContext>();

            services.AddMemoryCache();
            
            services
                .AddSingleton<Cache>()
                .AddSingleton<BMUpdater>()
                .AddSingleton<PisstaubeDbContext>()
                .AddSingleton<BeatmapSearchEngine>()
                .AddSingleton<Storage>(dataStorage)
                .AddSingleton(cacheContextFactory)
                .AddSingleton(dbContextFactory)
                .AddSingleton<OsuConfigManager>()
                .AddSingleton<APIAccess>()
                .AddSingleton<Cleaner>()
                .AddSingleton(new FileStore(osuContextFactory, dataStorage))
                .AddSingleton(new RulesetStore(osuContextFactory))
                .AddSingleton<BeatmapDownloader>()
                .AddSingleton<Crawler>()
                .AddSingleton<SetDownloader>()
                .AddSingleton(new RequestLimiter(1200, TimeSpan.FromMinutes(1)))
                .AddSingleton<Kaesereibe>();
            
            services
                .AddMvc(
                    options =>
                    {
                        options.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>();
                        options.EnableEndpointRouting = false;
                    })
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue;
            });
            
            services.AddRouting();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            Crawler crawler, APIAccess apiv2, Kaesereibe reibe, BeatmapSearchEngine searchEngine, BMUpdater bmUpdater)
        {
            if (Environment.GetEnvironmentVariable("LOG_LEVEL") != null)
                if (Enum.TryParse(Environment.GetEnvironmentVariable("LOG_LEVEL"), out LogLevel level))
                    Logger.Level = level;

            Logger.Storage = dataStorage.GetStorageForDirectory("logs");

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            if (searchEngine.Search("test") == null)
            {
                Logger.LogPrint("Failed to Connect to ElasticSearch!", LoggingTarget.Network, LogLevel.Error);
                Environment.Exit(0);
            }
            
            apiv2.Login(Environment.GetEnvironmentVariable("OSU_EMAIL"), Environment.GetEnvironmentVariable("OSU_PASSWORD"));

            DogStatsd.Configure(new StatsdConfig { Prefix = "pisstaube" });
            
            DogStatsd.ServiceCheck("crawler.is_crawling", Status.UNKNOWN);
            
            if (Environment.GetEnvironmentVariable("CRAWLER_DISABLED") != "true")
                crawler.BeginCrawling();
            else
                DogStatsd.ServiceCheck("crawler.is_crawling", Status.CRITICAL);

            if (Environment.GetEnvironmentVariable("CHEESEGULL_CRAWLER_DISABLED") != "true")
                reibe.BeginCrawling();
            else
                DogStatsd.ServiceCheck("kaesereibe.is_crawling", Status.CRITICAL);

            if (!Directory.Exists("data"))
                Directory.CreateDirectory("data");
            
            if (!Directory.Exists("data/cache"))
                Directory.CreateDirectory("data/cache");
            
            DogStatsd.ServiceCheck("is_active", Status.OK);
            
            if (Environment.GetEnvironmentVariable("UPDATER_DISABLED") != "true")
                bmUpdater.BeginUpdaterAsync();

            app.UseMvc(routes => routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}"));
        }
    }
}