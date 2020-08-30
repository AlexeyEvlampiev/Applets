using System.Threading.Tasks;

namespace Applets.ComponentModel
{
    public interface IFanOutRequest<T>
    {
        Task<T> FanInAsync(IAppletChannel channel, IFanInPolicy<T> policy = null);
    }
}
