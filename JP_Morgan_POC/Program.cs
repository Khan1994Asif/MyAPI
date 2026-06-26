
using JP_Morgan_POC.Model;
using JP_Morgan_POC.MyContext;
using JP_Morgan_POC.Repositories;
using JP_Morgan_POC.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();


builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IContactService, ContactService>();

builder.Services.Configure<SalesforceSettings>(
    builder.Configuration.GetSection(SalesforceSettings.SectionName));

builder.Services.Configure<SalesforceConfig>(
    builder.Configuration.GetSection("Salesforce"));

//builder.Services.AddSingleton<SalesforceAuthService>(sp =>
//{
//    var config = sp.GetRequiredService<IOptions<SalesforceConfig>>().Value;
//    var factory = sp.GetRequiredService<IHttpClientFactory>();
//    return new SalesforceAuthService(factory.CreateClient(), config);
//});

builder.Services.AddHttpClient<SalesforceAuthService>();

builder.Services.AddCors(option =>
{
    option.AddPolicy("Cors", opt =>
    {
        opt.AllowAnyHeader().AllowAnyMethod().WithOrigins("http://localhost:4200");
    });
});

builder.Services.AddHttpClient<SalesforceClient>();
builder.Services.AddHttpClient<SalesforceMetadataService>();

builder.Services.AddScoped<OutboxRepository>();
builder.Services.AddScoped<SyncControlRepository>();
builder.Services.AddScoped<SalesforceSchemaGuard>();
builder.Services.AddScoped<SyncProcessor>();

builder.Services.AddHostedService<SyncWorker>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Cors");

app.UseAuthorization();

app.MapControllers();

app.Run();
