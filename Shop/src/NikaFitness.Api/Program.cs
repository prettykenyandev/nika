using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NikaFitness.Api.Common;
using NikaFitness.Api.Endpoints;
using NikaFitness.Application;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults (telemetry, health checks, service discovery).
builder.AddServiceDefaults();

// Caller context + auth helpers.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<TokenService>();

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations();

// Problem-details based error handling.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Application + Infrastructure.
builder.Services.AddApplication();
builder.AddInfrastructure();

// Authentication / Authorization.
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name
        };
    });
builder.Services.AddAuthorization();

// CORS for the Next.js storefront.
const string StorefrontCors = "storefront";
var storefrontOrigin = builder.Configuration["Cors:StorefrontOrigin"] ?? "http://localhost:3000";
builder.Services.AddCors(options =>
    options.AddPolicy(StorefrontCors, policy => policy
        .WithOrigins(storefrontOrigin)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders(CartEndpoints.CartIdHeader)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(StorefrontCors);

// Serve uploaded files (receipts, logos, document assets) from a dedicated folder that
// is guaranteed to exist, independent of the default wwwroot.
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapCatalogEndpoints();
app.MapCartEndpoints();
app.MapOrderEndpoints();
app.MapAdminEndpoints();
app.MapSettingsEndpoints();
app.MapBillEndpoints();
app.MapInvoiceEndpoints();
app.MapVendorEndpoints();
app.MapPurchaseOrderEndpoints();
app.MapCustomerEndpoints();
app.MapReportsEndpoints();
app.MapStaffEndpoints();
app.MapAuditEndpoints();
app.MapPaymentWebhookEndpoints();

await app.SeedDatabaseAsync();

app.Run();
