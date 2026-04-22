using Amazon.Lambda.APIGatewayEvents;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lambdas;

public class ViewImages
{
    private readonly AmazonS3Client _s3 = new();

    private static readonly string BucketName =
        Environment.GetEnvironmentVariable("IMAGE_BUCKET_NAME")
        ?? throw new InvalidOperationException("IMAGE_BUCKET_NAME is not set.");

    private static readonly int UrlExpiryMinutes =
        int.TryParse(Environment.GetEnvironmentVariable("PRESIGNED_URL_TTL_MINUTES"), out var m) ? m : 15;

    public async Task<APIGatewayProxyResponse> Handle(APIGatewayProxyRequest request)
    {
        var userId = AuthHelpers.GetUserId(request);
        if (string.IsNullOrWhiteSpace(userId))
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Body = JsonSerializer.Serialize(new { error = "Unauthorized" }),
                Headers = AuthHelpers.CreateCorsHeaders(request, "OPTIONS", "GET")
            };

        var prefix = $"processed/{userId}/";
        var listResponse = await _s3.ListObjectsAsync(new ListObjectsRequest { BucketName = BucketName, Prefix = prefix });

        var s3Objects = (listResponse.S3Objects ?? new List<S3Object>())
            .Where(o => !string.IsNullOrWhiteSpace(o.Key))
            .ToList();

        var items = await Task.WhenAll(s3Objects.Select(BuildImageItem));

        return new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Body = JsonSerializer.Serialize(new { items }),
            Headers = AuthHelpers.CreateCorsHeaders(request, "OPTIONS", "GET")
        };
    }

    private async Task<object> BuildImageItem(S3Object obj)
    {
        var key = obj.Key!;
        var downloadUrl = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(UrlExpiryMinutes)
        });

        string sha256 = string.Empty;
        try
        {
            var head = await _s3.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = BucketName,
                Key = key
            });

            if (head.Metadata.Keys.Contains("x-amz-meta-sha256"))
                sha256 = head.Metadata["x-amz-meta-sha256"] ?? string.Empty;
            else if (head.Metadata.Keys.Contains("sha256"))
                sha256 = head.Metadata["sha256"] ?? string.Empty;
        }
        catch
        {
            // leave sha256 empty
        }

        return new
        {
            key,
            downloadUrl,
            sha256,
            lastModified = obj.LastModified,
            size = obj.Size
        };
    }
}