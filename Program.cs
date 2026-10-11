using Web2Hand.Common.Extensions;
using Web2Hand.Data.Seed;
using Web2Hand.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Mỗi nhóm cấu hình nằm trong Common/Extensions/ServiceCollectionExtensions.cs
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentityAndAuth();
builder.Services.AddAppServices();
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<AuctionHub>("/hubs/auction");
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notification");

await DbSeeder.SeedAsync(app.Services);

app.Run();
