﻿using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Applets.ComponentModel
{
    public abstract class FanOutRequest<T> : IFanOutRequest<T> where T : class
    {
        
        public async Task<T> AggregateResponsesAsync(IAppletChannel channel, IFanInPolicy<T> policy = null)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            policy ??= FanInPolicy.FirstInWins<T>();
            await channel
                .GetResponses(this)
                .Select(reply =>
                {
                    if(reply.IsError)
                        throw new BadFanOutRequestException(reply);
                    policy.TryCompleteWith(reply);
                    return reply;
                })
                .TakeWhile(reply => policy.HasResult == false)
                .LastOrDefaultAsync()
                .Timeout(policy.Timeout);
            Debug.Assert(policy.HasResult);
            return policy.Result;
        }
    }
}
