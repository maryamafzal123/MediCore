using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MediCore.Data;
using MediCore.Services;

var builder = WebApplication.CreateBuilder(args);

// ===== 1. Database (PostgreSQL) =====
// Django equivalent: DATABASES in settings.py
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== 2. JWT Authentication =====
// Django equivalent: REST_FRAMEWORK auth settings
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
        };
    });

// ===== 3. Authorization =====
builder.Services.AddAuthorization();

// Register TokenService
builder.Services.AddScoped<TokenService>();

// ===== 4. Controllers =====
// Django equivalent: urls.py (registers all routes)
builder.Services.AddControllers();

// ===== 5. Swagger (API Documentation) =====
// Django equivalent: drf-yasg or drf-spectacular
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ===== 6. CORS =====
// Django equivalent: django-cors-headers
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJs", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ===== Middleware Pipeline =====
// Django equivalent: MIDDLEWARE in settings.py
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowNextJs");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();