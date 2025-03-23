using System.Net;
using System.Net.Http.Json;
using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Testing.Common;
using CleanWebApiTemplate.Testing.Common.Attributes;

namespace CleanWebApiTemplate.Testing.FunctionalTests.API.Todo;

[Collection(nameof(TestServerFixtureCollection))]
public class Get(TestServerFixture fixture)
{
    private readonly TestServerFixture Fixture = fixture;

    [Fact]
    [ResetDatabase]
    [RoleAsignationAttribute(Constants.USER_POLICY)]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task GetTodoById_Should_ReturnTodo_Ok()
    {
        // Arrange
        var defaultTodo = await Fixture.AddDefaultTodo();

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.GetById(defaultTodo.Id.ToString()));

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseModel = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.Equal(defaultTodo.Title, responseModel.Title);
        Assert.Equal(defaultTodo.Description, responseModel.Description);
        Assert.Equal(defaultTodo.Status, responseModel.Status);
        Assert.Equal(defaultTodo.CreatedAt, responseModel.CreatedAt);
        Assert.Equal(defaultTodo.CreatedBy, responseModel.CreatedBy);
        Assert.Equal(defaultTodo.UpdatedBy, responseModel.UpdatedBy);
        Assert.Equal(defaultTodo.Id, responseModel.Id);
    }
}
