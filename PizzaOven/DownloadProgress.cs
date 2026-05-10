namespace PizzaOven;

public class DownloadProgress
{
    public DownloadProgress(float percentage, long downloadedBytes, long totalBytes, string fileName)
    {
        Percentage      = percentage;
        DownloadedBytes = downloadedBytes;
        TotalBytes      = totalBytes;
        FileName        = fileName;
    }

    public float  Percentage      { get; set; }
    public long   DownloadedBytes { get; set; }
    public long   TotalBytes      { get; set; }
    public string FileName        { get; set; }
}
