using CleanWebApiTemplate.Host;
using CleanWebApiTemplate.Testing.Common;

namespace CleanWebApiTemplate.Testing.IntegrationTests.Host;

public class ProgramTests
{
    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.INTEGRATION)]
    public void Main_WithNoEnvironment_Should_ThrowException()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        try
        {
            Program.Main(args);
        }
        catch (Exception ex)
        {
            // Assert
            Assert.NotNull(ex);
            Assert.Equal("No environment variable was setted!", ex.Message);
            return;
        }

        throw new Exception("Test should be catched");
    }
}
