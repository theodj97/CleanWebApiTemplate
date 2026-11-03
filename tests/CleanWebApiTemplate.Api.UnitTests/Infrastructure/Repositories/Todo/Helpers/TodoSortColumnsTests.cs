using CleanWebApiTemplate.Application.Todo.Sorting;
using CleanWebApiTemplate.Infrastructure.Repositories.Todo.Helpers;

namespace CleanWebApiTemplate.Api.UnitTests.Infrastructure.Repositories.Todo.Helpers;

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
