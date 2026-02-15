---
name: minimal-api-organization
description: Expert in organizing ASP.NET Core Minimal APIs for production apps - extension methods, TypedResults, endpoint groups, Swagger documentation, and model organization
---

## What I Do

- Organize minimal API endpoints into clean, maintainable structure
- Replace inline lambdas with extension method registrations
- Convert Results to TypedResults for compile-time safety and auto OpenAPI docs
- **Implement comprehensive Swagger documentation (XML comments, typed results, summaries)**
- **Organize models into Requests, Responses, and DTOs with validation and documentation**
- Extract handlers into testable static methods
- Group related endpoints with MapGroup for DRY configuration

## When to Use Me

**Trigger phrases:**
- "Add API endpoint"
- "Organize minimal APIs"
- "Create new endpoint group"
- "Refactor Program.cs endpoints"
- "Set up endpoint registration"
- "Clean up API routes"
- "Add endpoint for [resource]"
- "Improve Swagger documentation"
- "Add XML comments to API"

Use this skill when working with ASP.NET Core minimal API endpoints, restructuring Program.cs, implementing new API routes, or improving API documentation in .NET projects.

---

## Core Principles

### 1. Extension Methods for Endpoint Registration

Organize endpoints into separate files with extension methods.

**File structure:**
```
backend/Aimy.API/
├── Program.cs           # Clean entry point
├── Aimy.API.csproj      # XML Doc configuration
├── Endpoints/
│   ├── AuthEndpoints.cs
│   └── TodoEndpoints.cs
└── Models/
    ├── Requests/        # Request models (records)
    ├── Responses/       # Response models (classes)
    └── DTOs/            # Shared DTOs
```

### 2. Comprehensive Documentation

- Enable XML documentation in `.csproj`.
- Use `WithSummary`, `WithDescription` for endpoints.
- Use XML comments (`///`) for handler methods and models.
- Explicitly define `Produces<T>` and `Accepts<T>` where TypedResults inference isn't enough.

### 3. TypedResults Instead of Results

TypedResults provides:
- Automatic OpenAPI/Swagger documentation
- Compile-time type checking
- Simpler unit testing

### 4. Separate Handlers from Registration

Extract lambda handlers into named static methods for testability and readability.

### 5. Group Related Endpoints

Use `MapGroup` to avoid route prefix repetition and apply shared configuration (e.g., Tags, Auth).

---

## Configuration Setup

### 1. Project File (`.csproj`)

Enable XML documentation generation to feed Swagger.

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn> <!-- Suppress missing comment warnings if desired -->
</PropertyGroup>
```

### 2. Program.cs (Swagger Setup)

Configure Swagger to use XML comments and handle Authentication.

```csharp
builder.Services.AddSwaggerGen(options =>
{
    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Auth Configuration (Bearer)
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```
*Note: If using `Swashbuckle.AspNetCore` > 10.x, use `Microsoft.OpenApi.Models` namespace directly as it depends on `Microsoft.OpenApi` 2.x which supports `Reference`. Avoid mixing `Microsoft.OpenApi` 3.x manually if Swashbuckle depends on 2.x.*

---

## Implementation Pattern

### Endpoint Definition

```csharp
// Endpoints/TodoEndpoints.cs
public static class TodoEndpoints
{
    public static void MapTodoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/todos")
            .WithTags("Todos")
            .RequireAuthorization();
        
        group.MapPost("/", CreateTodo)
            .WithName("CreateTodo")
            .WithSummary("Create a new todo item")
            .WithDescription("Creates a new todo item and returns the created resource.")
            .Produces<TodoResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);
    }
    
    /// <summary>
    /// Creates a new todo item
    /// </summary>
    /// <param name="request">Creation request model</param>
    /// <param name="db">Database context</param>
    /// <returns>Created todo item</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /todos
    ///     {
    ///        "title": "Buy milk",
    ///        "isComplete": false
    ///     }
    /// </remarks>
    private static async Task<Results<Created<TodoResponse>, BadRequest<ErrorResponse>>> CreateTodo(
        CreateTodoRequest request,
        TodoDb db)
    {
        if (string.IsNullOrEmpty(request.Title))
        {
            return TypedResults.BadRequest(new ErrorResponse { Message = "Title is required" });
        }

        var todo = new Todo { Title = request.Title };
        db.Todos.Add(todo);
        await db.SaveChangesAsync();
        
        return TypedResults.Created($"/todos/{todo.Id}", new TodoResponse(todo));
    }
}
```

### Model Organization

**Request Model (Record):**
```csharp
// Models/Requests/CreateTodoRequest.cs
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request model for creating a todo item
/// </summary>
public record CreateTodoRequest
{
    /// <summary>
    /// Title of the todo item
    /// </summary>
    /// <example>Buy groceries</example>
    [Required]
    public required string Title { get; init; }
    
    /// <summary>
    /// Whether the item is completed
    /// </summary>
    /// <example>false</example>
    public bool IsComplete { get; init; }
}
```

**Response Model (Class/Record):**
```csharp
// Models/Responses/TodoResponse.cs
/// <summary>
/// Response model for todo item
/// </summary>
public class TodoResponse
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }
    
    /// <summary>
    /// Title of the item
    /// </summary>
    /// <example>Buy groceries</example>
    public required string Title { get; set; }
}
```

---

## TypedResults Reference

| Response       | TypedResult                 | Usage              |
| -------------- | --------------------------- | ------------------ |
| 200 + data     | `Task<Ok<T>>`               | GET success        |
| 201 Created    | `Task<Created<T>>`          | POST create        |
| 204 No Content | `Task<NoContent>`           | PUT/DELETE success |
| 404            | `NotFound`                  | Not found          |
| 400            | `BadRequest<ErrorResponse>` | Validation error   |
| 401            | `UnauthorizedHttpResult`    | Auth failure       |
| 403            | `ForbidHttpResult`          | Permission failure |
| 500            | `ProblemHttpResult`         | Server error       |

**Union pattern:**
```csharp
static async Task<Results<Ok<Todo>, NotFound, BadRequest>> GetTodo(int id, TodoDb db)
```

---

## Anti-Patterns & Fixes

| Avoid                           | Problem               | Solution                             |
| ------------------------------- | --------------------- | ------------------------------------ |
| Inline lambdas                  | Untestable, cluttered | **Static extension methods**         |
| `Results` (untyped)             | No OpenAPI inference  | **TypedResults**                     |
| Missing XML docs                | Empty Swagger UI      | **Enable GenerateDocumentationFile** |
| `Swashbuckle` + `MS.OpenApi` v3 | Version conflict      | **Use transitive dependency (v2.x)** |
| Magic strings in routes         | Refactoring errors    | **MapGroup & NameOf**                |
| Logic in handlers               | Tightly coupled       | **Inject Services/Mediator**         |

---

## Testing

Test handlers directly without HTTP:
```csharp
var result = await TodoEndpoints.GetTodoById(1, inMemoryDb);
Assert.IsType<Ok<Todo>>(result.Result);
```
