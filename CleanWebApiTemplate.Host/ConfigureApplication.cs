namespace CleanWebApiTemplate.Host;

public static class ConfigureApplication
{
    public static IApplicationBuilder ConfigureHostApplication(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        app.UseResponseCompression();
        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
