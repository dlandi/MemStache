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
        public Stasher Serialized { get; set; }
        /// <summary>
        /// Stasher employing the Serialization and Compression Plan
        /// </summary>
        public Stasher Compressed { get; set; }
        /// <summary>
        /// Stasher employing the Protection Plan
        /// </summary>
        public Stasher Protected { get; set; }
        /// <summary>
        /// Stasher employing the Protection and Compression Plan
        /// </summary>
        public Stasher PrctCmpr { get; set; }

        public StacheMeister(string purpose, ServiceCollection services = null)
        {
            Purpose = purpose;
            DatabasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "memstache.db"); // Get an absolute path to the database file
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
            Serialized = new Stasher("Stasher.Serialized", StashPlan.spSerialize, DB, DataProtector, Cache);
            Compressed = new Stasher("Stasher.Serialized.Compressed", StashPlan.spSerializeCompress, DB, DataProtector, Cache);
            Protected = new Stasher("Stasher.Protected", StashPlan.spProtect, DB, DataProtector, Cache);
            PrctCmpr = new Stasher("Stasher.Protected.Compressed", StashPlan.spProtectCompress, DB, DataProtector, Cache);
        }

        public Stasher MakeStasher(string purpose, StashPlan plan = StashPlan.spSerialize)
        {
            return new Stasher(purpose, plan, DB, DataProtector, Cache);
        }
    }
}
