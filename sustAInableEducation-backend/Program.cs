﻿using Microsoft.EntityFrameworkCore;
using sustAInableEducation_backend.Repository;
using sustAInableEducation_backend.Controllers;
using sustAInableEducation_backend.Hubs;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Identity;
using sustAInableEducation_backend.Models;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "sustAInableEducation API", Version = "v1" });
    options.AddSignalRSwaggerGen();
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        var connectionString = string.Format(
            builder.Configuration.GetConnectionString("ApplicationDatabase")!,
            builder.Configuration["Db:Host"],
            builder.Configuration["Db:User"],
            builder.Configuration["Db:Password"]
        );
        options.UseSqlServer(connectionString);
    }
);
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<ApplicationUser>(options =>
{
    options.Password = new PasswordOptions()
    {
        RequiredLength = 8,
        RequireDigit = true,
        RequireLowercase = true,
        RequireUppercase = true,
        RequireNonAlphanumeric = true
    };
}).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddTransient<IAIService, AIService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

using (var Scope = app.Services.CreateScope())
{
    var context = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

app.MapControllers();

app.MapGroup("/account").MapIdentityApi<ApplicationUser>().WithTags("Account");

app.MapHub<SpaceHub>("/spaceHub/{id}");

app.Run();
