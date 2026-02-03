namespace fs_front.Utils
{
    public abstract class ErrorResponse
    {
        public string TypeError { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string OriginError { get; set; } = string.Empty;
    }
}
