﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#if NETSTANDARD || NETCOREAPP
namespace Kogel.Cacheing.Memory
{
    using Microsoft.Extensions.Caching.Memory;
    using Newtonsoft.Json;
    using Polly;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Channels;

    public class MemoryCacheManage : ProviderManage, ICacheManager
    {
        #region 全局变量

        private readonly static IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions() 
        { 
            ExpirationScanFrequency = TimeSpan.FromSeconds(1) 
        });

        private readonly static ConcurrentDictionary<string, Channel<string>> _channels = new ConcurrentDictionary<string, Channel<string>>();

        #endregion

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
            cacheKey = $"{cacheKey}:{dataKey}";
            using (LockMutex($"{cacheKey}:Lock", TimeSpan.FromSeconds(5)))
            {
                double cacheValue = StringGet<double>(cacheKey);
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
                {
                    T cacheItemValue;
                    var cacheItemType = cacheItem.Value.GetType();
                    if (cacheItemType.FullName.Contains("Microsoft.Extensions.Caching.Memory.CacheEntry"))
                    {
                        cacheItemValue = (T)cacheItemType.GetProperty("Value").GetValue(cacheItem.Value);
                    }
                    else
                    {
                        cacheItemValue = (T)cacheItem.Value;
                    }
                    data.Add(cacheItemKey.Replace($"{cacheKey}:", ""), cacheItemValue);
                }
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
            cacheKey = $"{cacheKey}:{dataKey}";
            using (LockMutex($"{cacheKey}:Lock", TimeSpan.FromSeconds(5)))
            {
                double cacheValue = StringGet<double>(cacheKey);
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

        public bool HashSet<T>(string cacheKey, string dataKey, T value, TimeSpan cacheOutTime)
        {
            return StringSet($"{cacheKey}:{dataKey}", value, cacheOutTime);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="dataKey"></param>
        /// <returns></returns>
        public bool HashDelete(string cacheKey, string dataKey)
        {
            return StringDelete($"{cacheKey}:{dataKey}");
        }

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

        public bool HLockTake(string cacheKey, List<string> dataKeys, TimeSpan expire)
        {
            using (LockMutex($"{cacheKey}_HLockTake", expire))
            {
                string hCacheKey = $"{cacheKey}_LockHash";
                var cacheDataKeys = HashGetAll<string>(hCacheKey);
                if ((cacheDataKeys is null || cacheDataKeys.Count == 0) && !cacheDataKeys.Any(x => dataKeys.Contains(x.Key)))
                {
                    dataKeys.ForEach(x => HashSet(hCacheKey, x, "", expire));
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public IMutexDisposable HLockMutex(string cacheKey,
           List<string> dataKeys,
           TimeSpan lockOutTime,
           int retryAttemptMillseconds = 300,
           int retryTimes = 100)
        {
            do
            {
                if (!HLockTake(cacheKey, dataKeys, lockOutTime))
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

        public void HExitMutex(string cacheKey, List<string> dataKeys)
        {
            string hCacheKey = $"{cacheKey}_LockHash";
            dataKeys.ForEach(x => HashDelete(hCacheKey, x));
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
                    return false;
                else
                {
                    StringSet(key, lockValue, expiry);
                    return true;
                }
            }
        }

        private Channel<string> GetChannel(string channelId)
        {
            lock (_channels)
            {
                if (!_channels.TryGetValue(channelId, out Channel<string> channel))
                {
                    channel = Channel.CreateUnbounded<string>();
                    _channels.TryAdd(channelId, channel);
                }
                return channel;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channelId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public long Publish<T>(string channelId, T msg)
        {
            var changel = GetChannel(channelId);
            changel.Writer.TryWrite(msg is string ? msg.ToString() : JsonConvert.SerializeObject(msg));
            return 1;
        }

        public async Task<long> PublishAsync<T>(string channelId, T msg)
        {
            var changel = GetChannel(channelId);
            await changel.Writer.WriteAsync(msg is string ? msg.ToString() : JsonConvert.SerializeObject(msg));
            return 1;
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

        public bool StringDelete(string cacheKey)
        {
            _cache.Remove(cacheKey);
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
        public void Subscribe<T>(string channelId, Action<T> handler)
            where T : class
        {
            Task.Run(async () =>
            {
                var changel = GetChannel(channelId);
                do
                {
                    await changel.Reader.WaitToReadAsync();
                    var msg = await changel.Reader.ReadAsync();
                    handler.Invoke(typeof(T) == typeof(string) ? Convert.ChangeType(msg, typeof(T)) as T : JsonConvert.DeserializeObject<T>(msg));
                    await Task.Delay(100);
                } while (true);
            });
        }
    }
}
#endif
