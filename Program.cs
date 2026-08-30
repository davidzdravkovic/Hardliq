using TaskManager.Composition;

var builder = WebApplication.CreateBuilder(args);

builder.AddTaskManagerConfiguration();
builder.Services.AddTaskManagerServices(builder.Configuration);

var app = builder.Build();

await app.UseTaskManagerPipelineAsync();

app.Run();

public partial class Program { }
