namespace CleanWebApiTemplate.Infrastructure.Secrets;

/// <summary>The OpenBao server answered with an error or with a payload this client does not understand.</summary>
public sealed class OpenBaoException(string message) : Exception(message);
