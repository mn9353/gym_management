namespace GymManagementBackend.Configuration
{
    public class ObjectStorageSettings
    {
        public bool Enabled { get; set; }
        public string BucketName { get; set; } = string.Empty;
        public string AccessKeyId { get; set; } = string.Empty;
        public string SecretAccessKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string Region { get; set; } = "auto";
        public string PublicBaseUrl { get; set; } = string.Empty;
        public string RootFolder { get; set; } = "images";
        public bool ForcePathStyle { get; set; } = true;
        public int MaxDimension { get; set; } = 1280;
        public int JpegQuality { get; set; } = 75;
    }
}
