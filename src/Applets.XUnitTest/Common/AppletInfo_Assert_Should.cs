using System;
using Xunit;

namespace Applets.Common
{
    public class AppletInfo_Assert_Should
    {
        [Fact]
        public void SucceedForBalancedApp()
        {
            IAppInfo target = new BalancedAppInfo();
            target.Assert();
        }
        sealed class BalancedAppInfo : AppInfo
        {
            private static readonly Guid RequestIntentId = Guid.NewGuid();
            private static readonly Guid ResponseIntentId = Guid.NewGuid();

            private static readonly Guid ClientAppletId = Guid.NewGuid();
            private static readonly Guid ServiceAppletId = Guid.NewGuid();


            public BalancedAppInfo() : base(Guid.NewGuid(), "Balanced applications")
            {
                RegisterApplet(ClientAppletId, "Client applet");
                RegisterApplet(ServiceAppletId, "Service applet");

                RegisterIntent(RequestIntentId, "Request");
                RegisterIntent(ResponseIntentId, "Response");

                RegisterFanInFanOutIntentBinding(RequestIntentId, ResponseIntentId);

                RegisterAppletNotifications(ServiceAppletId, 
                    incoming: new[]{ RequestIntentId }, 
                    outgoing: new[] { ResponseIntentId });
            }
        }
    }
}
