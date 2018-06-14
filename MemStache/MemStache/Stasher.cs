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
    /// <summary>
    /// How the Stash will go down
    /// </summary>
    public enum StashPlan
    {
        spSerialize,         
        spProtect,       //includes serialization also
        spSerializeCompress,             
        spProtectCompress    
    };
    /// <summary>
    /// The stash that gets stashed
    /// </summary>
    public class Stash
    {
        [PrimaryKey]
        public string key { get; set; }
        public string value { get; set; }
        public bool encrypted { get; set; }
        public bool serialized { get; set; }
        public bool compressed { get; set; }
        public int size { get; set; }
        public string hash { get; set; }
        public DateTime ExpirationDate { get; set; }//stored as UTC
    }
    /// <summary>
    /// It Stashes the Stash
    /// </summary>
    public class Stasher
    {
        public string Purpose { get; set; }
        public ServiceCollection services { get; set; }
        public ServiceProvider serviceProvider { get; set; }
        public IDataProtectionProvider DataProtectionProvider { get; set; }
        public IDataProtector DataProtector { get; set; }
        public IMemoryCache Cache { get; set; }
        public Stash this[string key]
        {
            get
            {
                try
                {
                    return GetItem(key);
                }
                catch
                {
                    return null;
                }
            }
            set
            {
                Stash clone = CloneItem(value);
                SetItem(clone);
            }
        }
        public string DatabasePath { get; set; }
        public SQLiteConnection DB { get; set; }
        public StashPlan Plan { get; set; } = StashPlan.spSerialize;
        public Stasher(string purpose, StashPlan plan, SQLiteConnection db,
                       IDataProtector dataProtector, IMemoryCache cache)
        {
            DB = db;
            Cache = cache;
            DataProtector = dataProtector;
            Purpose = purpose;
            Plan = plan;
        }

        public Stash GetItem(string key)
        {
            Stash item = Cache.Get<Stash>(key);            
            if(item == null) item = DbGet(key);
            if (item == null) return null;
            try
            {
                string s = item.value;
                byte[] t = GetBytes(s);
                byte[] x = DataProtector.Unprotect(t);                
                item.value = GetString(x);
            }
            catch (Exception e)
            {
                throw;
            }
            item.value = JsonConvert.DeserializeObject<string>(item.value);
            item.encrypted = false;
            item.serialized = false;
            return item;
        }
        public void SetItem(Stash item)
        {
            string key = item.key;
            item.value = JsonConvert.SerializeObject(item.value);
            byte[] x = DataProtector.Protect(GetBytes(item.value));
            item.value = GetString(x);
            item.encrypted = true;            
            item.serialized = true;
            Cache.Set<Stash>(key, item);
            Stash item2 = Cache.Get<Stash>(key);
            DbAddOrUpdate(item);
        }
        protected Stash CloneItem(Stash stash)
        {
            return new Stash()
            {
                compressed = stash.compressed,
                encrypted = stash.encrypted,
                ExpirationDate = stash.ExpirationDate,
                hash = stash.hash,
                key = stash.key,
                serialized = stash.serialized,
                size = stash.size,
                value = stash.value
            };
        }
        public Stash DbGet(string key)
        {            
            return DB.Get<Stash>(key);
        }
        public int DbAddOrUpdate(Stash item)
        {            
            int numRowsAffected = (DB.Find<Stash>(item.key) == null) ? DB.Insert(item) : DB.Update(item);//If record found, update; else, insert
            return numRowsAffected;
        }
        #region Helpers

        private byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
        private string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
        public static string Hash(string input)
        {
            var md5Hasher = MD5.Create();
            var data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
            return BitConverter.ToString(data);
        } 
        #endregion
    }
}

