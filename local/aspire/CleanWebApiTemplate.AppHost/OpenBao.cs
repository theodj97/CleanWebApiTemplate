using static CleanWebApiTemplate.AppHost.AppHostContract;

namespace CleanWebApiTemplate.AppHost;

/// <summary>
/// Local OpenBao: an ephemeral secrets server (dev mode, in memory, no volume) plus the one-shot job that writes the
/// application's secrets into it before the application starts. In deployed environments the server is external: only
/// the <c>OpenBao:*</c> configuration changes, never the application code.
/// </summary>
internal static class OpenBaoResources
{
    private const string Image = "openbao/openbao";
    private const string ImageTag = "2.7.2";
    private const int Port = 8200;

    /// <summary>Starts the server unsealed in dev mode. Every start begins empty, like the PostgreSQL container.</summary>
    public static IResourceBuilder<ContainerResource> AddOpenBao(
        this IDistributedApplicationBuilder builder, string rootToken) =>
        builder.AddContainer(Resources.OpenBao, Image, ImageTag)
               .WithArgs("server", "-dev", $"-dev-listen-address=0.0.0.0:{Port}", $"-dev-root-token-id={rootToken}")
               .WithHttpEndpoint(targetPort: Port, name: "http")
               // Unsealed in dev mode: 200 means the store is ready to accept the seed.
               .WithHttpHealthCheck("/v1/sys/health", endpointName: "http");

    /// <summary>
    /// Seeds OpenBao with <see cref="Secrets"/> before the applications start. The signing key reaches this job through
    /// the AppHost parameter; the application reads it from OpenBao, never from its own environment.
    /// </summary>
    public static IResourceBuilder<ProjectResource> AddOpenBaoInitializer(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ContainerResource> openBao,
        string rootToken,
        string secretPath,
        LocalJwt localJwt)
    {
        var signingKey = localJwt.SigningKey
                         ?? throw new InvalidOperationException(
                             $"Seeding OpenBao needs the HS256 signing key; LocalAuth:Algorithm is '{localJwt.Algorithm}'.");

        return builder.AddProject<Projects.CleanWebApiTemplate_OpenBaoInitializer>(Resources.OpenBaoInitializer)
                      .WithEnvironment(ToEnvironmentVariable(OpenBao.AddressKey), openBao.GetEndpoint("http"))
                      .WithEnvironment(ToEnvironmentVariable(OpenBao.TokenKey), rootToken)
                      .WithEnvironment(ToEnvironmentVariable(OpenBao.MountKey), OpenBao.Mount)
                      .WithEnvironment(ToEnvironmentVariable(OpenBao.PathKey), secretPath)
                      .WithEnvironment(AppSettings.ApiKey, signingKey)
                      .WaitFor(openBao);
    }

    /// <summary>Tells the application where OpenBao is and how to authenticate. Never the secret values themselves.</summary>
    public static IResourceBuilder<ProjectResource> WithOpenBaoSecrets(
        this IResourceBuilder<ProjectResource> application,
        IResourceBuilder<ContainerResource> openBao,
        string rootToken,
        string secretPath) =>
        application
            .WithEnvironment(ToEnvironmentVariable(OpenBao.AddressKey), openBao.GetEndpoint("http"))
            .WithEnvironment(ToEnvironmentVariable(OpenBao.TokenKey), rootToken)
            .WithEnvironment(ToEnvironmentVariable(OpenBao.MountKey), OpenBao.Mount)
            .WithEnvironment(ToEnvironmentVariable(OpenBao.PathKey), secretPath);
}
