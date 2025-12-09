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
})
.AddStandardResilienceHandler(options =>
 {

     options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
     options.Retry.MaxRetryAttempts = 3;
     options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(20);
     options.CircuitBreaker.FailureRatio = 0.5;
     options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
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

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
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

builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, PepperedPasswordHasher>();

var key = Encoding.UTF8.GetBytes(jwtKey);

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
            OnMessageReceived = context => { return Task.CompletedTask; },
            OnTokenValidated = async context =>
            {
                var blacklistService = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();
                if (context.SecurityToken is JwtSecurityToken jwtToken)
                    if (await blacklistService.IsTokenBlacklistedAsync(jwtToken.RawData))
                        context.Fail("Token revogado.");
            }
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
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Grafica API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "img-src 'self' data: https:; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "font-src 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'none';");
    await next();
});

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("CorsPolicy");
app.UseResponseCompression();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

internal static class SanitizerRules
{
    public static readonly string[] AllowedTags =
    [
        "p", "h1", "h2", "h3", "h4", "h5", "h6", "br", "hr",
        "b", "strong", "i", "em", "u", "s", "strike", "sub", "sup",
        "div", "span", "blockquote", "pre", "code",
        "ul", "ol", "li", "dl", "dt", "dd",
        "table", "thead", "tbody", "tfoot", "tr", "th", "td",
        "a", "img"
    ];

    public static readonly string[] AllowedAttributes =
    [
        "class", "id", "style", "title", "alt",
        "href", "target", "rel",
        "src", "width", "height",
        "colspan", "rowspan", "align", "valign"
    ];

    public static readonly string[] AllowedCssProperties =
    [
        "text-align", "padding", "margin", "color", "background-color",
        "font-size", "font-weight", "text-decoration", "width", "height"
    ];
}
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
