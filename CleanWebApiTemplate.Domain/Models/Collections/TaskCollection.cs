using MongoDB.Bson.Serialization.Attributes;

namespace CleanWebApiTemplate.Domain.Models.Collections;

public class TaskCollection : BaseCollection
{
    [BsonElement("title")]
    public required string Title { get; set; }
    [BsonElement("description")]
    public string? Description { get; set; }
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
    [BsonElement("status")]
    public int Status { get; set; }
    [BsonElement("createdBy")]
    public required string CreatedBy { get; set; }
}
