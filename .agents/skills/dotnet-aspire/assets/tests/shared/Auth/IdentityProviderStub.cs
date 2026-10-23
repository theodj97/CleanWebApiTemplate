using WireMock.Admin.Mappings;
using WireMock.Client;

namespace {{SharedNamespace}}.Auth;

/// <summary>
/// LAST RESORT (strategy C of references/local-jwt-authentication.md, only after asking): turns a loopback "identity"
/// WireMock resource of the AppHost into the metadata endpoint the application trusts in functional tests. It serves
/// the OpenID Connect discovery document and the JWKS with the RS256 public key of <see cref="TestTokenIssuer"/>; the
/// app's Authority (or MetadataAddress) points to this resource through the AppHost. Prefer explicit keys through the
/// trust mapping (strategies A/B): this stub is still an HTTP metadata download, although it never leaves the machine.
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
