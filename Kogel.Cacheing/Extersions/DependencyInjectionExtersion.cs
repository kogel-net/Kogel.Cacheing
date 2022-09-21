using Kogel.Cacheing;
using Kogel.Cacheing.Redis;
using System;

#if NETSTANDARD || NETCOREAPP
using Kogel.Cacheing.Memory;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// 
    /// </summary>
    public static class DependencyInjectionExtersion
    {
        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="services"></param>
        /// <param name="setup"></param>
        /// <returns></returns>
        public static IServiceCollection AddCacheing(this IServiceCollection services, Action<RedisCacheConfig> setup = null)
        {
            ProviderManage.services = services;
            //内存缓存注入
            var memoryCache = CacheFactory.BuildMemoryCache();
            services.AddSingleton(memoryCache);
            //redis缓存注入
            if (setup != null)
            {
                var redisCacheFactory = CacheFactory.Build(setup);
                services.AddSingleton(redisCacheFactory);
                services.AddSingleton<ICacheManager>(redisCacheFactory);
            }
            else
            {
                services.AddSingleton<ICacheManager>(memoryCache);
            }
            return services;
        }
    }
}
#endif

namespace Kogel.Cacheing
{

    public static class CacheFactory
    {
        public static RedisCacheManage Build(Action<RedisCacheConfig> action)
        {
            var option = new RedisCacheConfig();
            action(option);

            var cacheManager = RedisCacheManage.Create(option);
            return cacheManager;
        }

        public static RedisCacheManage Build(RedisCacheConfig option)
        {
            var cacheManager = RedisCacheManage.Create(option);
            return cacheManager;
        }


#if NETSTANDARD || NETCOREAPP
        public static MemoryCacheManage BuildMemoryCache()
        {
            var cacheManager = new MemoryCacheManage();
            return cacheManager;
        }
#endif
    }
}