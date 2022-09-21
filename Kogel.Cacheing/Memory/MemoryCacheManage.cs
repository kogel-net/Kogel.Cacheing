using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NETSTANDARD || NETCOREAPP
namespace Kogel.Cacheing.Memory
{
    using Microsoft.Extensions.Caching.Memory;
    using Polly;
    using System.Collections;
    using System.Reflection;
    using System.Threading;

    public class MemoryCacheManage : ProviderManage, ICacheManager
    {
        #region 全局变量

        private readonly static IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions() { });

        /// <summary>
        /// 频道列表
        /// </summary>
        private readonly static Dictionary<string, Queue<object>> _channel = new Dictionary<string, Queue<object>>();

        public dynamic Execute(string script, params object[] objs)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public Task<dynamic> ExecuteAsync(string script, params object[] objs)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        /// <summary>
        /// 释放互斥锁
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void ExitMutex(string cacheKey)
        {
            var polly = Policy.Handle<Exception>()
                  .WaitAndRetry(10, retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt)), (exception, timespan, retryCount, context) =>
                  {
                      Console.WriteLine($"执行异常,重试次数：{retryCount},【异常来自：{exception.GetType().Name}】.");
                  });
            polly.Execute(() =>
            {
                LockRelease(cacheKey, "");
            });
        }

        /// <summary>
        /// 设置过期时间
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="cacheOutTime"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool ExpireEntryAt(string cacheKey, TimeSpan cacheOutTime)
        {
            _cache.Set(cacheKey, StringGet<object>(cacheKey), new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(cacheOutTime.TotalSeconds)
            });
            return true;
        }

        /// <summary>
        /// 为数字减少val
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="dataKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public double HashDecrement(string cacheKey, string dataKey, double value = 1)
        {
            using (LockMutex($"{cacheKey}:{dataKey}", TimeSpan.FromSeconds(5)))
            {
                double cacheValue = StringGet<double>($"{cacheKey}:{dataKey}");
                cacheValue -= value;
                _cache.Set(cacheKey, cacheValue);
                return cacheValue;
            }
        }

        /// <summary>
        /// 获取hash缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataKey"></param>
        /// <returns></returns>
        public T HashGet<T>(string dataKey)
        {
            string cacheKey = dataKey;
            if (dataKey.IndexOf("_") != -1)
                cacheKey = dataKey.Substring(0, dataKey.IndexOf("_"));
            return HashGet<T>(cacheKey, dataKey);
        }

        /// <summary>
        /// 获取hash缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="dataKey"></param>
        /// <returns></returns>
        public T HashGet<T>(string cacheKey, string dataKey)
        {
            return StringGet<T>($"{cacheKey}:{dataKey}");
        }

        /// <summary>
        /// 获取键下所有缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public IDictionary<string, T> HashGetAll<T>(string cacheKey)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var entries = _cache.GetType().GetField("_entries", flags).GetValue(_cache);
            var cacheItems = entries as IDictionary;
            var data = new Dictionary<string, T>();
            if (cacheItems == null) return data;
            foreach (DictionaryEntry cacheItem in cacheItems)
            {
                var cacheItemKey = cacheItem.Key.ToString();
                if (cacheItemKey.StartsWith($"{cacheKey}:"))
                    data.Add(cacheItemKey.Replace($"{cacheKey}:", ""), (T)cacheItem.Value);
            }
            return data;
        }

        /// <summary>
        /// 为数字增加val
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="dataKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public double HashIncrement(string cacheKey, string dataKey, double value = 1)
        {
            using (LockMutex($"{cacheKey}:{dataKey}", TimeSpan.FromSeconds(5)))
            {
                double cacheValue = StringGet<double>($"{cacheKey}:{dataKey}");
                cacheValue += value;
                _cache.Set(cacheKey, cacheValue);
                return cacheValue;
            }
        }

        /// <summary>
        /// 获取键下所有缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public List<T> HashKeys<T>(string cacheKey)
        {
            return HashGetAll<T>(cacheKey).Select(x => x.Value).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="dataKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool HashSet<T>(string cacheKey, string dataKey, T value)
        {
            return StringSet($"{cacheKey}:{dataKey}", value);
        }

        #endregion

        /// <summary>
        /// 缓存是否存在
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public bool KeyExists(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
                throw new ArgumentNullException(nameof(cacheKey));
            return _cache.TryGetValue(cacheKey, out _);
        }

        public T ListLeftPop<T>(string cacheKey)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public void ListLeftPush<T>(string cacheKey, T value)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public long ListLength(string cacheKey)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public List<T> ListRange<T>(string cacheKey)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public void ListRemove<T>(string cacheKey, T value)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public T ListRightPopLeftPush<T>(string source, string destination)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public void ListRightPush<T>(string cacheKey, T value)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public T ListRightPush<T>(string cacheKey)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        /// <summary>
        /// 设置互斥锁
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="lockOutTime"></param>
        /// <param name="retryAttemptMillseconds"></param>
        /// <param name="retryTimes"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public IMutexDisposable LockMutex(string cacheKey, TimeSpan lockOutTime, int retryAttemptMillseconds = 300, int retryTimes = 100)
        {
            do
            {
                if (!LockTake(cacheKey, "", lockOutTime))
                {
                    retryTimes--;
                    if (retryTimes < 0)
                    {
                        //超时异常
                        throw new Exception($"互斥锁超时,cacheKey:{cacheKey}");
                    }

                    if (retryAttemptMillseconds > 0)
                    {
                        Console.WriteLine($"Wait Lock {cacheKey} to {retryAttemptMillseconds} millseconds");
                        //获取锁失败则进行锁等待
                        Thread.Sleep(retryAttemptMillseconds);
                    }
                }
                else
                {
                    return new MutexDisposable(this, cacheKey);
                }
            }
            while (retryTimes > 0);
            //超时异常
            throw new Exception($"互斥锁超时,cacheKey:{cacheKey}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public string LockQuery(string key)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="lockValue"></param>
        /// <returns></returns>
        public bool LockRelease(string key, string lockValue)
        {
            return RemoveCache(key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="lockValue"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public bool LockTake(string key, string lockValue, TimeSpan expiry)
        {
            lock (_cache)
            {
                if (KeyExists(key))
                    return true;
                else
                {
                    StringSet(key, lockValue, expiry);
                    return false;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channelId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public long Publish<T>(string channelId, T msg) => PublishObj(channelId, msg);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channelId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public long PublishObj(string channelId, object msg)
        {
            lock (_channel)
            {
                Queue<object> queue;
                if (_channel.ContainsKey(channelId))
                {
                    queue = _channel[channelId];
                    queue.Enqueue(msg);
                    _channel[channelId] = queue;
                }
                else
                {
                    queue = new Queue<object>();
                    queue.Enqueue(msg);
                    _channel.Add(channelId, queue);
                }
                return queue.LongCount();
            }
        }

        /// <summary>
        /// 删除缓存
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool RemoveCache(string cacheKey)
        {
            _cache.Remove(cacheKey);
            return true;
        }

        public bool SetAdd<T>(string key, T value)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public bool SetContains<T>(string key, T value)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public long SetLength(string key)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public List<T> SetMembers<T>(string key)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public T SetPop<T>(string key)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public T SetRandomMember<T>(string key)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public List<T> SetRandomMembers<T>(string key, long count)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public bool SetRemove<T>(string key, T value)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public long SetRemove<T>(string key, T[] values)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        /// <summary>
        /// 为数字减少val
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public double StringDecrement(string cacheKey, double val = 1)
        {
            return HashDecrement(cacheKey, "String", val);
        }

        /// <summary>
        /// 为数字减少val
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public Task<double> StringDecrementAsync(string cacheKey, double val = 1)
        {
            return Task.Run(() => StringDecrement(cacheKey, val));
        }

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public T StringGet<T>(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
                throw new ArgumentNullException(nameof(cacheKey));

            if (_cache.TryGetValue(cacheKey, out T _value))
                return _value;

            return default;
        }

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public async Task<T> StringGetAsync<T>(string cacheKey)
        {
            return await Task.Run(() => StringGet<T>(cacheKey));
        }

        /// <summary>
        /// 为数字增加val
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public double StringIncrement(string cacheKey, double val = 1)
        {
            return HashIncrement(cacheKey, "String", val);
        }

        /// <summary>
        /// 为数字增加val
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public async Task<double> StringIncrementAsync(string cacheKey, double val = 1)
        {
            return await Task.Run(() => StringIncrement(cacheKey, val));
        }

        /// <summary>
        /// 写入缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="cacheValue"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool StringSet<T>(string cacheKey, T cacheValue)
        {
            return StringSet(cacheKey, cacheValue, TimeSpan.FromMinutes(30));
        }

        /// <summary>
        /// 写入缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="cacheValue"></param>
        /// <param name="cacheOutTime"></param>
        /// <returns></returns>
        public bool StringSet<T>(string cacheKey, T cacheValue, TimeSpan cacheOutTime)
        {
            _cache.Set(cacheKey, cacheValue, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(cacheOutTime.TotalSeconds)
            });
            return true;
        }

        /// <summary>
        /// 写入缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="cacheValue"></param>
        /// <returns></returns>
        public async Task<bool> StringSetAsync<T>(string cacheKey, T cacheValue)
        {
            return await Task.Run(() => StringSet(cacheKey, cacheValue));
        }

        /// <summary>
        /// 写入缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="cacheValue"></param>
        /// <param name="cacheOutTime"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<bool> StringSetAsync<T>(string cacheKey, T cacheValue, TimeSpan cacheOutTime)
        {
            return await Task.Run(() => StringSet(cacheKey, cacheValue, cacheOutTime));
        }

        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="handler"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void Subscribe(string channelId, Action<object> handler)
        {
            Thread currentThread = Thread.CurrentThread;
            Task.Run(async () =>
            {
                Thread _currentThread = currentThread;
                do
                {
                    if ((_currentThread.ThreadState & ThreadState.AbortRequested) != 0)
                        break;
                    if (_channel.ContainsKey(channelId))
                    {
                        lock (_channel)
                        {
                            Queue<object> queue = _channel[channelId];
                            object msg = queue.Dequeue();
                            if (msg != null)
                            {
                                handler(msg);
                                _channel[channelId] = queue;
                            }
                        }
                    }
                    await Task.Delay(100);
                } while (true);
            });
        }
    }
}
#endif
