using FastFashionCatalogSync.Application.DependencyInjection;
using FastFashionCatalogSync.Infrastructure.DependencyInjection;
using FastFashionCatalogSync.Scheduling.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProductRolloutApplication();
builder.Services.AddProductRolloutInfrastructure(builder.Configuration);
builder.Services.AddProductRolloutScheduling();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
