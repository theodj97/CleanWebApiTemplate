using System.Net;
using System.Net.Http.Json;
using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Host.Routes.Api.Todo.Update;
using CleanWebApiTemplate.Host.Routes.ResponseModels.Todo;
using CleanWebApiTemplate.Infrastructure.EntityConfiguration;
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
    public async Task UpdateTodo_EmptyId_Should_Return_MethodNotAllowed()
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
        await Fixture.AddDefaultTodo(title: request.Title);

        // Act
        var response = await Fixture.HttpClient.PutAsync(ApiRoutes.Todo.Update(defaultTodo.Id.ToString()), request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task UpdateTodo_TooLongTitle_Should_Return_BadRequest()
    {
        // Arrange
        var defaultTodo = await Fixture.AddDefaultTodo(title: "defaultTitle", description: "defaultDescription", status: (int)ETodoStatus.Pending);
        UpdateTodoRequest request = new() { Title = TestServerFixtureExtension.GenerateRandomString(TodoEntityConfiguration.TitleLenght + 1), Description = "updatedDescription", Status = (int)ETodoStatus.InProgress };

        // Act
        var response = await Fixture.HttpClient.PutAsync(ApiRoutes.Todo.Update(defaultTodo.Id.ToString()), request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task UpdateTodo_TooLongDescription_Should_Return_BadRequest()
    {
        // Arrange
        var defaultTodo = await Fixture.AddDefaultTodo(title: "defaultTitle", description: "defaultDescription", status: (int)ETodoStatus.Pending);
        UpdateTodoRequest request = new() { Title = "updatedTitle", Description = TestServerFixtureExtension.GenerateRandomString(TodoEntityConfiguration.DescriptionLenght + 1), Status = (int)ETodoStatus.InProgress };

        // Act
        var response = await Fixture.HttpClient.PutAsync(ApiRoutes.Todo.Update(defaultTodo.Id.ToString()), request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [IdentityAsignation(userEmail: "wrongEmail")]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task UpdateTodo_WrongEmail_Should_Return_BadRequest()
    {
        // Arrange
        var defaultTodo = await Fixture.AddDefaultTodo(title: "defaultTitle", description: "defaultDescription", status: (int)ETodoStatus.Pending);
        UpdateTodoRequest request = new() { Title = "updatedTitle", Status = (int)ETodoStatus.InProgress };

        // Act
        var response = await Fixture.HttpClient.PutAsync(ApiRoutes.Todo.Update(defaultTodo.Id.ToString()), request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task UpdateTodo_WrongStatus_Should_Return_BadRequest()
    {
        // Arrange
        var defaultTodo = await Fixture.AddDefaultTodo(title: "defaultTitle", description: "defaultDescription", status: (int)ETodoStatus.Pending);
        UpdateTodoRequest request = new() { Title = "updatedTitle", Description = TestServerFixtureExtension.GenerateRandomString(TodoEntityConfiguration.DescriptionLenght + 1), Status = Enum.GetValues<ETodoStatus>().Length + 1 };

        // Act
        var response = await Fixture.HttpClient.PutAsync(ApiRoutes.Todo.Update(defaultTodo.Id.ToString()), request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task UpdateTodo_TodoNotFound_Should_Return_NotFound()
    {
        // Arrange
        var defaultTodo = await Fixture.AddDefaultTodo(title: "defaultTitle", description: "defaultDescription", status: (int)ETodoStatus.Pending);
        UpdateTodoRequest request = new() { Title = "updatedTitle", Description = "updatedDescription", Status = (int)ETodoStatus.InProgress };

        // Act
        var response = await Fixture.HttpClient.PutAsync(ApiRoutes.Todo.Update(Ulid.NewUlid().ToString()), request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task UpdateTodo__AnyUpdate_Should_Return_Ok()
    {
        // Arrange
        DateTime actualUtcMoment = DateTime.UtcNow;
        var defaultTodo = await Fixture.AddDefaultTodo(title: "defaultTitle", description: "defaultDescription", status: (int)ETodoStatus.Pending);
        UpdateTodoRequest request = new() { Title = defaultTodo.Title, Description = defaultTodo.Description, Status = defaultTodo.Status };

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
    [IdentityAsignation(role: Constants.EXTERNAL_POLICY)]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task UpdateTodo_ExternalUser_Should_Return_Forbidden()
    {
        // Arrange
        var defaultTodo = await Fixture.AddDefaultTodo(title: "defaultTitle", description: "defaultDescription", status: (int)ETodoStatus.Pending);
        var request = new UpdateTodoRequest { Title = "testTitle", Description = "testDescription" };

        // Act
        var response = await Fixture.HttpClient.PutAsync(ApiRoutes.Todo.Update(defaultTodo.Id.ToString()), request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
