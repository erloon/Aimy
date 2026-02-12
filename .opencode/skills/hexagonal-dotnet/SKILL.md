---
name: hexagonal-dotnet
description: Hexagonal Architecture (Ports & Adapters) for .NET applications. Use when implementing Clean Architecture, deciding where to place entities/interfaces/repositories, structuring Core/Infrastructure/API layers, or ensuring domain purity. Handles project structure, port/adapter placement, EF Core Fluent API patterns, and dependency direction rules.
---

# Hexagonal Architecture for .NET

## Core Principle

Domain (Core) must have **ZERO dependencies** on infrastructure, external libraries, or frameworks.

```
┌─────────────────────────────────────────────┐
│                API (Primary Adapter)         │
│  - Endpoints                                │
│  - DI Registration (Composition Root)       │
└─────────────────┬───────────────────────────┘
                  │ references (DI only)
┌─────────────────▼───────────────────────────┐
│           INFRASTRUCTURE (Adapters)          │
│  - EF Core DbContext                        │
│  - Repository Implementations               │
│  - External Services (BCrypt, JWT, etc.)    │
└─────────────────┬───────────────────────────┘
                  │ implements
┌─────────────────▼───────────────────────────┐
│              CORE (Hexagon)                  │
│  - Domain Entities (POCOs)                  │
│  - Port Interfaces (IUserRepository, etc.)  │
│  - Application Services (AuthLogic)         │
└─────────────────────────────────────────────┘
```

## Project Structure

```
backend/
├── Aimy.Core/                    # NO external deps
│   ├── Domain/
│   │   └── Entities/
│   │       └── User.cs           # POCO only
│   └── Application/
│       ├── Interfaces/
│       │   ├── IUserRepository.cs
│       │   ├── IPasswordHasher.cs
│       │   └── ITokenProvider.cs
│       └── Services/
│           └── AuthService.cs
├── Aimy.Infrastructure/          # Implements Core interfaces
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   └── Configurations/
│   │       └── UserConfiguration.cs
│   ├── Repositories/
│   │   └── UserRepository.cs
│   ├── Security/
│   │   ├── BCryptPasswordHasher.cs
│   │   └── JwtTokenProvider.cs
│   └── DependencyInjection.cs
└── Aimy.API/                     # Composition Root
    ├── Program.cs
    └── Endpoints/
```

## Entity Rules

### Core Entity (POCO - No EF Attributes)

```csharp
// Aimy.Core/Domain/Entities/User.cs
namespace Aimy.Core.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public string? Role { get; set; }
}
```

### EF Core Configuration (Infrastructure)

```csharp
// Aimy.Infrastructure/Data/Configurations/UserConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Aimy.Core.Domain.Entities;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.Username).IsUnique();
        builder.Property(u => u.Username).IsRequired().HasMaxLength(100);
    }
}
```

## Port Interfaces (Core)

```csharp
// Aimy.Core/Application/Interfaces/IUserRepository.cs
public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task AddAsync(User user);
}

// Aimy.Core/Application/Interfaces/IPasswordHasher.cs
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

// Aimy.Core/Application/Interfaces/ITokenProvider.cs
public interface ITokenProvider
{
    string GenerateToken(User user);
}
```

## Adapters (Infrastructure)

### Repository Implementation

```csharp
// Aimy.Infrastructure/Repositories/UserRepository.cs
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context) => _context = context;

    public async Task<User?> GetByUsernameAsync(string username) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

    public async Task AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }
}
```

### External Service Adapter

```csharp
// Aimy.Infrastructure/Security/BCryptPasswordHasher.cs
using BCrypt.Net;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.HashPassword(password);
    
    public bool Verify(string password, string hash) => 
        BCrypt.Verify(password, hash);
}
```

## DI Extension Method

```csharp
// Aimy.Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IHostApplicationBuilder AddInfrastructure(
        this IHostApplicationBuilder builder)
    {
        // Database
        builder.AddNpgsqlDbContext<ApplicationDbContext>("postgres");
        
        // Repositories
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        
        // External services
        builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        builder.Services.AddScoped<ITokenProvider, JwtTokenProvider>();
        
        return builder;
    }
}
```

## Composition Root (API)

```csharp
// Aimy.API/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddInfrastructure();  // Registers all adapters
builder.AddCore(); // Register Core logic

var app = builder.Build();
app.MapAuthEndpoints();
app.Run();
```

## Migrations

With DbContext in Infrastructure:

```bash
dotnet ef migrations add Initial \
  --project backend/Aimy.Infrastructure \
  --startup-project backend/Aimy.API
```

## Verification

```bash
# Core must have NO project references
dotnet list backend/Aimy.Core/Aimy.Core.csproj reference
# Expected: empty output

# Core must have NO external packages (BCrypt, JWT, EF)
dotnet list backend/Aimy.Core/Aimy.Core.csproj package
# Expected: only .NET SDK packages
```

## Anti-Patterns

| Violation | Problem | Solution |
|-----------|---------|----------|
| `[Key]` attribute on entity | EF leak into domain | Fluent API in Infrastructure |
| `BCrypt.Verify()` in Core | External lib dependency | `IPasswordHasher` port |
| `UserRepository` in Core | Infrastructure in domain | Move to Infrastructure |
| `using EF;` in entity | ORM coupling | POCO + Configuration |
| `new JwtToken()` in Core | Framework dependency | `ITokenProvider` port |

## When to Create New Ports

Create interface in Core when:
1. Service uses external library (BCrypt, JWT, HTTP client)
2. Service touches database, filesystem, or network
3. Service varies by environment (dev/prod)
4. Service needs mocking in tests

## Quick Reference

| Component | Location | Depends On |
|-----------|----------|------------|
| Entity | `Core/Domain/Entities/` | Nothing |
| Port Interface | `Core/Application/Interfaces/` | Nothing |
| Application Service | `Core/Application/Services/` | Core interfaces only |
| EF Configuration | `Infrastructure/Data/Configurations/` | Core entities |
| Repository Impl | `Infrastructure/Repositories/` | Core interface + EF |
| External Adapter | `Infrastructure/Security/` | Core interface + Lib |
| DI Registration | `Infrastructure/DependencyInjection.cs` | All Infrastructure |
