using System.Reflection;
using CleanWebApiTemplate.Testing.Configuration;
using Xunit.Sdk;

namespace CleanWebApiTemplate.Testing.Common.Attributes;

public class IdentityAsignationAttribute(string? userName = null,
                                         string? userEmail = null,
                                         string? role = null) : BeforeAfterTestAttribute
{
    private readonly string? UserName = userName;
    private readonly string? UserEmail = userEmail;
    private readonly string? Role = role;

    public override void Before(MethodInfo methodUnderTest)
    {
        if (string.IsNullOrEmpty(Role) is false)
            TestAuthHandler.SetRole(Role);
        if (string.IsNullOrEmpty(UserName) is false)
            TestAuthHandler.SetUserName(UserName);
        if (string.IsNullOrEmpty(UserEmail) is false)
            TestAuthHandler.SetUserEmail(UserEmail);
    }

    public override void After(MethodInfo methodUnderTest) => TestAuthHandler.ResetDefault();

}
