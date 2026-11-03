namespace CleanWebApiTemplate.Tests.Shared.Attributes;

/// <summary>
/// Forces an anonymous <c>Client</c> for this test, overriding an <c>[AuthenticatedAs]</c> on the class.
/// Typical use: the 401 case inside a class whose other tests all run as the same user.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class UnauthenticatedAttribute : Attribute;
