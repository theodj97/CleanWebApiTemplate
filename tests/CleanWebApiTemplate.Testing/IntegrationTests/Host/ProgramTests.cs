using CleanWebApiTemplate.Host;
using CleanWebApiTemplate.Testing.Common;

namespace CleanWebApiTemplate.Testing.IntegrationTests.Host;

[Collection(nameof(NonParallelCollection))]
public class ProgramTests
{
    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.INTEGRATION)]
    public void Main_WithNoEnvironment_Should_ThrowException()
    {
        // Arrange
        var args = Array.Empty<string>();
        // The functional-test fixture sets ASPNETCORE_ENVIRONMENT process-wide; the
        // "no environment" precondition must be forced explicitly (and restored).
        var originalEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);

        try
        {
            // Act
            Program.Main(args);
        }
        catch (Exception ex)
        {
            // Assert
            Assert.NotNull(ex);
            Assert.Equal("No environment variable was setted!", ex.Message);
            return;
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnvironment);
        }

        throw new Exception("Test should be catched");
    }
}
