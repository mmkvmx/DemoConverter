using DemoConverter.Services;

var builder = WebApplication.CreateBuilder(args);
// ƒобавление сервисов в контейнер
builder.Services.AddScoped<IZipService, ZipService>(); // –егистраци€ сервиса IZipService дл€ работы с архивом

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
