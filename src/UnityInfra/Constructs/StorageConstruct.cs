using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace UnityInfra.Constructs;

public sealed class StorageConstruct : Construct
{
    public Bucket WebGlBucket { get; }
    public Bucket ImageBucket { get; }

    public StorageConstruct(Construct scope, string id) : base(scope, id)
    {
        WebGlBucket = new Bucket(this, "WebGLBucket", new BucketProps
        {
            BlockPublicAccess = BlockPublicAccess.BLOCK_ACLS_ONLY,
            PublicReadAccess = true,
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true,
            WebsiteIndexDocument = "index.html"
        });

        var webGlWebsiteOrigin = WebGlBucket.BucketWebsiteUrl;
        var webGlHttpsOrigin = $"https://{WebGlBucket.BucketRegionalDomainName}";

        ImageBucket = new Bucket(this, "ImageBucket", new BucketProps
        {
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true,
            Cors =
            [
                new CorsRule
                {
                    AllowedMethods = [HttpMethods.PUT, HttpMethods.GET, HttpMethods.HEAD],
                    AllowedOrigins = [webGlWebsiteOrigin, webGlHttpsOrigin],
                    AllowedHeaders = ["*"],
                    ExposedHeaders = ["ETag"],
                    MaxAge = 3000
                }
            ]
        });
    }
}