using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#if NETSTANDARD || NETCOREAPP
using Kogel.Cacheing.Memory;
using Microsoft.Extensions.DependencyInjection;
#endif

namespace Kogel.Cacheing
{
    public interface ICacheManager
    {
        bool KeyExists(string cacheKey);

        bool RemoveCache(string cacheKey);

        bool ExpireEntryAt(string cacheKey, TimeSpan cacheOutTime);

        T StringGet<T>(string cacheKey);

        Task<T> StringGetAsync<T>(string cacheKey);

        bool StringSet<T>(string cacheKey, T cacheValue);

        Task<bool> StringSetAsync<T>(string cacheKey, T cacheValue);

        bool StringSet<T>(string cacheKey, T cacheValue, TimeSpan cacheOutTime);

        bool StringDelete(string cacheKey);

        Task<bool> StringSetAsync<T>(string cacheKey, T cacheValue, TimeSpan cacheOutTime);

        double StringDecrement(string cacheKey, double val = 1.0);

        double StringIncrement(string cacheKey, double val = 1.0);

        Task<double> StringDecrementAsync(string cacheKey, double val = 1.0);

        Task<double> StringIncrementAsync(string cacheKey, double val = 1.0);

        bool LockTake(string key, string lockValue, TimeSpan expiry);

        string LockQuery(string key);

        bool LockRelease(string key, string lockValue);

        IMutexDisposable LockMutex(string cacheKey, TimeSpan lockOutTime, int retryAttemptMillseconds = 300, int retryTimes = 100);

        void ExitMutex(string cacheKey);

        IMutexDisposable HLockMutex(string cacheKey, List<string> dataKeys, TimeSpan lockOutTime, int retryAttemptMillseconds = 300, int retryTimes = 100);

        void HExitMutex(string cacheKey, List<string> dataKeys);

        long Publish<T>(string channelId, T msg);

        Task<long> PublishAsync<T>(string channelId, T msg);

        void Subscribe<T>(string channelId, Action<T> handler) where T : class;

        double HashIncrement(string cacheKey, string dataKey, double value = 1.0);

        double HashDecrement(string cacheKey, string dataKey, double value = 1.0);

        List<T> HashKeys<T>(string cacheKey);

        T HashGet<T>(string dataKey);

        T HashGet<T>(string cacheKey, string dataKey);

        IDictionary<string, T> HashGetAll<T>(string cacheKey);

        bool HashSet<T>(string cacheKey, string dataKey, T value);

        bool HashDelete(string cacheKey, string dataKey);

        T ListLeftPop<T>(string cacheKey);

        void ListLeftPush<T>(string cacheKey, T value);

        long ListLength(string cacheKey);

        List<T> ListRange<T>(string cacheKey);

        void ListRemove<T>(string cacheKey, T value);

        void ListRightPush<T>(string cacheKey, T value);

        T ListRightPush<T>(string cacheKey);

        T ListRightPopLeftPush<T>(string source, string destination);

        bool SetAdd<T>(string key, T value);

        bool SetContains<T>(string key, T value);

        long SetLength(string key);

        List<T> SetMembers<T>(string key);

        T SetPop<T>(string key);

        T SetRandomMember<T>(string key);

        List<T> SetRandomMembers<T>(string key, long count);

        bool SetRemove<T>(string key, T value);

        long SetRemove<T>(string key, T[] values);

        dynamic Execute(string script, params object[] objs);

        Task<dynamic> ExecuteAsync(string script, params object[] objs);

        ICacheManager GetMemoryCache();
    }

    /// <summary>
    /// 提供方配置
    /// </summary>
    public class ProviderManage
    {
        internal static object services;

        /// <summary>
        /// 提供方 0为redis 1为memorycache ,默认为redis
        /// </summary>
        /// <param name="value"></param>
        public ICacheManager GetMemoryCache()
        {

#if NETSTANDARD || NETCOREAPP
            var _serviceProvider = (services as IServiceCollection).BuildServiceProvider();
            return _serviceProvider.GetService(typeof(MemoryCacheManage)) as ICacheManager;
#else
            return null;
#endif
        }
    }
}
