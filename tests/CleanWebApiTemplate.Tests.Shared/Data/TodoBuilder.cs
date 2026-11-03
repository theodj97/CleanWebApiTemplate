using Bogus;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;

namespace CleanWebApiTemplate.Tests.Shared.Data;

/// <summary>
/// Builder for <see cref="TodoEntity"/> with valid defaults, so each test states only what it cares about.
/// Titles are unique by default because the API rejects duplicates.
/// </summary>
public sealed class TodoBuilder
{
    private readonly Faker _faker = TestData.CreateFaker();
    private string _title;
    private string _description;
    private string _createdBy;
    private int _status = (int)ETodoStatus.Pending;

    public TodoBuilder()
    {
        _title = TestData.Unique("todo");
        _description = _faker.Lorem.Sentence();
        _createdBy = _faker.Internet.Email();
    }

    public TodoBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public TodoBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public TodoBuilder CreatedBy(string email)
    {
        _createdBy = email;
        return this;
    }

    public TodoBuilder WithStatus(ETodoStatus status)
    {
        _status = (int)status;
        return this;
    }

    public TodoEntity Build()
    {
        var now = DateTime.UtcNow;
        return new TodoEntity
        {
            Id = Ulid.NewUlid(),
            Title = _title,
            Description = _description,
            CreatedAt = now,
            UpdatedAt = now,
            Status = _status,
            CreatedBy = _createdBy,
            UpdatedBy = _createdBy,
        };
    }
}
