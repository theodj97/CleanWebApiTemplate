using System;
using System.Net;
using System.Net.Http.Json;
using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Domain.Models.Enums;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Host.Routes.Todo.Update;
using CleanWebApiTemplate.Testing.Common;
using CleanWebApiTemplate.Testing.Common.Attributes;
using CleanWebApiTemplate.Testing.Extension;

namespace CleanWebApiTemplate.Testing.FunctionalTests.API.Todo;

[Collection(nameof(TestServerFixtureCollection))]
public class Put(TestServerFixture fixture)
{
    private readonly TestServerFixture Fixture = fixture;

    [Fact]
    [ResetDatabase]
    [RoleAsignation(Constants.USER_POLICY)]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task UpdateTodo_Should_ReturnTodoUpdated_Ok()
    {
        // Arrange
        DateTime actualUtcMoment = DateTime.UtcNow;
        var defaultTodo = await Fixture.AddDefaultTodo(title: "defaultTitle", description: "defaultDescription", status: (int)TodoStatusEnum.Pending);
        UpdateTodoRequest request = new() { Title = "updatedTitle", Description = "updatedDescription", Status = (int)TodoStatusEnum.InProgress };

        // Act
        var response = await Fixture.HttpClient.PutAsync(ApiRoutes.Todo.Update(defaultTodo.Id.ToString()), request);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseModel = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.Equal(request.Title, responseModel.Title);
        Assert.Equal(request.Description, responseModel.Description);

    }
}
