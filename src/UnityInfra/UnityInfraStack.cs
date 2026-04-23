using Amazon.CDK;
using Constructs;
using UnityInfra.Constructs;

namespace UnityInfra;

public class UnityInfraStack : Stack
{
    internal UnityInfraStack(Construct scope, string id, IStackProps? props = null)
        : base(scope, id, props)
    {
        var storage = new StorageConstruct(this, "Storage");

        var webGlHttpsUrl = $"https://{storage.WebGlBucket.BucketRegionalDomainName}/index.html";
        var domainPrefix = $"{StackName.ToLowerInvariant()}-{Node.Addr[..8].ToLowerInvariant()}";
        var auth = new AuthConstruct(this, "Auth", webGlHttpsUrl, domainPrefix);

        var lambdas = new LambdaConstruct(this, "Lambdas", storage.ImageBucket);

        var api = new ApiConstruct(
            this,
            "Api",
            auth.UserPool,
            lambdas.UploadImage,
            lambdas.ViewImages,
            lambdas.DeleteImage);

        var runtimeConfigJson = RuntimeConfigBuilder.Build(
            api.UploadUrl,
            api.ViewImagesUrl,
            api.DeleteImageUrlTemplate,
            auth.UserPool.UserPoolId,
            auth.UserPoolClient.UserPoolClientId,
            auth.DomainName,
            Region,
            webGlHttpsUrl);

        var hostedUiLoginUrl =
            $"https://{auth.DomainName}/login?client_id={auth.UserPoolClient.UserPoolClientId}&response_type=token&scope=openid+email+profile&redirect_uri={webGlHttpsUrl}";

        _ = new FrontendDeploymentConstruct(this, "Frontend", storage.WebGlBucket, runtimeConfigJson);

        _ = new CfnOutput(this, "WebGLBucketName", new CfnOutputProps { Value = storage.WebGlBucket.BucketName });
        _ = new CfnOutput(this, "WebGLWebsiteUrl", new CfnOutputProps { Value = storage.WebGlBucket.BucketWebsiteUrl });
        _ = new CfnOutput(this, "WebGLHttpsUrl", new CfnOutputProps { Value = webGlHttpsUrl });
        _ = new CfnOutput(this, "HostedUiLoginUrl", new CfnOutputProps { Value = hostedUiLoginUrl });
        _ = new CfnOutput(this, "ApiUrl", new CfnOutputProps { Value = api.Api.Url });
    }
}