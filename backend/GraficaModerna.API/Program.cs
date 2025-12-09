using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using Ganss.Xss;
using GraficaModerna.API.Middlewares;
using GraficaModerna.Application.Interfaces;
using GraficaModerna.Application.Services;
using GraficaModerna.Application.Validators;
using GraficaModerna.Domain.Entities;
using GraficaModerna.Domain.Interfaces;
using GraficaModerna.Infrastructure.Context;
using GraficaModerna.Infrastructure.Repositories;
using GraficaModerna.Infrastructure.Security;
using GraficaModerna.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
    DotNetEnv.Env.Load();

builder.Configuration.AddEnvironmentVariables();

var jwtKey = Env.Required("JWT_SECRET_KEY", 64);
var melhorEnvioUrl = Env.Required("MELHOR_ENVIO_URL");
var melhorEnvioToken = Env.Required("MELHOR_ENVIO_TOKEN");
var melhorEnvioUserAgent = Env.Required("MELHOR_ENVIO_USER_AGENT");
var defaultConnection = Env.Required("ConnectionStrings__DefaultConnection");
var stripeSecretKey = Env.Required("Stripe__SecretKey");
var stripeWebhookSecret = Env.Required("Stripe__WebhookSecret");
var adminEmail = Env.Required("ADMIN_EMAIL");
var adminPassword = Env.Required("ADMIN_PASSWORD");
var pepperActiveVersion = Env.Required("Security_PepperRotation_ActiveVersion");
var pepperV1 = Env.Required("Security_PepperRotationPeppers_v1");
var corsOriginsRaw = Env.Required("CorsOrigins");
var metadataEncKey = Env.Required("METADATA_ENC_KEY");
var metadataHmacKey = Env.Required("METADATA_HMAC_KEY");
var smtpHost = Env.Required("SMTP_HOST");
var smtpPort = Env.RequiredInt("SMTP_PORT");
var smtpUsername = Env.Required("SMTP_USERNAME");
var smtpPassword = Env.Required("SMTP_PASSWORD");
var smtpFromEmail = Env.Required("SMTP_FROM_EMAIL");
var smtpFromName = Env.Required("SMTP_FROM_NAME");
var frontendUrl = Env.Required("FRONTEND_URL");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

builder.Services.AddStackExchangeRedisCache(o =>
{
    o.Configuration = "localhost:6379";
    o.InstanceName = "GraficaModerna_";
});

builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<GzipCompressionProvider>();
});

builder.Services.AddHttpClient("MelhorEnvio", client =>
{
    client.BaseAddress = new Uri(melhorEnvioUrl);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.UserAgent.ParseAdd(melhorEnvioUserAgent);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", melhorEnvioToken);
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 300,
                QueueLimit = 2,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddPolicy("AuthPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(5)
            }));

    options.AddPolicy("UploadPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddPolicy("ShippingPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 15,
                QueueLimit = 2,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddPolicy("PaymentPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.AddPolicy("AdminPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 20,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddPolicy("UserActionPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 60,
                QueueLimit = 2,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.AddPolicy("WebhookPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 20,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.AddPolicy("StrictPaymentPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(5)
            }));
});


builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(defaultConnection, b => b.MigrationsAssembly("GraficaModerna.Infrastructure")));

builder.Services.Configure<PepperSettings>(o =>
{
    o.ActiveVersion = pepperActiveVersion;
    o.Peppers = new Dictionary<string, string> { { "v1", pepperV1 } };
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, PepperedPasswordHasher>();

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

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

builder.Services.AddScoped<IShippingService, MelhorEnvioShippingService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<MetadataSecurityService>();

builder.Services.AddValidatorsFromAssemblyContaining<ProductValidator>();
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "Grafica API", Version = "v1" });
});

var allowedOrigins = corsOriginsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(o =>
{
    o.AddPolicy("CorsPolicy", p =>
        p.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("CorsPolicy");
app.UseResponseCompression();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static class Env
{
    public static string Required(string name, int? minLength = null)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new Exception($"FATAL: {name} não configurada.");
        if (minLength.HasValue && value.Length < minLength.Value)
            throw new Exception($"FATAL: {name} insegura.");
        return value;
    }

    public static int RequiredInt(string name)
    {
        var value = Required(name);
        if (!int.TryParse(value, out var n))
            throw new Exception($"FATAL: {name} inválida.");
        return n;
    }
}
