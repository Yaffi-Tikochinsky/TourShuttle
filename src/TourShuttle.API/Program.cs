using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NLog.Web;
using TourShuttle.API.Middleware;
using TourShuttle.Application;
using TourShuttle.Application.Common;
using TourShuttle.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Logging: the code writes to ILogger<T>; NLog is the provider that actually writes (console + file, see nlog.config).
// ClearProviders removes Microsoft's default console provider, which would otherwise print every line a second time.
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// JWT settings: Issuer/Audience/ExpiresMinutes from appsettings.json, Key from user-secrets or Jwt__Key.
// A missing or short key stops the server at startup instead of failing on the first login.
var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
var jwt = jwtSection.Get<JwtSettings>() ?? throw new InvalidOperationException("The 'Jwt' configuration section is missing.");
if (string.IsNullOrWhiteSpace(jwt.Key) || Encoding.UTF8.GetByteCount(jwt.Key) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must be at least 32 bytes. In development set it with dotnet user-secrets; " +
        "in production set the Jwt__Key environment variable.");
}
builder.Services.Configure<JwtSettings>(jwtSection);
builder.Services.Configure<PickupTimeEstimationSettings>(
    builder.Configuration.GetSection(PickupTimeEstimationSettings.SectionName));

// Add services to the container.

builder.Services.AddApplication(builder.Configuration["AutoMapper:LicenseKey"]);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep the claim names as they appear in the token ("sub", "role") instead of the long .NET names.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers()
    // Enums travel as names ("Driver") rather than numbers, in requests and in responses.
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// One error format everywhere: every ProblemDetails (our Problem(...), automatic 400 validation,
// 500 from the exception middleware, empty 401/403 bodies filled by UseStatusCodePages) gets the correlation id.
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    context.ProblemDetails.Extensions.Remove("traceId");
    context.ProblemDetails.Extensions["correlationId"] =
        CorrelationIdMiddleware.Get(context.HttpContext) ?? context.HttpContext.TraceIdentifier;
});
// CORS: the React client is served from another origin (its own Render Static Site), so the browser asks the
// API for permission before every cross-origin call. Only the origins listed in Cors:AllowedOrigins get it:
// in development the Vite servers (appsettings.Development.json), on Render the client's URL through
// the environment variable Cors__AllowedOrigins__0. The token travels in the Authorization header, not
// in a cookie, so credentials are not allowed (no AllowCredentials).
const string ClientCorsPolicy = "Client";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(ClientCorsPolicy, policy => policy
    .WithOrigins(allowedOrigins.Select(o => o.TrimEnd('/')).ToArray())
    .WithMethods("GET", "POST", "PUT", "DELETE")
    .WithHeaders("Authorization", "Content-Type", CorrelationIdMiddleware.HeaderName)
    // Response headers the client's JavaScript may read: the correlation id it shows with errors,
    // and Location of a created resource.
    .WithExposedHeaders(CorrelationIdMiddleware.HeaderName, "Location")
    // The browser may reuse a preflight answer for 10 minutes instead of asking before every request.
    .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));

// Liveness check for Render: answers 200 as long as the process is up, without touching the database.
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Adds the "Authorize" button: paste a token once and Swagger sends it with every request.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the token from /api/auth/login (without the word Bearer)."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

// A missing Cors__AllowedOrigins__0 on Render shows up here, rather than only as "blocked by CORS" in a browser.
if (allowedOrigins.Length == 0)
{
    app.Logger.LogWarning("CORS: no allowed origins are configured (Cors:AllowedOrigins); browsers on other origins will be refused");
}
else
{
    app.Logger.LogInformation("CORS: allowed origins {Origins}", string.Join(", ", allowedOrigins));
}

// Create or update the database schema and seed demo data before serving requests.
await app.Services.InitializeDatabaseAsync();

// Configure the HTTP request pipeline. The order is the order every request passes through (and the reverse on the way out).
// 1. CorrelationId first: its logging scope wraps everything, so even the exception log line carries the id.
app.UseMiddleware<CorrelationIdMiddleware>();
// 2. Request logging outside the exception handler, so a failed request is logged with its final status (500).
app.UseMiddleware<RequestLoggingMiddleware>();
// 3. CORS outside the exception handler, so error responses (400, 409, 500) carry the CORS headers too;
//    without them the browser would hide the ProblemDetails body from the client. It also answers preflight
//    OPTIONS requests here, before authentication (a preflight never carries the token).
app.UseCors(ClientCorsPolicy);
// 4. Exception handling around everything that does real work.
app.UseMiddleware<ExceptionHandlingMiddleware>();
// 5. Error responses with no body (401/403 from JwtBearer, 404 for unknown URLs) get a ProblemDetails body.
app.UseStatusCodePages();

// Swagger is on in every environment: it is the API documentation the course reviews on Render.
app.UseSwagger();
app.UseSwaggerUI();

// The bare service URL opens the documentation.
// HEAD too: Render probes "/" with HEAD after a deploy, which used to get 405.
app.MapMethods("/", [HttpMethods.Get, HttpMethods.Head], () => Results.Redirect("/swagger")).ExcludeFromDescription();

// On Render, HTTPS ends at Render's proxy and the container only receives HTTP,
// so redirecting is only meaningful when running locally.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Authentication (who are you?) must run before authorization (what may you do?).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
// Render's Health Check Path. Cheaper than the old /swagger/v1/swagger.json, which rebuilt the whole document every few seconds.
app.MapHealthChecks(RequestLoggingMiddleware.HealthPath);

app.Run();
