using System.Reflection;
using Xunit.Sdk;

namespace CleanWebApiTemplate.Testing.Common.Attributes;

public class ResetDatabaseAttribute : BeforeAfterTestAttribute
{
    public override void After(MethodInfo methodUnderTest) => base.After(methodUnderTest);


    public override void Before(MethodInfo methodUnderTest) => TestServerFixture.ResetDatabaseAsync().Wait();

}