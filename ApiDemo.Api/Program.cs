using ApiDemo.Api.Options;
using ApiDemo.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CsvStorageOptions>(builder.Configuration.GetSection("CsvStorage"));
builder.Services.AddSingleton<IProductRepository, CsvProductRepository>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
