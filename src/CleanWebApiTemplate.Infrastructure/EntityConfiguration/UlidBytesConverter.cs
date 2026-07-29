namespace CleanWebApiTemplate.Infrastructure.EntityConfiguration;

/// <summary>
/// Helper for the Ulid &lt;-&gt; byte[] EF Core value conversion.
/// The implicit byte[] -&gt; ReadOnlySpan&lt;byte&gt; conversion done inside <see cref="FromBytes"/>
/// cannot be expressed in the expression tree itself: it breaks the EF Core compiled model
/// code generation ('dotnet ef dbcontext optimize') used for Native AOT.
/// </summary>
internal static class UlidBytesConverter
{
    public static Ulid FromBytes(byte[] bytes) => new(bytes);
}
