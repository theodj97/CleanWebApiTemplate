# Unit tests

Template: `assets/tests/unit/OrderPricingServiceTests.cs`. Unit tests never reference Shared, the AppHost or any Aspire
package: they run without Docker (SKILL.md Phase 9).

## Scope

One class, or a small and very cohesive group of classes, in isolation:
- domain logic (entities, value objects, domain services, state transitions, calculations);
- validators (FluentValidation validators, custom validation logic);
- mappings with logic (not property-to-property copies);
- command/query handlers and application services, with their dependencies substituted.

Do not test trivial code (DTOs, auto-properties, constructors that only assign) or the framework (model binding,
EF Core itself, DI registration). They add maintenance without catching bugs.

If the class under test talks to the database directly (a handler using `DbContext`), do not mock `DbContext` or
`DbSet`: a mocked query proves nothing about the real one. Cover that behaviour with a component test instead, and unit
test only the logic that can be separated from the query.

## Rules

- Every dependency is substituted with NSubstitute (or the mocking library the repository already uses).
- No I/O, network, database, file system, Docker or Aspire, and no fixture unless something concrete justifies it.
  Unit tests run in milliseconds, without Docker, in parallel (xUnit's default), and share no state.
- Time: inject `TimeProvider` and use `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`). If the code uses
  `DateTime.Now/UtcNow` directly, report it as an improvement instead of changing production code.
- Logging: `NullLogger<T>.Instance`, or a substitute when the log itself is the behaviour under test.
- Random data: Bogus with a constant seed created locally (`new Faker<T>().UseSeed(1234)` or
  `new Faker { Random = new Randomizer(1234) }`). UnitTests do not reference Shared (it would bring Aspire), and the
  shared per-test seed in `TestData` is meant for serial projects anyway.
- Internal classes: `<InternalsVisibleTo Include="{App}.UnitTests" />` in the production csproj is an acceptable,
  declared testability change; prefer testing through the public surface when it is reasonable.

## NSubstitute patterns

```csharp
var repository = Substitute.For<IOrderRepository>();

// Arrange return values
repository.FindAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
repository.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Order?)null);
discounts.GetPercentage(Arg.Is<Guid>(id => id == customerId), Arg.Any<DateTimeOffset>()).Returns(10m);

// Make a dependency fail (using NSubstitute.ExceptionExtensions)
gateway.ChargeAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>()).ThrowsAsync(new PaymentDeclinedException());

// Assert interactions (only when the interaction is the behaviour: a save, a notification, a call to a gateway)
await repository.Received(1).SaveAsync(Arg.Is<Order>(o => o.Status == OrderStatus.Confirmed), Arg.Any<CancellationToken>());
await notifier.DidNotReceive().NotifyAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
```

Prefer asserting on returned values and state over interactions; verify interactions only for side effects that have
no observable result in the unit.

## Structure

AAA with comments, one Act, one behaviour per test, `[Theory]` for data variations (`InlineData` for primitives and
enums, `MemberData`/`TheoryData<...>` for decimals, dates or objects — attribute arguments cannot be `decimal`).

Exceptions keep the three sections — the Act captures the call as a delegate and the Assert awaits it:

```csharp
// Act
var act = () => sut.ConfirmAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

// Assert
var exception = await Assert.ThrowsAsync<OrderNotFoundException>(act);
Assert.Contains("not found", exception.Message);
```

`Assert.ThrowsAsync<T>` requires the exact type; use `Assert.ThrowsAnyAsync<T>` to accept derived exceptions.

## Location and naming

Folders mirror the production project (`Orders/OrderPricingService.cs` → `Orders/OrderPricingServiceTests.cs`), one
test class per production class, names `Method_Scenario_ExpectedResult` unless the repository uses another convention.
