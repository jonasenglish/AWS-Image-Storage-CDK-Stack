using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Constructs;

namespace UnityInfra.Constructs;

public sealed class AuthConstruct : Construct
{
    public UserPool UserPool { get; }
    public UserPoolClient UserPoolClient { get; }
    public string DomainName { get; }

    public AuthConstruct(Construct scope, string id, string callbackUrl, string domainPrefix) : base(scope, id)
    {
        UserPool = new UserPool(this, "UserPool", new UserPoolProps
        {
            SelfSignUpEnabled = true,
            SignInAliases = new SignInAliases { Email = true }
        });

        UserPoolClient = new UserPoolClient(this, "UserPoolClient", new UserPoolClientProps
        {
            UserPool = UserPool,
            GenerateSecret = false,
            OAuth = new OAuthSettings
            {
                Flows = new OAuthFlows
                {
                    ImplicitCodeGrant = true
                },
                Scopes =
                [
                    OAuthScope.OPENID,
                    OAuthScope.EMAIL,
                    OAuthScope.PROFILE
                ],
                CallbackUrls = [callbackUrl],
                LogoutUrls = [callbackUrl]
            }
        });

        _ = UserPool.AddDomain("UserPoolDomain", new UserPoolDomainOptions
        {
            CognitoDomain = new CognitoDomainOptions
            {
                DomainPrefix = domainPrefix
            }
        });

        DomainName = $"{domainPrefix}.auth.{Stack.Of(this).Region}.amazoncognito.com";
    }
}