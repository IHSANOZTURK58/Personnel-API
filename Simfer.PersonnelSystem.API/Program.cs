using Microsoft.AspNetCore.Authentication.JwtBearer; // YENÝ EKLENDÝ
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens; // YENÝ EKLENDÝ
using Simfer.PersonnelSystem.API.Data;
using Simfer.PersonnelSystem.API.Services;
using System.Text; // YENÝ EKLENDÝ

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ----- 1. JWT GÜVENLÝK AYARLARI (YENÝ EKLENDÝ) -----
// Bu kod app.Build() satýrýndan ÖNCE yazýlmak zorundadýr!
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

        // Eðer bunlarý da appsettings.json'dan okuyorsan builder.Configuration["Jwt:Issuer"] þeklinde yazabilirsin
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],

        // ÝÞTE SENÝN AuthController'DAKÝ MANTIÐIN AYNISI BURADA:
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});
builder.Services.AddScoped<MinioService>();
// ---------------------------------------------------

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ----- 2. GÜVENLÝK KONTROLLERÝNÝ AKTÝF ETME -----
app.UseAuthentication(); // YENÝ EKLENDÝ (Token okuyucu çalýþsýn)
app.UseAuthorization();  // Zaten vardý (Yetki onaylayýcý çalýþsýn)
// ------------------------------------------------

app.MapControllers();

app.Run();