using MyApp.API.Endpoints;
using MyApp.API.Extensions;
using MyApp.Application;
using MyApp.Application.Security;
using MyApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is required.");
var jwtSection = builder.Configuration.GetRequiredSection("Jwt");
var jwtOptions = new JwtOptions(
    jwtSection.GetValue<string>("Key")
        ?? throw new InvalidOperationException("Configuration value 'Jwt:Key' is required."),
    jwtSection.GetValue<string>("Issuer")
        ?? throw new InvalidOperationException("Configuration value 'Jwt:Issuer' is required."),
    jwtSection.GetValue<string>("Audience")
        ?? throw new InvalidOperationException("Configuration value 'Jwt:Audience' is required."));
var componentPdfTemplatePath = builder.Configuration.GetValue<string>(
    "DocumentTemplates:ComponentIssuePath")
    ?? "/mnt/dietpi_userdata/Document/t.pdf";
var componentPdfFontPath = builder.Configuration.GetValue<string>(
    "DocumentTemplates:FontPath")
    ?? "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf";

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    connectionString,
    jwtOptions,
    componentPdfTemplatePath,
    componentPdfFontPath);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

await app.Services.InitializeDatabaseAsync();

app.MapAuthEndpoints();
app.MapAdminUserEndpoints();
app.MapProfessionEndpoints();
app.MapTableViewEndpoints();
app.MapComponentDocumentEndpoints();

app.Run();
