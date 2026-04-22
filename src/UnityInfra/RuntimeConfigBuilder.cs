using Amazon.CDK;
using System.Collections.Generic;

namespace UnityInfra;

public static class RuntimeConfigBuilder
{
    public static string Build(
        string uploadUrl,
        string viewImagesUrl,
        string deleteImageUrlTemplate,
        string userPoolId,
        string clientId,
        string domain,
        string region,
        string callbackUrl)
    {
        return Fn.Sub(
            "{\"uploadUrl\":\"${UploadUrl}\",\"viewImagesUrl\":\"${ViewImagesUrl}\",\"deleteImageUrlTemplate\":\"${DeleteImageUrlTemplate}\",\"cognitoUserPoolId\":\"${UserPoolId}\",\"cognitoClientId\":\"${ClientId}\",\"cognitoDomain\":\"${Domain}\",\"cognitoRegion\":\"${Region}\",\"cognitoCallbackUrl\":\"${CallbackUrl}\",\"hostedUiLoginUrl\":\"https://${Domain}/login?client_id=${ClientId}&response_type=token&scope=openid+email+profile&redirect_uri=${CallbackUrl}\",\"hostedUiLogoutUrl\":\"https://${Domain}/logout?client_id=${ClientId}&logout_uri=${CallbackUrl}\"}",
            new Dictionary<string, string>
            {
                ["UploadUrl"] = uploadUrl,
                ["ViewImagesUrl"] = viewImagesUrl,
                ["DeleteImageUrlTemplate"] = deleteImageUrlTemplate,
                ["UserPoolId"] = userPoolId,
                ["ClientId"] = clientId,
                ["Domain"] = domain,
                ["Region"] = region,
                ["CallbackUrl"] = callbackUrl
            });
    }
}