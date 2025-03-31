using System.Net;
using System.Net.Http.Json;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Host.Routes.Todo.Update;
using CleanWebApiTemplate.Testing.Common;
using CleanWebApiTemplate.Testing.Common.Attributes;
using CleanWebApiTemplate.Testing.Configuration;
using CleanWebApiTemplate.Testing.Extension;

namespace CleanWebApiTemplate.Testing.FunctionalTests.API.Todo;

[Collection(nameof(TestServerFixtureCollection))]
public class Put(TestServerFixture fixture)
{
    private readonly TestServerFixture Fixture = fixture;

    [Fact]
    [ResetDatabase]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task UpdateTodo_Should_ReturnTodoUpdated_Ok()
    {
        // Arrange
        DateTime actualUtcMoment = DateTime.UtcNow;
        var defaultTodo = await Fixture.AddDefaultTodo(title: "defaultTitle", description: "defaultDescription", status: (int)ETodoStatus.Pending);
        UpdateTodoRequest request = new() { Title = "updatedTitle", Description = "updatedDescription", Status = (int)ETodoStatus.InProgress };

        // Act
        var response = await Fixture.HttpClient.PutAsync(ApiRoutes.Todo.Update(defaultTodo.Id.ToString()), request);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseModel = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.Equal(request.Title, responseModel.Title);
        Assert.Equal(request.Description, responseModel.Description);
        Assert.Equal(request.Status, responseModel.Status);
        var todoDb = await Fixture.GetTodo(defaultTodo.Id);
        Assert.NotNull(todoDb);
        Assert.InRange(todoDb.UpdatedAt, actualUtcMoment.AddSeconds(-2), actualUtcMoment.AddSeconds(2));
        Assert.Equal(responseModel.Status, todoDb.Status);
        Assert.Equal(request.Title, todoDb.Title);
        Assert.Equal(request.Description, todoDb.Description);
        Assert.Equal(TestAuthHandler.UserEmail, todoDb.UpdatedBy);
        Assert.Equal(defaultTodo.CreatedBy, todoDb.CreatedBy);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task UpdateTodo_EmptyId_Should_Return_BadRequest()
    {
        // Arrange
        UpdateTodoRequest request = new() { Title = "updatedTitle", Description = "updatedDescription", Status = (int)ETodoStatus.InProgress };

        // Act
        var response = await Fixture.HttpClient.PutAsync(ApiRoutes.Todo.Update(string.Empty), request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }


    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task UpdateTodo_InvalidId_Should_Return_BadRequest()
    {
        // Arrange
        string wrongId = "wrong-id";
        UpdateTodoRequest request = new() { Title = "updatedTitle", Description = "updatedDescription", Status = (int)ETodoStatus.InProgress };

        // Act
        var response = await Fixture.HttpClient.PutAsync(ApiRoutes.Todo.Update(wrongId), request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Fact]
    [ResetDatabase]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task UpdateTodo_SameTitle_Should_ReturnTodoUpdated_Ok()
    {
        // Arrange
        DateTime actualUtcMoment = DateTime.UtcNow;
        var defaultTodo = await Fixture.AddDefaultTodo(title: "defaultTitle", description: "defaultDescription", status: (int)ETodoStatus.Pending);
        UpdateTodoRequest request = new() { Title = defaultTodo.Title, Description = "updatedDescription", Status = (int)ETodoStatus.InProgress };

        // Act
        var response = await Fixture.HttpClient.PutAsync(ApiRoutes.Todo.Update(defaultTodo.Id.ToString()), request);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseModel = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.Equal(request.Title, responseModel.Title);
        Assert.Equal(request.Description, responseModel.Description);
        Assert.Equal(request.Status, responseModel.Status);
        var todoDb = await Fixture.GetTodo(defaultTodo.Id);
        Assert.NotNull(todoDb);
        Assert.InRange(todoDb.UpdatedAt, actualUtcMoment.AddSeconds(-2), actualUtcMoment.AddSeconds(2));
        Assert.Equal(responseModel.Status, todoDb.Status);
        Assert.Equal(request.Title, todoDb.Title);
        Assert.Equal(request.Description, todoDb.Description);
        Assert.Equal(TestAuthHandler.UserEmail, todoDb.UpdatedBy);
        Assert.Equal(defaultTodo.CreatedBy, todoDb.CreatedBy);
    }

    [Fact]
    [ResetDatabase]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task UpdateTodo_ExistingTitle_Should_Return_BadRequest()
    {
        // Arrange
        var defaultTodo = await Fixture.AddDefaultTodo(title: "defaultTitle", description: "defaultDescription", status: (int)ETodoStatus.Pending);
        UpdateTodoRequest request = new() { Title = "updatedTitle", Description = "updatedDescription", Status = (int)ETodoStatus.InProgress };
        var existingTitle = await Fixture.AddDefaultTodo(title: request.Title);

        // Act
        var response = await Fixture.HttpClient.PutAsync(ApiRoutes.Todo.Update(defaultTodo.Id.ToString()), request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
