﻿namespace NServiceBus.Features
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Outbox;

    /// <summary>
    /// Used to configure in memory outbox persistence.
    /// </summary>
    public class InMemoryOutboxPersistence : Feature
    {
        internal const string TimeToKeepDeduplicationEntries = "Outbox.TimeToKeepDeduplicationEntries";

        internal InMemoryOutboxPersistence()
        {
            DependsOn<Outbox>();
            Defaults(s => s.EnableFeature(typeof(InMemoryTransactionalStorageFeature)));
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var outboxStorage = new InMemoryOutboxStorage();
            context.Container.RegisterSingleton<IOutboxStorage>(outboxStorage);

            var timeSpan = context.Settings.Get<TimeSpan>(TimeToKeepDeduplicationEntries);

            context.RegisterStartupTask(new OutboxCleaner(outboxStorage, timeSpan));
        }

        class OutboxCleaner : FeatureStartupTask
        {
            public OutboxCleaner(InMemoryOutboxStorage storage, TimeSpan timeToKeepDeduplicationData)
            {
                this.timeToKeepDeduplicationData = timeToKeepDeduplicationData;
                inMemoryOutboxStorage = storage;
            }

            protected override Task OnStart(IBusSession session)
            {
                cleanupTimer = new Timer(PerformCleanup, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
                return TaskEx.Completed;
            }

            protected override Task OnStop(IBusSession session)
            {
                using (var waitHandle = new ManualResetEvent(false))
                {
                    cleanupTimer.Dispose(waitHandle);

                    // TODO: Use async synchronization primitive
                    waitHandle.WaitOne();
                }
                return TaskEx.Completed;
            }

            void PerformCleanup(object state)
            {
                inMemoryOutboxStorage.RemoveEntriesOlderThan(DateTime.UtcNow - timeToKeepDeduplicationData);
            }

// ReSharper disable once NotAccessedField.Local
            Timer cleanupTimer;
            readonly InMemoryOutboxStorage inMemoryOutboxStorage;
            readonly TimeSpan timeToKeepDeduplicationData;
        }
    }
}