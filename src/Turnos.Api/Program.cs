using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Turnos.Api.Auth;
using Turnos.Api.Middleware;
using Turnos.Application.Interfaces;
using Turnos.Application.Services;
using Turnos.Domain.Interfaces;
using Turnos.Infrastructure.BackgroundServices;
using Turnos.Infrastructure.Data;
using Turnos.Infrastructure.Repositories;
using Turnos.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuración ----------
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<EmpleadoDemoSettings>(builder.Configuration.GetSection("EmpleadoDemo"));

// ---------- Persistencia ----------
builder.Services.AddDbContext<TurnosDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ---------- Inyección de dependencias (patrón Repository + Service Layer) ----------
builder.Services.AddScoped<ITurnoRepository, TurnoRepository>();
builder.Services.AddScoped<ISucursalRepository, SucursalRepository>();
builder.Services.AddScoped<ITurnoService, TurnoService>();
builder.Services.AddScoped<ISucursalService, SucursalService>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

// Proceso en segundo plano que expira turnos vencidos (ver ExpiracionTurnosService).
builder.Services.AddHostedService<ExpiracionTurnosService>();

// ---------- Autenticación / Autorización ----------
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();

// ---------- CORS para el front-end Angular ----------
var origenesPermitidos = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
        policy.WithOrigins(origenesPermitidos).AllowAnyHeader().AllowAnyMethod());
});

// ---------- MVC / Swagger ----------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Banco Turnos API", Version = "v1" });
    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese: Bearer {token}"
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

var app = builder.Build();

// ---------- Middleware pipeline ----------
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AngularApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ---------- Migración/seed de base de datos al iniciar ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TurnosDbContext>();
    DbSeeder.Seed(db);
}

app.Run();

// Necesario para que WebApplicationFactory<Program> lo detecte en las pruebas de integración.
public partial class Program { }
