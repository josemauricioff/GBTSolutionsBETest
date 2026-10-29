using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Products.Api.Errors;
using Products.Application;
using Products.Application.Abstractions;
using Products.Infrastructure;
using Products.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainValidationExceptionHandler>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddProductInfrastructure(
    builder.Configuration.GetConnectionString("Products")
    ?? throw new InvalidOperationException("The Products connection string is required."));

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GBTSolutionsTest",
        Version = "v1"
    });
});

builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Products")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();

    var db = scope.ServiceProvider
        .GetRequiredService<ProductDbContext>();

    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "v1/swagger.json",
            "GBTSolutionsTest v1");

        options.DocumentTitle = "GBTSolutionsTest - Swagger";
    });

    if (app.Configuration.GetValue<bool>("LaunchSwaggerBrowser"))
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var address = app.Urls.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(address))
                return;

            var swaggerUrl = $"{address.TrimEnd('/')}/swagger";

            try
            {
                Process.Start(new ProcessStartInfo(swaggerUrl)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception exception)
            {
                app.Logger.LogWarning(
                    exception,
                    "Could not open the browser. Open {SwaggerUrl} manually.",
                    swaggerUrl);
            }
        });
    }
}

app.MapControllers();
app.Run();

public partial class Program { }