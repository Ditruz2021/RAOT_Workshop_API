using ExampleApi.Data.DbContexts;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Scan(scan => scan
                .FromAssemblyOf<Program>()
                .AddClasses(classes => classes.InNamespaces("ExampleApi.Services"))
                .AsMatchingInterface()
                .WithScopedLifetime()
);


builder.Services.AddCors(options =>
{
    options.AddPolicy("private_policy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://127.0.0.1:5173",
                "http://<<ALLOWED_IP>>:3000",
                "https://<<ALLOWED_DOMAIN>>"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
        // ถ้าคุณใช้ cookie/credentials ค่อยเปิดอันนี้:
        // .AllowCredentials();
    });

    options.AddPolicy("public_policy", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("public_policy");

app.UseAuthorization();

app.MapControllers();

app.Run();
