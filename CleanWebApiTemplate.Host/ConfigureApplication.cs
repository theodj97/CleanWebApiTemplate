using CleanWebApiTemplate.Domain.Configuration;

namespace CleanWebApiTemplate.Host;

public static class ConfigureApplication
{
    public static IApplicationBuilder ConfigureHostApplication(this IApplicationBuilder app, string environment)
    {
        app.UseExceptionHandler();
        app.UseResponseCompression();
        app.UseHttpsRedirection();

        if (environment is not Constants.DEV_ENVIRONMNET)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        return app;
    }
}
