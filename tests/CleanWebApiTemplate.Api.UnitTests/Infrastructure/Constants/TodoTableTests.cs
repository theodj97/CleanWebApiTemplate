using CleanWebApiTemplate.Infrastructure.Constants;

namespace CleanWebApiTemplate.Api.UnitTests.Infrastructure.Constants;

/// <summary>The table metadata is the single source of truth shared by SQL and validators.</summary>
public class TodoTableTests
{
    [Fact]
    public void ColumnNamesAndConstraints_HaveTheExpectedValues()
    {
        Assert.Equal("Todo", TodoTable.Name);
        Assert.Equal("Id", TodoTable.IdColumn);
        Assert.Equal("Title", TodoTable.TitleColumn);
        Assert.Equal("Description", TodoTable.DescriptionColumn);
        Assert.Equal("CreatedAt", TodoTable.CreatedAtColumn);
        Assert.Equal("UpdatedAt", TodoTable.UpdatedAtColumn);
        Assert.Equal("Status", TodoTable.StatusColumn);
        Assert.Equal("CreatedBy", TodoTable.CreatedByColumn);
        Assert.Equal("UpdatedBy", TodoTable.UpdatedByColumn);
        Assert.Equal("UX_Todo_CreatedBy_Title", TodoTable.UniqueTitleIndexName);
        Assert.Equal(255, TodoTable.TitleMaxLength);
        Assert.Equal(1000, TodoTable.DescriptionMaxLength);
        Assert.Equal(255, TodoTable.CreatedByMaxLength);
        Assert.Equal(255, TodoTable.UpdatedByMaxLength);
    }
}
