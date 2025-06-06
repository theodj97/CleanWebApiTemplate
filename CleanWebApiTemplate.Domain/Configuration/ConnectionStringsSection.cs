// using MongoDB.Driver;

namespace CleanWebApiTemplate.Domain.Configuration;

public sealed class ConnectionStringsSection
{
    public static readonly string SectionName = nameof(ConnectionStringsSection).EndsWith(AppSettings.SECTION_EXTENSION)
    ? nameof(ConnectionStringsSection)[..^AppSettings.SECTION_EXTENSION.Length]
    : throw new ArgumentException($"Section name {nameof(ConnectionStringsSection)} doesn't match syntax finishing in {AppSettings.SECTION_EXTENSION}");
    public required string SqlServer { get; init; }
    // public required MongoUrl MongoDb { get; set; }
}
