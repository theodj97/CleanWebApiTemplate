namespace CleanWebApiTemplate.Host.Routes.Todo.Filter;

public record FilteredTodoRequest
{
    public IEnumerable<string>? Title { get; set; }
    public IEnumerable<int>? Status { get; set; }
    public IEnumerable<string>? CreatedBy { get; set; }
}
