using Microsoft.EntityFrameworkCore;
using Teste.Data;
using Teste.Messaging;
using Teste.Services;
using Teste.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=pedidos.db"));

var apiBus = new MessageBus();
var emailBus = new MessageBus();
var compositeBus = new CompositeMessageBus();
compositeBus.Subscribe(apiBus);
compositeBus.Subscribe(emailBus);
builder.Services.AddSingleton(compositeBus);

builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<EmailService>();

builder.Services.AddHostedService(sp =>
    new ExternalApiWorker(apiBus, sp.GetRequiredService<ILogger<ExternalApiWorker>>()));
builder.Services.AddHostedService(sp =>
    new EmailWorker(
        emailBus,
        sp.GetRequiredService<ILogger<EmailWorker>>(),
        sp.GetRequiredService<IServiceScopeFactory>()));

builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();