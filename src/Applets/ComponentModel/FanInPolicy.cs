using System;

namespace Applets.ComponentModel
{
    public static class FanInPolicy 
    {
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        public static IFanInPolicy<T> FirstInWins<T>() => new FirstInWinsFanInPolicy<T>(DefaultTimeout);
        public static IFanInPolicy<T> FirstInWins<T>(TimeSpan timeout) => new FirstInWinsFanInPolicy<T>(timeout);
    }
}
