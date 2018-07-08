﻿// <copyright file="StacheMeister.cs" company="Dennis Landi">
// Copyright (c) Dennis Landi. All rights reserved.
// </copyright>

using System;
using System.IO;
using MemStache.LiteDB;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SQLite;

namespace MemStache
{
    public class StacheMeister
    {
        public string AppId { get; set; }

        public ServiceCollection Services { get; set; }

        public ServiceProvider ServiceProvider { get; set; }

        public IDataProtectionProvider DataProtectionProvider { get; set; }

        public IDataProtector DataProtector { get; set; }

        public IMemoryCache Cache { get; set; }

        public string DatabasePath { get; set; }

        //public SQLiteConnection DB { get; set; }
        public StashRepo DB { get; set; }

        /// <summary>
        /// Stasher employing the Serialization Plan
        /// </summary>
        public Stasher Stasher { get; set; }

        public StashPlan Plan { get; set; } = StashPlan.spSerialize;

        /// <summary>
        /// Optons must include a Size Limit. If one it not provided the Size Limit will be 900Kb per item
        /// </summary>
        public MemoryCacheEntryOptions MemoryItemOptions { get; set; }

#pragma warning disable SA1600 // Elements should be documented
        public dynamic this[string key]
#pragma warning restore SA1600 // Elements should be documented
        {
            get
            {
                try
                {
                    return this.Stasher[key].Object;
                }
                catch
                {
                    return null;
                }
            }

            set
            {
                using (

                    Stash stash = new Stash()
                    {
                        Key = key,
                        StashPlan = this.Plan,
                        Object = value,
                    })
                {
                    this.Stasher[key] = stash;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StacheMeister"/> class.
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="plan"></param>
        /// <param name="memCacheOptions"></param>
        /// <param name="memoryItemOptions"></param>
        /// <param name="services"></param>
        public StacheMeister(
            string appId,
            StashPlan plan = StashPlan.spSerialize,
            MemoryCacheOptions memCacheOptions = null,
            MemoryCacheEntryOptions memoryItemOptions = null,
            ServiceCollection services = null)
        {
            this.Plan = plan;
            this.AppId = appId;
            this.DatabasePath = GetBasePath("memstache.db");

            //this.DB = new SQLiteConnection(this.DatabasePath);
            //this.DB.CreateTable<Stash>();
            this.DB = new StashRepo(appId);
            ServiceCollection svcs = services ?? new ServiceCollection();
            this.Services = svcs;
            svcs.AddDataProtection();
            svcs.AddMemoryCache();
            this.ServiceProvider = svcs.BuildServiceProvider();
            if (memCacheOptions == null)
            {
                this.Cache = new MemoryCache(new MemoryCacheOptions()
                {
                    SizeLimit = 5242880, // bytes = 5 megs
                    CompactionPercentage = .50,
                    ExpirationScanFrequency = TimeSpan.FromMinutes(1),
                });
            }
            else
            {
                this.Cache = new MemoryCache(memCacheOptions);
            }

            if (memoryItemOptions == null)
            {
                this.MemoryItemOptions = new MemoryCacheEntryOptions()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
                    Size = 921600, // bytes = 900 kb
                    Priority = CacheItemPriority.High,
                };
            }
            else
            {
                if (memoryItemOptions.Size == null)
                {
                    memoryItemOptions.Size = 921600;
                }

                this.MemoryItemOptions = memoryItemOptions;
            }

            this.DataProtectionProvider = this.ServiceProvider.GetService<IDataProtectionProvider>();
            this.DataProtector = this.DataProtectionProvider.CreateProtector(appId);
            this.DefaultStashers();
        }

        public void DefaultStashers()
        {
            this.Stasher = new Stasher("Stasher.Default", this.Plan, this.DB, this.DataProtector, this.Cache);
        }

        public Stasher MakeStasher(string purpose, StashPlan plan = StashPlan.spSerialize)
        {
            return new Stasher(purpose, plan, this.DB, this.DataProtector, this.Cache);
        }

        #region Utils
        public static string GetBasePath(string applicationId)
        {
            if (string.IsNullOrWhiteSpace(applicationId))
            {
                throw new ArgumentException("You must set a ApplicationId in the Stachemeister constructor");
            }

            if (applicationId.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            {
                throw new ArgumentException("ApplicationId has invalid characters");
            }

            var path = string.Empty;

            // Gets full path based on device type.
#if __IOS__ || __MACOS__
            path = NSSearchPath.GetDirectories(NSSearchPathDirectory.CachesDirectory, NSSearchPathDomain.User)[0];
#elif __ANDROID__
            path = Application.Context.CacheDir.AbsolutePath;
#elif __UWP__
            path = Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
#else
            path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
#endif
            return Path.Combine(path, applicationId);
        }

        public static DateTime GetExpiration(TimeSpan timeSpan)
        {
            try
            {
                return DateTime.UtcNow.Add(timeSpan);
            }
            catch
            {
                if (timeSpan.Milliseconds < 0)
                {
                    return DateTime.MinValue;
                }

                return DateTime.MaxValue;
            }
        }
        #endregion

    }
}
