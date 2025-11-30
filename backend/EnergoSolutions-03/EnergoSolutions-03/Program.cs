using System.Globalization;
using System.Net.Http.Headers;
using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.Agents;
using EnergoSolutions_03.Services;

var builder = WebApplication.CreateBuilder(args);




builder.Services.AddSingleton<IOpenAIService, OpenAIService>();
builder.Services.AddSingleton<IWeatherApiService, WeatherApiService>();
builder.Services.AddSingleton<ISessionManager, SessionManager>();

// Register agents
builder.Services.AddScoped<IDataCollectorAgent, DataCollectorAgent>();
builder.Services.AddScoped<IAnalysisAgent, AnalysisAgent>();
builder.Services.AddScoped<ICalculationAgent, CalculationAgent>();
builder.Services.AddScoped<IReportAgent, ReportAgent>();
builder.Services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();

// Add services to the container.
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("sk-SK"), new CultureInfo("en-US") };
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("sk-SK");
    options.SupportedCultures = supportedCultures.ToList();
    options.SupportedUICultures = supportedCultures.ToList();
});

builder.Services.AddControllers();

builder.Services.AddHttpClient<IGeocodingService, GeocodingService>(client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("GreenEnergyApp/1.0");
});

builder.Services.AddHttpClient<IClimateService, ClimateService>(client =>
{
    client.BaseAddress = new Uri("https://archive-api.open-meteo.com/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("GreenEnergyApp/1.0");
});

builder.Services.AddHttpClient<IWindService, WindService>(client =>
{
    client.BaseAddress = new Uri("https://archive-api.open-meteo.com/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("GreenEnergyApp/1.0");
});

builder.Services.AddHttpClient<ISolarService, SolarService>(client =>
{
    client.BaseAddress = new Uri("https://re.jrc.ec.europa.eu/api/v5_2/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("GreenEnergyApp/1.0");
});

builder.Services.AddHttpClient<IChatService, ChatService>(client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", builder.Configuration["OpenAI:ApiKey"]);
});

builder.Services.AddScoped<ISummaryService, SummaryService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins("10.10.95.105:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Add Swashbuckle/Swagger generator
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo {
        Title = "EnergoSolutions API",
        Version = "v1",
        Description = "API for EnergoSolutions Hackathon project"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if(app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EnergoSolutions API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();