using System;
using System.IO;
using LiteDB;
using Newtonsoft.Json;
#if __IOS__ || __MACOS__
using Foundation;
#elif __ANDROID__
using Android.App;
#endif

namespace MemStache.LiteDB
{
    /// <summary>
    /// Persistent Key/Value data store for any data object.
    /// Allows for saving data along with expiration dates and Hashs.
    /// </summary>
    public class StashRepo 
    {
        public static string ApplicationId { get; set; } = string.Empty;

        public static string EncryptionKey { get; set; } = string.Empty;

        static readonly Lazy<string> baseCacheDir = new Lazy<string>(() =>
        {
            return Path.Combine(Utils.GetBasePath(ApplicationId), "memstache");
        });

        private readonly LiteDatabase db;


        static StashRepo instance = null;
        public static LiteCollection<Stash> col;

        /// <summary>
        /// Gets the instance of the StashRepo
        /// </summary>
        public static StashRepo Current => instance ?? (instance = new StashRepo(ApplicationId));

        JsonSerializerSettings jsonSettings;

        public StashRepo(string appId)
        {
            ApplicationId = appId;
            var directory = baseCacheDir.Value;
            string path = Path.Combine(directory, "memstache.db");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!string.IsNullOrWhiteSpace(EncryptionKey))
            {
                path = $"Filename={path}; Password={EncryptionKey}";
            }

            this.db = new LiteDatabase(path);
            col = this.db.GetCollection<Stash>();

            this.jsonSettings = new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All,
            };
        }

        #region Exist and Expiration Methods

        /// <summary>
        /// Checks to see if the key exists in the StashRepo.
        /// </summary>
        /// <param name="key">Unique identifier for the entry to check</param>
        /// <returns>If the key exists</returns>
        public bool Exists(string key)
        {
            var ent = col.FindById(key);

            return ent != null;
        }

        /// <summary>
        /// Checks to see if the entry for the key is expired.
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>If the expiration data has been met</returns>
        public bool IsExpired(string key)
        {
            var ent = col.FindById(key);

            if (ent == null)
            {
                return true;
            }

            return DateTime.UtcNow > ent.ExpirationDate.ToUniversalTime();
        }

        #endregion

        #region Get Methods

        /// <summary>
        /// Gets the data entry for the specified key.
        /// </summary>
        /// <param name="key">Unique identifier for the entry to get</param>
        /// <returns>The data object that was stored if found, else default(T)</returns>
        public Stash Get<T>(string key)
        {
            Stash ent = col.FindById(key);

            if (ent == null)
            {
                return null;//default(T);
            }

            //return JsonConvert.DeserializeObject<T>(ent.Value, this.jsonSettings);
            return ent;
        }

        /// <summary>
        /// Gets the string entry for the specified key.
        /// </summary>
        /// <param name="key">Unique identifier for the entry to get</param>
        /// <returns>The string that was stored if found, else null</returns>
        public string Get(string key)
        {
            var ent = col.FindById(key);

            if (ent == null)
            {
                return null;
            }

            return ent.Value;
        }

        /// <summary>
        /// Gets the Hash for the specified key.
        /// </summary>
        /// <param name="key">Unique identifier for entry to get</param>
        /// <returns>The Hash if the key is found, else null</returns>
        public string GetHash(string key)
        {
            var ent = col.FindById(key);

            if (ent == null)
            {
                return null;
            }

            return ent.Hash;
        }

        /// <summary>
        /// Gets the DateTime that the item will expire for the specified key.
        /// </summary>
        /// <param name="key">Unique identifier for entry to get</param>
        /// <returns>The expiration date if the key is found, else null</returns>
        public DateTime? GetExpiration(string key)
        {
            var ent = col.FindById(key);

            if (ent == null)
            {
                return null;
            }

            return ent.ExpirationDate;
        }

        #endregion

        #region Add Methods

        /// <summary>
        /// Adds a string netry to the StashRepo
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Unique identifier for the entry</param>
        /// <param name="data">Data string to store</param>
        /// <param name="expireIn">Time from UtcNow to expire entry in</param>
        /// <param name="hash">Optional Hash information</param>
        public void Add(string key, string data, TimeSpan expireIn, string hash = null)
        {
            if (data == null)
            {
                return;
            }

            var ent = new Stash
            {
                Key = key,
                ExpirationDate = Utils.GetExpiration(expireIn),
                Hash = hash,
                Value = data
            };

            col.Upsert(ent);
        }

        /// <summary>
        /// Adds an entry to the StashRepo
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Unique identifier for the entry</param>
        /// <param name="data">Data object to store</param>
        /// <param name="expireIn">Time from UtcNow to expire entry in</param>
        /// <param name="Hash">Optional Hash information</param>
        public void Add<T>(string key, T data, TimeSpan expireIn, string Hash = null)
        {
            if (data == null)
            {
                return;
            }

            //this.Add(key, JsonConvert.SerializeObject(data, this.jsonSettings), expireIn, Hash);
            this.Add(key, JsonConvert.SerializeObject(data, this.jsonSettings), expireIn, Hash);
        }

        #endregion

        #region Empty Methods
        /// <summary>
        /// Empties all expired entries that are in the StashRepo.
        /// Throws an exception if any deletions fail and rolls back changes.
        /// </summary>
        public void EmptyExpired()
        {
            col.Delete(b => b.ExpirationDate.ToUniversalTime() < DateTime.UtcNow);
        }

        /// <summary>
        /// Empties all expired entries that are in the StashRepo.
        /// Throws an exception if any deletions fail and rolls back changes.
        /// </summary>
        public void EmptyAll()
        {
            col.Delete(Query.All());

        }

        /// <summary>
        /// Empties all specified entries regardless if they are expired.
        /// Throws an exception if any deletions fail and rolls back changes.
        /// </summary>
        /// <param name="key">keys to empty</param>
        public void Empty(params string[] key)
        {
            foreach (var k in key)
            {
                col.Delete(k);
            }
        }
        #endregion
    }
}