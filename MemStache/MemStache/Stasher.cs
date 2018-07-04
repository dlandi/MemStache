using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
namespace MemStache
{
    /// <summary>
    /// How the Stash will go down
    /// </summary>
    public enum StashPlan
    {
        spSerialize = 0,
        spSerializeCompress = 1,
        spProtect = 2,       //includes serialization also                    
        spProtectCompress = 3  
    };
    /// <summary>
    /// The stash that gets stashed
    /// </summary>
    public class Stash : IDisposable
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
        public string StoredType { get; set; }
        public int Plan { get; set; }
        private dynamic _Object;
        [Ignore]
        public dynamic Object
        {
            get { return _Object; }
            set {
                //serial the object's type, so we can deserialize later
                this.StoredType = JsonConvert.SerializeObject(value.GetType());
                //serialize object and save to value property
                this.value = JsonConvert.SerializeObject(value);
                this.serialized = true;
                
            }
        }

        private StashPlan _stashPlan;
        [Ignore]
        public StashPlan stashPlan
        {
            get { return _stashPlan; }
            set {
                _stashPlan = value;
                this.Plan = (int)this._stashPlan;
            }
        }
        internal void SetPrivateObject(dynamic value)
        {
            this._Object = value;
        }

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
                this._Object = null;
                this.value = null;
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

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
                    return GetItemCommon(key);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return null;
                }
            }
            set
            {
                Stash clone = CloneItem(value);
                SetItemCommon(clone);
                //value.Dispose();
            }
        }
        public string DatabasePath { get; set; }
        public SQLiteConnection DB { get; set; }
        public StashPlan Plan { get; set; } = StashPlan.spSerialize;
        public MemoryCacheEntryOptions MemoryItemOptions { get; set; }
        public Stasher(string purpose, StashPlan plan, SQLiteConnection db,
                       IDataProtector dataProtector, IMemoryCache cache,
                       MemoryCacheEntryOptions memoryItemOptions = null)
        {
            DB = db;
            Cache = cache;
            DataProtector = dataProtector;
            Purpose = purpose;
            Plan = plan;
            //If developer chooses to instantiate standalone Stasher then we will need to deal with 
            //memItemOptions directly
            if (memoryItemOptions == null)
            {
                MemoryItemOptions = new MemoryCacheEntryOptions()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
                    Size = 921600, //bytes = 900 kb
                    Priority = CacheItemPriority.High
                };
            }
            else
            {
                if (memoryItemOptions.Size == null)
                    memoryItemOptions.Size = 921600;
                MemoryItemOptions = memoryItemOptions;
            }
        }

        private Type GetStoredType(string typeName)
        {
            Type type = JsonConvert.DeserializeObject<Type>(typeName);
            return type;
        }
        protected Stash CloneItem(Stash stash)
        {
            StashPlan itemPlan = GetPlanFromValue(stash.Plan);
            if(this.Plan != itemPlan)
                throw new Exception("Cache Item Plan does not match container's Plan.");
            return new Stash()
            {
                compressed = stash.compressed,
                encrypted = stash.encrypted,
                ExpirationDate = stash.ExpirationDate,
                hash = stash.hash,
                key = stash.key,
                serialized = stash.serialized,
                size = stash.size,
                value = stash.value,
                StoredType = stash.StoredType,
                stashPlan = itemPlan //GetPlanFromValue(stash.Plan)
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
            return DB.Get<Stash>(key);
        }
        public int DbAddOrUpdate(Stash item)
        {            
            int numRowsAffected = (DB.Find<Stash>(item.key) == null) ? DB.Insert(item) : DB.Update(item);//If record found, update; else, insert
            return numRowsAffected;
        }

        #region Item Processing Functions

        public byte[] Protect(string input)
        {
            byte[] arInput = GetBytes(input);
            return Protect(arInput);
        }
        public byte[] Protect(byte[] input)
        {
            return DataProtector.Protect(input);
        }
        /// <summary>
        /// Depends on unprotected data being originally encoded
        /// in Base64
        /// </summary>
        /// <param name="arProtected"></param>
        /// <returns></returns>
        public string UnprotectToStr(byte[] arProtected)
        {
            byte[] arUnprotected = DataProtector.Unprotect(arProtected);
            return GetString(arUnprotected);
        }
        /// <summary>
        /// Depends on unprotected data being originally encoded
        /// in Base64
        /// </summary>
        /// <param name="arProtected"></param>
        /// <returns></returns>
        public byte[] Unprotect(byte[] arProtected)
        {
            byte[] arUnprotected = DataProtector.Unprotect(arProtected);
            return arUnprotected;
        }
        public byte[] Compress(byte[] input)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory,
                    CompressionMode.Compress, true))
                {
                    gzip.Write(input, 0, input.Length);
                }
                return memory.ToArray();
            }
        }
        public byte[] Uncompress(byte[] input)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(input),
                CompressionMode.Decompress))
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
            Stash item = Cache.Get<Stash>(key);
            if (item == null) item = DbGet(key);
            if (item == null) return null;

            item.stashPlan = GetPlanFromValue(item.Plan);//if DBGet fired then we need to re-hydrate this prop
            
            if (item.stashPlan == StashPlan.spSerialize)
                return GetItemSerializationOnly(item);
            else
            if (item.stashPlan == StashPlan.spSerializeCompress)
                return GetItemSerializeCompress(item);
            else
            if (item.stashPlan == StashPlan.spProtectCompress)
                return GetItemSerializeCompressEncrypt(item);
            return null;
        }

        public void SetItemCommon(Stash item)
        {
            if (item.stashPlan == StashPlan.spSerialize)
                SetItemSerializationOnly(item);
            else
            if (item.stashPlan == StashPlan.spSerializeCompress)
                SetItemSerializeCompress(item);
            else
            if (item.stashPlan == StashPlan.spProtectCompress)
                SetItemSerializeCompressEncrypt(item);
        }

        #region Item Serialization Only

        Stash GetItemSerializationOnly(Stash item)
        {
            if (item.serialized)
            {
                Type serializedType = GetStoredType(item.StoredType);
                item.SetPrivateObject(JsonConvert.DeserializeObject(item.value, serializedType));//deserialized data assigned to Object property.
                item.encrypted = false;
                item.serialized = true;//the value property remains serialized...
            }
            return item;
        }

        public void SetItemSerializationOnly(Stash item)
        {
            string key = item.key;
            if (!item.serialized)//don't serialize twice.  The first time was in the Stash Object Property Setter
                item.value = JsonConvert.SerializeObject(item.value);
            item.encrypted = false;
            item.serialized = true;
            Cache.Set<Stash>(key, item, MemoryItemOptions);
            Stash item2 = Cache.Get<Stash>(key);
            DbAddOrUpdate(item);
        }


    #endregion

        #region Item Serialize And Compress

        Stash GetItemSerializeCompress(Stash item)
        {
            byte[] itembytes = Convert.FromBase64String(item.value);
            byte[] arCompressed = Uncompress(itembytes);
            item.value = Convert.ToBase64String(arCompressed);
            item.value = DecodeFrom64(item.value);
            if (item.serialized)
            {
                Type serializedType = GetStoredType(item.StoredType);
                item.SetPrivateObject(JsonConvert.DeserializeObject(item.value, serializedType));//deserialized data assigned to Object property.
                item.encrypted = false;
                item.serialized = true;//the value property remains serialized...
            }
            return item;
        }

        public void SetItemSerializeCompress(Stash item)
        {
            string key = item.key;
            if (!item.serialized)//don't serialize twice.  The first time was in the Stash Object Property Setter
                item.value = JsonConvert.SerializeObject(item.value);
            item.value = EncodeTo64(item.value);
            byte[] itembytes = Convert.FromBase64String(item.value);

            byte[] arCompressed = Compress(itembytes);
            item.value = Convert.ToBase64String(arCompressed);

            item.encrypted = false;
            item.serialized = true;
            Cache.Set<Stash>(key, item, MemoryItemOptions);
            Stash item2 = Cache.Get<Stash>(key);
            DbAddOrUpdate(item);
        }


        #endregion

        #region Item Serialize, Compress And Encrypt
        
        Stash GetItemSerializeCompressEncrypt(Stash item)
        {
            byte[] arProtected;
            byte[] arCompressed;
            arProtected = Convert.FromBase64String(item.value);
            try
            {
                arCompressed = Unprotect(arProtected);
            }
            catch (Exception e)
            {

                throw;
            }
            item.value = GetString(Uncompress(arCompressed));
            item.value = DecodeFrom64(item.value);
            if (item.serialized)
            {
                Type serializedType = GetStoredType(item.StoredType);
                dynamic deserialized = JsonConvert.DeserializeObject(item.value, serializedType);
                item.SetPrivateObject(deserialized);//deserialized data assigned to Object property.
                item.encrypted = false;
                item.serialized = true;//the value property remains serialized...
            }
            return item;
        }

        public void SetItemSerializeCompressEncrypt(Stash item)
        {
            string key = item.key;
            if (!item.serialized)//don't serialize twice.  The first time was in the Stash Object Property Setter
                item.value = JsonConvert.SerializeObject(item.value);

            string str64 = EncodeTo64(item.value);
            byte[] arCompressed = Compress(GetBytes(str64));
            byte[] arProtected = Protect(arCompressed);
            //byte[] arCompressed = Compress(arProtected);
            item.value = Convert.ToBase64String(arProtected);
            //item.value = Convert.ToBase64String(arCompressed);//GetString(arCompressed);

            item.encrypted = false;
            item.serialized = true;
            Cache.Set<Stash>(key, item, MemoryItemOptions);
            DbAddOrUpdate(item);
        }


        #endregion


        #endregion



        #region Helpers
        static public string EncodeTo64(string toEncode)

        {

            byte[] toEncodeAsBytes

                  = System.Text.ASCIIEncoding.UTF8.GetBytes(toEncode);

            string returnValue

                  = System.Convert.ToBase64String(toEncodeAsBytes);

            return returnValue;

        }

        static public string DecodeFrom64(string encodedData)

        {

            byte[] encodedDataAsBytes

                = System.Convert.FromBase64String(encodedData);

            string returnValue =

               System.Text.ASCIIEncoding.UTF8.GetString(encodedDataAsBytes);

            return returnValue;

        }

        /// <summary>
        /// Input should alwase be a Base64 encoded string!
        /// </summary>
        /// <param name="strBase64"></param>
        /// <returns></returns>
        private byte[] GetBytes(string strBase64)
        {
            byte[] bytes = new byte[strBase64.Length * sizeof(char)];
            System.Buffer.BlockCopy(strBase64.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
        /// <summary>
        /// Only use this method with data that was originally Base64 encoded
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private string GetString(byte[] bytes)
        {
            if (bytes.Length % 2 != 0) // if input length is odd, add a #0
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

