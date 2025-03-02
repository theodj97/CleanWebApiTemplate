using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CleanWebApiTemplate.Domain.Models;

public abstract class BaseCollection
{
    [BsonId]
    public required ObjectId Id { get; set; } = ObjectId.GenerateNewId();
}
