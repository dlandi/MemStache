// <copyright file="StacheMeister.cs" company="Dennis Landi">
// Copyright (c) Dennis Landi. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.IO;
using MemStache.LiteDB;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace MemStache
{
    public class StacheMeister : IDisposable
    {
        public string AppId { get; set; }

        public ServiceCollection Services { get; set; }

        public ServiceProvider ServiceProvider { get; set; }

        public IDataProtectionProvider DataProtectionProvider { get; set; }

        public IDataProtector DataProtector { get; set; }

        public IMemoryCache Cache { get; set; }

        // public string DatabasePath { get; set; }
        public StashRepo DB { get; set; }

        /// <summary>
        /// Gets or sets stasher employing the Serialization Plan.
        /// </summary>
        public Stasher Stasher { get; set; }

        public StashPlan Plan { get; set; } = StashPlan.spSerialize;

        /// <summary>
        /// Gets or sets options must include a Size Limit. If one it not provided the Size Limit will be 900Kb per item.
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
                    dynamic item = this.Stasher[key].Object;
                    if (item == null)
                    {
                        return null;
                    }
                    else
                    {
                        return item;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error: " + e.Message);
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
        /// <param name="appId">appId.</param>
        /// <param name="filename">filename.</param>
        /// <param name="password">password.</param>
        /// <param name="plan">plan.</param>
        /// <param name="memCacheOptions">memCacheOptions.</param>
        /// <param name="memoryItemOptions">memoryItemOptions.</param>
        /// <param name="services">services.</param>
        public StacheMeister(
            string appId,
            string filename = null,
            string password = null,
            StashPlan plan = StashPlan.spSerialize,
            MemoryCacheOptions memCacheOptions = null,
            MemoryCacheEntryOptions memoryItemOptions = null,
            ServiceCollection services = null)
        {
            this.Plan = plan;
            this.AppId = appId;

            this.DB = new StashRepo(appId, filename, password);
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

        // public static string GetBasePath(string applicationId)
        //        {
        //            if (string.IsNullOrWhiteSpace(applicationId))
        //            {
        //                throw new ArgumentException("You must set a ApplicationId in the Stachemeister constructor");
        //            }

        // if (applicationId.IndexOfAny(Path.GetInvalidPathChars()) != -1)
        //            {
        //                throw new ArgumentException("ApplicationId has invalid characters");
        //            }

        // var path = string.Empty;

        // // Gets full path based on device type.
        // #if __IOS__ || __MACOS__
        //            path = NSSearchPath.GetDirectories(NSSearchPathDirectory.CachesDirectory, NSSearchPathDomain.User)[0];
        // #elif __ANDROID__
        //            path = Application.Context.CacheDir.AbsolutePath;
        // #elif __UWP__
        //            path = Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
        // #else
        //            path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        // #endif
        //            return Path.Combine(path, applicationId);
        //        }
#pragma warning disable SA1204 // Static elements should appear before instance elements
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
#pragma warning restore SA1204 // Static elements should appear before instance elements

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~StacheMeister() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);

            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
        #endregion

    }
}
