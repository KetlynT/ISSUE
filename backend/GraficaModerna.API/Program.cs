using GraficaModerna.API.Middlewares;
using GraficaModerna.Application.Interfaces;
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
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Http.Resilience; // NOVO: Pacote moderno de resiliência

var builder = WebApplication.CreateBuilder(args);

// 1. Carrega variáveis de ambiente
if (builder.Environment.IsDevelopment())
{
    DotNetEnv.Env.Load();
}
builder.Configuration.AddEnvironmentVariables();

// 2. Configura Headers de Proxy (Importante para IP real no Rate Limit)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// 3. SEGURANÇA: Validação Crítica da Chave JWT
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
{
    throw new Exception("FATAL: JWT_SECRET_KEY não configurada ou insegura (mínimo 32 caracteres).");
}

// 4. Serviços Básicos
builder.Services.AddHttpClient(); // Cliente genérico
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// Redis (Opcional/Configurável)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "GraficaModerna_";
});

// Compressão Gzip/Brotli
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// 5. CONFIGURAÇÃO DE CLIENTES HTTP COM RESILIÊNCIA (NOVO)
// Cliente do Melhor Envio blindado contra falhas e lentidão
builder.Services.AddHttpClient("MelhorEnvio", client =>
{
    var url = Environment.GetEnvironmentVariable("MELHOR_ENVIO_URL")
              ?? builder.Configuration["MelhorEnvio:Url"]
              ?? "https://melhorenvio.com.br/api/v2/";

    client.BaseAddress = new Uri(url);
    client.DefaultRequestHeaders.Add("Accept", "application/json");

    var userAgent = builder.Configuration["MelhorEnvio:UserAgent"] ?? "GraficaModernaAPI/1.0";
    client.DefaultRequestHeaders.Add("User-Agent", userAgent);
})
.AddStandardResilienceHandler(options =>
{
    // Configurações otimizadas de resiliência
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10); // Proteção Slow Loris
    options.Retry.MaxRetryAttempts = 3;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
});

// 6. SEGURANÇA: Rate Limiting Granular
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // A. Limite Global (Padrão)
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 300,
                QueueLimit = 2,
                Window = TimeSpan.FromMinutes(1)
            }));

    // B. Auth (Login/Register) - Estrito para evitar Brute Force
    options.AddPolicy("AuthPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(5)
            }));

    // C. Uploads - Muito Estrito (Disco/CPU Heavy)
    options.AddPolicy("UploadPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));

    // D. Frete (Shipping) - Moderado (Custo de API externa)
    options.AddPolicy("ShippingPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 15,
                QueueLimit = 2,
                Window = TimeSpan.FromMinutes(1)
            }));

    // E. Pagamentos - Restrito (Segurança Financeira)
    options.AddPolicy("PaymentPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// 7. Banco de Dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("GraficaModerna.Infrastructure")));

// 8. Identity (Senhas Fortes)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, GraficaModerna.Infrastructure.Security.PepperedPasswordHasher<ApplicationUser>>();
// 9. Autenticação JWT
var key = Encoding.ASCII.GetBytes(jwtKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
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

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var blacklistService = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();
            if (context.SecurityToken is System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwtToken)
            {
                if (await blacklistService.IsTokenBlacklistedAsync(jwtToken.RawData))
                {
                    context.Fail("Token revogado.");
                }
            }
        }
    };
});

// 10. Injeção de Dependências
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<ICouponRepository, CouponRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IContentRepository, ContentRepository>();

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IPaymentService, StripePaymentService>();
builder.Services.AddScoped<IContentService, ContentService>();

builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IShippingService, MelhorEnvioShippingService>();
builder.Services.AddScoped<IEmailService, ConsoleEmailService>();
builder.Services.AddSingleton<IHtmlSanitizer, HtmlSanitizer>(s => new HtmlSanitizer());

builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddTransient<JwtValidationMiddleware>();

// 11. Swagger Config
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Grafica API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Insira o token JWT (Authorization: Bearer <token>)",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, new string[] { } }
    });
});

// 12. CORS Config
var allowedOrigins = builder.Configuration.GetSection("AllowedHosts").Get<string[]>()
                     ?? new[] { "http://localhost:5173", "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", b => b
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

// ================= PIPELINE DE REQUISIÇÃO =================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Grafica API v1");
        c.RoutePrefix = "swagger";
    });
}

// 1. Headers de Proxy e Segurança (Primeiro)
app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// 2. Exception Middleware
app.UseMiddleware<ExceptionMiddleware>();

// 3. CORS
app.UseCors("AllowFrontend");

if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
    app.UseHttpsRedirection();
}

// 4. Rate Limiter (Antes da Autenticação para proteger Auth endpoints)
app.UseRateLimiter();

app.UseStaticFiles();

// 5. Autenticação e Autorização
app.UseAuthentication();
app.UseMiddleware<JwtValidationMiddleware>();
app.UseAuthorization();

// 6. Controllers
app.MapControllers();

// ================= INICIALIZAÇÃO DE DADOS =================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        db.Database.Migrate();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var roles = new[] { "Admin", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? app.Configuration["Admin:Email"];
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? app.Configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            if (app.Environment.IsDevelopment())
            {
                var guidPart = Guid.NewGuid().ToString("N").Substring(0, 12);
                var tempPassword = $"Adm!{guidPart}A1";
                adminEmail ??= "admin.local@local.local";
                adminPassword ??= tempPassword;
                logger.LogInformation("Admin temporário criado (Development): {Email} / {Password}", adminEmail, tempPassword);
            }
        }

        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true, FullName = "Administrador" };
                var res = await userManager.CreateAsync(adminUser, adminPassword);
                if (res.Succeeded) await userManager.AddToRoleAsync(adminUser, "Admin");
            }
            else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Seed de Páginas Padrão
        if (!db.ContentPages.Any())
        {
            var defaultPages = new List<GraficaModerna.Domain.Entities.ContentPage>
            {
                new GraficaModerna.Domain.Entities.ContentPage { Title = "Sobre Nós", Slug = "about-us", Content = "<h1>Sobre</h1><p>...</p>", LastUpdated = DateTime.UtcNow },
                new GraficaModerna.Domain.Entities.ContentPage { Title = "Política de Privacidade", Slug = "privacy-policy", Content = "<h1>Privacidade</h1><p>...</p>", LastUpdated = DateTime.UtcNow },
                new GraficaModerna.Domain.Entities.ContentPage { Title = "Termos de Uso", Slug = "terms-of-use", Content = "<h1>Termos</h1><p>...</p>", LastUpdated = DateTime.UtcNow }
            };
            await db.ContentPages.AddRangeAsync(defaultPages);
            await db.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro na inicialização do banco de dados.");
    }
}

app.Run();