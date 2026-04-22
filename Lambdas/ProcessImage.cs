using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using System.Threading.Tasks;

namespace Lambdas;

public class ProcessImage
{
    private readonly AmazonS3Client _s3 = new();

    public async Task Handle(S3Event evnt)
    {
        foreach (var record in evnt.Records)
        {
            var bucket = record.S3.Bucket.Name;
            var key = record.S3.Object.Key;

            if (!key.StartsWith("uploads/"))
                continue;

            var processedKey = key.Replace("uploads/", "processed/");

            await _s3.CopyObjectAsync(new CopyObjectRequest
            {
                SourceBucket = bucket,
                SourceKey = key,
                DestinationBucket = bucket,
                DestinationKey = processedKey
            });
        }
    }
}