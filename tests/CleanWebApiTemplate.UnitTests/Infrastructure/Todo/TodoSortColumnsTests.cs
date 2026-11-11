using CleanWebApiTemplate.Application.Todo;
using CleanWebApiTemplate.Infrastructure.Todo;

namespace CleanWebApiTemplate.UnitTests.Infrastructure.Todo;

/// <summary>The Application sort vocabulary and the SQL whitelist must name the same fields.</summary>
public class TodoSortColumnsTests
{
    [Fact]
    public void Map_HasExactlyTheApplicationKeys()
    {
        Assert.True(TodoSortCatalog.KeySet.SetEquals(TodoSortColumns.Map.Keys));
    }

    [Fact]
    public void Map_ResolvesEveryApplicationKeyCaseInsensitively()
    {
        foreach (string key in TodoSortCatalog.Keys)
        {
            string lowerCaseKey = char.ToLowerInvariant(key[0]) + key[1..];

            Assert.True(TodoSortColumns.Map.ContainsKey(lowerCaseKey), $"Missing sort column for '{lowerCaseKey}'.");
        }
    }
}
