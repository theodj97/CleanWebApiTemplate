using CleanWebApiTemplate.Infrastructure.Constants;

namespace CleanWebApiTemplate.Testing.UnitTests.Infrastructure;

public class TodoTableTests
{
    [Fact]
    public void TitleMaxLength_Should_Return255()
    {
        // Act & Assert
        Assert.Equal((byte)255, TodoTable.TitleMaxLength);
    }

    [Fact]
    public void DescriptionMaxLength_Should_Return1000()
    {
        // Act & Assert
        Assert.Equal(1000, TodoTable.DescriptionMaxLength);
    }

    [Fact]
    public void CreatedByMaxLength_Should_Return255()
    {
        // Act & Assert
        Assert.Equal((byte)255, TodoTable.CreatedByMaxLength);
    }

    [Fact]
    public void UpdatedByMaxLength_Should_Return255()
    {
        // Act & Assert
        Assert.Equal((byte)255, TodoTable.UpdatedByMaxLength);
    }

    [Fact]
    public void Name_Should_BeTodo()
    {
        // Assert
        Assert.Equal("Todo", TodoTable.Name);
    }

    [Fact]
    public void IdColumn_Should_BeId()
    {
        // Assert
        Assert.Equal("Id", TodoTable.IdColumn);
    }

    [Fact]
    public void TitleColumn_Should_BeTitle()
    {
        // Assert
        Assert.Equal("Title", TodoTable.TitleColumn);
    }

    [Fact]
    public void DescriptionColumn_Should_BeDescription()
    {
        // Assert
        Assert.Equal("Description", TodoTable.DescriptionColumn);
    }

    [Fact]
    public void CreatedAtColumn_Should_BeCreatedAt()
    {
        // Assert
        Assert.Equal("CreatedAt", TodoTable.CreatedAtColumn);
    }

    [Fact]
    public void UpdatedAtColumn_Should_BeUpdatedAt()
    {
        // Assert
        Assert.Equal("UpdatedAt", TodoTable.UpdatedAtColumn);
    }

    [Fact]
    public void StatusColumn_Should_BeStatus()
    {
        // Assert
        Assert.Equal("Status", TodoTable.StatusColumn);
    }

    [Fact]
    public void CreatedByColumn_Should_BeCreatedBy()
    {
        // Assert
        Assert.Equal("CreatedBy", TodoTable.CreatedByColumn);
    }

    [Fact]
    public void UpdatedByColumn_Should_BeUpdatedBy()
    {
        // Assert
        Assert.Equal("UpdatedBy", TodoTable.UpdatedByColumn);
    }

    [Fact]
    public void CreateTableSql_Should_NotBeNullOrEmpty()
    {
        // Assert
        Assert.False(string.IsNullOrWhiteSpace(TodoTable.CreateTableSql));
        Assert.Contains("CREATE TABLE", TodoTable.CreateTableSql);
        Assert.Contains("Todo", TodoTable.CreateTableSql);
    }
}
