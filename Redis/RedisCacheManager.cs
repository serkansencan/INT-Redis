using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Redis.Constant;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace INT.Redis
{
    /// <summary>
    /// Redis cache manager class
    /// </summary>
    public class RedisCacheManager : IRedisCacheManager
    {
        #region Fields

        private readonly string _connectionString;
        private volatile ConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _database;
        private volatile RedLockFactory _redisLockFactory;
        private readonly object _lock = new object();

        #endregion Fields

        #region Ctor

        /// <summary>
        /// Redis cache manager constructor
        /// </summary>
        /// <param name="redisSetting"></param>
        public RedisCacheManager(IOptions<RedisSettings> redisSetting)
        {
            if (string.IsNullOrEmpty(redisSetting.Value.ConnectionString))
            {
                throw new ArgumentNullException("Redis conneciton string is null or empty please set valid connection string");
            }
            _connectionString = redisSetting.Value.ConnectionString;
            this._redisLockFactory = CreateRedisLockFactory();
            this._database = GetDatabase();
        }

        #endregion Ctor

        #region Methods

        /// <summary>
        ///  Get the value with the specified key from the cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            return this.IsSetAsync(key).Result ? this.GetAsync<T>(key).Result : default(T);
        }

        /// <summary>
        /// Get the values with the specified key from the cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<T> GetMany<T>(string key)
        {
            return this.IsSetAsync(key).Result ? this.GetManyAsync<T>(key).Result : default(List<T>);
        }

        /// <summary>
        ///  Add the value with the specified key from the cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="expiry"></param>
        public async void Set(string key, object data, TimeSpan expiry)
        {
            await this.SetAsync(key, data, expiry);
        }

        /// <summary>
        /// Add the value with the specified key from the cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        public async void Set(string key, object data)
        {
            await this.SetAsync(key, data, null);
        }

        /// <summary>
        /// Gets a value indicating whether the value associated with the specified key is cached
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsSet(string key)
        {
            return this.IsSetAsync(key).Result;
        }

        /// <summary>
        /// Removes the value with the specified key from the cache
        /// </summary>
        /// <param name="key"></param>
        public async void Remove(string key)
        {
            await this.RemoveAsync(key);
        }

        /// <summary>
        /// Returns the remaining time to live of a key that has a timeout.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TimeSpan? GetKeyRemainingTimeout(string key)
        {
            return _database.KeyTimeToLive(key);
        }

        /// <summary>
        /// Clear all cache data
        /// </summary>
        public async void Clear()
        {
            await this.ClearAsync();
        }

        /// <summary>
        /// Release all resources associated with this object
        /// </summary>
        public void Dispose()
        {
            //dispose ConnectionMultiplexer
            _connectionMultiplexer?.Dispose();

            //dispose RedLock factory
            _redisLockFactory?.Dispose();
        }

        #endregion Methods

        #region Utilities

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        private async Task<T> GetAsync<T>(string key)
        {
            var serializedItem = await _database.StringGetAsync(key);
            if (!serializedItem.HasValue)
                return default(T);

            var item = JsonConvert.DeserializeObject<T>(serializedItem);
            if (item == null)
                return default(T);

            return item;
        }

        /// <summary>
        /// Gets or sets the values associated with the specified key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        private async Task<List<T>> GetManyAsync<T>(string key)
        {
            var serializedItem = await _database.StringGetAsync(key);
            if (!serializedItem.HasValue)
                return default(List<T>);

            var item = JsonConvert.DeserializeObject<List<T>>(serializedItem);
            if (item.Count == default(int))
                return default(List<T>);

            return item;
        }

        /// <summary>
        /// Adds the specified key and object to the cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        private async Task SetAsync(string key, object data, TimeSpan? expiry)
        {
            if (data == null)
                return;

            var serializedItem = JsonConvert.SerializeObject(data);

            await _database.StringSetAsync(key, serializedItem, expiry);
        }

        /// <summary>
        /// Gets a value indicating whether the value associated with the specified key is cached
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private async Task<bool> IsSetAsync(string key)
        {
            return await _database.KeyExistsAsync(key);
        }

        /// <summary>
        /// Removes the value with the specified key from the cache
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private async Task RemoveAsync(string key)
        {
            //remove item from caches
            await _database.KeyDeleteAsync(key);
        }

        /// <summary>
        /// Clear all cache data
        /// </summary>
        private async Task ClearAsync()
        {
            foreach (var endPoint in GetEndPoints())
            {
                var server = GetServer(endPoint);

                //we can use the code below (commented), but it requires administration permission - ",allowAdmin=true"
                //server.FlushDatabase();

                var keys = server.Keys(database: _database.Database);

                await _database.KeyDeleteAsync(keys.ToArray());
            }
        }

        /// <summary>
        /// Get connection to Redis servers
        /// </summary>
        /// <returns></returns>
        private ConnectionMultiplexer GetConnection()
        {
            if (_connectionMultiplexer != null && _connectionMultiplexer.IsConnected) return _connectionMultiplexer;

            lock (_lock)
            {
                if (_connectionMultiplexer != null && _connectionMultiplexer.IsConnected) return _connectionMultiplexer;

                _connectionMultiplexer?.Dispose();

                _connectionMultiplexer = ConnectionMultiplexer.Connect(_connectionString);
            }

            return _connectionMultiplexer;
        }

        /// <summary>
        ///  Create instance of RedLock factory
        /// </summary>
        /// <returns></returns>
        private RedLockFactory CreateRedisLockFactory()
        {
            var configurationOptions = ConfigurationOptions.Parse(_connectionString);
            var redLockEndPoints = GetEndPoints().Select(endPoint => new RedLockEndPoint
            {
                EndPoint = endPoint,
                Password = configurationOptions.Password,
                Ssl = configurationOptions.Ssl,
                RedisDatabase = configurationOptions.DefaultDatabase,
                ConfigCheckSeconds = configurationOptions.ConfigCheckSeconds,
                ConnectionTimeout = configurationOptions.ConnectTimeout,
                SyncTimeout = configurationOptions.SyncTimeout
            }).ToList();

            return RedLockFactory.Create(redLockEndPoints);
        }

        /// <summary>
        /// Obtain an interactive connection to a database inside Redis
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public IDatabase GetDatabase(int? db = null)
        {
            return GetConnection().GetDatabase(db ?? -1);
        }

        /// <summary>
        /// Obtain a configuration API for an individual server
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        private IServer GetServer(EndPoint endPoint)
        {
            return GetConnection().GetServer(endPoint);
        }

        /// <summary>
        /// Gets all endpoints defined on the server
        /// </summary>
        /// <returns></returns>
        private EndPoint[] GetEndPoints()
        {
            return GetConnection().GetEndPoints();
        }

        #endregion Utilities
    }
}
