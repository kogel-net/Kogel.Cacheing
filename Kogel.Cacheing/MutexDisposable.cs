using System.Collections.Generic;
using System;

namespace Kogel.Cacheing
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMutexDisposable : IDisposable
    {
    }

    /// <summary>
    /// 互斥锁释放器
    /// </summary>
    internal class MutexDisposable : IMutexDisposable
    {
        private readonly ICacheManager _cacheManager;
        private readonly string _cacheKey;
        private readonly bool _isHLockMutex;
        private readonly List<string> _dataKeys;
        public MutexDisposable(ICacheManager cacheManager, string cacheKey, bool isHLockMutex = false, List<string> dataKeys = null)
        {
            _cacheManager = cacheManager;
            _cacheKey = cacheKey;
            _isHLockMutex = isHLockMutex;
            _dataKeys = dataKeys;
        }

        public void Dispose()
        {
            if (!_isHLockMutex)
                _cacheManager.ExitMutex(_cacheKey);
            else
                _cacheManager.HExitMutex(_cacheKey, _dataKeys);
        }

        /// <summary>
        /// 析构函数释放（防止异常不释放）
        /// </summary>
        ~MutexDisposable() => Dispose();
    }
}
