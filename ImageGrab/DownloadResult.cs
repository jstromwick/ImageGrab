namespace ImageGrab
{
    public class DownloadResult
    {
        public string Url { get; set; }
        public string ErrorReason { get; set; }
        public bool WasSuccessful => string.IsNullOrWhiteSpace(ErrorReason);

        public string FileLocation { get; set; }
        public long? FileSize { get; set; }
    }
}