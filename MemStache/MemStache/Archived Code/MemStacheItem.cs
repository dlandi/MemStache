using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MemStache
{
    public interface IMemStacheItem<T> where T : class
    {
        T Data { get; }
        DateTimeOffset Timestamp { get; }
        void AddRef();
        int ResetRef();
    }

    /// <summary>
    /// Abstract Base Class
    /// </summary>
    public abstract class MemStacheItemBase<T>: IMemStacheItem<T> where T : class
    {
        public T Data { get; }
        public DateTimeOffset Timestamp { get; }
        public virtual void AddRef() { }
        public virtual int ResetRef() { return -1; }
    }
    /// <summary>
    /// Basic Cache Item with no serialization or encryption
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class MemStacheItem<T> : MemStacheItemBase<T>, IDisposable, IMemStacheItem<T> where T : class
    {
        private int _hitCount = 0;
        public new DateTimeOffset Timestamp
        {
            get;
            private set;
        }
        private T _data;
        //[DebuggerHidden]
        public new T Data
        {
            get
            {
                return (_data);
            }

            private set
            {
                _data = (value);
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
        public static MemStacheItem<T> Create(T obj)
        {
            return new MemStacheItem<T>()
            {
                Timestamp = DateTimeOffset.Now,
                Data = obj
            };
        }
        private MemStacheItem(T obj)
        {
            Timestamp = DateTimeOffset.Now;
            Data = obj;
        }
        private MemStacheItem()
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
                //set large fields to null.
                this._data = null;               
                disposedValue = true;
            }
        }
        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
