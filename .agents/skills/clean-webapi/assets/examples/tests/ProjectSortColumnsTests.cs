using {App}.Application.Project.Sorting;
using {App}.Infrastructure.Repositories.Project.Helpers;

namespace {App}.Api.UnitTests.Infrastructure.Repositories.Project.Helpers;

/// <summary>The Application sort vocabulary and the SQL column map must name the same fields.</summary>
public class ProjectSortColumnsTests
{
    [Fact]
    public void Map_HasExactlyTheApplicationKeys()
    {
        Assert.True(ProjectSortCatalog.KeySet.SetEquals(ProjectSortColumns.Map.Keys));
    }

    [Fact]
    public void Map_ResolvesEveryApplicationKeyCaseInsensitively()
    {
        foreach (string key in ProjectSortCatalog.Keys)
        {
            string lowerCaseKey = char.ToLowerInvariant(key[0]) + key[1..];

            Assert.True(ProjectSortColumns.Map.ContainsKey(lowerCaseKey),
                        $"Missing sort column for '{lowerCaseKey}'.");
        }
    }
}
