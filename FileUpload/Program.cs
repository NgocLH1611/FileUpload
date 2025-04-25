using FileUpload.Helper;
using FileUpload.Data;
using FileUpload.Entities;
using Microsoft.EntityFrameworkCore;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Firebase.Storage;
using Google.Cloud.Storage.V1;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://*:80", "http://*:443", "http://*:901");

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IUploadHandler, UploadHandler>();
builder.Services.AddScoped<ISignedUrl, SignedUrl>();
builder.Services.AddScoped<IStorageService, StorageService>();

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.Configure<FirebaseConfig>(builder.Configuration.GetSection("FirebaseConfig"));

var firebaseCredentialPath = Path.Combine(Directory.GetCurrentDirectory(), "Firebase", "firebase-service-account.json");

//var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
//var dbName = Environment.GetEnvironmentVariable("DB_NAME");
//var dbPassword = Environment.GetEnvironmentVariable("DB_SA_PASSWORD");
//var connectionString = $"Data Source={dbHost};Initial Catalog={dbName};User ID=sa;Password={dbPassword};Encrypt=False;TrustServerCertificate=True";
//builder.Services.AddDbContext<ApplicationDbContext>(opt => opt.UseSqlServer(connectionString));

if (!System.IO.File.Exists(firebaseCredentialPath))
{
    throw new FileNotFoundException("Firebase service account file not found", firebaseCredentialPath);
}

var credential = GoogleCredential.FromFile(firebaseCredentialPath);
FirebaseApp.Create(new AppOptions()
{
    Credential = credential,
});

builder.Services.AddSingleton(StorageClient.Create(credential));

//builder.Services.Configure<FileUpload.Entities.FirebaseStorageOptions>(builder.Configuration.GetSection("Firebase"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FileUpload API", Version = "v1" });
    c.AddServer(new Microsoft.OpenApi.Models.OpenApiServer
    {
        Url = "http://localhost:901"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
