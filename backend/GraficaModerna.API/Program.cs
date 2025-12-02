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
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURAÇÃO DE AMBIENTE ---
builder.Configuration.AddEnvironmentVariables();

var jwtKey = builder.Configuration["Jwt:Key"];

// Validação de Segurança (Só falha em produção se não tiver chave)
if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new Exception("FATAL: A chave JWT (Jwt:Key) não está configurada ou é insegura.");
    }
}

// --- 2. INFRAESTRUTURA ---
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// Compressão
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// Rate Limiting (Proteção contra ataques de força bruta)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions { AutoReplenishment = true, PermitLimit = 300, QueueLimit = 2, Window = TimeSpan.FromMinutes(1) }));

    options.AddPolicy("AuthPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "auth",
            factory: _ => new FixedWindowRateLimiterOptions { AutoReplenishment = true, PermitLimit = 10, QueueLimit = 0, Window = TimeSpan.FromMinutes(1) }));
});

// Banco de Dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// --- 3. AUTENTICAÇÃO SEGURA (COOKIES) ---
var key = Encoding.ASCII.GetBytes(jwtKey!);
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
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Extrai o token do Cookie automaticamente
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey("jwt"))
            {
                context.Token = context.Request.Cookies["jwt"];
            }
            return Task.CompletedTask;
        }
    };
});

// Injeção de Dependências
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IShippingService, MelhorEnvioShippingService>();
builder.Services.AddScoped<IEmailService, ConsoleEmailService>();
builder.Services.AddSingleton<IHtmlSanitizer, HtmlSanitizer>(s => new HtmlSanitizer());
builder.Services.AddAutoMapper(typeof(DomainMappingProfile));
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Grafica API", Version = "v1" });
});

// CORS: Configuração vital para o Frontend acessar o Backend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", b => b
        .WithOrigins("http://localhost:5173", "http://localhost:3000") // Permite o seu Frontend
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()); // Permite o envio dos Cookies de Segurança
});

var app = builder.Build();

// --- 4. PIPELINE DE EXECUÇÃO (A ordem importa!) ---

app.UseMiddleware<ExceptionMiddleware>();

// 1º: CORS deve vir antes de tudo para garantir que o navegador aceite a resposta
app.UseCors("AllowFrontend");

app.UseResponseCompression();

// Só força HTTPS se NÃO estiver em desenvolvimento
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRateLimiter();

// Seeding do Banco
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        if (app.Environment.IsDevelopment()) await DbSeeder.SeedAsync(context, userManager, config);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro no Seed: {ex.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();