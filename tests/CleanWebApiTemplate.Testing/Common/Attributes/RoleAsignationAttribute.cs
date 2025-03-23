using System.Reflection;
using CleanWebApiTemplate.Testing.Configuration;
using Xunit.Sdk;

namespace CleanWebApiTemplate.Testing.Common.Attributes;

public class RoleAsignationAttribute(string role) : BeforeAfterTestAttribute
{
    private readonly string Role = role;

    public override void Before(MethodInfo methodUnderTest)
    {
        TestAuthHandler.SetRole(Role);
    }

    public override void After(MethodInfo methodUnderTest)
    {
        TestAuthHandler.ClearRole();
    }
}
