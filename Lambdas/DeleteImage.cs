using Amazon.Lambda.APIGatewayEvents;
using Amazon.S3;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lambdas;

public class DeleteImage
{
    private readonly AmazonS3Client _s3 = new();
    private static readonly string BucketName =
        Environment.GetEnvironmentVariable("IMAGE_BUCKET_NAME")
        ?? throw new InvalidOperationException("IMAGE_BUCKET_NAME is not set.");

    public async Task<APIGatewayProxyResponse> Handle(APIGatewayProxyRequest request)
    {
        var userId = AuthHelpers.GetUserId(request);
        if (string.IsNullOrWhiteSpace(userId))
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Body = JsonSerializer.Serialize(new { error = "Unauthorized" }),
                Headers = AuthHelpers.CreateCorsHeaders(request, "OPTIONS", "DELETE")
            };

        if (request.PathParameters == null || !request.PathParameters.TryGetValue("id", out var rawId) || string.IsNullOrWhiteSpace(rawId))
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = JsonSerializer.Serialize(new { error = "Missing id." }),
                Headers = AuthHelpers.CreateCorsHeaders(request, "OPTIONS", "DELETE")
            };

        var key = Uri.UnescapeDataString(rawId);
        if (!key.StartsWith($"processed/{userId}/", StringComparison.Ordinal))
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Forbidden,
                Body = JsonSerializer.Serialize(new { error = "Forbidden" }),
                Headers = AuthHelpers.CreateCorsHeaders(request, "OPTIONS", "DELETE")
            };

        await _s3.DeleteObjectAsync(BucketName, key);
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.NoContent,
            Headers = AuthHelpers.CreateCorsHeaders(request, "OPTIONS", "DELETE")
        };
    }
}