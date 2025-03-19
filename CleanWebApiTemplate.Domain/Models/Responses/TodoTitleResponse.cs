namespace CleanWebApiTemplate.Domain.Models.Responses;

public struct TodoTitleResponse
{
    public Ulid Id { get; set; }
    public string Title { get; set; }
}
