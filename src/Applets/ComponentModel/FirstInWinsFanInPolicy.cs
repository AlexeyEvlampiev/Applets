using System;
using System.Threading;
using Applets.Common;

namespace Applets.ComponentModel
{
    sealed class FirstInWinsFanInPolicy<T> : IFanInPolicy<T> 
    {
        private int _isCompleted;

        public FirstInWinsFanInPolicy(TimeSpan timeout)
        {
            Timeout = timeout;
        }

        public TimeSpan Timeout { get; }
        public bool HasResult => Thread.VolatileRead(ref _isCompleted) != 0;

        public bool TryCompleteWith(IDeliveryArgs reply)
        {
            if (reply.Dto is T result)
            {
                if (Interlocked.CompareExchange(ref _isCompleted, 1, 0) == 0)
                {
                    Result = result;
                }
            }

            return Thread.VolatileRead(ref _isCompleted) == 1;
        }

        

        public T Result { get; private set; }
    }
}
