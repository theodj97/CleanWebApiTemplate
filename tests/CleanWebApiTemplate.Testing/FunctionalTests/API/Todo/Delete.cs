using System.Net;
using System.Net.Http.Json;
using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Testing.Common;
using CleanWebApiTemplate.Testing.Common.Attributes;

namespace CleanWebApiTemplate.Testing.FunctionalTests.API.Todo;

[Collection(nameof(TestServerFixtureCollection))]
public class Delete(TestServerFixture fixture)
{
    private readonly TestServerFixture Fixture = fixture;

    [Fact]
    [ResetDatabase]
    [IdentityAsignation(role: Constants.OPERATOR_POLICY)]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task DeleteTodo_Should_ReturnTrue_Ok()
    {
        // Arrange
        var defaultTodo = await Fixture.AddDefaultTodo(title: "defaultTitle", description: "defaultDescription", status: (int)ETodoStatus.Pending);

        // Act
        var response = await Fixture.HttpClient.DeleteAsync(ApiRoutes.Todo.Delete(defaultTodo.Id.ToString()));

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseModel = await response.Content.ReadFromJsonAsync<bool>();
        Assert.True(responseModel);
        var todoDb = await Fixture.GetTodo(defaultTodo.Id);
        Assert.Null(todoDb);
    }

    [Fact]
    [ResetDatabase]
    [IdentityAsignation(role: Constants.OPERATOR_POLICY)]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task DeleteNonExistingTodo_Should_ReturnFalse_Ok()
    {
        // Arrange

        // Act
        var response = await Fixture.HttpClient.DeleteAsync(ApiRoutes.Todo.Delete(Ulid.NewUlid().ToString()));

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseModel = await response.Content.ReadFromJsonAsync<bool>();
        Assert.False(responseModel);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task DeleteTodo_EmptyId_Should_Return_MethodNotAllowed()
    {
        // Arrange

        // Act
        var response = await Fixture.HttpClient.DeleteAsync(ApiRoutes.Todo.Delete(string.Empty));

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    [IdentityAsignation(role: Constants.OPERATOR_POLICY)]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task DeleteTodo_InvalidId_Should_Return_BadRequest()
    {
        // Arrange
        string wrongId = "wrong-id";

        // Act
        var response = await Fixture.HttpClient.DeleteAsync(ApiRoutes.Todo.Delete(wrongId));

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [IdentityAsignation(role: Constants.USER_POLICY)]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task DeleteTodo_UserPolicy_Should_Return_Forbidden()
    {
        // Arrange

        // Act
        var response = await Fixture.HttpClient.DeleteAsync(ApiRoutes.Todo.Delete(Ulid.NewUlid().ToString()));

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

}
