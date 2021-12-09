using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kogel.Cacheing.StackExchange
{
    /// <summary>
    /// 互斥锁释放器
    /// </summary>
    internal class RedisMutexDisposable: IMutexDisposable
    {
        private readonly ICacheManager cacheManager;
        private readonly string cacheKey;
        public RedisMutexDisposable(ICacheManager cacheManager, string cacheKey)
        {
            this.cacheManager = cacheManager;
            this.cacheKey = cacheKey;
        }

        public void Dispose()
        {
            cacheManager.ExitMutex(cacheKey);
        }

        /// <summary>
        /// 析构函数释放（防止异常不释放）
        /// </summary>
        ~RedisMutexDisposable() => Dispose();
    }
}
