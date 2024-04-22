using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kogel.Cacheing.Redis
{
    internal class RedisClientHelper
    {
        private readonly ConnectionMultiplexer _conn;

        public string CustomKey;

        private string KeyPrefix = "";

        private int DbNum { get; }

        public RedisClientHelper(int dbNum, string connectionString, string KeyPrefix)
        {
            DbNum = dbNum;
            this.KeyPrefix = KeyPrefix;
            _conn = RedisConnectionHelp.CreateConnect(connectionString);
        }

        public RedisClientHelper(int dbNum, ConfigurationOptions configOptions, string KeyPrefix)
        {
            DbNum = dbNum;
            this.KeyPrefix = KeyPrefix;
            _conn = RedisConnectionHelp.CreateConnect(configOptions);
        }

        public dynamic Execute(string command, params object[] objs)
        {
            return Do((IDatabase db) => db.Execute(command, objs));
        }

        public async Task<dynamic> ExecuteAsync(string script, params object[] objs)
        {
            return await Do((IDatabase db) => db.ExecuteAsync(script, objs));
        }

        public bool StringSet(string key, string value, TimeSpan? expiry = null)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase db) => db.StringSet(key, value, expiry));
        }

        public bool StringSet(List<KeyValuePair<RedisKey, RedisValue>> keyValues)
        {
            List<KeyValuePair<RedisKey, RedisValue>> newkeyValues = keyValues.Select((KeyValuePair<RedisKey, RedisValue> p) => new KeyValuePair<RedisKey, RedisValue>(AddSysCustomKey(p.Key), p.Value)).ToList();
            return Do((IDatabase db) => db.StringSet(newkeyValues.ToArray()));
        }

        public bool StringSet<T>(string key, T obj, TimeSpan? expiry = null)
        {
            key = AddSysCustomKey(key);
            string json = ConvertJson(obj);
            return Do((IDatabase db) => db.StringSet(key, json, expiry));
        }

        public string StringGet(string key)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase db) => db.StringGet(key));
        }

        public RedisValue[] StringGet(List<string> listKey)
        {
            List<string> newKeys = listKey.Select(AddSysCustomKey).ToList();
            return Do((IDatabase db) => db.StringGet(ConvertRedisKeys(newKeys)));
        }

        public T StringGet<T>(string key)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase db) => ConvertObj<T>(db.StringGet(key)));
        }

        public bool StringDelete(string key)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase db) => db.KeyDelete(key));
        }

        public double StringIncrement(string key, double val = 1.0)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase db) => db.StringIncrement(key, val));
        }

        public double StringDecrement(string key, double val = 1.0)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase db) => db.StringDecrement(key, val));
        }

        public async Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null)
        {
            key = AddSysCustomKey(key);
            return await Do((IDatabase db) => db.StringSetAsync(key, value, expiry));
        }

        public async Task<bool> StringSetAsync(List<KeyValuePair<RedisKey, RedisValue>> keyValues)
        {
            List<KeyValuePair<RedisKey, RedisValue>> newkeyValues = keyValues.Select((KeyValuePair<RedisKey, RedisValue> p) => new KeyValuePair<RedisKey, RedisValue>(AddSysCustomKey(p.Key), p.Value)).ToList();
            return await Do((IDatabase db) => db.StringSetAsync(newkeyValues.ToArray()));
        }

        public async Task<bool> StringSetAsync<T>(string key, T obj, TimeSpan? expiry = null)
        {
            key = AddSysCustomKey(key);
            string json = ConvertJson(obj);
            return await Do((IDatabase db) => db.StringSetAsync(key, json, expiry));
        }

        public async Task<string> StringGetAsync(string key)
        {
            key = AddSysCustomKey(key);
            return await Do((IDatabase db) => db.StringGetAsync(key));
        }

        public async Task<RedisValue[]> StringGetAsync(List<string> listKey)
        {
            List<string> newKeys = listKey.Select(AddSysCustomKey).ToList();
            return await Do((IDatabase db) => db.StringGetAsync(ConvertRedisKeys(newKeys)));
        }

        public async Task<T> StringGetAsync<T>(string key)
        {
            key = AddSysCustomKey(key);
            string text = await Do((IDatabase db) => db.StringGetAsync(key));
            return ConvertObj<T>(text);
        }

        public async Task<double> StringIncrementAsync(string key, double val = 1.0)
        {
            key = AddSysCustomKey(key);
            return await Do((IDatabase db) => db.StringIncrementAsync(key, val));
        }

        public async Task<double> StringDecrementAsync(string key, double val = 1.0)
        {
            key = AddSysCustomKey(key);
            return await Do((IDatabase db) => db.StringDecrementAsync(key, val));
        }

        public bool HashExists(string key, string dataKey)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase db) => db.HashExists(key, dataKey));
        }

        public bool HashSet<T>(string key, string dataKey, T t)
        {
            key = AddSysCustomKey(key);
            return Do(delegate (IDatabase db)
            {
                string text = ConvertJson(t);
                return db.HashSet(key, dataKey, text);
            });
        }

        public bool HashDelete(string key, string dataKey)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase db) => db.HashDelete(key, dataKey));
        }

        public long HashDelete(string key, List<RedisValue> dataKeys)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase db) => db.HashDelete(key, dataKeys.ToArray()));
        }

        public T HashGet<T>(string key, string dataKey)
        {
            key = AddSysCustomKey(key);
            return Do(delegate (IDatabase db)
            {
                string text = db.HashGet(key, dataKey);
                return ConvertObj<T>(text);
            });
        }

        public double HashIncrement(string key, string dataKey, double val = 1.0)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase db) => db.HashIncrement(key, dataKey, val));
        }

        public double HashDecrement(string key, string dataKey, double val = 1.0)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase db) => db.HashDecrement(key, dataKey, val));
        }

        public List<T> HashKeys<T>(string key)
        {
            key = AddSysCustomKey(key);
            return Do(delegate (IDatabase db)
            {
                RedisValue[] values = db.HashKeys(key);
                return ConvetList<T>(values);
            });
        }

        public IDictionary<string, T> HashGetAll<T>(string key)
        {
            key = AddSysCustomKey(key);
            return Do(delegate (IDatabase db)
            {
                HashEntry[] array = db.HashGetAll(key);
                Dictionary<string, T> dictionary = new Dictionary<string, T>();
                HashEntry[] array2 = array;
                for (int i = 0; i < array2.Length; i++)
                {
                    HashEntry hashEntry = array2[i];
                    dictionary.Add(hashEntry.Name, ConvertObj<T>(hashEntry.Value));
                }

                return dictionary;
            });
        }

        public async Task<bool> HashExistsAsync(string key, string dataKey)
        {
            key = AddSysCustomKey(key);
            return await Do((IDatabase db) => db.HashExistsAsync(key, dataKey));
        }

        public async Task<bool> HashSetAsync<T>(string key, string dataKey, T t)
        {
            key = AddSysCustomKey(key);
            return await Do(delegate (IDatabase db)
            {
                string text = ConvertJson(t);
                return db.HashSetAsync(key, dataKey, text);
            });
        }

        public async Task<bool> HashDeleteAsync(string key, string dataKey)
        {
            key = AddSysCustomKey(key);
            return await Do((IDatabase db) => db.HashDeleteAsync(key, dataKey));
        }

        public async Task<long> HashDeleteAsync(string key, List<RedisValue> dataKeys)
        {
            key = AddSysCustomKey(key);
            return await Do((IDatabase db) => db.HashDeleteAsync(key, dataKeys.ToArray()));
        }

        public async Task<T> HashGeAsync<T>(string key, string dataKey)
        {
            key = AddSysCustomKey(key);
            string text = await Do((IDatabase db) => db.HashGetAsync(key, dataKey));
            return ConvertObj<T>(text);
        }

        public async Task<double> HashIncrementAsync(string key, string dataKey, double val = 1.0)
        {
            key = AddSysCustomKey(key);
            return await Do((IDatabase db) => db.HashIncrementAsync(key, dataKey, val));
        }

        public async Task<double> HashDecrementAsync(string key, string dataKey, double val = 1.0)
        {
            key = AddSysCustomKey(key);
            return await Do((IDatabase db) => db.HashDecrementAsync(key, dataKey, val));
        }

        public async Task<List<T>> HashKeysAsync<T>(string key)
        {
            key = AddSysCustomKey(key);
            return ConvetList<T>(await Do((IDatabase db) => db.HashKeysAsync(key)));
        }

        public void ListRemove<T>(string key, T value)
        {
            key = AddSysCustomKey(key);
            Do((IDatabase db) => db.ListRemove(key, ConvertJson(value), 0L));
        }

        public List<T> ListRange<T>(string key)
        {
            key = AddSysCustomKey(key);
            return Do(delegate (IDatabase redis)
            {
                RedisValue[] values = redis.ListRange(key, 0L, -1L);
                return ConvetList<T>(values);
            });
        }

        public void ListRightPush<T>(string key, T value)
        {
            key = AddSysCustomKey(key);
            Do((IDatabase db) => db.ListRightPush(key, ConvertJson(value)));
        }

        public T ListRightPop<T>(string key)
        {
            key = AddSysCustomKey(key);
            return Do(delegate (IDatabase db)
            {
                RedisValue value = db.ListRightPop(key);
                return ConvertObj<T>(value);
            });
        }

        public void ListLeftPush<T>(string key, T value)
        {
            key = AddSysCustomKey(key);
            Do((IDatabase db) => db.ListLeftPush(key, ConvertJson(value)));
        }

        public T ListLeftPop<T>(string key)
        {
            key = AddSysCustomKey(key);
            return Do(delegate (IDatabase db)
            {
                RedisValue value = db.ListLeftPop(key);
                return ConvertObj<T>(value);
            });
        }

        public long ListLength(string key)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase redis) => redis.ListLength(key));
        }

        public T ListRightPopLeftPush<T>(string source, string destination)
        {
            source = AddSysCustomKey(source);
            destination = AddSysCustomKey(destination);
            RedisValue value = Do((IDatabase db) => db.ListRightPopLeftPush(source, destination));
            return ConvertObj<T>(value);
        }

        public async Task<long> ListRemoveAsync<T>(string key, T value)
        {
            key = AddSysCustomKey(key);
            return await Do((IDatabase db) => db.ListRemoveAsync(key, ConvertJson(value), 0L));
        }

        public async Task<List<T>> ListRangeAsync<T>(string key)
        {
            key = AddSysCustomKey(key);
            return ConvetList<T>(await Do((IDatabase redis) => redis.ListRangeAsync(key, 0L, -1L)));
        }

        public async Task<long> ListRightPushAsync<T>(string key, T value)
        {
            key = AddSysCustomKey(key);
            return await Do((IDatabase db) => db.ListRightPushAsync(key, ConvertJson(value)));
        }

        public async Task<T> ListRightPopAsync<T>(string key)
        {
            key = AddSysCustomKey(key);
            return ConvertObj<T>(await Do((IDatabase db) => db.ListRightPopAsync(key)));
        }

        public async Task<long> ListLeftPushAsync<T>(string key, T value)
        {
            key = AddSysCustomKey(key);
            return await Do((IDatabase db) => db.ListLeftPushAsync(key, ConvertJson(value)));
        }

        public async Task<T> ListLeftPopAsync<T>(string key)
        {
            key = AddSysCustomKey(key);
            return ConvertObj<T>(await Do((IDatabase db) => db.ListLeftPopAsync(key)));
        }

        public async Task<long> ListLengthAsync(string key)
        {
            key = AddSysCustomKey(key);
            return await Do((IDatabase redis) => redis.ListLengthAsync(key));
        }

        public bool SetAdd<T>(string key, T value)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase p) => p.SetAdd(key, ConvertJson(value)));
        }

        public bool SetContains<T>(string key, T value)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase p) => p.SetContains(key, ConvertJson(value)));
        }

        public long SetLength(string key)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase p) => p.SetLength(key));
        }

        public List<T> SetMembers<T>(string key)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase p) => ConvetList<T>(p.SetMembers(key)));
        }

        public T SetPop<T>(string key)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase p) => ConvertObj<T>(p.SetPop(key)));
        }

        public T SetRandomMember<T>(string key)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase p) => ConvertObj<T>(p.SetRandomMember(key)));
        }

        public List<T> SetRandomMembers<T>(string key, long count)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase p) => ConvetList<T>(p.SetRandomMembers(key, count)));
        }

        public bool SetRemove<T>(string key, T value)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase p) => p.SetRemove(key, ConvertJson(value)));
        }

        public long SetRemove<T>(string key, T[] values)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase p) => p.SetRemove(key, ((IEnumerable<T>)values).Select((Func<T, RedisValue>)((T x) => ConvertJson(x))).ToArray()));
        }

        public bool SortedSetAdd<T>(string key, T value, double score)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase redis) => redis.SortedSetAdd(key, ConvertJson(value), score));
        }

        public bool SortedSetRemove<T>(string key, T value)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase redis) => redis.SortedSetRemove(key, ConvertJson(value)));
        }

        public List<T> SortedSetRangeByRank<T>(string key)
        {
            key = AddSysCustomKey(key);
            return Do(delegate (IDatabase redis)
            {
                RedisValue[] values = redis.SortedSetRangeByRank(key, 0L, -1L);
                return ConvetList<T>(values);
            });
        }

        public long SortedSetLength(string key)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase redis) => redis.SortedSetLength(key));
        }

        public async Task<bool> SortedSetAddAsync<T>(string key, T value, double score)
        {
            key = AddSysCustomKey(key);
            return await Do((IDatabase redis) => redis.SortedSetAddAsync(key, ConvertJson(value), score));
        }

        public async Task<bool> SortedSetRemoveAsync<T>(string key, T value)
        {
            key = AddSysCustomKey(key);
            return await Do((IDatabase redis) => redis.SortedSetRemoveAsync(key, ConvertJson(value)));
        }

        public async Task<List<T>> SortedSetRangeByRankAsync<T>(string key)
        {
            key = AddSysCustomKey(key);
            return ConvetList<T>(await Do((IDatabase redis) => redis.SortedSetRangeByRankAsync(key, 0L, -1L)));
        }

        public async Task<long> SortedSetLengthAsync(string key)
        {
            key = AddSysCustomKey(key);
            return await Do((IDatabase redis) => redis.SortedSetLengthAsync(key));
        }

        public bool KeyDelete(string key)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase db) => db.KeyDelete(key));
        }

        public long KeyDelete(List<string> keys)
        {
            List<string> newKeys = keys.Select(AddSysCustomKey).ToList();
            return Do((IDatabase db) => db.KeyDelete(ConvertRedisKeys(newKeys)));
        }

        public bool KeyExists(string key)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase db) => db.KeyExists(key));
        }

        public bool KeyRename(string key, string newKey)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase db) => db.KeyRename(key, newKey));
        }

        public bool KeyExpire(string key, TimeSpan? expiry = null)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase db) => db.KeyExpire(key, expiry));
        }

        public bool LockTake(string key, string lockValue, TimeSpan expiry)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase db) => db.LockTake(key, lockValue, expiry));
        }

        public string LockQuery(string key)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase db) => db.LockQuery(key));
        }

        public bool LockRelease(string key, string lockValue)
        {
            key = AddSysCustomKey(key);
            return Do((IDatabase db) => db.LockRelease(key, lockValue));
        }

        public void Subscribe<T>(string subChannel, Action<RedisChannel, T> handler = null)
        {
            _conn.GetSubscriber().Subscribe(subChannel, delegate (RedisChannel channel, RedisValue message)
            {
                Console.WriteLine(subChannel + " 订阅收到消息：" + message);
                if (handler != null)
                {
                    T arg = ConvertObj<T>(message);
                    handler(channel, arg);
                }
            });
        }

        public void Subscribe(string subChannel, Action<RedisChannel, object> handler = null)
        {
            _conn.GetSubscriber().Subscribe(subChannel, delegate (RedisChannel channel, RedisValue message)
            {
                Console.WriteLine(subChannel + " 订阅收到消息：" + message);
                if (handler != null)
                {
                    handler(channel, message);
                }
            });
        }

        public long Publish<T>(string channel, T msg)
        {
            return _conn.GetSubscriber().Publish(channel, ConvertJson(msg));
        }

        public Task<long> PublishAsync<T>(string channel, T msg)
        {
            return _conn.GetSubscriber().PublishAsync(channel, ConvertJson(msg));
        }

        public void Unsubscribe(string channel)
        {
            _conn.GetSubscriber().Unsubscribe(channel);
        }

        public void UnsubscribeAll()
        {
            _conn.GetSubscriber().UnsubscribeAll();
        }

        public ITransaction CreateTransaction()
        {
            return GetDatabase().CreateTransaction();
        }

        public IDatabase GetDatabase()
        {
            return _conn.GetDatabase(DbNum);
        }

        public IServer GetServer(string hostAndPort)
        {
            return _conn.GetServer(hostAndPort);
        }

        public void SetSysCustomKey(string customKey)
        {
            CustomKey = customKey;
        }

        private string AddSysCustomKey(string oldKey)
        {
            return (CustomKey ?? KeyPrefix) + oldKey;
        }

        private T Do<T>(Func<IDatabase, T> func)
        {
            if (_conn != null)
            {
                IDatabase database = _conn.GetDatabase(DbNum);
                return func(database);
            }

            return default(T);
        }

        private string ConvertJson<T>(T value)
        {
            return JsonConvert.SerializeObject(value);
        }

        private T ConvertObj<T>(RedisValue value)
        {
            if (value.IsNull)
            {
                return default(T);
            }

            string text = value.ToString();
            if (typeof(T).IsValueType || typeof(T).FullName == "System.String")
            {
                return (T)Convert.ChangeType(text, typeof(T));
            }

            return text.FromJsonSafe<T>();
        }

        private List<T> ConvetList<T>(RedisValue[] values)
        {
            List<T> list = new List<T>();
            foreach (RedisValue value in values)
            {
                T item = ConvertObj<T>(value);
                list.Add(item);
            }

            return list;
        }

        private RedisKey[] ConvertRedisKeys(List<string> redisKeys)
        {
            return ((IEnumerable<string>)redisKeys).Select((Func<string, RedisKey>)((string redisKey) => redisKey)).ToArray();
        }
    }
}