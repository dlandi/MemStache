using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MemStache
{
    /// <summary>
    /// MetaData about MemStacheItem Data!
    /// Immutable Class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class MemStacheItemInfo<T> where T : class
    {
        public string Key { get; }
        public DateTimeOffset Expiration { get; }
        public Func<string, Task<T>> GetData { get; }
        public Func<string, string, Task> SaveData { get; }

        public MemStacheItemInfo(string key, DateTimeOffset expiration , Func<string, string, Task> saveData, Func<string, Task<T>> getData = null)
        {
            Key = key;
            Expiration = expiration;
            GetData = getData;
            SaveData = saveData;

        }
    }
}
