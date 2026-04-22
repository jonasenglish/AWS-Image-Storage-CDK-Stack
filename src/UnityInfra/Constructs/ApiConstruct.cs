using System.Collections.Generic;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.Lambda;
using Constructs;

namespace UnityInfra.Constructs;

public sealed class ApiConstruct : Construct
{
    public RestApi Api { get; }
    public string UploadUrl { get; }
    public string ViewImagesUrl { get; }
    public string DeleteImageUrlTemplate { get; }

    public ApiConstruct(
        Construct scope,
        string id,
        UserPool userPool,
        IFunction uploadImage,
        IFunction viewImages,
        IFunction deleteImage) : base(scope, id)
    {
        var corsOptions = new CorsOptions
        {
            AllowOrigins = Cors.ALL_ORIGINS,
            AllowMethods =
            [
                "OPTIONS",
                "GET",
                "POST",
                "DELETE"
            ],
            AllowHeaders =
            [
                "Content-Type",
                "Authorization"
            ]
        };

        Api = new RestApi(this, "UnityApi", new RestApiProps
        {
            DefaultCorsPreflightOptions = corsOptions
        });

        var authorizer = new CognitoUserPoolsAuthorizer(this, "CognitoAuthorizer", new CognitoUserPoolsAuthorizerProps
        {
            CognitoUserPools = [userPool]
        });

        _ = new GatewayResponse(this, "Default4xxCors", new GatewayResponseProps
        {
            RestApi = Api,
            Type = ResponseType_.DEFAULT_4XX,
            ResponseHeaders = new Dictionary<string, string>
            {
                ["Access-Control-Allow-Origin"] = "'*'",
                ["Access-Control-Allow-Headers"] = "'Content-Type,Authorization'",
                ["Access-Control-Allow-Methods"] = "'OPTIONS,GET,POST,DELETE'"
            }
        });

        _ = new GatewayResponse(this, "Default5xxCors", new GatewayResponseProps
        {
            RestApi = Api,
            Type = ResponseType_.DEFAULT_5XX,
            ResponseHeaders = new Dictionary<string, string>
            {
                ["Access-Control-Allow-Origin"] = "'*'",
                ["Access-Control-Allow-Headers"] = "'Content-Type,Authorization'",
                ["Access-Control-Allow-Methods"] = "'OPTIONS,GET,POST,DELETE'"
            }
        });

        var uploadIntegration = new LambdaIntegration(uploadImage);
        var viewIntegration = new LambdaIntegration(viewImages);
        var deleteIntegration = new LambdaIntegration(deleteImage);

        Api.Root.AddResource("upload").AddMethod("POST", uploadIntegration, new MethodOptions
        {
            AuthorizationType = AuthorizationType.COGNITO,
            Authorizer = authorizer
        });

        var images = Api.Root.AddResource("images");
        images.AddMethod("GET", viewIntegration, new MethodOptions
        {
            AuthorizationType = AuthorizationType.COGNITO,
            Authorizer = authorizer
        });

        images.AddResource("{id+}").AddMethod("DELETE", deleteIntegration, new MethodOptions
        {
            AuthorizationType = AuthorizationType.COGNITO,
            Authorizer = authorizer
        });

        UploadUrl = Api.UrlForPath("/upload");
        ViewImagesUrl = Api.UrlForPath("/images");
        DeleteImageUrlTemplate = Api.UrlForPath("/images/{id+}");
    }
}