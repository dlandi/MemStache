using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using Microsoft.CSharp;

using Stache = MemStache.MemStache<string, MemStache.IMemStacheProtectedItem<string>>;

namespace MemStache
{
    public enum StacheItemEnum
    {
        stchBasic,              // Basic In-Mem Cache Item
        stchSerialized,         //In-Mem Cache Item, serialized to support objects
        stchProtected,          //In-Mem Cache, Serialized and Protected
    };

    public class MemStache<T, MemStacheItemType>  where T : class where MemStacheItemType : IMemStacheItem<T>
    {
        private Func<string, Task<T>> GetCacheItemData { get; }
        private Func<string, string, Task> SaveCacheItemData { get; }
        private const int LimitedCacheThreshold = 1000;

        private readonly ConcurrentDictionary<string, WeakReference<IMemStacheItem<T>>> _weakCache;
        private readonly ConcurrentDictionary<string, IMemStacheItem<T>> _limitedCache;
        private readonly ConcurrentDictionary<string, Task<T>> _pendingTasks;

        private static ConcurrentDictionary<string, MemStacheItemInfo<T>> ItemMetaData { get; }
                       = new ConcurrentDictionary<string, MemStacheItemInfo<T>>();

        private MemStache(Func<string, string, Task> _SaveCacheItemData, Func<string, Task<T>> _GetCacheItemData = null)
        {
            SaveCacheItemData = _SaveCacheItemData;
            GetCacheItemData = _GetCacheItemData;
            _weakCache = new ConcurrentDictionary<string, WeakReference<IMemStacheItem<T>>>(StringComparer.Ordinal);
            _limitedCache = new ConcurrentDictionary<string, IMemStacheItem<T>>(StringComparer.Ordinal);
            _pendingTasks = new ConcurrentDictionary<string, Task<T>>(StringComparer.Ordinal);
        }

        public static MemStache<X, IMemStacheItem<X>> Create<X>(StacheItemEnum stacheItemEnum = StacheItemEnum.stchBasic,
                                                                 Func<string,string, Task> saveCacheItemData = null,
                                                                 Func<string, Task<X>> getCacheItemData = null)
                                                                 where X : class
        {
            MemStache<X, IMemStacheItem<X>> result = null;

            if (stacheItemEnum == StacheItemEnum.stchSerialized)
                result = new MemStache<X, IMemStacheSerializedItem<X>>(saveCacheItemData, getCacheItemData);
            else
                result = new MemStache<X, IMemStacheProtectedItem<X>>(saveCacheItemData, getCacheItemData);
            
            return result;
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

            WeakReference<IMemStacheItem<T>> cachedReference;

            if (_weakCache.TryGetValue(key, out cachedReference))
            {
                IMemStacheItem<T> cachedValue;
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
                            IMemStacheItem<T> unused;
                            _limitedCache.TryRemove(k, out unused);
                        }
                    }

                    IMemStacheItem<T> reference = Activator.CreateInstance(typeof(IMemStacheItem<T>)) as IMemStacheItem<T>;
                    _weakCache[key] = new WeakReference<IMemStacheItem<T>>(reference);
                    _limitedCache[key] = reference;

                    if (saveCacheItemData != null)
                    {
                        saveCacheItemData(key, key).Wait();
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

        public static implicit operator MemStache<T, MemStacheItemType>(MemStache<T, IMemStacheSerializedItem<T>> v)
        {
            return v;
        }

        public static implicit operator MemStache<T, MemStacheItemType>(MemStache<T, IMemStacheProtectedItem<T>> v)
        {
            return v;
        }
    }
}
