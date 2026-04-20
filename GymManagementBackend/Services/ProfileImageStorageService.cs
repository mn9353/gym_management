using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using GymManagementBackend.Configuration;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace GymManagementBackend.Services
{
    public interface IProfileImageStorageService
    {
        Task<string?> StoreMemberImageAsync(Guid memberId, string? rawImageValue, CancellationToken cancellationToken = default);
        Task<string?> StoreUserImageAsync(Guid userId, string? rawImageValue, CancellationToken cancellationToken = default);
        Task DeleteImageByUrlAsync(string? imageUrl, CancellationToken cancellationToken = default);
    }

    public class ProfileImageStorageService : IProfileImageStorageService
    {
        private readonly ObjectStorageSettings _settings;
        private readonly ILogger<ProfileImageStorageService> _logger;

        public ProfileImageStorageService(
            IOptions<ObjectStorageSettings> settings,
            ILogger<ProfileImageStorageService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public Task<string?> StoreMemberImageAsync(Guid memberId, string? rawImageValue, CancellationToken cancellationToken = default)
        {
            return StoreImageAsync("members", memberId, rawImageValue, cancellationToken);
        }

        public Task<string?> StoreUserImageAsync(Guid userId, string? rawImageValue, CancellationToken cancellationToken = default)
        {
            return StoreImageAsync("users", userId, rawImageValue, cancellationToken);
        }

        public async Task DeleteImageByUrlAsync(string? imageUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || !_settings.Enabled)
            {
                return;
            }

            if (!TryExtractObjectKey(imageUrl, out var key) || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            ValidateStorageSettings();
            using var client = BuildS3Client();
            try
            {
                await client.DeleteObjectAsync(new DeleteObjectRequest
                {
                    BucketName = _settings.BucketName,
                    Key = key
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete image object from storage. Key={Key}", key);
            }
        }

        private async Task<string?> StoreImageAsync(string entityFolder, Guid entityId, string? rawImageValue, CancellationToken cancellationToken)
        {
            if (rawImageValue is null)
            {
                return null;
            }

            var trimmed = rawImageValue.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return null;
            }

            if (Uri.TryCreate(trimmed, UriKind.Absolute, out _))
            {
                return trimmed;
            }

            if (!_settings.Enabled)
            {
                throw new InvalidOperationException("Object storage is not configured. Enable ObjectStorage settings.");
            }

            ValidateStorageSettings();
            var bytes = DecodeImageBytes(trimmed);
            await using var compressed = await CompressToJpegAsync(bytes, cancellationToken);

            var key = BuildObjectKey(entityFolder, entityId);
            var put = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                InputStream = compressed,
                ContentType = "image/jpeg",
                DisablePayloadSigning = true
            };

            using var client = BuildS3Client();
            await client.PutObjectAsync(put, cancellationToken);
            return BuildPublicUrl(key);
        }

        private AmazonS3Client BuildS3Client()
        {
            var endpoint = _settings.Endpoint.Trim();
            var config = new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = _settings.ForcePathStyle,
                AuthenticationRegion = string.IsNullOrWhiteSpace(_settings.Region) ? "auto" : _settings.Region
            };

            var credentials = new BasicAWSCredentials(_settings.AccessKeyId, _settings.SecretAccessKey);
            return new AmazonS3Client(credentials, config);
        }

        private void ValidateStorageSettings()
        {
            if (string.IsNullOrWhiteSpace(_settings.BucketName) ||
                string.IsNullOrWhiteSpace(_settings.AccessKeyId) ||
                string.IsNullOrWhiteSpace(_settings.SecretAccessKey) ||
                string.IsNullOrWhiteSpace(_settings.Endpoint))
            {
                throw new InvalidOperationException("ObjectStorage configuration is incomplete.");
            }
        }

        private string BuildObjectKey(string entityFolder, Guid entityId)
        {
            var root = (string.IsNullOrWhiteSpace(_settings.RootFolder) ? "images" : _settings.RootFolder.Trim()).Trim('/');
            return $"{root}/{entityFolder}/{entityId:N}.jpg";
        }

        private string BuildPublicUrl(string key)
        {
            if (!string.IsNullOrWhiteSpace(_settings.PublicBaseUrl))
            {
                return $"{_settings.PublicBaseUrl.TrimEnd('/')}/{key}";
            }

            var endpoint = _settings.Endpoint.TrimEnd('/');
            return $"{endpoint}/{_settings.BucketName}/{key}";
        }

        private bool TryExtractObjectKey(string imageUrl, out string key)
        {
            key = string.Empty;
            var normalized = imageUrl.Trim();

            if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            {
                return false;
            }

            var path = Uri.UnescapeDataString(uri.AbsolutePath).Trim('/');
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var bucketPrefix = $"{_settings.BucketName}/";
            var objectPublicPrefix = $"storage/v1/object/public/{_settings.BucketName}/";
            var objectSignPrefix = $"storage/v1/object/sign/{_settings.BucketName}/";
            var s3Prefix = "storage/v1/s3/";

            if (path.StartsWith(objectPublicPrefix, StringComparison.OrdinalIgnoreCase))
            {
                key = path[objectPublicPrefix.Length..];
                return true;
            }

            if (path.StartsWith(objectSignPrefix, StringComparison.OrdinalIgnoreCase))
            {
                key = path[objectSignPrefix.Length..];
                return true;
            }

            if (path.StartsWith(s3Prefix, StringComparison.OrdinalIgnoreCase))
            {
                var remainder = path[s3Prefix.Length..];
                if (remainder.StartsWith(bucketPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    key = remainder[bucketPrefix.Length..];
                    return true;
                }
            }

            if (path.StartsWith(bucketPrefix, StringComparison.OrdinalIgnoreCase))
            {
                key = path[bucketPrefix.Length..];
                return true;
            }

            return false;
        }

        private static byte[] DecodeImageBytes(string value)
        {
            if (value.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                var commaIndex = value.IndexOf(',');
                if (commaIndex <= 0 || commaIndex == value.Length - 1)
                {
                    throw new InvalidOperationException("Invalid image data URL payload.");
                }

                var base64Segment = value[(commaIndex + 1)..];
                return Convert.FromBase64String(base64Segment);
            }

            try
            {
                return Convert.FromBase64String(value);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Image payload must be a valid base64 string or data URL.", ex);
            }
        }

        private async Task<MemoryStream> CompressToJpegAsync(byte[] sourceBytes, CancellationToken cancellationToken)
        {
            await using var sourceStream = new MemoryStream(sourceBytes);
            using var image = await Image.LoadAsync(sourceStream, cancellationToken);
            var maxDimension = Math.Max(320, _settings.MaxDimension);
            if (image.Width > maxDimension || image.Height > maxDimension)
            {
                var resize = new ResizeOptions
                {
                    Size = new Size(maxDimension, maxDimension),
                    Mode = ResizeMode.Max
                };
                image.Mutate(x => x.Resize(resize));
            }

            var quality = Math.Clamp(_settings.JpegQuality, 50, 90);
            var output = new MemoryStream();
            await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = quality }, cancellationToken);
            output.Position = 0;
            return output;
        }
    }
}
