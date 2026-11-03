using System.Text.Json.Nodes;

namespace CleanWebApiTemplate.OpenApiExamples.UnitTests;

public sealed class OpenApiExamplesMergerTests
{
    [Fact]
    public void Merge_AddsBodyExampleFromFile()
    {
        using var temp = new TestDirectory();
        temp.Write("bodies/create.json", """{"title":"from-file"}""");

        var document = Document(MinimalPostDocument());
        var catalog = Catalog("""
        {
          "operations": {
            "POST /api/Todo": {
              "requestBody": {
                "examples": {
                  "from-file": { "summary": "From file", "body": "bodies/create.json" }
                }
              }
            }
          }
        }
        """);

        var problems = OpenApiExamplesMerger.Merge(document, catalog, temp.Root);

        Assert.Empty(problems);
        var example = document["paths"]!["/api/Todo/"]!["post"]!["requestBody"]!["content"]!["application/json"]!["examples"]!["from-file"]!;
        Assert.Equal("From file", example["summary"]!.GetValue<string>());
        Assert.Equal("from-file", example["value"]!["title"]!.GetValue<string>());
    }

    [Fact]
    public void Merge_MatchesRouteConstraintsAndTrailingSlashes()
    {
        var document = Document("""
        {
          "openapi": "3.1.0",
          "info": { "title": "Test", "version": "1.0.0" },
          "paths": {
            "/api/Todo/{id:ulid}": {
              "get": {
                "parameters": [
                  { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } }
                ],
                "responses": { "200": { "description": "OK" } }
              }
            }
          }
        }
        """);

        var catalog = Catalog("""
        {
          "operations": {
            "GET /api/Todo/{id}": {
              "parameters": [
                { "name": "id", "in": "path", "value": "01HWXHRB80000G000000000001" }
              ]
            }
          }
        }
        """);

        var problems = OpenApiExamplesMerger.Merge(document, catalog, ".");

        Assert.Empty(problems);
        Assert.Equal(
            "01HWXHRB80000G000000000001",
            document["paths"]!["/api/Todo/{id:ulid}"]!["get"]!["parameters"]![0]!["example"]!.GetValue<string>());
    }

    [Fact]
    public void Merge_AddsInlineValueAndSummary()
    {
        var document = Document(MinimalPostDocument());
        var catalog = Catalog("""
        {
          "operations": {
            "POST /api/Todo/": {
              "requestBody": {
                "examples": {
                  "inline": { "summary": "Inline", "value": { "title": "inline" } }
                }
              }
            }
          }
        }
        """);

        var problems = OpenApiExamplesMerger.Merge(document, catalog, ".");

        Assert.Empty(problems);
        var example = document["paths"]!["/api/Todo/"]!["post"]!["requestBody"]!["content"]!["application/json"]!["examples"]!["inline"]!;
        Assert.Equal("Inline", example["summary"]!.GetValue<string>());
        Assert.Equal("inline", example["value"]!["title"]!.GetValue<string>());
    }

    [Fact]
    public void Merge_KeepsExistingExamples()
    {
        var document = Document("""
        {
          "openapi": "3.1.0",
          "info": { "title": "Test", "version": "1.0.0" },
          "paths": {
            "/api/Todo/": {
              "post": {
                "requestBody": {
                  "content": {
                    "application/json": {
                      "schema": { "type": "object" },
                      "examples": {
                        "existing": { "value": { "title": "existing" } }
                      }
                    }
                  }
                },
                "responses": { "201": { "description": "Created" } }
              }
            }
          }
        }
        """);

        var catalog = Catalog("""
        {
          "operations": {
            "POST /api/Todo/": {
              "requestBody": {
                "examples": {
                  "new": { "value": { "title": "new" } }
                }
              }
            }
          }
        }
        """);

        var problems = OpenApiExamplesMerger.Merge(document, catalog, ".");

        Assert.Empty(problems);
        var examples = document["paths"]!["/api/Todo/"]!["post"]!["requestBody"]!["content"]!["application/json"]!["examples"]!;
        Assert.NotNull(examples["existing"]);
        Assert.NotNull(examples["new"]);
    }

    [Fact]
    public void Merge_ReportsUnknownOperation()
    {
        var document = Document(MinimalDocument());
        var catalog = Catalog("""
        {
          "operations": {
            "DELETE /api/Nope": { "parameters": [] }
          }
        }
        """);

        var problems = OpenApiExamplesMerger.Merge(document, catalog, ".");

        Assert.Single(problems);
        Assert.Contains("no OpenAPI path matches", problems[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Merge_ReportsUnknownParameter()
    {
        var document = Document(MinimalDocument());
        var catalog = Catalog("""
        {
          "operations": {
            "GET /api/Todo/{id}": {
              "parameters": [
                { "name": "page", "in": "query", "value": 1 }
              ]
            }
          }
        }
        """);

        var problems = OpenApiExamplesMerger.Merge(document, catalog, ".");

        Assert.Single(problems);
        Assert.Contains("no parameter 'page' in 'query'", problems[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Merge_ReportsAmbiguousParameterWithoutIn()
    {
        var document = Document("""
        {
          "openapi": "3.1.0",
          "info": { "title": "Test", "version": "1.0.0" },
          "paths": {
            "/api/Todo/{id}": {
              "get": {
                "parameters": [
                  { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } },
                  { "name": "id", "in": "query", "schema": { "type": "string" } }
                ],
                "responses": { "200": { "description": "OK" } }
              }
            }
          }
        }
        """);

        var catalog = Catalog("""
        {
          "operations": {
            "GET /api/Todo/{id}": {
              "parameters": [
                { "name": "id", "value": "x" }
              ]
            }
          }
        }
        """);

        var problems = OpenApiExamplesMerger.Merge(document, catalog, ".");

        Assert.Single(problems);
        Assert.Contains("ambiguous", problems[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Merge_ReportsMissingBodyFile()
    {
        using var temp = new TestDirectory();

        var document = Document(MinimalPostDocument());
        var catalog = Catalog("""
        {
          "operations": {
            "POST /api/Todo/": {
              "requestBody": {
                "examples": {
                  "missing": { "body": "bodies/missing.json" }
                }
              }
            }
          }
        }
        """);

        var problems = OpenApiExamplesMerger.Merge(document, catalog, temp.Root);

        Assert.Single(problems);
        Assert.Contains("does not exist", problems[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Merge_ReportsBodyAndValueTogether()
    {
        var document = Document(MinimalPostDocument());
        var catalog = Catalog("""
        {
          "operations": {
            "POST /api/Todo/": {
              "requestBody": {
                "examples": {
                  "bad": { "body": "bodies/x.json", "value": { "title": "x" } }
                }
              }
            }
          }
        }
        """);

        var problems = OpenApiExamplesMerger.Merge(document, catalog, ".");

        Assert.Single(problems);
        Assert.Contains("either 'body' or 'value'", problems[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Merge_ResolvesParameterReferences()
    {
        var document = Document("""
        {
          "openapi": "3.1.0",
          "info": { "title": "Test", "version": "1.0.0" },
          "paths": {
            "/api/Todo/{id}": {
              "get": {
                "parameters": [
                  { "$ref": "#/components/parameters/Id" }
                ],
                "responses": { "200": { "description": "OK" } }
              }
            }
          },
          "components": {
            "parameters": {
              "Id": { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } }
            }
          }
        }
        """);

        var catalog = Catalog("""
        {
          "operations": {
            "GET /api/Todo/{id}": {
              "parameters": [
                { "name": "id", "in": "path", "value": "abc" }
              ]
            }
          }
        }
        """);

        var problems = OpenApiExamplesMerger.Merge(document, catalog, ".");

        Assert.Empty(problems);
        Assert.Equal("abc", document["components"]!["parameters"]!["Id"]!["example"]!.GetValue<string>());
    }

    [Fact]
    public void Merge_PrefersOperationParameterOverPathParameter()
    {
        var document = Document("""
        {
          "openapi": "3.1.0",
          "info": { "title": "Test", "version": "1.0.0" },
          "paths": {
            "/api/Todo/{id}": {
              "parameters": [
                { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } }
              ],
              "get": {
                "parameters": [
                  { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } }
                ],
                "responses": { "200": { "description": "OK" } }
              }
            }
          }
        }
        """);

        var catalog = Catalog("""
        {
          "operations": {
            "GET /api/Todo/{id}": {
              "parameters": [
                { "name": "id", "in": "path", "value": "abc" }
              ]
            }
          }
        }
        """);

        var problems = OpenApiExamplesMerger.Merge(document, catalog, ".");

        Assert.Empty(problems);
        Assert.Equal(
            "abc",
            document["paths"]!["/api/Todo/{id}"]!["get"]!["parameters"]![0]!["example"]!.GetValue<string>());
    }

    [Fact]
    public void Merge_AddsBearerSchemeAndRelativeServer()
    {
        var document = Document(MinimalDocument());
        var catalog = Catalog("""{ "operations": {} }""");

        var problems = OpenApiExamplesMerger.Merge(document, catalog, ".");

        Assert.Empty(problems);
        Assert.Equal("/", document["servers"]![0]!["url"]!.GetValue<string>());
        Assert.Equal("http", document["components"]!["securitySchemes"]!["Bearer"]!["type"]!.GetValue<string>());
        Assert.Equal("bearer", document["components"]!["securitySchemes"]!["Bearer"]!["scheme"]!.GetValue<string>());
        Assert.NotNull(document["security"]![0]!["Bearer"]);
        Assert.Equal("Test", document["info"]!["title"]!.GetValue<string>());
        Assert.Contains("Local explorer", document["info"]!["description"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public void Merge_AddsDefaultProblemExample()
    {
        var document = Document("""
        {
          "openapi": "3.1.0",
          "info": { "title": "Test", "version": "1.0.0" },
          "paths": {
            "/api/Todo/{id}": {
              "get": {
                "responses": {
                  "400": {
                    "description": "Bad Request",
                    "content": {
                      "application/problem+json": {
                        "schema": { "$ref": "#/components/schemas/ProblemDetails" }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """);

        var catalog = Catalog("""{ "operations": {} }""");
        var problems = OpenApiExamplesMerger.Merge(document, catalog, ".");

        Assert.Empty(problems);
        var example = document["paths"]!["/api/Todo/{id}"]!["get"]!["responses"]!["400"]!["content"]!["application/problem+json"]!["examples"]!["problem"]!;
        Assert.Equal(400, example["value"]!["status"]!.GetValue<int>());
        Assert.Equal("Bad Request", example["value"]!["title"]!.GetValue<string>());
        Assert.False(string.IsNullOrWhiteSpace(example["value"]!["detail"]!.GetValue<string>()));
    }

    [Fact]
    public void Merge_KeepsCatalogProblemExample()
    {
        var document = Document("""
        {
          "openapi": "3.1.0",
          "info": { "title": "Test", "version": "1.0.0" },
          "paths": {
            "/api/Todo/{id}": {
              "get": {
                "responses": {
                  "400": {
                    "description": "Bad Request",
                    "content": {
                      "application/problem+json": {
                        "schema": { "$ref": "#/components/schemas/ProblemDetails" }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """);

        var catalog = Catalog("""
        {
          "operations": {
            "GET /api/Todo/{id}": {
              "responses": {
                "400": {
                  "examples": {
                    "custom": { "value": { "title": "Custom", "status": 400, "detail": "Custom detail." } }
                  }
                }
              }
            }
          }
        }
        """);

        var problems = OpenApiExamplesMerger.Merge(document, catalog, ".");

        Assert.Empty(problems);
        var examples = document["paths"]!["/api/Todo/{id}"]!["get"]!["responses"]!["400"]!["content"]!["application/problem+json"]!["examples"]!;
        Assert.NotNull(examples["custom"]);
        Assert.Null(examples["problem"]);
    }

    [Fact]
    public void Merge_AddsResponseExampleFromFile()
    {
        using var temp = new TestDirectory();
        temp.Write("responses/todo.json", """{"id":"01HWXHRB80000G000000000001"}""");

        var document = Document("""
        {
          "openapi": "3.1.0",
          "info": { "title": "Test", "version": "1.0.0" },
          "paths": {
            "/api/Todo/{id}": {
              "get": {
                "parameters": [
                  { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } }
                ],
                "responses": {
                  "200": {
                    "description": "OK",
                    "content": { "application/json": { "schema": { "type": "object" } } }
                  }
                }
              }
            }
          }
        }
        """);

        var catalog = Catalog("""
        {
          "operations": {
            "GET /api/Todo/{id}": {
              "responses": {
                "200": {
                  "examples": {
                    "todo": { "summary": "A TODO", "body": "responses/todo.json" }
                  }
                }
              }
            }
          }
        }
        """);

        var problems = OpenApiExamplesMerger.Merge(document, catalog, temp.Root);

        Assert.Empty(problems);
        var example = document["paths"]!["/api/Todo/{id}"]!["get"]!["responses"]!["200"]!["content"]!["application/json"]!["examples"]!["todo"]!;
        Assert.Equal("A TODO", example["summary"]!.GetValue<string>());
        Assert.Equal("01HWXHRB80000G000000000001", example["value"]!["id"]!.GetValue<string>());
    }

    [Fact]
    public void Merge_ReportsUnknownResponse()
    {
        var document = Document(MinimalDocument());
        var catalog = Catalog("""
        {
          "operations": {
            "GET /api/Todo/{id}": {
              "responses": {
                "404": {
                  "examples": {
                    "missing": { "value": { "title": "missing" } }
                  }
                }
              }
            }
          }
        }
        """);

        var problems = OpenApiExamplesMerger.Merge(document, catalog, ".");

        Assert.Single(problems);
        Assert.Contains("no response '404'", problems[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Merge_ReportsResponseWithoutContent()
    {
        var document = Document("""
        {
          "openapi": "3.1.0",
          "info": { "title": "Test", "version": "1.0.0" },
          "paths": {
            "/api/Todo/{id}": {
              "get": {
                "responses": { "204": { "description": "No Content" } }
              }
            }
          }
        }
        """);

        var catalog = Catalog("""
        {
          "operations": {
            "GET /api/Todo/{id}": {
              "responses": {
                "204": {
                  "examples": {
                    "empty": { "value": null }
                  }
                }
              }
            }
          }
        }
        """);

        var problems = OpenApiExamplesMerger.Merge(document, catalog, ".");

        Assert.Single(problems);
        Assert.Contains("has no content", problems[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Merge_ReportsRequestBodyMissingInOperation()
    {
        var document = Document(MinimalDocument());
        var catalog = Catalog("""
        {
          "operations": {
            "GET /api/Todo/{id}": {
              "requestBody": {
                "examples": {
                  "x": { "value": {} }
                }
              }
            }
          }
        }
        """);

        var problems = OpenApiExamplesMerger.Merge(document, catalog, ".");

        Assert.Single(problems);
        Assert.Contains("no request body content", problems[0], StringComparison.Ordinal);
    }

    private static JsonObject Document(string json) => (JsonObject)JsonNode.Parse(json)!;

    private static JsonObject Catalog(string json) => (JsonObject)JsonNode.Parse(json)!;

    private static string MinimalDocument() => """
        {
          "openapi": "3.1.0",
          "info": { "title": "Test", "version": "1.0.0" },
          "paths": {
            "/api/Todo/{id}": {
              "get": {
                "parameters": [
                  { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } }
                ],
                "responses": { "200": { "description": "OK" } }
              }
            }
          }
        }
        """;

    private static string MinimalPostDocument() => """
        {
          "openapi": "3.1.0",
          "info": { "title": "Test", "version": "1.0.0" },
          "paths": {
            "/api/Todo/": {
              "post": {
                "requestBody": {
                  "content": {
                    "application/json": { "schema": { "type": "object" } }
                  }
                },
                "responses": { "201": { "description": "Created" } }
              }
            }
          }
        }
        """;

}
