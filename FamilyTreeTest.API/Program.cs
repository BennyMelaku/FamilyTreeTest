using FamilyTreeTest.API.Models;
using FamilyTreeTest.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Neo4jClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var client = new BoltGraphClient(new Uri("bolt://localhost:7687"), "neo4j", "bennytest");
client.ConnectAsync();
builder.Services.AddSingleton<IGraphClient>(client);
builder.Services.AddSingleton<IFamilyTreeService, FamilyTreeService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();

app.Run();
