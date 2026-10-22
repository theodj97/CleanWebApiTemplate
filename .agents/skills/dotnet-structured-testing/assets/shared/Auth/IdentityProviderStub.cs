using WireMock.Admin.Mappings;
using WireMock.Client;

namespace {{SharedNamespace}}.Auth;

/// <summary>
/// Turns the "identity" WireMock resource into an identity provider the application trusts in functional tests:
/// it serves the OpenID Connect discovery document and the JWKS with the public key of <see cref="TestTokenIssuer"/>.
/// The app's Authority (or MetadataAddress) points to this resource through the AppHost, so no production code changes.
/// This resource is never reset between tests: per-test resets only touch the third-party WireMock.
/// </summary>
public static class IdentityProviderStub
{
    public static async Task RegisterAsync(IWireMockAdminApi identityAdmin, string authority, TestTokenIssuer issuer)
    {
        // JwtBearer requests "{Authority}/.well-known/openid-configuration" unless MetadataAddress is set.
        // ADAPT: Microsoft.Identity.Web builds "{Instance}{TenantId}/v2.0/.well-known/openid-configuration";
        // register that path too (with the matching "authority" base URL) when the app uses it.
        await identityAdmin.PostMappingAsync(JsonGet("/.well-known/openid-configuration", issuer.GetDiscoveryDocumentJson(authority)));
        await identityAdmin.PostMappingAsync(JsonGet("/.well-known/jwks.json", issuer.GetJwksJson()));
    }

    private static MappingModel JsonGet(string path, string json) => new()
    {
        Guid = Guid.NewGuid(),
        Request = new RequestModel { Methods = ["GET"], Path = path },
        Response = new ResponseModel
        {
            StatusCode = 200,
            Body = json,
            Headers = new Dictionary<string, object> { ["Content-Type"] = "application/json" },
        },
    };
}
