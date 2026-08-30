using Microsoft.EntityFrameworkCore;
using TaskManager.Data;

namespace TaskManager.Composition;

public static class WebApplicationExtensions
{
    public static async Task UseTaskManagerPipelineAsync(this WebApplication app)
    {
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
        {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        }

        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }
}
