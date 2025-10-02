using Microsoft.EntityFrameworkCore;
using Quartz;
using SAP_API.Data;
using SAP_API.Model;
using SAP_API.Service;
using System;

var builder = WebApplication.CreateBuilder(args);
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine(connectionString);
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<APISetting>(
    builder.Configuration.GetSection("APISetting"));
builder.Services.Configure<APISyn>(
    builder.Configuration.GetSection("APISyn"));
builder.Services.Configure<APIEcomaint>(
    builder.Configuration.GetSection("APIEcomaint"));
builder.Services.AddScoped<OWTRService>();
builder.Services.AddScoped<ItemService>();
builder.Services.AddSingleton<SapDiApiHelper>();

builder.Services.AddScoped<SapInvoiceService>();
builder.Services.AddQuartz(q =>
{
    var jobItem = new JobKey("jobItem");
    q.AddJob<JobItem>(opts => opts.WithIdentity(jobItem));
    q.AddTrigger(opts => opts
        .ForJob(jobItem)
        .WithIdentity("jobItem-trigger")
        .WithCronSchedule("0 */10 * * * ?")
    );

    var jobItemPrice = new JobKey("jobItemPrice");
    q.AddJob<JobPriceItem>(opts => opts.WithIdentity(jobItemPrice));
    q.AddTrigger(opts => opts
        .ForJob(jobItemPrice)
        .WithIdentity("jobItemPrice-trigger")
        .WithCronSchedule("0 0 7 * * ?")
    );

    var jobItemSyn = new JobKey("jobItemSyn");
    q.AddJob<JobItemSyn>(opts => opts.WithIdentity(jobItemSyn));
    q.AddTrigger(opts => opts
        .ForJob(jobItemSyn)
        .WithIdentity("jobItemSyn-trigger")
        .WithCronSchedule("0 */1 * * * ?")
    );

    var jobGoodIssue = new JobKey("jobGoodIssue");
    q.AddJob<JobGoodissue>(opts => opts.WithIdentity(jobGoodIssue));
    q.AddTrigger(opts => opts
        .ForJob(jobGoodIssue)
        .WithIdentity("jobGoodIssue-trigger")
        .WithCronSchedule("0 */2 * * * ?")
    );
    var jobGoodReceipt = new JobKey("jobGoodReceipt");
    q.AddJob<JobGoodReceipt>(opts => opts.WithIdentity(jobGoodReceipt));
    q.AddTrigger(opts => opts
        .ForJob(jobGoodReceipt)
        .WithIdentity("jobGoodReceipt-trigger")
        .WithCronSchedule("0 */2 * * * ?")
    );


});
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
builder.Services.AddHttpClient<SapSessionManager>()
    .ConfigurePrimaryHttpMessageHandler(() =>
        new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        });
var app = builder.Build();

app.UseSwagger();
app.UseCors("AllowAll");
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
