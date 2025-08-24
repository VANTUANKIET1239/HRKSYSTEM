using AUTH.Infrastructure.Configuration;
using HRK.AUTH.Configuration;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

DependencyInjection.AddCustomDependency(builder.Services, builder.Configuration);



var app = builder.Build();



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // default path: /swagger
}


// Configure the HTTP request pipeline.

app.UseAuthentication(); // must come before UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();
