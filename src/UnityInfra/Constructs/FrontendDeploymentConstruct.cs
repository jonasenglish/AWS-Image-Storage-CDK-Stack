using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Deployment;
using Constructs;

namespace UnityInfra.Constructs;

public sealed class FrontendDeploymentConstruct : Construct
{
    public FrontendDeploymentConstruct(Construct scope, string id, Bucket webGlBucket, string runtimeConfigJson) : base(scope, id)
    {
        _ = new BucketDeployment(this, "DeployWebAndRuntimeConfig", new BucketDeploymentProps
        {
            Sources =
            [
                Source.Asset("webgl/unity-webgl.zip"),
                Source.Data("runtime-config.json", runtimeConfigJson)
            ],
            DestinationBucket = webGlBucket,
            MemoryLimit = 2560,
        });
    }
}