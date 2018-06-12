using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace MemStache
{
    public interface IMemStacheProtectedItem<T> : IMemStacheItem<T> where T : class
    {
        string ProtectedData { get; }
    }
    public sealed class MemStacheProtectedItem<T>: MemStacheItem, IDisposable,  IMemStacheProtectedItem<T> where T : class
    {
        public IDataProtector DataProtector { get; set; }
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
                return JsonConvert.DeserializeObject<T>(
                                    DataProtector.Unprotect(_data)
                                    );
            }
            private set
            {
                _data = DataProtector.Protect(
                                    JsonConvert.SerializeObject(value, Formatting.Indented)
                                    );
            }
        }
        /// <summary>
        /// Return the Data without Unprotecting it. For instance, to write it directly to disk.
        /// </summary>
        public string ProtectedData
        {
            get
            { 
                return ( _data  );
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
        public static MemStacheProtectedItem<T> Create(T obj, IDataProtector DataProtector)
        {
            return new MemStacheProtectedItem<T>(DataProtector)
            {
                Timestamp = DateTimeOffset.Now,
                Data = obj
            };
        }
        //public void SaveItem(PersistCacheRepository<PersistRecord> DbRepo)
        public void SaveItem(dynamic DbRepo)
        {
            //DbRepo.AddOrUpdate(new PersistRecord()
            //    {
            //        Contents = (string)ProtectedData.ToString(),
            //    }
            //);
        }
        public void SaveItem(T Item)
        {
            throw new NotImplementedException();
        }
        //https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-ephemeral?view=aspnetcore-2.0
        private  MemStacheProtectedItem(IDataProtector DataProtector)
        {
            this.DataProtector = DataProtector;
        }
        private  MemStacheProtectedItem(T obj, IDataProtector DataProtector)
        {
            this.DataProtector = DataProtector;
            Timestamp = DateTimeOffset.Now;
            Data = obj;
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
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
