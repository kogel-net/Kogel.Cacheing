using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kogel.Cacheing.Test.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        ICacheManager cacheManager;
        public ValuesController(ICacheManager cacheManager)
        {
            this.cacheManager = cacheManager;
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
        /// 互斥锁
        /// </summary>
        /// <param name="cacheKey">缓存键</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<object>> Mutex(string cacheKey = "test_cache_mutex")
        {
            using (var mutex = cacheManager.LockMutex(cacheKey, TimeSpan.FromSeconds(10)))
            {
                await Task.Delay(10000);
                return "互斥锁xxx";
            }
        }
    }
}
