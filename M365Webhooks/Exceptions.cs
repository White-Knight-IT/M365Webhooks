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
}

