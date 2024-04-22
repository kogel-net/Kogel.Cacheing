using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;

using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json;

using Polly;


namespace Kogel.Cacheing.Memory
{
    public class MemoryCacheManage : ProviderManage, ICacheManager
    {
        private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions
        {
            ExpirationScanFrequency = TimeSpan.FromSeconds(1.0)
        });

        private static readonly ConcurrentDictionary<string, Channel<string>> _channels = new ConcurrentDictionary<string, Channel<string>>();

        public dynamic Execute(string script, params object[] objs)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public Task<dynamic> ExecuteAsync(string script, params object[] objs)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public void ExitMutex(string cacheKey)
        {
            Policy.Handle<Exception>().WaitAndRetry(10, (int retryAttempt) => TimeSpan.FromMilliseconds(Math.Pow(2.0, retryAttempt)), delegate (Exception exception, TimeSpan timespan, int retryCount, Context context)
            {
                Console.WriteLine($"执行异常,重试次数：{retryCount},【异常来自：{exception.GetType().Name}】.");
            }).Execute(delegate
            {
                LockRelease(cacheKey, "");
            });
        }

        public bool ExpireEntryAt(string cacheKey, TimeSpan cacheOutTime)
        {
            _cache.Set(cacheKey, StringGet<object>(cacheKey), new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(cacheOutTime.TotalSeconds)
            });
            return true;
        }

        public double HashDecrement(string cacheKey, string dataKey, double value = 1.0)
        {
            cacheKey = cacheKey + ":" + dataKey;
            using (LockMutex(cacheKey + ":Lock", TimeSpan.FromSeconds(5.0)))
            {
                double num = StringGet<double>(cacheKey);
                num -= value;
                _cache.Set(cacheKey, num);
                return num;
            }
        }

        public T HashGet<T>(string dataKey)
        {
            string cacheKey = dataKey;
            if (dataKey.IndexOf("_") != -1)
            {
                cacheKey = dataKey.Substring(0, dataKey.IndexOf("_"));
            }

            return HashGet<T>(cacheKey, dataKey);
        }

        public T HashGet<T>(string cacheKey, string dataKey)
        {
            return StringGet<T>(cacheKey + ":" + dataKey);
        }

        public IDictionary<string, T> HashGetAll<T>(string cacheKey)
        {
            IDictionary dictionary = GetEntries();
            Dictionary<string, T> dictionary2 = new Dictionary<string, T>();
            if (dictionary == null)
            {
                return dictionary2;
            }

            foreach (DictionaryEntry item in dictionary)
            {
                string text = item.Key.ToString();
                if (text.StartsWith(cacheKey + ":"))
                {
                    Type type = item.Value.GetType();
                    dictionary2.Add(value: (!type.FullName.Contains("Microsoft.Extensions.Caching.Memory.CacheEntry")) ? ((T)item.Value) : ((T)type.GetProperty("Value").GetValue(item.Value)), key: text.Replace(cacheKey + ":", ""));
                }
            }

            return dictionary2;
        }

        private IDictionary GetEntries()
        {
            var coherentState = _cache.GetType().GetField("_coherentState", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_cache);
            return coherentState.GetType().GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(coherentState) as IDictionary;
        }

        public double HashIncrement(string cacheKey, string dataKey, double value = 1.0)
        {
            cacheKey = cacheKey + ":" + dataKey;
            using (LockMutex(cacheKey + ":Lock", TimeSpan.FromSeconds(5.0)))
            {
                double num = StringGet<double>(cacheKey);
                num += value;
                _cache.Set(cacheKey, num);
                return num;
            }
        }

        public List<T> HashKeys<T>(string cacheKey)
        {
            return (from x in HashGetAll<T>(cacheKey)
                    select x.Value).ToList();
        }

        public bool HashSet<T>(string cacheKey, string dataKey, T value)
        {
            return StringSet(cacheKey + ":" + dataKey, value);
        }

        public bool HashSet<T>(string cacheKey, string dataKey, T value, TimeSpan cacheOutTime)
        {
            return StringSet(cacheKey + ":" + dataKey, value, cacheOutTime);
        }

        public bool HashDelete(string cacheKey, string dataKey)
        {
            return StringDelete(cacheKey + ":" + dataKey);
        }

        public bool KeyExists(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                throw new ArgumentNullException("cacheKey");
            }

            object value;
            return _cache.TryGetValue(cacheKey, out value);
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

        public IMutexDisposable LockMutex(string cacheKey, TimeSpan lockOutTime, int retryAttemptMillseconds = 300, int retryTimes = 100)
        {
            do
            {
                if (!LockTake(cacheKey, "", lockOutTime))
                {
                    retryTimes--;
                    if (retryTimes < 0)
                    {
                        throw new Exception("互斥锁超时,cacheKey:" + cacheKey);
                    }

                    if (retryAttemptMillseconds > 0)
                    {
                        Console.WriteLine($"Wait Lock {cacheKey} to {retryAttemptMillseconds} millseconds");
                        Thread.Sleep(retryAttemptMillseconds);
                    }

                    continue;
                }

                return new MutexDisposable(this, cacheKey);
            }
            while (retryTimes > 0);
            throw new Exception("互斥锁超时,cacheKey:" + cacheKey);
        }

        public bool HLockTake(string cacheKey, List<string> dataKeys, TimeSpan expire)
        {
            using (LockMutex(cacheKey + "_HLockTake", expire))
            {
                string hCacheKey = cacheKey + "_LockHash";
                IDictionary<string, string> dictionary = HashGetAll<string>(hCacheKey);
                if ((dictionary == null || dictionary.Count == 0) && !dictionary.Any((KeyValuePair<string, string> x) => dataKeys.Contains(x.Key)))
                {
                    dataKeys.ForEach(delegate (string x)
                    {
                        HashSet(hCacheKey, x, "", expire);
                    });
                    return true;
                }

                return false;
            }
        }

        public IMutexDisposable HLockMutex(string cacheKey, List<string> dataKeys, TimeSpan lockOutTime, int retryAttemptMillseconds = 300, int retryTimes = 100)
        {
            do
            {
                if (!HLockTake(cacheKey, dataKeys, lockOutTime))
                {
                    retryTimes--;
                    if (retryTimes < 0)
                    {
                        throw new Exception("互斥锁超时,cacheKey:" + cacheKey);
                    }

                    if (retryAttemptMillseconds > 0)
                    {
                        Console.WriteLine($"Wait Lock {cacheKey} to {retryAttemptMillseconds} millseconds");
                        Thread.Sleep(retryAttemptMillseconds);
                    }

                    continue;
                }

                return new MutexDisposable(this, cacheKey, isHLockMutex: true, dataKeys);
            }
            while (retryTimes > 0);
            throw new Exception("互斥锁超时,cacheKey:" + cacheKey);
        }

        public void HExitMutex(string cacheKey, List<string> dataKeys)
        {
            string hCacheKey = cacheKey + "_LockHash";
            dataKeys.ForEach(delegate (string x)
            {
                HashDelete(hCacheKey, x);
            });
        }

        public string LockQuery(string key)
        {
            throw new NotImplementedException("内存缓存没有此操作");
        }

        public bool LockRelease(string key, string lockValue)
        {
            return RemoveCache(key);
        }

        public bool LockTake(string key, string lockValue, TimeSpan expiry)
        {
            lock (_cache)
            {
                if (KeyExists(key))
                {
                    return false;
                }

                StringSet(key, lockValue, expiry);
                return true;
            }
        }

        private Channel<string> GetChannel(string channelId)
        {
            lock (_channels)
            {
                if (!_channels.TryGetValue(channelId, out var value))
                {
                    value = Channel.CreateUnbounded<string>();
                    _channels.TryAdd(channelId, value);
                }

                return value;
            }
        }

        public long Publish<T>(string channelId, T msg)
        {
            GetChannel(channelId).Writer.TryWrite((msg is string) ? msg.ToString() : JsonConvert.SerializeObject(msg));
            return 1L;
        }

        public async Task<long> PublishAsync<T>(string channelId, T msg)
        {
            await GetChannel(channelId).Writer.WriteAsync((msg is string) ? msg.ToString() : JsonConvert.SerializeObject(msg));
            return 1L;
        }

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

        public double StringDecrement(string cacheKey, double val = 1.0)
        {
            return HashDecrement(cacheKey, "String", val);
        }

        public Task<double> StringDecrementAsync(string cacheKey, double val = 1.0)
        {
            return Task.Run(() => StringDecrement(cacheKey, val));
        }

        public T StringGet<T>(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                throw new ArgumentNullException("cacheKey");
            }

            if (_cache.TryGetValue<T>(cacheKey, out T value))
            {
                return value;
            }

            return default(T);
        }

        public async Task<T> StringGetAsync<T>(string cacheKey)
        {
            return await Task.Run(() => StringGet<T>(cacheKey));
        }

        public double StringIncrement(string cacheKey, double val = 1.0)
        {
            return HashIncrement(cacheKey, "String", val);
        }

        public async Task<double> StringIncrementAsync(string cacheKey, double val = 1.0)
        {
            return await Task.Run(() => StringIncrement(cacheKey, val));
        }

        public bool StringSet<T>(string cacheKey, T cacheValue)
        {
            return StringSet(cacheKey, cacheValue, TimeSpan.FromMinutes(30.0));
        }

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

        public async Task<bool> StringSetAsync<T>(string cacheKey, T cacheValue)
        {
            return await Task.Run(() => StringSet(cacheKey, cacheValue));
        }

        public async Task<bool> StringSetAsync<T>(string cacheKey, T cacheValue, TimeSpan cacheOutTime)
        {
            return await Task.Run(() => StringSet(cacheKey, cacheValue, cacheOutTime));
        }

        public void Subscribe<T>(string channelId, Action<T> handler) where T : class
        {
            Task.Run(async delegate
            {
                Channel<string> changel = GetChannel(channelId);
                while (true)
                {
                    await changel.Reader.WaitToReadAsync();
                    string value = await changel.Reader.ReadAsync();
                    handler((T)((typeof(T) == typeof(string)) ? ((object)(Convert.ChangeType(value, typeof(T)) as T)) : ((object)JsonConvert.DeserializeObject<T>(value))));
                    await Task.Delay(100);
                }
            });
        }
    }
}
