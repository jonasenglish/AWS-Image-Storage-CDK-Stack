using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

[assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Lambdas;

public class UploadImage
{
    private readonly AmazonS3Client _s3 = new();
    private static readonly string BucketName =
        Environment.GetEnvironmentVariable("IMAGE_BUCKET_NAME")
        ?? throw new InvalidOperationException("IMAGE_BUCKET_NAME is not set.");

    public Task<APIGatewayProxyResponse> Handle(APIGatewayProxyRequest request)
    {
        var userId = AuthHelpers.GetUserId(request);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Body = JsonSerializer.Serialize(new { error = "Unauthorized" }),
                Headers = AuthHelpers.CreateCorsHeaders(request, "OPTIONS", "POST")
            });
        }

        var key = $"uploads/{userId}/{Guid.NewGuid()}.png";
        var url = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(5)
        });

        return Task.FromResult(new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Body = JsonSerializer.Serialize(new { uploadUrl = url, key }),
            Headers = AuthHelpers.CreateCorsHeaders(request, "OPTIONS", "POST")
        });
    }
}