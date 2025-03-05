namespace CleanWebApiTemplate.Host.Routes.Todo.Filter;

public record FilteredTodoRequest
{
    public IEnumerable<string>? Ids { get; set; }
    public IEnumerable<string>? Title { get; set; }
    public IEnumerable<byte>? Status { get; set; }
    public IEnumerable<string>? CreatedBy { get; set; }
}
