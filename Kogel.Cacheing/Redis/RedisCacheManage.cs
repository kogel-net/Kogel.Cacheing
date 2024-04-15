using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.Threading;
using Polly;
using Kogel.Cacheing.LoadBalancers;
using Kogel.Cacheing.KetamaHash;

namespace Kogel.Cacheing.Redis
{

    public class RedisCacheManage : ProviderManage, ICacheManager
    {
        private static object _syncCreateInstance = new object();

        private static object _syncCreateClient = new object();

        private static bool _supportSentinal = false;

        private static string _KeyPrefix = "";

        private static readonly int _VIRTUAL_NODE_COUNT = 1024;

        private static KetamaNodeLocator _Locator;

        private static Dictionary<string, ConfigurationOptions> _clusterConfigOptions = new Dictionary<string, ConfigurationOptions>();

        private static Dictionary<string, Dictionary<int, ILoadBalancer<RedisClientHelper>>> _nodeClients = new Dictionary<string, Dictionary<int, ILoadBalancer<RedisClientHelper>>>();

        private readonly int _DbNum;

        private readonly int _NumberOfConnections = 10;

        private RedisCacheManage(int DbNum = 0, int NumberOfConnections = 10)
        {
            _DbNum = DbNum;
            _NumberOfConnections = NumberOfConnections;
        }

        public static RedisCacheManage Create(RedisCacheConfig config)
        {
            ThreadPool.SetMinThreads(200, 200);
            if (string.IsNullOrEmpty(config.KeyPrefix))
            {
                _KeyPrefix = string.Empty;
            }
            else
            {
                _KeyPrefix = config.KeyPrefix + ":";
            }

            if (_Locator == null)
            {
                lock (_syncCreateInstance)
                {
                    if (_Locator == null)
                    {
                        if (string.IsNullOrEmpty(config.SentineList) || !_supportSentinal)
                        {
                            string writeServerList = config.WriteServerList;
                            string readServerList = config.ReadServerList;
                            List<string> list = RedisCacheConfigHelper.SplitString(writeServerList, ",").ToList();
                            List<string> second = RedisCacheConfigHelper.SplitString(readServerList, ",").ToList();
                            List<string> list2 = new List<string>();
                            if (list.Count == 1)
                            {
                                string key = list[0];
                                string text = list[0];
                                if (!_clusterConfigOptions.ContainsKey(text))
                                {
                                    ConfigurationOptions configurationOptions = new ConfigurationOptions();
                                    configurationOptions.ServiceName = text;
                                    configurationOptions.Password = config.Password;
                                    configurationOptions.AbortOnConnectFail = false;
                                    configurationOptions.DefaultDatabase = config.DBNum;
                                    configurationOptions.Ssl = config.Ssl;
                                    foreach (string item in list.Union(second))
                                    {
                                        configurationOptions.EndPoints.Add(item);
                                    }

                                    _clusterConfigOptions.Add(key, configurationOptions);
                                }

                                list2.Add(text);
                            }
                            else
                            {
                                for (int i = 0; i < list.Count; i++)
                                {
                                    if (list[i].IndexOf("@") > 0)
                                    {
                                        string serverClusterName = RedisCacheConfigHelper.GetServerClusterName(list[i]);
                                        RedisCacheConfigHelper.GetServerHost(list[i]);
                                        List<string> serverList = RedisCacheConfigHelper.GetServerList(config.WriteServerList, serverClusterName);
                                        List<string> serverList2 = RedisCacheConfigHelper.GetServerList(config.ReadServerList, serverClusterName);
                                        if (!_clusterConfigOptions.ContainsKey(serverClusterName))
                                        {
                                            ConfigurationOptions configurationOptions2 = new ConfigurationOptions();
                                            configurationOptions2.ServiceName = serverClusterName;
                                            configurationOptions2.Password = config.Password;
                                            configurationOptions2.AbortOnConnectFail = false;
                                            configurationOptions2.DefaultDatabase = config.DBNum;
                                            configurationOptions2.Ssl = config.Ssl;
                                            configurationOptions2.ConnectTimeout = 15000;
                                            configurationOptions2.SyncTimeout = 5000;
                                            configurationOptions2.ResponseTimeout = 15000;
                                            foreach (string item2 in serverList.Union(serverList2).Distinct())
                                            {
                                                configurationOptions2.EndPoints.Add(RedisCacheConfigHelper.GetIP(item2), RedisCacheConfigHelper.GetPort(item2));
                                            }

                                            _clusterConfigOptions.Add(serverClusterName, configurationOptions2);
                                        }

                                        list2.Add(serverClusterName);
                                    }
                                    else
                                    {
                                        string text2 = list[i];
                                        if (!_clusterConfigOptions.ContainsKey(text2))
                                        {
                                            ConfigurationOptions configurationOptions3 = new ConfigurationOptions();
                                            configurationOptions3.ServiceName = text2;
                                            configurationOptions3.Password = config.Password;
                                            configurationOptions3.AbortOnConnectFail = false;
                                            configurationOptions3.DefaultDatabase = config.DBNum;
                                            configurationOptions3.Ssl = config.Ssl;
                                            configurationOptions3.ConnectTimeout = 15000;
                                            configurationOptions3.SyncTimeout = 5000;
                                            configurationOptions3.ResponseTimeout = 15000;
                                            configurationOptions3.EndPoints.Add(RedisCacheConfigHelper.GetIP(text2), RedisCacheConfigHelper.GetPort(text2));
                                            _clusterConfigOptions.Add(text2, configurationOptions3);
                                        }

                                        list2.Add(text2);
                                    }
                                }
                            }

                            _Locator = new KetamaNodeLocator(list2, _VIRTUAL_NODE_COUNT);
                        }
                        else
                        {
                            List<string> list3 = new List<string>();
                            List<string> list4 = new List<string>();
                            List<string> list5 = RedisCacheConfigHelper.SplitString(config.SentineList, ",").ToList();
                            for (int j = 0; j < list5.Count; j++)
                            {
                                List<string> list6 = RedisCacheConfigHelper.SplitString(list5[j], "@").ToList();
                                string text3 = list6[0];
                                string text4 = list6[1];
                                List<string> list7 = RedisCacheConfigHelper.SplitString(text4, ":").ToList();
                                string host = list7[0];
                                int port = int.Parse(list7[1]);
                                list3.Add(text3);
                                list4.Add(text4);
                                if (!_clusterConfigOptions.ContainsKey(text4))
                                {
                                    ConfigurationOptions configurationOptions4 = new ConfigurationOptions();
                                    configurationOptions4.ServiceName = text3;
                                    configurationOptions4.EndPoints.Add(host, port);
                                    configurationOptions4.AbortOnConnectFail = false;
                                    configurationOptions4.DefaultDatabase = config.DBNum;
                                    configurationOptions4.TieBreaker = "";
                                    configurationOptions4.CommandMap = CommandMap.Sentinel;
                                    configurationOptions4.DefaultVersion = new Version(3, 0);
                                    _clusterConfigOptions[text4] = configurationOptions4;
                                }
                                else
                                {
                                    ConfigurationOptions configurationOptions5 = _clusterConfigOptions[text4];
                                    configurationOptions5.EndPoints.Add(host, port);
                                    _clusterConfigOptions[text4] = configurationOptions5;
                                }
                            }

                            _Locator = new KetamaNodeLocator(list4, _VIRTUAL_NODE_COUNT);
                        }
                    }
                }
            }

            return new RedisCacheManage(config.DBNum, config.NumberOfConnections);
        }

        private RedisClientHelper GetPooledClientManager(string cacheKey)
        {
            string primary = _Locator.GetPrimary(_KeyPrefix + cacheKey);
            if (_nodeClients.ContainsKey(primary))
            {
                Dictionary<int, ILoadBalancer<RedisClientHelper>> dictionary = _nodeClients[primary];
                if (dictionary.ContainsKey(_DbNum))
                {
                    return dictionary[_DbNum].Lease();
                }

                return GetClientHelper(primary);
            }

            return GetClientHelper(primary);
        }

        private RedisClientHelper GetClientHelper(string nodeName)
        {
            lock (_syncCreateClient)
            {
                if (_nodeClients.ContainsKey(nodeName))
                {
                    Dictionary<int, ILoadBalancer<RedisClientHelper>> dictionary = _nodeClients[nodeName];
                    if (!dictionary.ContainsKey(_DbNum))
                    {
                        dictionary[_DbNum] = GetConnectionLoadBalancer(nodeName);
                    }
                }
                else
                {
                    Dictionary<int, ILoadBalancer<RedisClientHelper>> dictionary2 = new Dictionary<int, ILoadBalancer<RedisClientHelper>>();
                    dictionary2[_DbNum] = GetConnectionLoadBalancer(nodeName);
                    _nodeClients[nodeName] = dictionary2;
                }

                return _nodeClients[nodeName][_DbNum].Lease();
            }
        }

        private ILoadBalancer<RedisClientHelper> GetConnectionLoadBalancer(string nodeName)
        {
            return new DefaultLoadBalancerFactory<RedisClientHelper>().Get(delegate
            {
                List<RedisClientHelper> list = new List<RedisClientHelper>();
                for (int i = 0; i < _NumberOfConnections; i++)
                {
                    list.Add(new RedisClientHelper(_DbNum, _clusterConfigOptions[nodeName], _KeyPrefix));
                }

                return list;
            });
        }

        public bool KeyExists(string cacheKey)
        {
            if (!string.IsNullOrEmpty(cacheKey))
            {
                return GetPooledClientManager(cacheKey).StringGet(cacheKey) != null;
            }

            return false;
        }

        public bool RemoveCache(string cacheKey)
        {
            return GetPooledClientManager(cacheKey).KeyDelete(cacheKey);
        }

        public bool ExpireEntryAt(string cacheKey, TimeSpan cacheOutTime)
        {
            return GetPooledClientManager(cacheKey).KeyExpire(cacheKey, cacheOutTime);
        }

        public T StringGet<T>(string cacheKey)
        {
            T result = default(T);
            if (!string.IsNullOrEmpty(cacheKey))
            {
                return GetPooledClientManager(cacheKey).StringGet<T>(cacheKey);
            }

            return result;
        }

        public async Task<T> StringGetAsync<T>(string cacheKey)
        {
            T result = default(T);
            if (!string.IsNullOrEmpty(cacheKey))
            {
                return await GetPooledClientManager(cacheKey).StringGetAsync<T>(cacheKey);
            }

            return result;
        }

        public bool StringSet<T>(string cacheKey, T cacheValue)
        {
            if (!string.IsNullOrEmpty(cacheKey) && cacheValue != null)
            {
                return GetPooledClientManager(cacheKey).StringSet(cacheKey, cacheValue);
            }

            return false;
        }

        public async Task<bool> StringSetAsync<T>(string cacheKey, T cacheValue)
        {
            if (!string.IsNullOrEmpty(cacheKey) && cacheValue != null)
            {
                return await GetPooledClientManager(cacheKey).StringSetAsync(cacheKey, cacheValue);
            }

            return false;
        }

        public bool StringSet<T>(string cacheKey, T cacheValue, TimeSpan cacheOutTime)
        {
            if (!string.IsNullOrEmpty(cacheKey) && cacheValue != null)
            {
                return GetPooledClientManager(cacheKey).StringSet(cacheKey, cacheValue, cacheOutTime);
            }

            return false;
        }

        public bool StringDelete(string cacheKey)
        {
            return GetPooledClientManager(cacheKey).StringDelete(cacheKey);
        }

        public async Task<bool> StringSetAsync<T>(string cacheKey, T cacheValue, TimeSpan cacheOutTime)
        {
            if (!string.IsNullOrEmpty(cacheKey) && cacheValue != null)
            {
                return await GetPooledClientManager(cacheKey).StringSetAsync(cacheKey, cacheValue, cacheOutTime);
            }

            return false;
        }

        public double StringDecrement(string cacheKey, double val = 1.0)
        {
            return GetPooledClientManager(cacheKey).StringDecrement(cacheKey);
        }

        public async Task<double> StringDecrementAsync(string cacheKey, double val = 1.0)
        {
            return await GetPooledClientManager(cacheKey).StringDecrementAsync(cacheKey);
        }

        public double StringIncrement(string cacheKey, double val = 1.0)
        {
            return GetPooledClientManager(cacheKey).StringIncrement(cacheKey);
        }

        public async Task<double> StringIncrementAsync(string cacheKey, double val = 1.0)
        {
            return await GetPooledClientManager(cacheKey).StringIncrementAsync(cacheKey);
        }

        public long Publish<T>(string channelId, T msg)
        {
            return GetPooledClientManager(channelId).Publish(channelId, msg);
        }

        public Task<long> PublishAsync<T>(string channelId, T msg)
        {
            return GetPooledClientManager(channelId).PublishAsync(channelId, msg);
        }

        public void Subscribe<T>(string channelId, Action<T> handler) where T : class
        {
            GetPooledClientManager(channelId).Subscribe(channelId, delegate (RedisChannel channel, T value)
            {
                handler(value);
            });
        }

        public void Subscribe(string channelId, Action<object> handler)
        {
            GetPooledClientManager(channelId).Subscribe(channelId, delegate (RedisChannel channel, object value)
            {
                handler(value);
            });
        }

        public double HashIncrement(string cacheKey, string dataKey, double value = 1.0)
        {
            return GetPooledClientManager(cacheKey).HashIncrement(cacheKey, dataKey, value);
        }

        public double HashDecrement(string cacheKey, string dataKey, double value = 1.0)
        {
            return GetPooledClientManager(cacheKey).HashDecrement(cacheKey, dataKey, value);
        }

        public List<T> HashKeys<T>(string cacheKey)
        {
            return GetPooledClientManager(cacheKey).HashKeys<T>(cacheKey);
        }

        public IDictionary<string, T> HashGetAll<T>(string cacheKey)
        {
            return GetPooledClientManager(cacheKey).HashGetAll<T>(cacheKey);
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
            return GetPooledClientManager(cacheKey).HashGet<T>(cacheKey, dataKey);
        }

        public bool HashSet<T>(string cacheKey, string dataKey, T value)
        {
            return GetPooledClientManager(cacheKey).HashSet(cacheKey, dataKey, value);
        }

        public bool HashDelete(string cacheKey, string dataKey)
        {
            return GetPooledClientManager(cacheKey).HashDelete(cacheKey, dataKey);
        }

        public bool LockTake(string cacheKey, string value, TimeSpan expire)
        {
            return GetPooledClientManager(cacheKey).LockTake(cacheKey, value, expire);
        }

        public bool LockRelease(string cacheKey, string value)
        {
            return GetPooledClientManager(cacheKey).LockRelease(cacheKey, value);
        }

        public string LockQuery(string cacheKey)
        {
            return GetPooledClientManager(cacheKey).LockQuery(cacheKey);
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
                        HashSet(hCacheKey, x, "");
                    });
                    ExpireEntryAt(hCacheKey, expire);
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

        public T ListLeftPop<T>(string cacheKey)
        {
            return GetPooledClientManager(cacheKey).ListLeftPop<T>(cacheKey);
        }

        public void ListLeftPush<T>(string cacheKey, T value)
        {
            GetPooledClientManager(cacheKey).ListLeftPush(cacheKey, value);
        }

        public long ListLength(string cacheKey)
        {
            return GetPooledClientManager(cacheKey).ListLength(cacheKey);
        }

        public List<T> ListRange<T>(string cacheKey)
        {
            return GetPooledClientManager(cacheKey).ListRange<T>(cacheKey);
        }

        public void ListRemove<T>(string cacheKey, T value)
        {
            GetPooledClientManager(cacheKey).ListRemove(cacheKey, value);
        }

        public void ListRightPush<T>(string cacheKey, T value)
        {
            GetPooledClientManager(cacheKey).ListRightPush(cacheKey, value);
        }

        public T ListRightPush<T>(string cacheKey)
        {
            return GetPooledClientManager(cacheKey).ListRightPop<T>(cacheKey);
        }

        public T ListRightPopLeftPush<T>(string sourceCacheKey, string destCacheKey)
        {
            return GetPooledClientManager(sourceCacheKey).ListRightPopLeftPush<T>(sourceCacheKey, destCacheKey);
        }

        public bool SetAdd<T>(string key, T value)
        {
            return GetPooledClientManager(key).SetAdd(key, value);
        }

        public bool SetContains<T>(string key, T value)
        {
            return GetPooledClientManager(key).SetContains(key, value);
        }

        public long SetLength(string key)
        {
            return GetPooledClientManager(key).SetLength(key);
        }

        public List<T> SetMembers<T>(string key)
        {
            return GetPooledClientManager(key).SetMembers<T>(key);
        }

        public T SetPop<T>(string key)
        {
            return GetPooledClientManager(key).SetPop<T>(key);
        }

        public T SetRandomMember<T>(string key)
        {
            return GetPooledClientManager(key).SetRandomMember<T>(key);
        }

        public List<T> SetRandomMembers<T>(string key, long count)
        {
            return GetPooledClientManager(key).SetRandomMembers<T>(key, count);
        }

        public bool SetRemove<T>(string key, T value)
        {
            return GetPooledClientManager(key).SetRemove(key, value);
        }

        public long SetRemove<T>(string key, T[] values)
        {
            return GetPooledClientManager(key).SetRemove(key, values);
        }

        public dynamic Execute(string command, params object[] objs)
        {
            return GetPooledClientManager(command).Execute(command, objs);
        }

        public Task<dynamic> ExecuteAsync(string command, params object[] objs)
        {
            return GetPooledClientManager(command).ExecuteAsync(command, objs);
        }
    }
}