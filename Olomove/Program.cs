using Microsoft.EntityFrameworkCore;
using Olomove.Data;
using Olomove.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// configure PostgreSQL database access
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// configure ASP.NET Core Identity for users, roles and password management
builder.Services
    .AddIdentity<User, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// The Nuxt app and API use different local origins, so the authentication
// cookie must be allowed on credentialed requests from the frontend.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// allow the separate Nuxt frontend to send authenticated requests to the API
builder.Services.AddCors(options => options.AddPolicy("Nuxt", policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

await IdentitySeeder.SeedAsync(app.Services, builder.Configuration);

// configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// enable cross-origin requests and identity cookies before authorization
app.UseCors("Nuxt");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
