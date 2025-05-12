using DemoConverter.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend3000", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
// Добавление сервисов в контейнер
builder.Services.AddScoped<IZipService, ZipService>(); // Регистрация сервиса IZipService для работы с архивом
builder.Services.AddScoped<ISvgService, SvgService>(); // Регистрация сервиса обработки svg
// Add services to the container.

// Добавление логирования
builder.Services.AddLogging();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend3000");

app.UseAuthorization();

app.MapControllers();

app.Run();
