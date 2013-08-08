namespace Runner.Saga
{
    using System;

    [Serializable]
    public class StartSagaMessage : MessageBase
    {
        public int SequenceNo { get; set; }
    }
}