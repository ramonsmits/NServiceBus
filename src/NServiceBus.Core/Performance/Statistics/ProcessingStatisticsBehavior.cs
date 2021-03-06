﻿namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class ProcessingStatisticsBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            var state = new State();

            string timeSentString;
            var headers = context.Message.Headers;

            if (headers.TryGetValue(Headers.TimeSent, out timeSentString))
            {
                state.TimeSent = DateTimeExtensions.ToUtcDateTime(timeSentString);
            }

            state.ProcessingStarted = DateTime.UtcNow;
            context.Extensions.Set(state);
            try
            {
                await next(context).ConfigureAwait(false);
            }
            finally
            {
                state.ProcessingEnded = DateTime.UtcNow;
            }
        }

        public class State
        {
            public DateTime? TimeSent { get; set; }
            public DateTime ProcessingStarted { get; set; }
            public DateTime ProcessingEnded { get; set; }
        }
    }
}