using GraficaModerna.API.Data;
using GraficaModerna.API.Middlewares;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Application.Mappings;
using GraficaModerna.Application.Services;
using GraficaModerna.Application.Validators;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;
using GraficaModerna.Infrastructure.Context;
using GraficaModerna.Infrastructure.Repositories;
using GraficaModerna.Infrastructure.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Ganss.Xss;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Adicionar HttpClient (Essencial para chamar API dos Correios) ---
builder.Services.AddHttpClient();

// --- OTIMIZAÇÃO: Compressão ---
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => { options.Level = CompressionLevel.Fastest; });
builder.Services.Configure<GzipCompressionProviderOptions>(options => { options.Level = CompressionLevel.SmallestSize; });

// Banco de Dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// JWT
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]!);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true
    };
});

// --- DI: Serviços ---
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddSingleton<IHtmlSanitizer, HtmlSanitizer>(s => new HtmlSanitizer());
builder.Services.AddScoped<IShippingService, MelhorEnvioShippingService>();

// --- ESTRATÉGIA DE FRETE ---
// Aqui registramos os provedores. 
// Para adicionar Jadlog no futuro: builder.Services.AddScoped<IShippingService, JadlogShippingService>();
builder.Services.AddScoped<IShippingService, MelhorEnvioShippingService>();

// Ferramentas
builder.Services.AddAutoMapper(typeof(DomainMappingProfile));
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Grafica A Moderna API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Bearer {seu token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        b => b.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseResponseCompression();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Seed inicial
        if (app.Environment.IsDevelopment())
        {
            await DbSeeder.SeedAsync(context, userManager);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erro ao popular banco.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();