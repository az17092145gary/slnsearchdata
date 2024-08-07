using Microsoft.Extensions.DependencyInjection;
using searchdata.Model;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//排除跨網域的問題
builder.Services.AddCors(p => p.AddPolicy("corsapp", builder =>
{
    builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
}));

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddSingleton<AIOTService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
//排除跨網域的問題
app.UseCors("corsapp");
app.UseAuthorization();

app.MapControllers();

app.Run();
