using GraficaModerna.API.Data;
using GraficaModerna.API.Middlewares; // Novo Middleware
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
using Ganss.Xss; // Novo Sanitizador

// 1. CRIA O BUILDER
var builder = WebApplication.CreateBuilder(args);

// ==========================================
// CONFIGURAÇÃO DE SERVIÇOS (DI)
// ==========================================

// Banco de Dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity (Autenticação e Usuários)
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

// Autenticação JWT
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

// --- Injeção de Dependência ---

// Serviços de Domínio/Aplicação
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Repositórios
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Serviços de Infraestrutura
builder.Services.AddHttpContextAccessor(); // Necessário para gerar URLs absolutas
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// Segurança: Sanitizador HTML (Singleton é suficiente para este serviço)
builder.Services.AddSingleton<IHtmlSanitizer, HtmlSanitizer>(s => new HtmlSanitizer());

// Ferramentas
builder.Services.AddAutoMapper(typeof(DomainMappingProfile));

// Validação (FluentValidation)
// Isso registra todos os validadores no assembly, incluindo CreateProductValidator e ContentPageValidator
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger Configurado com Suporte a JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Grafica A Moderna API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT desta maneira: Bearer {seu token}",
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
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// CORS (Permitir Frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        b => b.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ==========================================
// 2. CONSTRÓI O APP
// ==========================================
var app = builder.Build();

// SEGURANÇA: Middleware de Exceção Global (Deve ser o primeiro ou um dos primeiros)
app.UseMiddleware<ExceptionMiddleware>();

// ==========================================
// SEEDER DE BANCO DE DADOS (Inicialização)
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await DbSeeder.SeedAsync(context, userManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Um erro ocorreu ao popular o banco de dados.");
    }
}

// ==========================================
// PIPELINE DE EXECUÇÃO
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(); // Importante para servir as imagens
app.UseCors("AllowFrontend");

app.UseAuthentication(); // Quem é você?
app.UseAuthorization();  // O que você pode fazer?

app.MapControllers();

app.Run();