using System.Threading.Tasks;

namespace Applets.ComponentModel
{
    public interface IFanOutRequest<T>
    {
        Task<T> AggregateResponsesAsync(IAppletChannel channel, IFanInPolicy<T> policy = null);
    }
}
