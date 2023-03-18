using FinanceAPI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<MySqlConnection>(_ => new MySqlConnection("Server=localhost;Port=3306;database=finance_db;User ID=root;Password=Avtizem123"));
builder.Services.AddSingleton<DbHelper>(_ => new DbHelper(new MySqlConnection("Server=localhost;Port=3306;database=finance_db;User ID=root;Password=Avtizem123")));
builder.Services.AddSingleton<TimeHelper>(_ => new TimeHelper());
builder.Services.AddSingleton<FilterHelper>(_ => new FilterHelper(new TimeHelper(), new DbHelper(new MySqlConnection("Server=localhost;Port=3306;database=finance_db;User ID=root;Password=Avtizem123"))));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


var app = builder.Build();

app.UseCors();
app.UseSession();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
