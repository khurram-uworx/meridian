using Microsoft.EntityFrameworkCore;
using Uworx.Meridian;
using Uworx.Meridian.Configuration;
using Uworx.Meridian.CourseSource;
using Uworx.Meridian.Infrastructure;
using Uworx.Meridian.Infrastructure.CourseSource;
using Uworx.Meridian.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<JiraOptions>(builder.Configuration.GetSection(JiraOptions.Jira));

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<MeridianDbContext>(options =>
    options.UseInMemoryDatabase("MeridianDb"));

builder.Services.AddScoped<ICourseSourceResolver, CourseSourceResolver>();
builder.Services.AddScoped<ICourseConfigParser, CourseConfigParser>();
builder.Services.AddScoped<ISectionParser, SectionParser>();
builder.Services.AddScoped<ICourseParser, CourseParser>();
builder.Services.AddScoped<IJiraService, JiraService>();

var app = builder.Build();

// Validate Jira configuration
var jiraOptions = builder.Configuration.GetSection(JiraOptions.Jira).Get<JiraOptions>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

if (string.IsNullOrWhiteSpace(jiraOptions?.BaseUrl) || jiraOptions.BaseUrl == "https://your-domain.atlassian.net")
{
    logger.LogWarning("Jira:BaseUrl is missing or using default value.");
}
if (string.IsNullOrWhiteSpace(jiraOptions?.ApiToken) || jiraOptions.ApiToken == "your-jira-api-token")
{
    logger.LogWarning("Jira:ApiToken is missing or using default value.");
}
if (string.IsNullOrWhiteSpace(jiraOptions?.UserEmail) || jiraOptions.UserEmail == "you@company.com")
{
    logger.LogWarning("Jira:UserEmail is missing or using default value.");
}
if (string.IsNullOrWhiteSpace(jiraOptions?.ProjectKey) || jiraOptions.ProjectKey == "LEARN")
{
    logger.LogWarning("Jira:ProjectKey is missing or using default value.");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
