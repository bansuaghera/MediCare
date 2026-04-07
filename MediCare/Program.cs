using MediCare.Services;
using MediCare.Data;
using Microsoft.EntityFrameworkCore;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<NotificationService>();
builder.Services.AddSingleton<QueueService>();

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

// ✅ Use Session BEFORE Authorization
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
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
