using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

public interface IImageService
{
    Task UploadAsync(int damageId, string fileName, string contentType, Stream stream);
    Task DeleteAsync(int damageId, string fileName);
    Task<List<ImageItem>> ListAsync(int damageId);
}

public sealed class S3ImageService : IImageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public S3ImageService(IAmazonS3 s3, IConfiguration config)
    {
        _s3 = s3;
        _bucket = config["AWS:BucketName"] ?? throw new InvalidOperationException("S3 bucket not configured.");
    }

    public async Task UploadAsync(int damageId, string fileName, string contentType, Stream stream)
    {
        var key = BuildKey(damageId, fileName);

        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            AutoCloseStream = false
        });
    }

    public async Task DeleteAsync(int damageId, string fileName)
    {
        var prefix = $"damage-images/{damageId}/";

        var response = await _s3.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = _bucket,
            Prefix = prefix
        });

        var objectToDelete = response.S3Objects
            .FirstOrDefault(x => Path.GetFileName(x.Key)
                .Equals(fileName, StringComparison.OrdinalIgnoreCase));

        if (objectToDelete is null)
        {
            return;
        }

        await _s3.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _bucket,
            Key = objectToDelete.Key
        });
    }

    public async Task<List<ImageItem>> ListAsync(int damageId)
    {
        var prefix = $"damage-images/{damageId}/";

        var response = await _s3.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = _bucket,
            Prefix = prefix
        });

        return response.S3Objects
            .Where(x => IsImageFile(x.Key))
            .Select(x => new ImageItem
            {
                FileName = Path.GetFileName(x.Key),
                Url = GeneratePreSignedUrl(x.Key)
            })
            .ToList();
    }

    private static string BuildKey(int damageId, string fileName)
    {
        return $"damage-images/{damageId}/{fileName}";
    }

    private string GeneratePreSignedUrl(string key)
    {
        return _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(30)
        });
    }

    private static bool IsImageFile(string key)
    {
        var extension = Path.GetExtension(key).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp";
    }
}

public sealed class ImageItem
{
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}