using MediCare.Data;
using MediCare.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Keep logging to console/debug only; avoids Windows EventLog permission issues.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<DoctorService>();
builder.Services.AddScoped<MedicineService>();
builder.Services.AddScoped<AppointmentService>();
builder.Services.AddScoped<PrescriptionService>();
builder.Services.AddScoped<OPDScheduleService>();
builder.Services.AddScoped<FeedbackService>();
builder.Services.AddScoped<PrescriptionTemplateService>();
builder.Services.AddScoped<ClinicBranchService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<UserPreferenceService>();
builder.Services.AddScoped<LoginSessionService>();
builder.Services.AddSingleton<QueueService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Login/Login";
    options.LogoutPath = "/Login/Logout";
})
.AddCookie("External");

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddOAuth("Google", options =>
        {
            options.SignInScheme = "External";
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.CallbackPath = "/signin-google";
            options.AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
            options.TokenEndpoint = "https://oauth2.googleapis.com/token";
            options.UserInformationEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";
            options.Scope.Add("email");
            options.Scope.Add("profile");

            options.Events = new OAuthEvents
            {
                OnCreatingTicket = async context =>
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                    using var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                    response.EnsureSuccessStatusCode();

                    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
                    var root = payload.RootElement;
                    if (root.TryGetProperty("id", out var googleId) && !string.IsNullOrWhiteSpace(googleId.ToString()))
                    {
                        context.Identity?.AddClaim(new Claim(ClaimTypes.NameIdentifier, googleId.ToString()));
                    }
                    if (root.TryGetProperty("name", out var name) && !string.IsNullOrWhiteSpace(name.GetString()))
                    {
                        context.Identity?.AddClaim(new Claim(ClaimTypes.Name, name.GetString()!));
                    }
                    if (root.TryGetProperty("given_name", out var givenName) && !string.IsNullOrWhiteSpace(givenName.GetString()))
                    {
                        context.Identity?.AddClaim(new Claim(ClaimTypes.GivenName, givenName.GetString()!));
                    }
                    if (root.TryGetProperty("family_name", out var familyName) && !string.IsNullOrWhiteSpace(familyName.GetString()))
                    {
                        context.Identity?.AddClaim(new Claim(ClaimTypes.Surname, familyName.GetString()!));
                    }
                    if (root.TryGetProperty("email", out var email) && !string.IsNullOrWhiteSpace(email.GetString()))
                    {
                        context.Identity?.AddClaim(new Claim(ClaimTypes.Email, email.GetString()!));
                    }
                }
            };
        });
}

var githubClientId = builder.Configuration["Authentication:GitHub:ClientId"];
var githubClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];
if (!string.IsNullOrWhiteSpace(githubClientId) && !string.IsNullOrWhiteSpace(githubClientSecret))
{
    builder.Services.AddAuthentication()
        .AddOAuth("GitHub", options =>
        {
            options.SignInScheme = "External";
            options.ClientId = githubClientId;
            options.ClientSecret = githubClientSecret;
            options.CallbackPath = "/signin-github";
            options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
            options.TokenEndpoint = "https://github.com/login/oauth/access_token";
            options.UserInformationEndpoint = "https://api.github.com/user";
            options.Scope.Add("user:email");
            options.ClaimsIssuer = "GitHub";

            options.Events = new OAuthEvents
            {
                OnCreatingTicket = async context =>
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                    request.Headers.UserAgent.ParseAdd("MediCare-App");
                    request.Headers.Accept.ParseAdd("application/vnd.github+json");

                    using var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                    response.EnsureSuccessStatusCode();

                    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
                    var root = payload.RootElement;
                    if (root.TryGetProperty("id", out var githubId) && !string.IsNullOrWhiteSpace(githubId.ToString()))
                    {
                        context.Identity?.AddClaim(new Claim(ClaimTypes.NameIdentifier, githubId.ToString()));
                    }
                    if (root.TryGetProperty("name", out var name) && !string.IsNullOrWhiteSpace(name.GetString()))
                    {
                        context.Identity?.AddClaim(new Claim(ClaimTypes.Name, name.GetString()!));
                    }
                    if (root.TryGetProperty("login", out var login) && !string.IsNullOrWhiteSpace(login.GetString()))
                    {
                        context.Identity?.AddClaim(new Claim("urn:github:login", login.GetString()!));
                    }
                    if (root.TryGetProperty("html_url", out var htmlUrl) && !string.IsNullOrWhiteSpace(htmlUrl.GetString()))
                    {
                        context.Identity?.AddClaim(new Claim("urn:github:url", htmlUrl.GetString()!));
                    }
                    if (root.TryGetProperty("email", out var email) && !string.IsNullOrWhiteSpace(email.GetString()))
                    {
                        context.Identity?.AddClaim(new Claim(ClaimTypes.Email, email.GetString()!));
                    }

                    if (!context.Identity!.HasClaim(c => c.Type == ClaimTypes.Email))
                    {
                        using var emailRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
                        emailRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                        emailRequest.Headers.UserAgent.ParseAdd("MediCare-App");
                        emailRequest.Headers.Accept.ParseAdd("application/vnd.github+json");

                        using var emailResponse = await context.Backchannel.SendAsync(emailRequest, context.HttpContext.RequestAborted);
                        if (emailResponse.IsSuccessStatusCode)
                        {
                            using var emailJson = JsonDocument.Parse(await emailResponse.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
                            var primaryEmail = emailJson.RootElement.EnumerateArray()
                                .FirstOrDefault(x => x.TryGetProperty("primary", out var primary) && primary.GetBoolean() && x.TryGetProperty("email", out var email) && !string.IsNullOrWhiteSpace(email.GetString()));

                            if (primaryEmail.ValueKind != JsonValueKind.Undefined && primaryEmail.TryGetProperty("email", out var primaryEmailValue))
                            {
                                context.Identity.AddClaim(new Claim(ClaimTypes.Email, primaryEmailValue.GetString() ?? string.Empty));
                            }
                        }
                    }
                }
            };
        });
}

// Add Session Service
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.ExecuteSqlRaw(
        @"CREATE TABLE IF NOT EXISTS ""UserPreferences"" (
            ""Id"" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            ""UserEmail"" text NOT NULL,
            ""PushNotificationsEnabled"" boolean NOT NULL DEFAULT TRUE,
            ""TwoFactorEnabled"" boolean NOT NULL DEFAULT FALSE
        );");
    context.Database.ExecuteSqlRaw(
        @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UserPreferences_UserEmail""
          ON ""UserPreferences"" (""UserEmail"");");
    context.Database.ExecuteSqlRaw(
        @"CREATE TABLE IF NOT EXISTS ""LoginSessionRecords"" (
            ""Id"" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            ""UserEmail"" text NOT NULL,
            ""UserName"" text NOT NULL,
            ""Role"" text NOT NULL,
            ""LoginAt"" timestamp with time zone NOT NULL,
            ""LogoutAt"" timestamp with time zone NULL,
            ""IsActive"" boolean NOT NULL DEFAULT TRUE
        );");
    context.Database.ExecuteSqlRaw(
        @"CREATE INDEX IF NOT EXISTS ""IX_LoginSessionRecords_UserEmail_IsActive""
          ON ""LoginSessionRecords"" (""UserEmail"", ""IsActive"");");
    context.Database.ExecuteSqlRaw(
        @"CREATE TABLE IF NOT EXISTS ""ClinicBranches"" (
            ""Id"" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            ""Name"" text NOT NULL,
            ""Location"" text NULL,
            ""CreatedAt"" timestamp with time zone NOT NULL
        );");
    context.Database.ExecuteSqlRaw(
        @"ALTER TABLE IF EXISTS ""PrescriptionTemplates""
          ADD COLUMN IF NOT EXISTS ""EntryType"" text NOT NULL DEFAULT 'Template';");
    context.Database.ExecuteSqlRaw(@"ALTER TABLE IF EXISTS ""Medicines"" ALTER COLUMN ""GenericName"" DROP NOT NULL;");
    context.Database.ExecuteSqlRaw(@"ALTER TABLE IF EXISTS ""Medicines"" ALTER COLUMN ""Category"" DROP NOT NULL;");
    context.Database.ExecuteSqlRaw(@"ALTER TABLE IF EXISTS ""Medicines"" ALTER COLUMN ""DosageForm"" DROP NOT NULL;");
    context.Database.ExecuteSqlRaw(@"ALTER TABLE IF EXISTS ""Medicines"" ALTER COLUMN ""Strength"" DROP NOT NULL;");
    context.Database.ExecuteSqlRaw(@"ALTER TABLE IF EXISTS ""Medicines"" ALTER COLUMN ""PackSize"" DROP NOT NULL;");
    context.Database.ExecuteSqlRaw(@"ALTER TABLE IF EXISTS ""Medicines"" ALTER COLUMN ""Manufacturer"" DROP NOT NULL;");
    context.Database.ExecuteSqlRaw(@"ALTER TABLE IF EXISTS ""Medicines"" ALTER COLUMN ""Supplier"" DROP NOT NULL;");
    context.Database.ExecuteSqlRaw(@"ALTER TABLE IF EXISTS ""Medicines"" ALTER COLUMN ""Unit"" DROP NOT NULL;");
    context.Database.ExecuteSqlRaw(@"ALTER TABLE IF EXISTS ""Medicines"" ALTER COLUMN ""Storage"" DROP NOT NULL;");
    context.Database.ExecuteSqlRaw(@"ALTER TABLE IF EXISTS ""Medicines"" ALTER COLUMN ""Usage"" DROP NOT NULL;");
    context.Database.ExecuteSqlRaw(@"ALTER TABLE IF EXISTS ""Medicines"" ALTER COLUMN ""SideEffects"" DROP NOT NULL;");
    context.Database.ExecuteSqlRaw(@"ALTER TABLE IF EXISTS ""Medicines"" ALTER COLUMN ""Instructions"" DROP NOT NULL;");
    context.Database.ExecuteSqlRaw(@"ALTER TABLE IF EXISTS ""Medicines"" ALTER COLUMN ""PrescriptionRequired"" DROP NOT NULL;");

    if (!context.Users.Any(u => u.Email == "staff@gmail.com"))
    {
        context.Users.Add(new MediCare.Models.AppUser { FirstName = "Default", LastName = "Staff", Email = "staff@gmail.com", Phone = "1234567890", Password = "123", Role = "Staff", Status = "Approved" });
    }
    if (!context.Users.Any(u => u.Email == "doctor@gmail.com"))
    {
        context.Users.Add(new MediCare.Models.AppUser { FirstName = "Default", LastName = "Doctor", Email = "doctor@gmail.com", Phone = "0987654321", Password = "123", Role = "Doctor", Status = "Approved" });
    }
    context.SaveChanges();
}

app.Run();
