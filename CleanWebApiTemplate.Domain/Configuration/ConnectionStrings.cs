// using MongoDB.Driver;

namespace CleanWebApiTemplate.Domain.Configuration;

public record ConnectionStrings
{
    public const string SECTION_NAME = "ConnectionStrings";
    public required string SqlServer { get; set; }
    // public required MongoUrl MongoDb { get; set; }
}
