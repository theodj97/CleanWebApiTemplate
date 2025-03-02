using MongoDB.Bson.Serialization.Attributes;

namespace CleanWebApiTemplate.Domain.Models.Collections;

public class TaskCollection : BaseCollection
{
    [BsonElement("title")]
    public required string Title { get; set; }
    [BsonElement("description")]
    public string? Description { get; set; }
    [BsonElement("createdAt")]
    public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("updatedAt")]
    public required DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("status")]
    public required int Status { get; set; } = 1;
    [BsonElement("createdBy")]
    public required string CreatedBy { get; set; }
}
