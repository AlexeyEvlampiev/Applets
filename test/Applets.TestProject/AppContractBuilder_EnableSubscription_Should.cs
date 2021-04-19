using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Applets
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public sealed class AppContractBuilder_EnableSubscription_Should
    {
        [Fact]
        public void ThrowIfUnknownIntent()
        {
            var builder = new AppContractBuilder();
            var applet = builder.AddApplet("MyApplet");
            var foreignIntent = MessageIntentId.Create("Foreign intent");
            Assert.Throws<AppContractBuilderException>(()=>
                builder.EnableSubscription(applet, foreignIntent, typeof(string)));
        }

        [Fact]
        public void ThrowIfUnknownApplet()
        {
            var builder = new AppContractBuilder();
            var foreignApplet = AppletId.Create("Foreign applet");
            var intent = builder.AddMessageIntent("MyIntent");
            Assert.Throws<AppContractBuilderException>(() =>
                builder.EnableSubscription(foreignApplet, intent, typeof(string)));
        }
    }
}
