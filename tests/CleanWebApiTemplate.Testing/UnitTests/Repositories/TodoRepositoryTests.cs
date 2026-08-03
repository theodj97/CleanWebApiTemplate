using CleanWebApiTemplate.Infrastructure.Data;
using CleanWebApiTemplate.Infrastructure.Repositories;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace CleanWebApiTemplate.Testing.UnitTests.Repositories;

public class TodoRepositoryTests : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"test_todo_{Guid.NewGuid():N}.db");
    private readonly TodoRepository _repository = null!;
    private SqliteConnection _connection = null!;

    public TodoRepositoryTests()
    {
        _repository = new TodoRepository(new TestSqliteConnectionFactory($"Data Source={_dbPath}"));
    }

    public async ValueTask InitializeAsync()
    {
        _connection = new SqliteConnection($"Data Source={_dbPath}");
        await _connection.OpenAsync();

        var createTableSql = """
            CREATE TABLE IF NOT EXISTS "Todo" (
                "Id"          BLOB    NOT NULL,
                "Title"       TEXT    NOT NULL,
                "Description" TEXT    NULL,
                "CreatedAt"   TEXT    NOT NULL,
                "UpdatedAt"   TEXT    NOT NULL,
                "Status"      INTEGER NOT NULL,
                "CreatedBy"   TEXT    NOT NULL,
                "UpdatedBy"   TEXT    NOT NULL,
                CONSTRAINT "PK_Todo" PRIMARY KEY ("Id")
            );
            """;

        using var command = new SqliteCommand(createTableSql, _connection);
        await command.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _connection?.Dispose();
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }

    [Fact]
    public async Task GetTitlesAsync_WithInvalidSortProperty_Should_ThrowArgumentException()
    {
        // Arrange
        var sortProperties = new List<KeyValuePair<string, bool>>
        {
            new("InvalidProperty", false)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _repository.GetTitlesAsync(null, null, sortProperties, TestContext.Current.CancellationToken));
        Assert.Contains("InvalidProperty", ex.Message);
    }

    [Fact]
    public async Task GetTitlesAsync_WithEmptySortProperty_Should_ThrowArgumentException()
    {
        // Arrange
        var sortProperties = new List<KeyValuePair<string, bool>>
        {
            new("   ", false)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _repository.GetTitlesAsync(null, null, sortProperties, TestContext.Current.CancellationToken));
        Assert.Equal("sortProperties", ex.ParamName);
    }

    [Fact]
    public async Task GetTitlesAsync_WithMultipleSortProperties_Should_ExecuteSuccessfully()
    {
        // Arrange
        var sortProperties = new List<KeyValuePair<string, bool>>
        {
            new("title", false),
            new("id", true)
        };

        // Act
        var result = await _repository.GetTitlesAsync(null, null, sortProperties, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTitlesAsync_WithInvalidPageNumber_Should_ThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _repository.GetTitlesAsync(0, 10, null, TestContext.Current.CancellationToken));
        Assert.Equal("pageNumber", ex.ParamName);
    }

    [Fact]
    public async Task GetTitlesAsync_WithNegativePageNumber_Should_ThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _repository.GetTitlesAsync(-1, 10, null, TestContext.Current.CancellationToken));
        Assert.Equal("pageNumber", ex.ParamName);
    }

    [Fact]
    public async Task GetTitlesAsync_WithInvalidPageSize_Should_ThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _repository.GetTitlesAsync(1, 0, null, TestContext.Current.CancellationToken));
        Assert.Equal("pageSize", ex.ParamName);
    }

    [Fact]
    public async Task GetTitlesAsync_WithNegativePageSize_Should_ThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _repository.GetTitlesAsync(1, -1, null, TestContext.Current.CancellationToken));
        Assert.Equal("pageSize", ex.ParamName);
    }

    [Fact]
    public async Task GetTitlesAsync_WithValidPagination_Should_ReturnEmptyList()
    {
        // Act
        var result = await _repository.GetTitlesAsync(1, 10, null, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_WithValidFilter_Should_ReturnEmptyList()
    {
        // Arrange
        var filter = new TodoFilter();

        // Act
        var result = await _repository.SearchAsync(filter, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_WithNullFilter_Should_ThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _repository.SearchAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_Should_ReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Ulid.NewUlid(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_Should_ReturnFalse()
    {
        // Act
        var result = await _repository.DeleteAsync(Ulid.NewUlid(), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TitleExistsAsync_WithNonExistentTitle_Should_ReturnFalse()
    {
        // Act
        var result = await _repository.TitleExistsAsync("nonexistent", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    private class TestSqliteConnectionFactory(string connectionString) : ISqliteConnectionFactory
    {
        private readonly string _connectionString = connectionString;
        public SqliteConnection CreateConnection() => new(_connectionString);
    }
}
