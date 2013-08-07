namespace Runner.Saga
{
    using System;
    using System.Threading;
    using System.Transactions;

    using NServiceBus;
    using NServiceBus.Saga;

    class TestSaga : Saga<SagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleMessages<CompleteSagaMessage>
    {
        private static TwoPhaseCommitEnlistment enlistment = new TwoPhaseCommitEnlistment();
        public override void ConfigureHowToFindSaga()
        {
            this.ConfigureMapping<StartSagaMessage>(m => m.Id).ToSaga(s => s.Number);
            this.ConfigureMapping<CompleteSagaMessage>(m => m.Id).ToSaga(s => s.Number);
        }

        public void Handle(StartSagaMessage message)
        {
            Transaction.Current.TransactionCompleted += AfterMessage;
            this.BeforeMessage(message);
            this.Data.Number = message.Id;
            Data.NumCalls++;
        }

        public void Handle(CompleteSagaMessage message)
        {
            this.BeforeMessage(message);
            this.MarkAsComplete();
        }

        private void AfterMessage(object sender, TransactionEventArgs transactionEventArgs)
        {
            if (transactionEventArgs.Transaction.TransactionInformation.Status != TransactionStatus.Committed)
                return;
            Timings.Last = DateTime.Now;
            Interlocked.Increment(ref Timings.NumberOfMessages);



        }

        private void BeforeMessage(MessageBase message)
        {
            if (!Timings.First.HasValue)
            {
                Timings.First = DateTime.Now;
            }
       
            if (message.TwoPhaseCommit)
            {
                Transaction.Current.EnlistDurable(Guid.NewGuid(), enlistment, EnlistmentOptions.None);
            }
        }
    }
}