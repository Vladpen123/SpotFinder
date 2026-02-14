using Confluent.Kafka;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using SpotFinder.Shared.Constants;
using SpotFinder.Shared.Protos;
using SpotFinder.Web.Components;
using SpotFinder.Web.Data;
using SpotFinder.Web.Services;
using SpotFinder.Web.Workers;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<AuthDbContext>("identitydb");
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication()
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        // Читаем из переменных среды (которые передал AppHost)
        var clientId = builder.Configuration["Authentication:Google:ClientId"];
        var clientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

        // Проверка на случай, если секреты не дошли
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException("Google Secrets are missing! Check configuration.");
        }

        options.ClientId = clientId;
        options.ClientSecret = clientSecret;

        options.SignInScheme = IdentityConstants.ApplicationScheme;
    });



builder.Services.AddGrpcClient<Geo.GeoClient>(o => o.Address = new Uri("https://geo"));
builder.Services.AddGrpcClient<Booking.BookingClient>(o => o.Address = new Uri("https://booking"));
builder.Services.AddGrpcClient<Payment.PaymentClient>(o => o.Address = new Uri("https://payment"));

builder.Services.AddGrpc();
builder.AddKafkaConsumer<string, string>("kafka", settings =>
{
    settings.Config.GroupId = "web-ui-group"; // Уникальная группа для UI
    settings.Config.AutoOffsetReset = AutoOffsetReset.Earliest;
});

builder.Services.AddSingleton<NotificationService>();
builder.Services.AddHostedService<KafkaNotificationWorker>();
builder.Services.AddScoped<MapStateService>();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();


var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    db.Database.EnsureCreated();
}

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/google-login",async (SignInManager<ApplicationUser> signInManager) =>
{
    var redirectUrl = "/auth/callback";
    var properties = signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, redirectUrl);
    return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
});

app.Map("auth/callback/", async (
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) =>
{
    var info = await signInManager.GetExternalLoginInfoAsync();
    if (info == null)
        return Results.Redirect("/");

    var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, true, true);
    if (result.Succeeded)
        return Results.Redirect("/");

    var email = info.Principal.FindFirstValue(ClaimTypes.Email);
    if (email is not null)
    {
        var user = new ApplicationUser { UserName = email, Email = email };
        var createResult = await userManager.CreateAsync(user);
        if (createResult.Succeeded)
        {
            await userManager.AddLoginAsync(user, info);
            await signInManager.SignInAsync(user, isPersistent: true);
            return Results.Redirect("/");
        };
    }
    return Results.Redirect("/");

});

app.MapGet("/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();