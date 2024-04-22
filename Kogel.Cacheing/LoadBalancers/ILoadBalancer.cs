using System.Collections.Generic;

namespace Kogel.Cacheing.LoadBalancers
{
    public interface ILoadBalancer<T>
    {
        T Lease();

        T Lease(List<T> connections);

    }
}