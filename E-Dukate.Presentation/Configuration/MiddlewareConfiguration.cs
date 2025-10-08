namespace E_Dukate.Presentation.Configuration;

public static class MiddlewareConfiguration
{
    public static IApplicationBuilder ConfigureMiddleware(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Dukate API V1");
            c.RoutePrefix = string.Empty;
        });

        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        return app;
    }
}