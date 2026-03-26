namespace SimpleModule.Storage.S3;

public sealed class S3StorageOptions
{
    public Uri? ServiceUrl { get; set; }
    public string BucketName { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public bool ForcePathStyle { get; set; }
}
