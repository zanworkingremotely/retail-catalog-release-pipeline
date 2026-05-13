using FastFashionCatalogSync.Application.DependencyInjection;
using FastFashionCatalogSync.Infrastructure.DependencyInjection;
using FastFashionCatalogSync.Scheduling.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCatalogReleaseApplication();
builder.Services.AddCatalogReleaseInfrastructure(builder.Configuration);
builder.Services.AddCatalogReleaseScheduling();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
