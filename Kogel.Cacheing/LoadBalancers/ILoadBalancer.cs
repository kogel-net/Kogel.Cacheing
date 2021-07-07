using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kogel.Cacheing.LoadBalancers
{
    public interface ILoadBalancer<T>
    {
        T Lease();

        T Lease(List<T> connections);

    }
}