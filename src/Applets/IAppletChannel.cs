
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Applets
{
    public interface IAppletChannel : IDisposable
    {
        string AppletName { get; }
        Guid Instance { get; }
        Guid AppletId { get; }

        IObservable<IDeliveryArgs> GetResponses(DispatchArgs request);

        Task SendAsync(DispatchArgs args, CancellationToken cancellation = default);

        IObservable<IDeliveryArgs> GetResponses(object request);
        IObservable<IDeliveryArgs> GetResponses(object request, Guid intent);

        IObservable<IDeliveryArgs> CreateProcessedEventNotificationsObservable(
            DEventNotificationHandler processOneAsync);

        IDisposable ProcessEventNotifications(
            DEventNotificationHandler processOneAsync);


        IAppInfo GetAppInfo();
        Task SendErrorAsync(object data, CancellationToken cancellation = default);

        Task SendErrorAsync(string message, CancellationToken cancellation = default);

        Task SendWarningAsync(object data, CancellationToken cancellation = default);

        Task SendWarningAsync(string message, CancellationToken cancellation = default);

        Task SendInfoAsync(object data, CancellationToken cancellation = default);

        Task SendInfoAsync(string message, CancellationToken cancellation = default);

        bool CanSend(Guid intent);
        bool CanReceiveEventNotification(Guid intent);

        bool CanSend(DispatchArgs args);
        bool CanReceiveEventNotification(IDeliveryArgs args);
    }
}
