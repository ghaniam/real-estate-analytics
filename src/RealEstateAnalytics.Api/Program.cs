using RealEstateAnalytics.Api.Configuration;
using RealEstateAnalytics.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.Configure<ExternalApiOptions>(
    builder.Configuration.GetSection(ExternalApiOptions.SectionName));

builder.Services.AddHttpClient<IPropertyService, PropertyService>(client =>
{
    var baseUrl = builder.Configuration[$"{ExternalApiOptions.SectionName}:BaseUrl"]
        ?? "http://partnerapi.funda.nl/feeds/Aanbod.svc/json/";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
