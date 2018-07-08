// <copyright file="Stash.cs" company="Dennis Landi">
// Copyright (c) Dennis Landi. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using LiteDB;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SQLite;

namespace MemStache
{
    /// <summary>
    /// The stash that gets stashed
    /// </summary>
    public class Stash : IDisposable
    {
#if SQLITE
        [PrimaryKey]
#elif LITEDB
        [BsonId]
#endif
        public string Key { get; set; }

        public string Value { get; set; }

        public bool Serialized { get; set; }

        public int Size { get; set; }

        public string Hash { get; set; }

        public DateTime ExpirationDate { get; set; }// stored as UTC

        public string StoredType { get; set; }

        public int Plan { get; set; }

     /// <summary>
        /// Private field for "Object" property.
        /// </summary>
        private dynamic _object;

        [Ignore]
        public dynamic Object
        {
            get
            {
                return this._object;
            }

            set
            {
                // serial the object's type, so we can deserialize later
                this.StoredType = JsonConvert.SerializeObject(value.GetType());

                // serialize object and save to value property
                this.Value = JsonConvert.SerializeObject(value);
                this.Serialized = true;
            }
        }

        private StashPlan stashPlan;

        [Ignore]
        public StashPlan StashPlan
        {
            get
            {
                return this.stashPlan;
            }

            set
            {
                this.stashPlan = value;
                this.Plan = (int)this.stashPlan;
            }
        }

        internal void SetPrivateObject(dynamic value)
        {
            this._object = value;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                this._object = null;
                this.Value = null;
                this.disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        /// <inheritdoc/>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);

            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
