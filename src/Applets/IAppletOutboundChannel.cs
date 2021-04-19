using System;
using System.Threading;
using System.Threading.Tasks;

namespace Applets
{
    public interface IAppletOutboundChannel
    {
        /// <summary>
        /// Gets the responses to the request broadcast using <see cref="intentId"/>  and <see cref="data"/>
        /// </summary>
        /// <param name="intentId">The intent.</param>
        /// <param name="data">The data.</param>
        /// <param name="conversationTtl"></param>
        /// <returns>Stream of responses</returns>
        /// <exception cref="AppContractViolationException"></exception>
        IObservable<IReplyArgs> GetResponses(MessageIntentId intentId, object data, TimeSpan conversationTtl);

        /// <summary>
        /// Raises an application wide event defined with the given <see cref="messageIntentId"/> and <see cref="data"/> parameters.
        /// </summary>
        /// <param name="messageIntentId"></param>
        /// <param name="data">The event data.</param>
        /// <param name="timeToLive">Event message time-to-live</param>
        /// <param name="enqueueDelay"></param>
        /// <param name="cancellation">The cancellation.</param>
        /// <exception cref="AppContractViolationException"></exception>
        Task EmitEventAsync(MessageIntentId messageIntentId, object data, TimeSpan? timeToLive = null, TimeSpan? enqueueDelay = null, CancellationToken cancellation = default);
    }
}
