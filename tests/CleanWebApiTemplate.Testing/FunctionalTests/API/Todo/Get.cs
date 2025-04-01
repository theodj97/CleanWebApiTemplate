using System.Net;
using System.Net.Http.Json;
using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Host.Routes.Todo.Filter;
using CleanWebApiTemplate.Host.Routes.Todo.Get;
using CleanWebApiTemplate.Infrastructure.EntityConfiguration;
using CleanWebApiTemplate.Testing.Common;
using CleanWebApiTemplate.Testing.Common.Attributes;

namespace CleanWebApiTemplate.Testing.FunctionalTests.API.Todo;

[Collection(nameof(TestServerFixtureCollection))]
public class Get(TestServerFixture fixture)
{
    private readonly TestServerFixture Fixture = fixture;

    [Fact]
    [ResetDatabase]
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
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task GetTodoById_EmptyResult_Should_Return_NoContent()
    {
        // Arrange

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.GetById(Ulid.NewUlid().ToString()));

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task GetTodoById_WrongId_Should_Return_BadRequest()
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
    [IdentityAsignation(role: Constants.EXTERNAL_POLICY)]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task GetTodoById_ExternalUser_Should_Return_Forbidden()
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
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task FilteredTodo_FilteredAllParams_Should_ReturnFilteredTodos_Ok()
    {
        // Arrange
        DateTime utcNow = DateTime.UtcNow;
        DateTime dayOne = utcNow.AddDays(-2);
        DateTime dayTwo = utcNow.AddDays(-1);
        DateTime dayThree = utcNow;

        var firstTodo = await Fixture.AddDefaultTodo(title: "BfirstTitle",
                                                     createdAt: dayOne,
                                                     status: (int)ETodoStatus.Pending,
                                                     createdBy: "user1@test.tst");

        var secondTodo = await Fixture.AddDefaultTodo(title: "AsecondTitle",
                                                      createdAt: dayTwo,
                                                      status: (int)ETodoStatus.Completed,
                                                      createdBy: "user2@test.tst");

        var thirdTodo = await Fixture.AddDefaultTodo(title: "CthirdTitle",
                                                     createdAt: dayThree,
                                                     status: (int)ETodoStatus.InProgress,
                                                     createdBy: "user1@test.tst");

        FilteredTodoRequest request = new()
        {
            Title = [firstTodo.Title, secondTodo.Title],
            CreatedBy = [firstTodo.CreatedBy, secondTodo.CreatedBy],
            Status = [firstTodo.Status, secondTodo.Status],
            Ids = [firstTodo.Id.ToString(), secondTodo.Id.ToString()],
            StartDate = dayOne.ToString(),
            EndDate = dayTwo.AddSeconds(1).ToString(),
            OrderBy = (byte?)ETodoOrderBy.CreatedAt,
            OrderDescending = false,
            PageNumber = 1,
            PageSize = 2
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
                                                     status: (int)ETodoStatus.Pending,
                                                     createdBy: "user1@test.tst");

        var secondTodo = await Fixture.AddDefaultTodo(title: "secondTitle",
                                                      createdAt: dayTwo,
                                                      status: (int)ETodoStatus.Completed,
                                                      createdBy: "user2@test.tst");

        var thirdTodo = await Fixture.AddDefaultTodo(title: "thirdTitle",
                                                     createdAt: dayThree,
                                                     status: (int)ETodoStatus.InProgress,
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
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task FilteredTodo_EmptyResponse_Should_Return_NoContent()
    {
        // Arrange
        FilteredTodoRequest request = new();

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.Filtered(request));

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task FilteredTodo_WrongRequestId_Should_Return_BadRequest()
    {
        // Arrange
        FilteredTodoRequest request = new() { Ids = ["wrongUlid"] };

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.Filtered(request));

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task FilteredTodo_TooLongTitle_Should_Return_BadRequest()
    {
        // Arrange
        FilteredTodoRequest request = new() { Title = [TestServerFixtureExtension.GenerateRandomString(TodoEntityConfiguration.TitleLenght + 1)] };

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.Filtered(request));

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task FilteredTodo_InvalidState_Should_Return_BadRequest()
    {
        // Arrange
        int invalidStatus = 0;
        for (int i = 1; i < 256; i++)
        {
            if (Enum.IsDefined(typeof(ETodoStatus), i) is false)
            {
                invalidStatus = i;
                break;
            }
        }
        FilteredTodoRequest request = new() { Status = [invalidStatus] };

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.Filtered(request));

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task FilteredTodo_InvalidEmailCreatedBy_Should_Return_BadRequest()
    {
        // Arrange
        string invalidUserEmail = "this is not an email";
        FilteredTodoRequest request = new() { CreatedBy = [invalidUserEmail] };

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.Filtered(request));

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task FilteredTodo_InvalidStartDate_Should_Return_BadRequest()
    {
        // Arrange
        string invalidStartDate = "this is not a date";
        FilteredTodoRequest request = new() { StartDate = invalidStartDate };

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.Filtered(request));

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task FilteredTodo_InvalidEndDate_Should_Return_BadRequest()
    {
        // Arrange
        string invalidEndDate = "this is not a date";
        FilteredTodoRequest request = new() { EndDate = invalidEndDate };

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.Filtered(request));

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task FilteredTodo_StartDateAfterEndDate_Should_Return_BadRequest()
    {
        // Arrange
        DateTime startDate = DateTime.UtcNow.AddDays(-1);
        DateTime endDate = DateTime.UtcNow.AddDays(-2);
        FilteredTodoRequest request = new() { StartDate = startDate.ToString(), EndDate = endDate.ToString() };

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.Filtered(request));

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task FilteredTodo_StartDateButNoEndDateInRequest_Should_Return_BadRequest()
    {
        // Arrange
        DateTime startDate = DateTime.UtcNow.AddDays(-1);
        FilteredTodoRequest request = new() { StartDate = startDate.ToString() };

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.Filtered(request));

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task FilteredTodo_NoStartDateButEndDateInRequest_Should_Return_BadRequest()
    {
        // Arrange
        DateTime endDate = DateTime.UtcNow.AddDays(1);
        FilteredTodoRequest request = new() { EndDate = endDate.ToString() };

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.Filtered(request));

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [IdentityAsignation(role: Constants.EXTERNAL_POLICY)]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task FilteredTodo_ExternalUser_Should_Return_Forbidden()
    {
        // Arrange
        FilteredTodoRequest request = new();

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.Filtered(request));

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    [ResetDatabase]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task GetTitles_NoOrder_Should_ReturnTitlesOrderById_Ok()
    {
        // Arrange
        var firstTodo = await Fixture.AddDefaultTodo(title: "BTitle");
        var secondTodo = await Fixture.AddDefaultTodo(title: "ATitle");
        var thirdTodo = await Fixture.AddDefaultTodo(title: "CTitle");
        var fourthTodo = await Fixture.AddDefaultTodo(title: "DTitle");
        GetTodoTitlesRequest firstRequest = new() { PageNumber = 1, PageSize = 3 };
        GetTodoTitlesRequest secondRequest = new() { PageNumber = 2, PageSize = 3 };

        // Act
        var firstRequestResponse = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.GetTitles(firstRequest));
        var secondRequestResponse = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.GetTitles(secondRequest));

        // Assert
        Assert.True(firstRequestResponse.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, firstRequestResponse.StatusCode);
        var firstResponseModel = await firstRequestResponse.Content.ReadFromJsonAsync<IEnumerable<TodoTitleResponse>>();
        Assert.NotNull(firstResponseModel);
        Assert.Equal(3, firstResponseModel.Count());

        Assert.True(secondRequestResponse.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, secondRequestResponse.StatusCode);
        var secondResponseModel = await secondRequestResponse.Content.ReadFromJsonAsync<IEnumerable<TodoTitleResponse>>();
        Assert.NotNull(secondResponseModel);
        Assert.Single(secondResponseModel);

        var firstTodoResponse = firstResponseModel.OrderBy(x => x.Id).ElementAt(0);
        Assert.Equal(firstTodo.Title, firstTodoResponse.Title);
        Assert.Equal(firstTodo.Id, firstTodoResponse.Id);

        var secondTodoResponse = firstResponseModel.OrderBy(x => x.Id).ElementAt(1);
        Assert.Equal(secondTodo.Title, secondTodoResponse.Title);
        Assert.Equal(secondTodo.Id, secondTodoResponse.Id);

        var thirdTodoResponse = firstResponseModel.OrderBy(x => x.Id).ElementAt(2);
        Assert.Equal(thirdTodo.Title, thirdTodoResponse.Title);
        Assert.Equal(thirdTodo.Id, thirdTodoResponse.Id);

        var fourthTodoResponse = secondResponseModel.First();
        Assert.Equal(fourthTodo.Title, fourthTodoResponse.Title);
        Assert.Equal(fourthTodo.Id, fourthTodoResponse.Id);
    }

    [Fact]
    [ResetDatabase]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task GetTitles_OrderByTitle_Should_ReturnTitles_Ok()
    {
        // Arrange
        var firstTodo = await Fixture.AddDefaultTodo(title: "BTitle");
        var secondTodo = await Fixture.AddDefaultTodo(title: "ATitle");
        var thirdTodo = await Fixture.AddDefaultTodo(title: "CTitle");
        var fourthTodo = await Fixture.AddDefaultTodo(title: "DTitle");
        GetTodoTitlesRequest firstRequest = new() { PageNumber = 1, PageSize = 3, OrderBy = (byte)ETodoOrderBy.Title, OrderDescending = true };
        GetTodoTitlesRequest secondRequest = new() { PageNumber = 2, PageSize = 3, OrderBy = (byte)ETodoOrderBy.Title, OrderDescending = true };

        // Act
        var firstRequestResponse = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.GetTitles(firstRequest));
        var secondRequestResponse = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.GetTitles(secondRequest));

        // Assert
        Assert.True(firstRequestResponse.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, firstRequestResponse.StatusCode);
        var firstResponseModel = await firstRequestResponse.Content.ReadFromJsonAsync<IEnumerable<TodoTitleResponse>>();
        Assert.NotNull(firstResponseModel);
        Assert.Equal(3, firstResponseModel.Count());

        Assert.True(secondRequestResponse.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, secondRequestResponse.StatusCode);
        var secondResponseModel = await secondRequestResponse.Content.ReadFromJsonAsync<IEnumerable<TodoTitleResponse>>();
        Assert.NotNull(secondResponseModel);
        Assert.Single(secondResponseModel);

        var firstTodoResponse = firstResponseModel.ElementAt(0);
        Assert.Equal(fourthTodo.Title, firstTodoResponse.Title);
        Assert.Equal(fourthTodo.Id, firstTodoResponse.Id);

        var secondTodoResponse = firstResponseModel.ElementAt(1);
        Assert.Equal(thirdTodo.Title, secondTodoResponse.Title);
        Assert.Equal(thirdTodo.Id, secondTodoResponse.Id);

        var thirdTodoResponse = firstResponseModel.ElementAt(2);
        Assert.Equal(firstTodo.Title, thirdTodoResponse.Title);
        Assert.Equal(firstTodo.Id, thirdTodoResponse.Id);

        var fourthTodoResponse = secondResponseModel.First();
        Assert.Equal(secondTodo.Title, fourthTodoResponse.Title);
        Assert.Equal(secondTodo.Id, fourthTodoResponse.Id);
    }

    [Fact]
    [ResetDatabase]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task GetTitles_OrderById_Should_ReturnTitles_Ok()
    {
        // Arrange
        var firstTodo = await Fixture.AddDefaultTodo(title: "BTitle");
        var secondTodo = await Fixture.AddDefaultTodo(title: "ATitle");
        var thirdTodo = await Fixture.AddDefaultTodo(title: "CTitle");
        var fourthTodo = await Fixture.AddDefaultTodo(title: "DTitle");
        GetTodoTitlesRequest firstRequest = new() { PageNumber = 1, PageSize = 3, OrderBy = (byte)ETodoOrderBy.Id };
        GetTodoTitlesRequest secondRequest = new() { PageNumber = 2, PageSize = 3, OrderBy = (byte)ETodoOrderBy.Id };

        // Act
        var firstRequestResponse = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.GetTitles(firstRequest));
        var secondRequestResponse = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.GetTitles(secondRequest));

        // Assert
        Assert.True(firstRequestResponse.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, firstRequestResponse.StatusCode);
        var firstResponseModel = await firstRequestResponse.Content.ReadFromJsonAsync<IEnumerable<TodoTitleResponse>>();
        Assert.NotNull(firstResponseModel);
        Assert.Equal(3, firstResponseModel.Count());

        Assert.True(secondRequestResponse.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, secondRequestResponse.StatusCode);
        var secondResponseModel = await secondRequestResponse.Content.ReadFromJsonAsync<IEnumerable<TodoTitleResponse>>();
        Assert.NotNull(secondResponseModel);
        Assert.Single(secondResponseModel);

        var firstTodoResponse = firstResponseModel.OrderBy(x => x.Id).ElementAt(0);
        Assert.Equal(firstTodo.Title, firstTodoResponse.Title);
        Assert.Equal(firstTodo.Id, firstTodoResponse.Id);

        var secondTodoResponse = firstResponseModel.OrderBy(x => x.Id).ElementAt(1);
        Assert.Equal(secondTodo.Title, secondTodoResponse.Title);
        Assert.Equal(secondTodo.Id, secondTodoResponse.Id);

        var thirdTodoResponse = firstResponseModel.OrderBy(x => x.Id).ElementAt(2);
        Assert.Equal(thirdTodo.Title, thirdTodoResponse.Title);
        Assert.Equal(thirdTodo.Id, thirdTodoResponse.Id);

        var fourthTodoResponse = secondResponseModel.First();
        Assert.Equal(fourthTodo.Title, fourthTodoResponse.Title);
        Assert.Equal(fourthTodo.Id, fourthTodoResponse.Id);
    }

    [Fact]
    [ResetDatabase]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task GetTitles_EmptyResult_Should_Return_NoContent()
    {
        // Arrange
        GetTodoTitlesRequest request = new() { PageNumber = 1, PageSize = 3, OrderBy = (byte)ETodoOrderBy.Id };

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.GetTitles(request));

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task GetTitles_WrongPagination_Should_Return_BadRequest()
    {
        // Arrange
        GetTodoTitlesRequest request = new() { PageNumber = 0, PageSize = 0 };

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.GetTitles(request));

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task GetTitles_WrongOrderBy_Should_Return_BadRequest()
    {
        // Arrange
        GetTodoTitlesRequest request = new() { PageNumber = 1, PageSize = 1, OrderBy = (byte)ETodoOrderBy.CreatedAt + 30 };

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.GetTitles(request));

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [IdentityAsignation(role: Constants.EXTERNAL_POLICY)]
    [Trait(CategoryTrait.CATEGORY, CategoryTrait.FUNCTIONAL)]
    public async Task GetTitles_ExternalUser_Should_Return_Forbidden()
    {
        // Arrange
        GetTodoTitlesRequest request = new() { PageNumber = 1, PageSize = 1 };

        // Act
        var response = await Fixture.HttpClient.GetAsync(ApiRoutes.Todo.GetTitles(request));

        // Assert
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
