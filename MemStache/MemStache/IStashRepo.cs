using System;
using System.Collections.Generic;
using System.Text;

namespace MemStache
{
    public interface IStashRepo
    {
        void Add(string key, string data, TimeSpan expireIn, string eTag = null);

        void Add<T>(string key, T data, TimeSpan expireIn, string eTag = null);

        void Empty(params string[] key);

        void EmptyAll();

        void EmptyExpired();

        bool Exists(string key);

        string Get(string key);

        T Get<T>(string key);

        string GetHash(string key);

        bool IsExpired(string key);

        DateTime? GetExpiration(string key);
    }
}
