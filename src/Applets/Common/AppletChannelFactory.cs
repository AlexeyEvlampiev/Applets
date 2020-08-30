using System;
using System.Text;

namespace Applets.Common
{
    public abstract class AppletChannelFactory : IAppletChannelFactory
    {
        protected AppletChannelFactory(IAppInfo appInfo)
        {
            AppInfo = appInfo ?? throw new ArgumentNullException(nameof(appInfo));
        }

        protected IAppInfo AppInfo { get; }

        public IAppletChannel Create(Guid appletId)
        {
            if(AppInfo.IsAppletId(appletId))
                throw new ArgumentException(
                    new StringBuilder($"Invalid applet ID.")
                        .Append($" '{appletId}' is not a known appled identifier.")
                        .ToString());
            return CreateAppletChannel(appletId) 
                ?? throw new NullReferenceException($"{nameof(GetType)}.{nameof(CreateAppletChannel)} returned null.");
        }

        protected abstract IAppletChannel CreateAppletChannel(Guid appletId);
    }
}
