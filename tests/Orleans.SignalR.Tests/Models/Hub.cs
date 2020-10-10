using Microsoft.AspNetCore.SignalR;

namespace Orleans.SignalR.Tests.Models
{
    public interface IDaHub { }

    public class DaGenericHubBase<THub> : Hub<THub> where THub : class
    {
    }

    // matching interface naming
    public class DaHub : Hub<IDaHub>
    {
    }

    // using base
    public class DaHubUsingBase : DaGenericHubBase<IDaHub>
    {
    }

    // non matching interface and class
    public class DaHubx : Hub<IDaHub>
    {
    }

    public class DifferentHub : Hub
    {
    }

    public class MyHub : Hub
    {
    }
}
