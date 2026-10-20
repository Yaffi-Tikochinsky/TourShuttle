using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using TourShuttle.Application;
using TourShuttle.Application.Common;
using TourShuttle.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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

// Create or update the database schema and seed demo data before serving requests.
await app.Services.InitializeDatabaseAsync();

// Configure the HTTP request pipeline.
// Swagger is on in every environment: it is the API documentation the course reviews on Render.
app.UseSwagger();
app.UseSwaggerUI();

// The bare service URL opens the documentation.
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

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

app.Run();
