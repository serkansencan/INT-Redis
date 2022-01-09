using System;
using System.Collections.Generic;

namespace INT.Redis
{
    /// <summary>
    /// Redis cache manager interface
    /// </summary>
    public interface IRedisCacheManager : IDisposable
    {
        #region Methods

        /// <summary>
        /// Get the value with the specified key from the cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        T Get<T>(string key);

        /// <summary>
        /// Get the values with the specified key from the cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        List<T> GetMany<T>(string key);

        /// <summary>
        /// Add the value with the specified key from the cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="expiry"></param>
        void Set(string key, object data, TimeSpan expiry);

        /// <summary>
        /// Add the value with the specified key from the cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        void Set(string key, object data);

        /// <summary>
        /// Gets a value indicating whether the value associated with the specified key is cached
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool IsSet(string key);

        /// <summary>
        /// Removes the value with the specified key from the cache
        /// </summary>
        /// <param name="key"></param>
        void Remove(string key);

        /// <summary>
        /// Returns the remaining time to live of a key that has a timeout.
        /// </summary>
        /// <param name="key"></param>
        TimeSpan? GetKeyRemainingTimeout(string key);

        /// <summary>
        /// Clear all cache data
        /// </summary>
        void Clear();

        #endregion Methods
    }
}
