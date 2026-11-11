using CleanWebApiTemplate.Application.Project;
using CleanWebApiTemplate.Infrastructure.Project;

namespace CleanWebApiTemplate.UnitTests.Infrastructure.Project;

/// <summary>The Application sort vocabulary and the SQL whitelist must name the same fields.</summary>
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

            Assert.True(ProjectSortColumns.Map.ContainsKey(lowerCaseKey), $"Missing sort column for '{lowerCaseKey}'.");
        }
    }
}
