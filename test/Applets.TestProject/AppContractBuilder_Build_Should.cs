using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Applets
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
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

        [Fact]
        public void ThrowIfIncomplete()
        {
            var builder = new AppContractBuilder();
            Assert.Throws<AppContractBuilderException>(builder.Build);

            var greeting = builder.AddMessageIntent("Greeting");
            Assert.Throws<AppContractBuilderException>(builder.Build);

            var greetingReply = builder.AddMessageIntent("GreetingReply");
            Assert.Throws<AppContractBuilderException>(builder.Build);

            var greetingSender = builder.AddApplet("GreetingSender");
            Assert.Throws<AppContractBuilderException>(builder.Build);

            var greetingReceiver = builder.AddApplet("GreetingReceiver");
            Assert.Throws<AppContractBuilderException>(builder.Build);

            builder.EnableResponseStream(greetingSender, greeting, typeof(string),
                responses => responses.Add(greetingReply, typeof(string)));
            Assert.Throws<AppContractBuilderException>(builder.Build);

            builder.EnableSubscription(greetingReceiver, greeting, typeof(string));
            Assert.Throws<AppContractBuilderException>(builder.Build);

            builder.EnableBroadcast(greetingReceiver, greetingReply, typeof(string));
            Assert.Throws<AppContractBuilderException>(builder.Build);

            builder.Build();
        }
    }

}
