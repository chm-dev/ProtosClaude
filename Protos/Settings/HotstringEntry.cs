namespace Protos.Settings
{
    public record HotstringEntry
    {
        public string Trigger         { get; init; } = string.Empty;
        public string Replacement     { get; init; } = string.Empty;
        public bool   RequireEndingChar { get; init; } = true;
        public bool   Enabled         { get; init; } = true;
    }
}
