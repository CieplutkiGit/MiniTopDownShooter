namespace Application.Flow
{
    public sealed class ReturnToBaseCommand
    {
        public string Reason { get; }

        public ReturnToBaseCommand(string reason = null)
        {
            Reason = reason ?? string.Empty;
        }

        public static ReturnToBaseCommand Default { get; } = new ReturnToBaseCommand();
    }
}
