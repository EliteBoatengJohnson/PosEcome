using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;//  for jwt token description
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore.SqlServer;
using PosSystem.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//  Serilog 
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// Ef Core + mss sql
var Connstr = builder.Configuration.GetConnectionString("Connection")!;
builder.Services.AddDbContext<PosDbContext>(opts =>
    opts.UseSqlServer(Connstr, sqlOpt =>
        {
            sqlOpt.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
            sqlOpt.CommandTimeout(30);
        }));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();



app.Run();

