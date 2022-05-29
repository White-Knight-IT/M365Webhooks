namespace M365Webhooks
{
    public class NotEnoughArguments : Exception
    {
        public NotEnoughArguments()
        {
        }

        public NotEnoughArguments(string message)
            : base(message)
        {
        }

        public NotEnoughArguments(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class MachineTime : Exception
    {
        public MachineTime()
        {
        }

        public MachineTime(string message)
            : base(message)
        {
        }

        public MachineTime(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}

