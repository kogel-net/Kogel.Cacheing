using System;
using System.Collections.Generic;

namespace Kogel.Cacheing.LoadBalancers
{
    public class DefaultLoadBalancerFactory<T> : ILoadBalancerFactory<T>
    {
        /// <summary>
        /// 获取一个负载均衡器
        /// </summary>
        /// <param name="func"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public ILoadBalancer<T> Get(Func<List<T>> func,string type= "RoundRobin")
        {
            switch (type)
            {
                case "RoundRobin":                    
                case "RoundRobinLoadBalancer":
                    return new RoundRobinLoadBalancer<T>(func);
                case "RandomRobin":
                case "RandomRobinLoadBalancer":
                    return new RandomRobinLoadBalancer<T>(func);
                default:
                    return new RoundRobinLoadBalancer<T>(func);
            }
        }
    }
}
