using Bogus;
using {{AppNamespace}};

namespace {{SharedNamespace}}.Data;

// EXAMPLE — one builder per entity/aggregate. Defaults always produce a VALID entity, so each test only states the
// values that matter to it. Use the domain's own constructor/factory so invariants keep being enforced.
public sealed class CustomerBuilder
{
    private readonly Faker _faker = TestData.CreateFaker();
    private string _name;
    private string _email;

    public CustomerBuilder()
    {
        _name = _faker.Name.FullName();
        _email = $"{TestData.Unique("customer")}@example.com";
    }

    public CustomerBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CustomerBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public Customer Build() => new(_name, _email);
}
