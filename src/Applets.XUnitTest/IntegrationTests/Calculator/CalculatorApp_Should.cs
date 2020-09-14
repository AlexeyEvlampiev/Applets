using System;
using System.Threading;
using System.Threading.Tasks;
using Applets.InMemory;
using Xunit;

namespace Applets.IntegrationTests.Calculator
{
    // ReSharper disable once InconsistentNaming
    public class CalculatorApp_Should : IDisposable
    {
        readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly IAppletChannel _clientChannel;

        public CalculatorApp_Should()
        {
            var appInfo = new CalculatorAppInfo();
            var channelFactory = new InMemoryAppletChannelFactory(appInfo);

            var _ = new CalcProcessor(channelFactory)
                .ProcessAsync(_cancellation.Token);
            _clientChannel = channelFactory.Create(CalculatorAppInfo.ClientAppletId);
        }

        [Fact]
        public async Task ThrowOnUnknownOperation()
        {
            var request = new CalcRequest(1, 1, (CalcOperation)int.MaxValue);
            var ex = await Assert.ThrowsAsync<BadFanOutRequestException>(()=>
                request.AggregateResponsesAsync(_clientChannel));
        }

        [Fact]
        public async Task ThrowOnInvalidOperation()
        {
            var request = new CalcRequest(1, 0, CalcOperation.Divide);
            var ex = await Assert.ThrowsAsync<BadFanOutRequestException>(() =>
                request.AggregateResponsesAsync(_clientChannel));
            var args = ex.DeliveryArgs.GetBodyAsString();
        }

        [Theory()]
        [InlineData(1, 2)]
        [InlineData(123, 1000)]
        public async Task Add(double lhs, double rhs)
        {
            var request = new CalcRequest(lhs, rhs, CalcOperation.Add);
            var response = await request.AggregateResponsesAsync(_clientChannel);
            Assert.Equal(lhs + rhs, response.Result);
        }


        [Theory()]
        [InlineData(1, 2)]
        [InlineData(123, 1000)]
        public async Task Subtract(double lhs, double rhs)
        {
            var request = new CalcRequest(lhs, rhs, CalcOperation.Subtract);
            var response = await request.AggregateResponsesAsync(_clientChannel);
            Assert.Equal(lhs - rhs, response.Result);
        }


        [Theory()]
        [InlineData(1, 2)]
        [InlineData(123, 1000)]
        public async Task Multiply(double lhs, double rhs)
        {
            var request = new CalcRequest(lhs, rhs, CalcOperation.Multiply);
            var response = await request.AggregateResponsesAsync(_clientChannel);
            Assert.Equal(lhs * rhs, response.Result);
        }

        [Theory()]
        [InlineData(1, 2)]
        [InlineData(123, 1000)]
        public async Task Divide(double lhs, double rhs)
        {
            var request = new CalcRequest(lhs, rhs, CalcOperation.Divide);
            var response = await request.AggregateResponsesAsync(_clientChannel);
            Assert.Equal(lhs / rhs, response.Result);
        }

        public void Dispose()
        {
            _cancellation.Cancel();
        }
    }
}
