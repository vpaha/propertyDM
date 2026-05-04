using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

public sealed class S3LogUploadService : BackgroundService
{
    private readonly IAmazonS3 _s3;
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;

    public S3LogUploadService(
        IAmazonS3 s3,
        IHostEnvironment env,
        IConfiguration config)
    {
        _s3 = s3;
        _env = env;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bucket = _config["AWS:BucketName"];
        var logDirectory = Path.Combine(_env.ContentRootPath, "Logs");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (Directory.Exists(logDirectory))
            {
                var today = DateTime.UtcNow.Date;
                foreach (var file in Directory.GetFiles(logDirectory, "log-*.txt"))
                {
                    var fileDate = File.GetLastWriteTimeUtc(file).Date;
                    if (fileDate >= today) continue; // skip active file

                    var fileName = Path.GetFileName(file);

                    await using var stream = File.OpenRead(file);

                    await _s3.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest
                    {
                        BucketName = bucket,
                        Key = $"logs/{DateTime.UtcNow:yyyy/MM/dd}/{fileName}",
                        InputStream = stream,
                        ContentType = "text/plain"
                    }, stoppingToken);
                }
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}