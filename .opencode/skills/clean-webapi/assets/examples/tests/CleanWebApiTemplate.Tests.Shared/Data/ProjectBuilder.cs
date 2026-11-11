using Bogus;
using CleanWebApiTemplate.Domain.Project;

namespace CleanWebApiTemplate.Tests.Shared.Data;

/// <summary>
/// Builder for <see cref="ProjectEntity"/> with valid defaults, so each test states only what it cares about.
/// Names are unique by default because the API rejects duplicates.
/// </summary>
public sealed class ProjectBuilder
{
    private readonly Faker _faker = TestData.CreateFaker();
    private string _name;
    private string _description;
    private string _owner;
    private EProjectStatus _status = EProjectStatus.Draft;

    public ProjectBuilder()
    {
        _name = TestData.Unique("project");
        _description = _faker.Lorem.Sentence();
        _owner = _faker.Internet.Email();
    }

    public ProjectBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ProjectBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>Sets the owner (the stored CreatedBy/UpdatedBy).</summary>
    public ProjectBuilder CreatedBy(string email)
    {
        _owner = email;
        return this;
    }

    public ProjectBuilder WithStatus(EProjectStatus status)
    {
        _status = status;
        return this;
    }

    public ProjectEntity Build()
    {
        DateTime now = DateTime.UtcNow;
        return new ProjectEntity
        {
            Id = Ulid.NewUlid(),
            Name = _name,
            Description = _description,
            CreatedAt = now,
            UpdatedAt = now,
            Status = _status,
            CreatedBy = _owner,
            UpdatedBy = _owner,
        };
    }
}
