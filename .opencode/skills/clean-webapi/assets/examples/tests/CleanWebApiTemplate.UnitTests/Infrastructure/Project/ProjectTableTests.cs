using CleanWebApiTemplate.Domain.Project;
using CleanWebApiTemplate.Infrastructure.Project;

namespace CleanWebApiTemplate.UnitTests.Infrastructure.Project;

/// <summary>The table metadata is the single source of truth shared by SQL and validators.</summary>
public class ProjectTableTests
{
    [Fact]
    public void ColumnNamesAndConstraints_HaveTheExpectedValues()
    {
        Assert.Equal("Project", ProjectTable.Name);
        Assert.Equal("Id", ProjectTable.IdColumn);
        Assert.Equal("Name", ProjectTable.NameColumn);
        Assert.Equal("Description", ProjectTable.DescriptionColumn);
        Assert.Equal("CreatedAt", ProjectTable.CreatedAtColumn);
        Assert.Equal("UpdatedAt", ProjectTable.UpdatedAtColumn);
        Assert.Equal("Status", ProjectTable.StatusColumn);
        Assert.Equal("CreatedBy", ProjectTable.CreatedByColumn);
        Assert.Equal("UpdatedBy", ProjectTable.UpdatedByColumn);
        Assert.Equal("UX_Project_CreatedBy_Name", ProjectTable.UniqueNameIndexName);
        Assert.Equal(255, ProjectConstraints.NameMaxLength);
        Assert.Equal(1000, ProjectConstraints.DescriptionMaxLength);
        Assert.Equal(255, ProjectConstraints.OwnerMaxLength);
    }
}
