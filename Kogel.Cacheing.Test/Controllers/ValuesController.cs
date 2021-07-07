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
        /// <returns></returns>
        [HttpGet]
        public ActionResult<object> Get()
        {
            //获取
            return cacheManager.StringGet<object>("test_cache_time");
        }

        /// <summary>
        /// 设置string类型缓存
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<object> Set(string value = "test")
        {
            //写入
            return cacheManager.StringSet("test_cache_time", value);
        }

        /// <summary>
        /// 互斥锁
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<object>> Mutex()
        {
            //互斥锁
            using (var mutex = cacheManager.LockMutex("test_cache_mutex", TimeSpan.FromSeconds(10)))
            {
                await Task.Delay(10000);
                return "互斥锁xxx";
            }
        }
    }
}
