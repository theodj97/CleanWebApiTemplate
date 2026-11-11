using {App}.Domain.Models.Entities;
using {App}.Domain.Models.Enums.Project;
using Bogus;

namespace {App}.Tests.Shared.Data;

/// <summary>
/// Builder for <see cref="ProjectEntity"/> with valid defaults, so each test states only what it cares
/// about. Names are unique by default because the API rejects duplicates per owner.
/// </summary>
public sealed class ProjectBuilder
{
    private readonly Faker _faker = TestData.CreateFaker();
    private string _name;
    private string _description;
    private string _createdBy;
    private int _status = (int)EProjectStatus.Draft;

    public ProjectBuilder()
    {
        _name = TestData.Unique("project");
        _description = _faker.Lorem.Sentence();
        _createdBy = _faker.Internet.Email();
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

    public ProjectBuilder CreatedBy(string email)
    {
        _createdBy = email;
        return this;
    }

    public ProjectBuilder WithStatus(EProjectStatus status)
    {
        _status = (int)status;
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
            CreatedBy = _createdBy,
            UpdatedBy = _createdBy
        };
    }
}
