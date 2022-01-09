using Microsoft.Extensions.DependencyInjection;
using System;
using Redis.Constant;
using Redis;

namespace INT.Redis.Extensions
{
    /// <summary>
    /// Redis cache factory extension class
    /// </summary>
    public static class RedisCacheFactoryExtension
    {
        #region Methods

        /// <summary>
        /// Add redis cache service
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IServiceCollection AddRedisCache(this IServiceCollection services, Action<RedisSettings> configure)
        {
            services.Configure<RedisSettings>(configure);
            return services.AddSingleton<IRedisCacheManager, RedisCacheManager>();
        }

        #endregion Methods
    }
}
