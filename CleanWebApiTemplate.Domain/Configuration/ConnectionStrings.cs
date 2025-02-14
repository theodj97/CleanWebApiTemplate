namespace CleanWebApiTemplate.Domain.Configuration;

public record ConnectionStrings
{
    public const string SECTION_NAME = "ConnectionStrings";
    public required string SqlServer { get; set; }

    //#if IsSQLServer
    //    public required string SqlServer { get; set; }
    //#elif IsMongoDB
    //    public required string MongoDb { get; set; }
    //#endif
}
