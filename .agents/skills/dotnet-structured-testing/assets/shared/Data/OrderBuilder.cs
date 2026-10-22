using Bogus;
using {{AppNamespace}};

namespace {{SharedNamespace}}.Data;

// EXAMPLE — see CustomerBuilder. Ids of related entities come from the entities the test inserted
// (ForCustomer(customer.Id)), never from literal values: tests must not depend on concrete id values.
public sealed class OrderBuilder
{
    private readonly Faker _faker = TestData.CreateFaker();
    private Guid _customerId;
    private decimal _amount;
    private OrderStatus _status = OrderStatus.Pending;

    public OrderBuilder()
    {
        _customerId = _faker.Random.Guid();
        _amount = _faker.Finance.Amount(10, 500);
    }

    public OrderBuilder ForCustomer(Guid customerId)
    {
        _customerId = customerId;
        return this;
    }

    public OrderBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    public OrderBuilder WithStatus(OrderStatus status)
    {
        _status = status;
        return this;
    }

    public Order Build() => new(_customerId, _amount) { Status = _status };
}
