---
name: minimal-api-organization
description: Expert in organizing ASP.NET Core Minimal APIs for production apps - extension methods, TypedResults, endpoint groups, testable handlers
---

## What I Do

- Organize minimal API endpoints into clean, maintainable structure
- Replace inline lambdas with extension method registrations
- Convert Results to TypedResults for compile-time safety and auto OpenAPI docs
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

Use this skill when working with ASP.NET Core minimal API endpoints, restructuring Program.cs, or implementing new API routes in .NET projects.

---

## Core Principles

### 1. Extension Methods for Endpoint Registration

Organize endpoints into separate files with extension methods.

**File structure:**
```
backend/Aimy.API/
├── Program.cs           # Clean entry point
└── Endpoints/
    ├── TodoEndpoints.cs
    └── UserEndpoints.cs
```

### 2. TypedResults Instead of Results

TypedResults provides:
- Automatic OpenAPI/Swagger documentation (no `.Produces()` needed)
- Compile-time type checking
- Simpler unit testing

### 3. Separate Handlers from Registration

Extract lambda handlers into named static methods for testability and readability.

### 4. Group Related Endpoints

Use `MapGroup` to avoid route prefix repetition and apply shared configuration.

---

## Implementation Pattern

```csharp
// Endpoints/TodoEndpoints.cs
public static class TodoEndpoints
{
    public static void MapTodoEndpoints(this WebApplication app)
    {
        var todos = app.MapGroup("/todos")
            .WithTags("Todos")
            .RequireAuthorization();
        
        todos.MapGet("/", GetAllTodos);
        todos.MapGet("/{id}", GetTodoById);
        todos.MapPost("/", CreateTodo);
        todos.MapPut("/{id}", UpdateTodo);
        todos.MapDelete("/{id}", DeleteTodo);
    }
    
    static async Task<Ok<List<Todo>>> GetAllTodos(TodoDb db) =>
        TypedResults.Ok(await db.Todos.ToListAsync());
    
    static async Task<Results<Ok<Todo>, NotFound>> GetTodoById(int id, TodoDb db) =>
        await db.Todos.FindAsync(id) is Todo todo
            ? TypedResults.Ok(todo)
            : TypedResults.NotFound();
    
    static async Task<Created<Todo>> CreateTodo(Todo todo, TodoDb db)
    {
        db.Todos.Add(todo);
        await db.SaveChangesAsync();
        return TypedResults.Created($"/todos/{todo.Id}", todo);
    }
    
    static async Task<Results<NoContent, NotFound>> UpdateTodo(int id, Todo input, TodoDb db)
    {
        var todo = await db.Todos.FindAsync(id);
        if (todo is null) return TypedResults.NotFound();
        
        todo.Name = input.Name;
        todo.IsComplete = input.IsComplete;
        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }
    
    static async Task<Results<NoContent, NotFound>> DeleteTodo(int id, TodoDb db)
    {
        var todo = await db.Todos.FindAsync(id);
        if (todo is null) return TypedResults.NotFound();
        
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }
}
```

**Clean Program.cs:**
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseHttpsRedirection();
app.MapDefaultEndpoints();

// One-line endpoint registrations
app.MapTodoEndpoints();
app.MapUserEndpoints();

app.Run();
```

---

## TypedResults Reference

| Response | TypedResult | Usage |
|----------|-------------|-------|
| 200 + data | `Task<Ok<T>>` | GET success |
| 201 Created | `Task<Created<T>>` | POST create |
| 204 No Content | `Task<NoContent>` | PUT/DELETE |
| 404 | `NotFound` in union | Not found |
| 400 | `BadRequest` in union | Validation |

**Union pattern:**
```csharp
static async Task<Results<Ok<Todo>, NotFound, BadRequest>> GetTodo(int id, TodoDb db)
{
    if (id <= 0) return TypedResults.BadRequest();
    if (await db.Todos.FindAsync(id) is not Todo todo) 
        return TypedResults.NotFound();
    return TypedResults.Ok(todo);
}
```

---

## Adding New Endpoints

1. Create `Endpoints/{Resource}Endpoints.cs`
2. Define `Map{Resource}Endpoints(this WebApplication app)`
3. Create route group with `app.MapGroup("/route")`
4. Add static handler methods with TypedResults
5. Register in Program.cs: `app.Map{Resource}Endpoints()`

---

## Anti-Patterns

| Avoid | Problem | Solution |
|-------|---------|----------|
| Inline lambdas | Untestable | Extension methods |
| `Results` | No type safety | TypedResults |
| Repeated prefixes | DRY violation | MapGroup |
| Business logic in handlers | Mixed concerns | Core services |

---

## Testing

Test handlers directly without HTTP:
```csharp
var result = await TodoEndpoints.GetTodoById(1, inMemoryDb);
Assert.IsType<Ok<Todo>>(result);
```
