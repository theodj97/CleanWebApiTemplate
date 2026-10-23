using Bogus;
using {{AppNamespace}};

namespace {{SharedNamespace}}.Data;

// EXAMPLE — one builder per entity.
/// <summary>Valid defaults, so each test only states what it cares about. Owners come from TestUsers, never literals.</summary>
public sealed class OrderBuilder
{
    private readonly Faker _faker = TestData.CreateFaker();
    private string _customerSubject;
    private decimal _amount;

    public OrderBuilder()
    {
        _customerSubject = _faker.Random.Guid().ToString();
        _amount = _faker.Finance.Amount(10, 500);
    }

    public OrderBuilder ForCustomer(string subject)
    {
        _customerSubject = subject;
        return this;
    }

    public OrderBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    public Order Build() => new()
    {
        Id = Guid.NewGuid(),
        CustomerSubject = _customerSubject,
        Amount = _amount,
        CreatedAt = DateTimeOffset.UtcNow,
    };
}
