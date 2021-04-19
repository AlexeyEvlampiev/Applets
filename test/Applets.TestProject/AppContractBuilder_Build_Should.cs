using Applets.Common;
using Xunit;

namespace Applets
{
    public class AppContractBuilder_Build_Should
    {
        [Fact]
        public void ManageGreetingDemoCase()
        {
            var builder = new AppContractBuilder();
            var greeting = builder.AddMessageIntent("Greeting");
            var greetingReply = builder.AddMessageIntent("GreetingReply");

            var greetingSender = builder.AddApplet("GreetingSender");
            var greetingReceiver = builder.AddApplet("GreetingReceiver");

            builder.EnableResponseStream(greetingSender, greeting, typeof(string),
                responses => responses.Add(greetingReply, typeof(string)));

            builder.EnableSubscription(greetingReceiver, greeting, typeof(string));
            builder.EnableBroadcast(greetingReceiver, greetingReply, typeof(string));

            var appContract = builder.Build();
            Assert.True(appContract.IsValidRequest(greetingSender, greeting, typeof(string)));
            Assert.True(appContract.IsValidResponse(greetingSender, greetingReply, typeof(string)));
            Assert.True(appContract.IsValidSubscription(greetingReceiver, greeting, typeof(string)));
            Assert.True(appContract.IsValidEvent(greetingReceiver, greetingReply, typeof(string)));

            Assert.False(appContract.IsValidRequest(greetingReceiver, greeting, typeof(string)));
            Assert.False(appContract.IsValidResponse(greetingReceiver, greetingReply, typeof(string)));
            Assert.False(appContract.IsValidSubscription(greetingSender, greeting, typeof(string)));
            Assert.False(appContract.IsValidEvent(greetingSender, greetingReply, typeof(string)));

            Assert.False(appContract.IsValidRequest(greetingSender, greeting, typeof(int)));
            Assert.False(appContract.IsValidResponse(greetingSender, greetingReply, typeof(int)));
            Assert.False(appContract.IsValidSubscription(greetingReceiver, greeting, typeof(int)));
            Assert.False(appContract.IsValidEvent(greetingReceiver, greetingReply, typeof(int)));
        }
    }
}
