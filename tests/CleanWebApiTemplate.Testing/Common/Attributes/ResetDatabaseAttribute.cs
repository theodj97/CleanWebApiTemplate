using System.Reflection;
using Xunit.v3;

namespace CleanWebApiTemplate.Testing.Common.Attributes;

public class ResetDatabaseAttribute : BeforeAfterTestAttribute
{
    public override void After(MethodInfo methodUnderTest, IXunitTest test) => base.After(methodUnderTest, test);


    public override void Before(MethodInfo methodUnderTest, IXunitTest test) => TestServerFixture.ResetDatabaseAsync().Wait();

}
