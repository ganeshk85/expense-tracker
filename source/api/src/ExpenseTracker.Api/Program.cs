using ExpenseTracker.Api;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Data.Repositories;
using ExpenseTracker.Audit;
using ExpenseTracker.Audit.Endpoints;
using ExpenseTracker.Audit.Middleware;
using ExpenseTracker.Audit.Repositories;
using ExpenseTracker.Auth;
using ExpenseTracker.Auth.Endpoints;
using ExpenseTracker.Auth.Repositories;
using ExpenseTracker.Budget;
using ExpenseTracker.Budget.Endpoints;
using ExpenseTracker.Budget.Repositories;
using ExpenseTracker.Budget.Services;
using ExpenseTracker.Expense;
using ExpenseTracker.Expense.Endpoints;
using ExpenseTracker.Expense.Repositories;
using ExpenseTracker.Ocr;
using ExpenseTracker.Ocr.Repositories;
using ExpenseTracker.Receipt;
using ExpenseTracker.Receipt.Endpoints;
using ExpenseTracker.Receipt.Repositories;
using ExpenseTracker.Receipt.Services;
using ExpenseTracker.Shared;
using ExpenseTracker.Shared.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = (builder.Configuration["AllowedOrigins"] ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// Session-based authentication + internal worker key scheme
builder.Services.AddAuthentication(SessionAuthHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthHandler>(SessionAuthHandler.SchemeName, null)
    .AddScheme<AuthenticationSchemeOptions, InternalKeyAuthHandler>(InternalKeyAuthHandler.SchemeName, null);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim("Role", "Admin")));

    options.AddPolicy("ContributorOrAdmin", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim("Role", "Admin") ||
            ctx.User.HasClaim("Role", "Contributor")));

    // Only the OCR worker (presenting X-Internal-Key) may call internal endpoints.
    options.AddPolicy("InternalOnly", policy =>
        policy.AddAuthenticationSchemes(InternalKeyAuthHandler.SchemeName)
              .RequireAuthenticatedUser());

    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Database
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=expense_tracker;Username=postgres;Password=postgres";
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connStr));

// Redis (abortConnect=false so startup is not blocked if Redis is briefly unavailable)
var redisConn = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
var redisConfig = ConfigurationOptions.Parse(redisConn);
redisConfig.AbortOnConnectFail = false;
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConfig));

// Storage options
builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection("Storage"));
builder.Services.Configure<ExpenseTracker.Expense.Services.AttachmentStorageOptions>(
    o => o.BasePath = builder.Configuration["Storage:BasePath"] ?? "/storage");

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IInviteTokenRepository, InviteTokenRepository>();
builder.Services.AddScoped<IReceiptRepository, ReceiptRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
// ExpenseRepository implements both IExpenseRepository and IExpenseManagementRepository.
builder.Services.AddScoped<ExpenseRepository>();
builder.Services.AddScoped<IExpenseRepository>(sp => sp.GetRequiredService<ExpenseRepository>());
builder.Services.AddScoped<IExpenseManagementRepository>(sp => sp.GetRequiredService<ExpenseRepository>());

// Budget repository (concrete impl lives in Api; interface in Budget module)
builder.Services.AddScoped<IBudgetRepository, BudgetRepository>();

// Budget alert trigger — decoupled via Shared interface so Expense module can call it without circular dep.
builder.Services.AddScoped<IBudgetAlertService>(sp => (IBudgetAlertService)sp.GetRequiredService<ExpenseTracker.Budget.Services.IBudgetService>());

// Modules
builder.Services.AddAuthModule();
builder.Services.AddReceiptModule();
builder.Services.AddAuditModule();
builder.Services.AddOcrModule();
builder.Services.AddExpenseModule();
builder.Services.AddBudgetModule();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply pending EF Core migrations on startup. Idempotent — skips already-applied migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Global exception handler
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        var (status, message) = ex switch
        {
            UnauthorizedException e => (401, e.Message),
            ForbiddenException e => (403, e.Message),
            NotFoundException e => (404, e.Message),
            ConflictException e => (409, e.Message),
            ValidationException e => (422, e.Message),
            _ => (500, "An unexpected error occurred.")
        };

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new { error = message });
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseAuditMiddleware();

// Mount module endpoints
app.MapAuthEndpoints();
app.MapReceiptEndpoints();
app.MapAuditEndpoints();
app.MapExpenseEndpoints();
app.MapDashboardEndpoints();
app.MapBudgetEndpoints();

app.Run();

public partial class Program { }
