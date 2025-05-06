using DemoConverter.Services;

var builder = WebApplication.CreateBuilder(args);
// ���������� �������� � ���������
builder.Services.AddScoped<IZipService, ZipService>(); // ����������� ������� IZipService ��� ������ � �������
builder.Services.AddScoped<ISvgService, SvgService>(); // ����������� ������� ��������� svg
// Add services to the container.

// ���������� �����������
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

app.UseAuthorization();

app.MapControllers();

app.Run();
