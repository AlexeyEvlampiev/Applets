using System;
using System.Reactive.Subjects;
using Applets.Common;

namespace Applets.InMemory
{
    public sealed class InMemoryAppletChannelFactory : AppletChannelFactory
    {
        private readonly Subject<DispatchArgs> _topic = new Subject<DispatchArgs>();

        public InMemoryAppletChannelFactory(IAppInfo appInfo) : base(appInfo)
        {
        }

        protected override IAppletChannel CreateAppletChannel(Guid appletId)
        {
            return new InMemoryAppletChannel(appletId, AppInfo, _topic);
        }
    }
}
