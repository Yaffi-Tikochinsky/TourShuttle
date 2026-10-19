using TourShuttle.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
