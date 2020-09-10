using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Applets
{
    public class Extensions_ToObservable_Should
    {
        [Fact]
        public void CreateFromCancellationToken()
        {
            var target = new CancellationTokenSource();
            var task = target.Token.ToObservable().ToTask(CancellationToken.None);
            Assert.False(task.IsCompleted);
            target.Cancel();
            Assert.True(task.IsCompleted);
        }
    }
}
