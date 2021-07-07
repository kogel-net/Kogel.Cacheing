using Kogel.Cacheing;
using Kogel.Cacheing.StackExchange;
using Kogel.Cacheing.StackExchangeImplement;
using System;
#if NETSTANDARD || NETCOREAPP
namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtersion
    {
        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="services"></param>
        /// <param name="setup"></param>
        /// <returns></returns>
        public static IServiceCollection AddCacheing(this IServiceCollection services, Action<RedisCacheConfig> setup)
        {
            //redis缓存注入
            services.AddSingleton<ICacheManager>(CacheFactory.Build(setup));
            return services;
        }
    }
}
#endif

namespace Kogel.Cacheing
{

    public static class CacheFactory
    {
        public static ICacheManager Build(Action<RedisCacheConfig> action)
        {
            var option = new RedisCacheConfig();
            action(option);

            var cacheManager = RedisCacheManage.Create(option);
            return cacheManager;
        }

        public static ICacheManager Build(RedisCacheConfig option)
        {
            var cacheManager = RedisCacheManage.Create(option);
            return cacheManager;
        }
    }
}