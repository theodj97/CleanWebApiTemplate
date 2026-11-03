using CleanWebApiTemplate.Application.Abstractions.Identity;

namespace CleanWebApiTemplate.Api.UnitTests.Application.Identity;

/// <summary>The owner rule lives in one value type, not in the HTTP mappers.</summary>
public class OwnerScopeTests
{
    private static readonly OwnerScope User = new("user@example.com", IsAdmin: false);
    private static readonly OwnerScope Admin = new("admin@example.com", IsAdmin: true);

    [Fact]
    public void VisibleOwner_AsAdmin_IsNull()
    {
        Assert.Null(Admin.VisibleOwner);
    }

    [Fact]
    public void VisibleOwner_AsUser_IsTheEmail()
    {
        Assert.Equal("user@example.com", User.VisibleOwner);
    }

    [Fact]
    public void OwnerForCreate_AsAdmin_IsTheAdminEmail()
    {
        Assert.Equal("admin@example.com", Admin.OwnerForCreate);
    }

    [Fact]
    public void FilterOwners_AsUser_ForcesTheCallerEmail()
    {
        Assert.Equal(new[] { "user@example.com" },
                     User.FilterOwners(["other@example.com"]));
    }

    [Fact]
    public void FilterOwners_AsAdmin_KeepsTheRequestedOwners()
    {
        Assert.Equal(new[] { "first@example.com", "second@example.com" },
                     Admin.FilterOwners(["first@example.com", "second@example.com"]));
    }
}
