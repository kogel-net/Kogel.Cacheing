﻿using Microsoft.AspNetCore.Mvc;

namespace Kogel.Cacheing.Test.Controllers
{
    /// <summary>
    /// 测试缓存
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        ICacheManager cacheManager;
        public ValuesController(ICacheManager cacheManager)
        {
            this.cacheManager = cacheManager.GetMemoryCache();
        }

        /// <summary>
        /// 获取string类型缓存
        /// </summary>
        /// <param name="cacheKey">缓存键</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<object> Get(string cacheKey)
        {
            return cacheManager.StringGet<object>(cacheKey);
        }

        /// <summary>
        /// 设置string类型缓存
        /// </summary>
        /// <param name="cacheKey">缓存键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<object> Set(string cacheKey, string value = "test")
        {
            return cacheManager.StringSet(cacheKey, value);
        }

        /// <summary>
        /// 获取hash类型缓存
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="dataKey"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<object> HashGet(string cacheKey, string dataKey)
        {
            return cacheManager.HashGet<object>(cacheKey, dataKey);
        }

        /// <summary>
        /// 设置hash类型缓存
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="dataKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<object> HashSet(string cacheKey, string dataKey, string value = "hash test")
        {
            return cacheManager.HashSet(cacheKey, dataKey, value);
        }

        /// <summary>
        /// 获取键下所有缓存
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        [HttpGet]
        public IDictionary<string, object> HashGetAll(string cacheKey)
        {
            return cacheManager.HashGetAll<object>(cacheKey);
        }

        /// <summary>
        /// 互斥锁
        /// </summary>
        /// <param name="cacheKey">缓存键</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<object>> Mutex(string cacheKey = "test_cache_mutex")
        {
            using (var mutex = cacheManager.LockMutex(cacheKey, TimeSpan.FromSeconds(10)))
            {
                await Task.Delay(1000);
                return "互斥锁xxx";
            }
        }

        /// <summary>
        /// 哈希互斥锁
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="dataKeys"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<object>> HMutex(string cacheKey, List<string> dataKeys)
        {
            string hCacheKey = "123" + "_LockHash";
            IDictionary<string, string> dictionary = cacheManager.HashGetAll<string>(hCacheKey);

            dataKeys = new List<string> { "48a52160-2975-4a92-e16a-08dc5f5434c4" };
            using (cacheManager.HLockMutex("FREEZE_ACCOUNT_SUMMONS_KEY_{0}", dataKeys, TimeSpan.FromSeconds(10)))
            {
                await Task.Delay(1000);
                return "哈希互斥锁xxx";
            }
        }

        /// <summary>
        /// 自增
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<object> StringIncrement(string cacheKey = "test_cache_rement")
        {
            return cacheManager.StringIncrement(cacheKey, 1);
        }

        /// <summary>
        /// 自减
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<object> StringDecrement(string cacheKey = "test_cache_rement")
        {
            return cacheManager.StringDecrement(cacheKey, 1);
        }

        /// <summary>
        /// 订阅
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<object>> Subscribe()
        {
            string channelName = "channel1";
            //订阅频道
            cacheManager.Subscribe<string>(channelName, (str) =>
            {
                Console.WriteLine($"test1:{str}");
            });
            cacheManager.Subscribe<string>(channelName, (str) =>
            {
                Console.WriteLine($"test2:{str}");
            });
            //发布频道
            cacheManager.Publish(channelName, "aaa");
            cacheManager.Publish(channelName, "bbb");
            //await Task.Delay(10000);
            return "测试发布订阅";
        }

        /// <summary>
        /// 发布一条消息
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public Task Publish(string msg = "")
        {
            string channelName = "channel1";
            //发布频道
            cacheManager.Publish(channelName, $"测试消息:{msg}");
            return Task.CompletedTask;
        }
    }
}
