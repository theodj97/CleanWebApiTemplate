using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace CleanWebApiTemplate.Secrets;

public static class OpenBaoConfigurationExtensions
{
    /// <summary>
    /// Adds the secret at <c>OpenBao:Path</c> as a configuration source. Without <c>OpenBao:Address</c> it is a no-op,
    /// so the application still starts where OpenBao is not configured (the unit and component test modes, for example).
    /// Sources added after this one win: the caller decides the precedence by where it calls this method.
    /// </summary>
    /// <param name="settings">Configuration read for the <c>OpenBao:*</c> keys. Pass the host's configuration.</param>
    /// <exception cref="InvalidOperationException">
    /// The address is set but the token, the path or the timeout is missing or invalid.
    /// </exception>
    public static IConfigurationBuilder AddOpenBaoSecrets(this IConfigurationBuilder builder, IConfiguration settings)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(settings);

        var address = settings[OpenBaoConfigurationKeys.Address];
        if (string.IsNullOrWhiteSpace(address)) return builder;

        var token = settings[OpenBaoConfigurationKeys.Token];
        if (string.IsNullOrWhiteSpace(token)) throw Missing(OpenBaoConfigurationKeys.Token);

        var secretPath = settings[OpenBaoConfigurationKeys.Path];
        if (string.IsNullOrWhiteSpace(secretPath)) throw Missing(OpenBaoConfigurationKeys.Path);

        var mountPath = settings[OpenBaoConfigurationKeys.Mount];
        var timeout = ReadTimeout(settings[OpenBaoConfigurationKeys.TimeoutSeconds]);

        builder.Add(new OpenBaoConfigurationSource
        {
            Options = new OpenBaoOptions
            {
                Address = address,
                Token = token,
                MountPath = string.IsNullOrWhiteSpace(mountPath) ? OpenBaoConfigurationKeys.DefaultMount : mountPath,
            },
            SecretPath = secretPath,
            Timeout = timeout,
        });

        return builder;
    }

    private static TimeSpan ReadTimeout(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return TimeSpan.FromSeconds(OpenBaoConfigurationKeys.DefaultTimeoutSeconds);

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) is false || seconds <= 0)
            throw new InvalidOperationException($"'{OpenBaoConfigurationKeys.TimeoutSeconds}' must be a positive integer.");

        return TimeSpan.FromSeconds(seconds);
    }

    private static InvalidOperationException Missing(string key) =>
        new($"'{key}' is required when '{OpenBaoConfigurationKeys.Address}' is set.");
}
