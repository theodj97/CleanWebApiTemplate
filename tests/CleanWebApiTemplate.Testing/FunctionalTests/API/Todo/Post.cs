using System.Net;
using System.Net.Http.Json;
using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Host.ResponseModels.Todo;
using CleanWebApiTemplate.Host.Routes.Todo.Create;
using CleanWebApiTemplate.Infrastructure.EntityConfiguration;
using CleanWebApiTemplate.Testing.Common;
using CleanWebApiTemplate.Testing.Common.Attributes;
using CleanWebApiTemplate.Testing.Configuration;
using CleanWebApiTemplate.Testing.Extension;

namespace CleanWebApiTemplate.Testing.FunctionalTests.API.Todo;

[Collection(nameof(TestServerFixtureCollection))]
public class Post(TestServerFixture fixture)
{
    private readonly TestServerFixture Fixture = fixture;

    [Fact]
    [ResetDatabase]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task CreateTodo_Should_Return_Created()
    {
        // Arrange
        DateTime actualUtcMoment = DateTime.UtcNow;
        CreateTodoRequest request = new() { Title = "todoTitle", Description = "todoDescription" };

        // Act
        var response = await Fixture.HttpClient.PostAsync(ApiRoutes.Todo.Create(), request);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseModel = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.Equal(request.Title, responseModel.Title);
        Assert.Equal(request.Description, responseModel.Description);
        Assert.Equal((int)ETodoStatus.Pending, responseModel.Status);
        Assert.True(Math.Abs((responseModel.CreatedAt - actualUtcMoment).TotalSeconds) <= 10);
        Assert.Equal(TestAuthHandler.UserEmail, responseModel.CreatedBy);
        Assert.Equal(TestAuthHandler.UserEmail, responseModel.UpdatedBy);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task CreateTodo_WithOutTitle_Should_Return_BadRequest()
    {
        // Arrange
        CreateTodoRequest request = new() { Title = string.Empty };

        // Act
        var response = await Fixture.HttpClient.PostAsync(ApiRoutes.Todo.Create(), request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [IdentityAsignation(userEmail: "wrongEmail")]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task CreateTodo_WithWrongEmail_Should_Return_BadRequest()
    {
        // Arrange
        CreateTodoRequest request = new() { Title = string.Empty };

        // Act
        var response = await Fixture.HttpClient.PostAsync(ApiRoutes.Todo.Create(), request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task CreateTodo_ExistingTitle_Should_Return_BadRequest()
    {
        // Arrange
        var defaultTodo = await Fixture.AddDefaultTodo();
        CreateTodoRequest request = new() { Title = defaultTodo.Title };

        // Act
        var response = await Fixture.HttpClient.PostAsync(ApiRoutes.Todo.Create(), request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task CreateTodo_TooLongTitle_Should_Return_BadRequest()
    {
        // Arrange
        CreateTodoRequest request = new() { Title = TestServerFixtureExtension.GenerateRandomString(TodoEntityConfiguration.TitleLenght + 1) };

        // Act
        var response = await Fixture.HttpClient.PostAsync(ApiRoutes.Todo.Create(), request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task CreateTodo_TooLongoDescription_Should_Return_BadRequest()
    {
        // Arrange
        CreateTodoRequest request = new() { Title = "testTitle", Description = TestServerFixtureExtension.GenerateRandomString(TodoEntityConfiguration.DescriptionLenght + 1) };

        // Act
        var response = await Fixture.HttpClient.PostAsync(ApiRoutes.Todo.Create(), request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [IdentityAsignation(role: Constants.EXTERNAL_POLICY)]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task CreateTodo_ExternalUser_Should_Return_Forbidden()
    {
        // Arrange
        CreateTodoRequest request = new() { Title = "testTitle" };

        // Act
        var response = await Fixture.HttpClient.PostAsync(ApiRoutes.Todo.Create(), request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

}
