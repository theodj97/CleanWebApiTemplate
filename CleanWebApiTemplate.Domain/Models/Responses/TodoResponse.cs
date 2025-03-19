namespace CleanWebApiTemplate.Domain.Models.Responses;

public struct TodoResponse
{
    public required Ulid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte Status { get; set; }
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
}
