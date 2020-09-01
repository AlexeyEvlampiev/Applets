using System;
using System.Threading.Tasks;
using Applets.Common;
using Applets.InMemory;
using Xunit;

namespace Applets
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            var factory = new InMemoryAppletChannelFactory(new MyAppInfo());
            var channel = factory.Create(MyAppInfo.ConsoleInputApplet);
            await channel.SendInfoAsync("Hello world!");
        }

        class MyAppInfo : AppInfo
        {
            public static readonly Guid ConsoleInputApplet = Guid.Parse("13caf54e-1476-4335-b472-fe599551dab1");
            public MyAppInfo()
                : base(Guid.Parse("4d5745f0-4a19-4ab4-9a93-de68ba4c9c99"), "My app")
            {
                base.RegisterApplet(ConsoleInputApplet, "Console input");
            }
        }
    }
}
