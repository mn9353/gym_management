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
                ContentType = "image/jpeg"
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
