namespace TaskManager.Composition;

public static class ConfigurationExtensions
{
    public static WebApplicationBuilder AddTaskManagerConfiguration(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile(
            Path.Combine("Configuration", "rag.json"),
            optional: false,
            reloadOnChange: true);

        // Env vars (e.g. Docker Rag__BaseUrl=http://ai:8000) must win over rag.json.
        builder.Configuration.AddEnvironmentVariables();

        return builder;
    }
}
