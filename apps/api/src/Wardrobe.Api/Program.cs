using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Wardrobe.Api.Services;
using Wardrobe.Application;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var jwtSecret = builder.Configuration["Jwt:Secret"] 
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException("Jwt:Secret must be at least 32 bytes (256 bits) long.");
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "WardrobeApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "WardrobeClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }

        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Too many requests. Please try again later." },
            cancellationToken);
    };

    options.AddPolicy("auth-strict", httpContext => CreateIpSlidingPartition(httpContext, permitLimit: 10));
    options.AddPolicy("auth-refresh", httpContext => CreateIpSlidingPartition(httpContext, permitLimit: 30));
});

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.TryAdd("X-Permitted-Cross-Domain-Policies", "none");
    await next();
});

app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTimeOffset.UtcNow }))
   .WithName("HealthCheck");

app.Run();

static RateLimitPartition<string> CreateIpSlidingPartition(HttpContext context, int permitLimit)
{
    var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return RateLimitPartition.GetSlidingWindowLimiter(
        partitionKey,
        _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 4,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
}
