using MongoDB.Bson.Serialization.Attributes;

namespace CleanWebApiTemplate.Domain.Models.Collections;

public class TodoCollection : BaseCollection
{
    [BsonElement("title")]
    public required string Title { get; set; }
    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
    [BsonElement("status")]
    public int Status { get; set; }
    [BsonElement("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;
    [BsonElement("updatedBy")]
    public string UpdatedBy { get; set; } = string.Empty;
}
