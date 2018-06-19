using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MemStache
{
    public class StacheMeister
    {
        public string Purpose { get; set; }
        public ServiceCollection services { get; set; }
        public ServiceProvider serviceProvider { get; set; }
        public IDataProtectionProvider DataProtectionProvider { get; set; }
        public IDataProtector DataProtector { get; set; }
        public IMemoryCache Cache { get; set; }
        public string DatabasePath { get; set; }
        public SQLiteConnection DB { get; set; }
        /// <summary>
        /// Stasher employing the Serialization Plan
        /// </summary>
        public Stasher Stasher { get; set; }
        public StashPlan Plan { get; set; } = StashPlan.spSerialize;
        /// <summary>
        /// Stasher employing the Serialization and Compression Plan
        /// </summary>

        public dynamic this[string key]
        {
            get
            {
                try
                {
                    return this.Stasher[key].Object;  //NOTE: Refactor to break the reference to the  wrapping stash object
                                                        // by calling "deserialize Value" here
                }
                catch
                {
                    return null;
                }
            }
            set
            {

                var stash = new Stash()
                {
                    key = key,
                    stashPlan = this.Plan,
                    Object = value
                };
                this.Stasher[key]=stash;
            }
        }

        public StacheMeister(string purpose, StashPlan plan = StashPlan.spSerialize, ServiceCollection services = null)
        {
            Plan = plan;
            Purpose = purpose;
            //DatabasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "memstache.db"); // Get an absolute path to the database file
            DatabasePath = GetBasePath("memstache.db");

            DB = new SQLiteConnection(DatabasePath);
            DB.CreateTable<Stash>();
            ServiceCollection svcs = services ?? new ServiceCollection();
            this.services = svcs;
            svcs.AddDataProtection();
            svcs.AddMemoryCache();
            serviceProvider = svcs.BuildServiceProvider();
            Cache = serviceProvider.GetService<IMemoryCache>();
            DataProtectionProvider = serviceProvider.GetService<IDataProtectionProvider>();
            DataProtector = DataProtectionProvider.CreateProtector(purpose);
            DefaultStashers();
        }
        public void DefaultStashers()
        {
            Stasher = new Stasher("Stasher.Default", this.Plan, DB, DataProtector, Cache);
        }

        public Stasher MakeStasher(string purpose, StashPlan plan = StashPlan.spSerialize)
        {
            return new Stasher(purpose, plan, DB, DataProtector, Cache);
        }

        #region Utils
        public static string GetBasePath(string applicationId)
        {
            if (string.IsNullOrWhiteSpace(applicationId))
                throw new ArgumentException("You must set a ApplicationId in the Stachemeister constructor");

            if (applicationId.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                throw new ArgumentException("ApplicationId has invalid characters");

            var path = string.Empty;
            ///Gets full path based on device type.
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
                    return DateTime.MinValue;

                return DateTime.MaxValue;
            }
        }
        #endregion

    }
}
