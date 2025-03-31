using Microsoft.Extensions.Options;
using System.Text;
using VirginMedia.TechTests.SalesSummary.Components;
using VirginMedia.TechTests.SalesSummary.Services;

// The source file we're testing with uses a legacy encoding not supported .NET by default: Windows-1252
// The System.Text.Encoding.CodePages package provides access to additional encodings and the following
// invocation registers the associated provider with the encoding subsystem.
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddTransient<ISalesSource, CsvFileSalesSource>();
builder.Services
    .AddOptions<CsvFileSalesSourceOptions>()
    .BindConfiguration("SalesData");
builder.Services.AddTransient<IPostConfigureOptions<CsvFileSalesSourceOptions>, PostConfigureCsvFileSalesSourceOptions>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();

app.Run();
