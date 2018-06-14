using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using Microsoft.CSharp;

namespace MemStache
{
    public enum StacheItemEnum
    {
        stchBasic,              // Basic In-Mem Cache Item
        stchSerialized,         //In-Mem Cache Item, serialized to support objects
        stchProtected,          //In-Mem Cache, Serialized and Protected
    };

    public class MemStache<T, MemStacheItemType>  where T : class where MemStacheItemType : MemStacheItemBase<T>  //IMemStacheItem<T>
    {
        private Func<string, Task<T>> GetCacheItemData { get; }
        private Func<string, string, Task> SaveCacheItemData { get; }
        private const int LimitedCacheThreshold = 1000;

        private readonly ConcurrentDictionary<string, WeakReference<MemStacheItemType>> _weakCache;
        private readonly ConcurrentDictionary<string, MemStacheItemType> _limitedCache;
        private readonly ConcurrentDictionary<string, Task<T>> _pendingTasks;

        private static ConcurrentDictionary<string, MemStacheItemInfo<T>> ItemMetaData { get; }
                       = new ConcurrentDictionary<string, MemStacheItemInfo<T>>();

        private StacheItemEnum _stacheItemEnum = StacheItemEnum.stchBasic;
        public StacheItemEnum Category
        {
            get { return _stacheItemEnum; }
            private set { _stacheItemEnum = value; }
        }
        
        private MemStache(StacheItemEnum category = StacheItemEnum.stchBasic, Func<string, string, Task> _SaveCacheItemData = null, Func<string, Task<T>> _GetCacheItemData = null)
        {
            _stacheItemEnum = category;
            SaveCacheItemData = _SaveCacheItemData;
            GetCacheItemData = _GetCacheItemData;
            _weakCache = new ConcurrentDictionary<string, WeakReference<MemStacheItemType>>(StringComparer.Ordinal);
            _limitedCache = new ConcurrentDictionary<string, MemStacheItemType>(StringComparer.Ordinal);
            _pendingTasks = new ConcurrentDictionary<string, Task<T>>(StringComparer.Ordinal);
        }
        //MemStache<X, IMemStacheItem<X>>
        public static dynamic Create<X>(StacheItemEnum category = StacheItemEnum.stchBasic,
                                                                 Func<string,string, Task> saveCacheItemData = null,
                                                                 Func<string, Task<X>> getCacheItemData = null)
                                                                 where X : class
        {
            try
            {
                //MemStache<X, IMemStacheItem<X>> result = null;
                dynamic result = null;                

                if (category == StacheItemEnum.stchSerialized)
                    result = new MemStache<X, MemStacheSerializedItem<X>>(category, saveCacheItemData, getCacheItemData);
                else
                    result = new MemStache<X, MemStacheProtectedItem<X>>(category, saveCacheItemData, getCacheItemData);

                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}" + e.Message);
                throw;
            }
        }

        public bool RegisterItem(MemStacheItemInfo<T> itemInfo)
        {
            return ItemMetaData.TryAdd(itemInfo.Key, itemInfo);
        }

        public async Task<T> GetOrAdd(string key)
        {
            DateTimeOffset expiration;
            Func<string, Task<T>> getCacheItemData = null;
            Func<string, string, Task> saveCacheItemData = null;

            var metaInfo = ItemMetaData.Where(a => a.Key.Equals(key)).FirstOrDefault().Value;
            if (metaInfo != null && metaInfo.GetData != null)
            {
                getCacheItemData = metaInfo.GetData;
                expiration = metaInfo.Expiration;
            }
            getCacheItemData = getCacheItemData ?? GetCacheItemData;

            if (metaInfo != null && metaInfo.SaveData != null)
            {
                saveCacheItemData = metaInfo.SaveData;
                expiration = metaInfo.Expiration;
            }
            saveCacheItemData = saveCacheItemData ?? SaveCacheItemData;

            WeakReference<MemStacheItemType> cachedReference;

            if (_weakCache.TryGetValue(key, out cachedReference))
            {
                MemStacheItemType cachedValue;
                if (cachedReference.TryGetTarget(out cachedValue) || cachedValue != null)
                {
                    if (cachedValue.Timestamp < expiration)
                    {
                        cachedValue.AddRef();
                        return cachedValue.Data;
                    }
                }
            }
            
            if (ItemMetaData.ContainsKey(key))
                getCacheItemData = ItemMetaData.Where(a => a.Key.Equals(key)).First().Value.GetData;

            if (getCacheItemData != null)
            {
                try
                {
                    T actualValue = await _pendingTasks.GetOrAdd(key, getCacheItemData);  //addFactory);

                    if (_limitedCache.Count > LimitedCacheThreshold)
                    {
                        var keysToRemove = _limitedCache
                            .Select(item => Tuple.Create(
                                item.Value.ResetRef(),
                                item.Value.Timestamp,
                                item.Key))
                            .ToArray()
                            .OrderBy(item => item.Item1)
                            .ThenBy(item => item.Item2)
                            .Select(item => item.Item3)
                            .Take(LimitedCacheThreshold / 2)
                            .ToArray();

                        foreach (var k in keysToRemove)
                        {
                            MemStacheItemType unused;
                            _limitedCache.TryRemove(k, out unused);
                        }
                    }
                    // MemStacheProtectedItem<T> Create(T obj, IDataProtector dataProtector)
                    //MemStacheItemType reference = Activator.CreateInstance(typeof(MemStacheItemType),false) as MemStacheItemType;
                    MemStacheItemType reference = CreateItem(actualValue);
                    _weakCache[key] = new WeakReference<MemStacheItemType>(reference);
                    _limitedCache[key] = reference;

                    if (saveCacheItemData != null)
                    {
                        if (typeof(IMemStacheSerializedItem<T>).IsAssignableFrom(reference.GetType()))
                        {
                            string _data = (reference as IMemStacheSerializedItem<T>).SerializedData;
                            saveCacheItemData(key, _data).Wait();
                        }
                    }

                    return actualValue;
                }
                finally
                {
                    Task<T> unused;
                    _pendingTasks.TryRemove(key, out unused);
                }
            }
            else
            {
                return null;
            }

        }

        private MemStacheItemType CreateItem(T data)
        {
            MemStacheItemType result = null;
            if (this.Category == StacheItemEnum.stchProtected)
            {
                IDataProtector dataProtector = null;

                var x = MemStacheProtectedItem<T>.Create(data, dataProtector);
            }

            return result;
        }

        //public static implicit operator MemStache<T, MemStacheItemType>(MemStache<T, IMemStacheSerializedItem<T>> v)
        //{
        //    return new MemStache<T, MemStacheItemType>(v.SaveCacheItemData, v.GetCacheItemData);
        //}

        //public static implicit operator MemStache<T, MemStacheItemType>(MemStache<T, IMemStacheProtectedItem<T>> v)
        //{
        //    return new MemStache<T, MemStacheItemType>(v.SaveCacheItemData, v.GetCacheItemData);
        //}
    }
}
