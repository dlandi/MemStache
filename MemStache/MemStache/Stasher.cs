// <copyright file="Stasher.cs" company="Dennis Landi">
// Copyright (c) Dennis Landi. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using MemStache.LiteDB;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace MemStache
{
    /// <summary>
    /// How the Stash will go down.
    /// </summary>
    public enum StashPlan
    {
        spSerialize = 0,
        spSerializeCompress = 1,
        spProtect = 2,       // includes serialization also
        spProtectCompress = 3,
    }

    /// <summary>
    /// It Stashes the Stash.
    /// </summary>
    public class Stasher
    {
        public string Purpose { get; set; }

        public ServiceCollection Services { get; set; }

        public ServiceProvider ServiceProvider { get; set; }

        public IDataProtectionProvider DataProtectionProvider { get; set; }

        public IDataProtector DataProtector { get; set; }

        public IMemoryCache Cache { get; set; }

        public Stash this[string key]
        {
            get
            {
                try
                {
                    return this.GetItemCommon(key);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return null;
                }
            }

            set
            {
                Stash clone = this.CloneItem(value);
                this.SetItemCommon(clone);

                // value.Dispose();
            }
        }

        public string DatabasePath { get; set; }

        public StashRepo DB { get; set; }

        public StashPlan Plan { get; set; } = StashPlan.spSerialize;

        public MemoryCacheEntryOptions MemoryItemOptions { get; set; }

        public Stasher(
                        string purpose,
                        StashPlan plan,
                        StashRepo db,
                        IDataProtector dataProtector,
                        IMemoryCache cache,
                        MemoryCacheEntryOptions memoryItemOptions = null)
        {
            this.DB = db;
            this.Cache = cache;
            this.DataProtector = dataProtector;
            this.Purpose = purpose;
            this.Plan = plan;

            // If developer chooses to instantiate standalone Stasher then we will need to deal with
            // memItemOptions directly
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
        }

        private Type GetStoredType(string typeName)
        {
            Type type = JsonConvert.DeserializeObject<Type>(typeName);
            return type;
        }

        protected Stash CloneItem(Stash stash)
        {
            StashPlan itemPlan = this.GetPlanFromValue(stash.Plan);
            if (this.Plan != itemPlan)
            {
                throw new Exception("Cache Item Plan does not match container's Plan.");
            }

            return new Stash()
            {
                ExpirationDate = stash.ExpirationDate,
                Hash = stash.Hash,
                Key = stash.Key,
                Serialized = stash.Serialized,
                Size = stash.Size,
                Value = stash.Value,
                StoredType = stash.StoredType,
                StashPlan = itemPlan, // GetPlanFromValue(stash.Plan)
            };
        }

        private StashPlan GetPlanFromValue(int value)
        {
            switch (value)
            {
                case 0: return StashPlan.spSerialize;
                case 1: return StashPlan.spSerializeCompress;
                case 2: return StashPlan.spProtect;
                case 3: return StashPlan.spProtectCompress;

                default: return StashPlan.spSerialize;
            }
        }

        public Stash DbGet(string key)
        {
            return this.DB.Get(key);
        }

        public bool DbAddOrUpdate(Stash item)
        {
            
            this.DB.Add<Stash>(item.Key, item, TimeSpan.FromDays(365), Hash(item.Key));
            return true;
        }

        #region Item Processing Functions

        public byte[] Protect(string input)
        {
            byte[] arInput = this.GetBytes(input);
            return this.Protect(arInput);
        }

        public byte[] Protect(byte[] input)
        {
            return this.DataProtector.Protect(input);
        }

        /// <summary>
        /// Depends on unprotected data being originally encoded
        /// in Base64.
        /// </summary>
        /// <param name="arProtected"></param>
        /// <returns>string.</returns>
        public string UnprotectToStr(byte[] arProtected)
        {
            byte[] arUnprotected = this.DataProtector.Unprotect(arProtected);
            return this.GetString(arUnprotected);
        }

        /// <summary>
        /// Depends on unprotected data being originally encoded
        /// in Base64.
        /// </summary>
        /// <param name="arProtected"></param>
        /// <returns>byte[].</returns>
        public byte[] Unprotect(byte[] arProtected)
        {
            byte[] arUnprotected = this.DataProtector.Unprotect(arProtected);
            return arUnprotected;
        }

        public byte[] Compress(byte[] input)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
                {
                    gzip.Write(input, 0, input.Length);
                }

                return memory.ToArray();
            }
        }

        public byte[] Uncompress(byte[] input)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(input), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }
        #endregion

        #region Plan Execution
        public Stash GetItemCommon(string key)
        {
            Stash item = this.Cache.Get<Stash>(key);
            
            if (item == null)
            {
                item = this.DbGet(key);
            }

            if (item == null)
            {
                return null;
            }

            item = this.CloneItem(item);

            item.StashPlan = this.GetPlanFromValue(item.Plan); // if DBGet fired then we need to re-hydrate this prop

            if (item.StashPlan == StashPlan.spSerialize)
            {
                return this.GetItemSerializationOnly(item);
            }
            else
            if (item.StashPlan == StashPlan.spSerializeCompress)
            {
                return this.GetItemSerializeCompress(item);
            }
            else
            if (item.StashPlan == StashPlan.spProtectCompress)
            {
                return this.GetItemSerializeCompressEncrypt(item);
            }

            return null;
        }

        public void SetItemCommon(Stash item)
        {
            if (item.StashPlan == StashPlan.spSerialize)
            {
                this.SetItemSerializationOnly(item);
            }
            else
            if (item.StashPlan == StashPlan.spSerializeCompress)
            {
                this.SetItemSerializeCompress(item);
            }
            else
            if (item.StashPlan == StashPlan.spProtectCompress)
            {
                this.SetItemSerializeCompressEncrypt(item);
            }
        }

        #region Item Serialization Only

        protected Stash GetItemSerializationOnly(Stash item)
        {
            if (item.Serialized)
            {
                Type serializedType = this.GetStoredType(item.StoredType);
                item.SetPrivateObject(JsonConvert.DeserializeObject(item.Value, serializedType)); // deserialized data assigned to Object property.
                item.Serialized = true; // the value property remains serialized...
            }

            return item;
        }

        protected void SetItemSerializationOnly(Stash item)
        {
            string key = item.Key;
            if (!item.Serialized) // don't serialize twice.  The first time was in the Stash Object Property Setter
            {
                item.Value = JsonConvert.SerializeObject(item.Value);
            }

            item.Serialized = true;
            this.Cache.Set<Stash>(key, item, this.MemoryItemOptions);
            Stash item2 = this.Cache.Get<Stash>(key);
            this.DbAddOrUpdate(item);
        }

        #endregion

        #region Item Serialize And Compress

        protected Stash GetItemSerializeCompress(Stash item)
        {
            byte[] itembytes = Convert.FromBase64String(item.Value);
            byte[] arCompressed = this.Uncompress(itembytes);
            item.Value = Convert.ToBase64String(arCompressed);
            item.Value = DecodeFrom64(item.Value);
            if (item.Serialized)
            {
                Type serializedType = this.GetStoredType(item.StoredType);
                item.SetPrivateObject(JsonConvert.DeserializeObject(item.Value, serializedType)); // deserialized data assigned to Object property.
                item.Serialized = true; // the value property remains serialized...
            }

            return item;
        }

        protected void SetItemSerializeCompress(Stash item)
        {
            string key = item.Key;
            if (!item.Serialized) // don't serialize twice.  The first time was in the Stash Object Property Setter
            {
                item.Value = JsonConvert.SerializeObject(item.Value);
            }

            item.Value = EncodeTo64(item.Value);
            byte[] itembytes = Convert.FromBase64String(item.Value);

            byte[] arCompressed = this.Compress(itembytes);
            item.Value = Convert.ToBase64String(arCompressed);
            item.Serialized = true;
            this.Cache.Set<Stash>(key, item, this.MemoryItemOptions);
            Stash item2 = this.Cache.Get<Stash>(key);
            this.DbAddOrUpdate(item);
        }

        #endregion

        #region Item Serialize, Compress And Encrypt

        protected Stash GetItemSerializeCompressEncrypt(Stash item)
        {
            byte[] arProtected;
            byte[] arCompressed;
            arProtected = Convert.FromBase64String(item.Value);
            try
            {
                arCompressed = this.Unprotect(arProtected);
            }
            catch (Exception)
            {
                throw;
            }

            item.Value = this.GetString(this.Uncompress(arCompressed));
            item.Value = DecodeFrom64(item.Value);
            if (item.Serialized)
            {
                Type serializedType = this.GetStoredType(item.StoredType);
                dynamic deserialized = JsonConvert.DeserializeObject(item.Value, serializedType);
                item.SetPrivateObject(deserialized); // deserialized data assigned to Object property.
                item.Serialized = true; // the value property remains serialized...
            }

            return item;
        }

        protected void SetItemSerializeCompressEncrypt(Stash item)
        {
            string key = item.Key;
                                    #pragma warning disable SA1108 // Block statements should not contain embedded comments
            if (!item.Serialized) // don't serialize twice.  The first time was in the Stash Object Property Setter
                                    #pragma warning restore SA1108 // Block statements should not contain embedded comments
            {
                item.Value = JsonConvert.SerializeObject(item.Value);
            }

            string str64 = EncodeTo64(item.Value);
            byte[] arCompressed = this.Compress(this.GetBytes(str64));
            byte[] arProtected = this.Protect(arCompressed);
            item.Value = Convert.ToBase64String(arProtected);

            item.Serialized = true;
            this.Cache.Set<Stash>(key, item, this.MemoryItemOptions);
            this.DbAddOrUpdate(item);
        }

        #endregion

        #endregion

        #region Helpers
        public static string EncodeTo64(string toEncode)
        {
            byte[] toEncodeAsBytes

                  = System.Text.ASCIIEncoding.UTF8.GetBytes(toEncode);

            string returnValue

                  = System.Convert.ToBase64String(toEncodeAsBytes);

            return returnValue;
        }

        public static string DecodeFrom64(string encodedData)
        {
            byte[] encodedDataAsBytes

                = System.Convert.FromBase64String(encodedData);

            string returnValue =

               System.Text.ASCIIEncoding.UTF8.GetString(encodedDataAsBytes);

            return returnValue;
        }

        /// <summary>
        /// Input should alwase be a Base64 encoded string!.
        /// </summary>
        /// <param name="strBase64"></param>
        /// <returns>byte[].</returns>
        private byte[] GetBytes(string strBase64)
        {
            byte[] bytes = new byte[strBase64.Length * sizeof(char)];
            System.Buffer.BlockCopy(strBase64.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>
        /// Only use this method with data that was originally Base64 encoded.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns>string.</returns>
        private string GetString(byte[] bytes)
        {
                                        #pragma warning disable SA1108 // Block statements should not contain embedded comments
            if (bytes.Length % 2 != 0) // if input length is odd, add a #0
                                        #pragma warning restore SA1108 // Block statements should not contain embedded comments
            {
                byte[] newArray = new byte[bytes.Length + 1];
                bytes.CopyTo(newArray, 1);
                newArray[0] = byte.Parse("0");
                bytes = newArray;
            }

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
