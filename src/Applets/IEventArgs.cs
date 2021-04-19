using System.Threading.Tasks;

namespace Applets
{
    public interface IEventArgs : IDeliveryArgs
    {
        Task ReactAsync(MessageIntentId reactionIntentId, object dto);
    }
}
