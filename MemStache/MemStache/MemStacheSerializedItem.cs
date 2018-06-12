using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MemStache
{
    public interface IMemStacheSerializedItem<T> : IMemStacheItem<T> where T : class
    {
        string SerializedData { get; }
    }
    public sealed class MemStacheSerializedItem<T> : MemStacheItem, IDisposable, IMemStacheSerializedItem<T> where T : class
    {
        private int _hitCount = 0;
        public new DateTimeOffset Timestamp
        {
            get;
            private set;
        }
        private string _data;
        //[DebuggerHidden]
        public new T Data
        {
            get
            {
                return JsonConvert.DeserializeObject<T>(_data);
            }
            private set
            {
                _data = JsonConvert.SerializeObject(value, Formatting.Indented);
            }
        }
                
        /// <summary>
        /// Return the Data without Deserializing it. For instance, to write it directly to disk.
        /// </summary>
        public string SerializedData
        {
            get
            {
                return (_data);
            }
        }
        public override void AddRef()
        {
            Interlocked.Increment(ref _hitCount);
        }
        public override int ResetRef()
        {
            var count = _hitCount;
            _hitCount = 0;
            return count;
        }
        public static MemStacheSerializedItem<T> Create(T obj)
        {
            return new MemStacheSerializedItem<T>()
            {
                Timestamp = DateTimeOffset.Now,
                Data = obj
            };
        }
        private MemStacheSerializedItem(T obj)
        {
            Timestamp = DateTimeOffset.Now;
            Data = obj;            
        }
        private MemStacheSerializedItem()
        {
        }
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }
                // set large fields to null.
                this._data = null;
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
