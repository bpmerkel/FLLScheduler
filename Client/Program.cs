// Create a WebAssemblyHostBuilder with default configuration
var builder = WebAssemblyHostBuilder.CreateDefault(args);
// Add the root component "App" to the builder
builder.RootComponents.Add<App>("#app");
// Add the root component "HeadOutlet" to the builder
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddHttpClient("API", client => client.BaseAddress = new Uri(builder.Configuration["API_Prefix"] ?? builder.HostEnvironment.BaseAddress));
builder.Services.AddBlazorDownloadFile(ServiceLifetime.Scoped);
// Add MudBlazor services to the builder
builder.Services.AddMudServices();

// Build and run the WebAssembly host
var app = builder.Build();
await app.RunAsync();