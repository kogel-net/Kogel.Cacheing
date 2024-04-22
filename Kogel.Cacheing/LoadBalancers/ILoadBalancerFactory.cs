using System;
using System.Collections.Generic;

namespace Kogel.Cacheing.LoadBalancers
{
    public interface ILoadBalancerFactory<T>
    {
        ILoadBalancer<T> Get(Func<List<T>> func,string Type);
    }
}