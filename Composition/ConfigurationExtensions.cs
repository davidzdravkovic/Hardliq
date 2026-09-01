namespace TaskManager.Composition;

public static class ConfigurationExtensions
{
    public static WebApplicationBuilder AddTaskManagerConfiguration(this WebApplicationBuilder builder)
    {
        var ragJson = Path.Combine("Configuration", "rag.json");
        var ragConfigPath = File.Exists(ragJson)
            ? ragJson
            : Path.Combine("Configuration", "rag.example.json");

        builder.Configuration.AddJsonFile(
            ragConfigPath,
            optional: false,
            reloadOnChange: ragConfigPath == ragJson);

        // Env vars (e.g. Docker Rag__BaseUrl=http://ai:8000) must win over rag.json.
        builder.Configuration.AddEnvironmentVariables();

        return builder;
    }
}
