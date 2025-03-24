using System.Net;
using System.Net.Http.Json;
using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Domain.Models.Enums;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Host.Routes.Todo.Filter;
using CleanWebApiTemplate.Testing.Common;
using CleanWebApiTemplate.Testing.Common.Attributes;

namespace CleanWebApiTemplate.Testing.FunctionalTests.API.Todo;

[Collection(nameof(TestServerFixtureCollection))]
public class Get(TestServerFixture fixture)
{
    private readonly TestServerFixture Fixture = fixture;

    [Fact]
    [ResetDatabase]
    [RoleAsignation(Constants.USER_POLICY)]
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

    [Fact]
    [ResetDatabase]
    [RoleAsignation(Constants.USER_POLICY)]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task GetTodoById_Should_ReturnNotFound()
    {
        // Arrange

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.GetById(Ulid.NewUlid().ToString()));

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [RoleAsignation(Constants.USER_POLICY)]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task GetTodoById_WrongId_Should_ReturnBadRequest()
    {
        // Arrange
        string wrongId = "wrong-id";

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.GetById(wrongId));

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [RoleAsignation(Constants.EXTERNAL_POLICY)]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task GetTodoById_ExternalUser_Should_ReturnForbidden()
    {
        // Arrange

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.GetById(Ulid.NewUlid().ToString()));

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [RoleAsignation(Constants.USER_POLICY)]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task FilteredTodo_FilteredAllParams_Should_ReturnFilteredTodos_Ok()
    {
        // Arrange
        DateTime utcNow = DateTime.UtcNow;
        DateTime dayOne = utcNow.AddDays(-2);
        DateTime dayTwo = utcNow.AddDays(-1);
        DateTime dayThree = utcNow;

        var firstTodo = await Fixture.AddDefaultTodo(title: "firstTitle",
                                                     createdAt: dayOne,
                                                     status: (int)TodoStatusEnum.Pending,
                                                     createdBy: "user1@test.tst");

        var secondTodo = await Fixture.AddDefaultTodo(title: "secondTitle",
                                                      createdAt: dayTwo,
                                                      status: (int)TodoStatusEnum.Completed,
                                                      createdBy: "user2@test.tst");

        var thirdTodo = await Fixture.AddDefaultTodo(title: "thirdTitle",
                                                     createdAt: dayThree,
                                                     status: (int)TodoStatusEnum.InProgress,
                                                     createdBy: "user1@test.tst");


        FilteredTodoRequest request = new()
        {
            Title = [firstTodo.Title, secondTodo.Title],
            CreatedBy = [firstTodo.CreatedBy, secondTodo.CreatedBy],
            Status = [firstTodo.Status, secondTodo.Status],
            Ids = [firstTodo.Id.ToString(), secondTodo.Id.ToString()],
            StartDate = dayOne.ToString(),
            EndDate = dayTwo.AddSeconds(1).ToString()
        };

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.Filtered(request));

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseModel = await response.Content.ReadFromJsonAsync<IEnumerable<TodoResponse>>();
        Assert.NotNull(responseModel);
        Assert.True(responseModel.Count() is 2);
        var firstTodoResponse = responseModel.First(x => x.Id == firstTodo.Id);
        var secondTodoResponse = responseModel.First(x => x.Id == secondTodo.Id);
        Assert.Equal(firstTodoResponse.Id.ToString(), firstTodo.Id.ToString());
        Assert.Equal(secondTodoResponse.Id.ToString(), secondTodo.Id.ToString());
        Assert.Equal(firstTodoResponse.Title, firstTodo.Title);
        Assert.Equal(secondTodoResponse.Title, secondTodo.Title);
        Assert.Equal(firstTodoResponse.Description, firstTodo.Description);
        Assert.Equal(secondTodoResponse.Description, secondTodo.Description);
        Assert.Equal(firstTodoResponse.CreatedBy, firstTodo.CreatedBy);
        Assert.Equal(secondTodoResponse.CreatedBy, secondTodo.CreatedBy);
        Assert.Equal(firstTodoResponse.UpdatedBy, firstTodo.UpdatedBy);
        Assert.Equal(secondTodoResponse.UpdatedBy, secondTodo.UpdatedBy);
        Assert.Equal(firstTodoResponse.CreatedAt, firstTodo.CreatedAt);
        Assert.Equal(secondTodoResponse.CreatedAt, secondTodo.CreatedAt);
        Assert.Equal(firstTodoResponse.Status, firstTodo.Status);
        Assert.Equal(secondTodoResponse.Status, secondTodo.Status);
    }

    [Fact]
    [ResetDatabase]
    [RoleAsignation(Constants.USER_POLICY)]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task FilteredTodo_FilteredAnyParams_Should_ReturnAllTodos_Ok()
    {
        // Arrange
        DateTime utcNow = DateTime.UtcNow;
        DateTime dayOne = utcNow.AddDays(-2);
        DateTime dayTwo = utcNow.AddDays(-1);
        DateTime dayThree = utcNow;

        var firstTodo = await Fixture.AddDefaultTodo(title: "firstTitle",
                                                     createdAt: dayOne,
                                                     status: (int)TodoStatusEnum.Pending,
                                                     createdBy: "user1@test.tst");

        var secondTodo = await Fixture.AddDefaultTodo(title: "secondTitle",
                                                      createdAt: dayTwo,
                                                      status: (int)TodoStatusEnum.Completed,
                                                      createdBy: "user2@test.tst");

        var thirdTodo = await Fixture.AddDefaultTodo(title: "thirdTitle",
                                                     createdAt: dayThree,
                                                     status: (int)TodoStatusEnum.InProgress,
                                                     createdBy: "user1@test.tst");


        FilteredTodoRequest request = new();

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.Filtered(request));

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseModel = await response.Content.ReadFromJsonAsync<IEnumerable<TodoResponse>>();
        Assert.NotNull(responseModel);
        Assert.True(responseModel.Count() is 3);
        var firstTodoResponse = responseModel.First(x => x.Id == firstTodo.Id);
        var secondTodoResponse = responseModel.First(x => x.Id == secondTodo.Id);
        var thirdTodoResponse = responseModel.First(x => x.Id == thirdTodo.Id);
        Assert.Equal(firstTodoResponse.Id.ToString(), firstTodo.Id.ToString());
        Assert.Equal(secondTodoResponse.Id.ToString(), secondTodo.Id.ToString());
        Assert.Equal(thirdTodoResponse.Id.ToString(), thirdTodo.Id.ToString());
        Assert.Equal(firstTodoResponse.Title, firstTodo.Title);
        Assert.Equal(secondTodoResponse.Title, secondTodo.Title);
        Assert.Equal(thirdTodoResponse.Title, thirdTodo.Title);
        Assert.Equal(firstTodoResponse.Description, firstTodo.Description);
        Assert.Equal(secondTodoResponse.Description, secondTodo.Description);
        Assert.Equal(thirdTodoResponse.Description, thirdTodo.Description);
        Assert.Equal(firstTodoResponse.CreatedBy, firstTodo.CreatedBy);
        Assert.Equal(secondTodoResponse.CreatedBy, secondTodo.CreatedBy);
        Assert.Equal(thirdTodoResponse.CreatedBy, thirdTodo.CreatedBy);
        Assert.Equal(firstTodoResponse.UpdatedBy, firstTodo.UpdatedBy);
        Assert.Equal(secondTodoResponse.UpdatedBy, secondTodo.UpdatedBy);
        Assert.Equal(thirdTodoResponse.UpdatedBy, thirdTodo.UpdatedBy);
        Assert.Equal(firstTodoResponse.CreatedAt, firstTodo.CreatedAt);
        Assert.Equal(secondTodoResponse.CreatedAt, secondTodo.CreatedAt);
        Assert.Equal(thirdTodoResponse.CreatedAt, thirdTodo.CreatedAt);
        Assert.Equal(firstTodoResponse.Status, firstTodo.Status);
        Assert.Equal(secondTodoResponse.Status, secondTodo.Status);
        Assert.Equal(thirdTodoResponse.Status, thirdTodo.Status);
    }

    [Fact]
    [ResetDatabase]
    [RoleAsignation(Constants.USER_POLICY)]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task FilteredTodo_WrongRequestId_Should_ReturnBadRequest()
    {
        // Arrange
        FilteredTodoRequest request = new() { Ids = ["wrongUlid"] };

        // Act
        var result = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.Filtered(request));

        // Assert
        Assert.False(result.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }
}
