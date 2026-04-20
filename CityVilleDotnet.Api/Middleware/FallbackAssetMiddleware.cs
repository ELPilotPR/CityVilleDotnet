namespace CityVilleDotnet.Api.Middleware;

public class FallbackAssetMiddleware(
    RequestDelegate next,
    IWebHostEnvironment env,
    ILogger<FallbackAssetMiddleware> logger)
{
    private static readonly Dictionary<string, string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".mp3"] = "audio/mpeg",
        [".swf"] = "application/x-shockwave-flash",
        [".css"] = "text/css"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/assets"))
        {
            var extension = Path.GetExtension(context.Request.Path).ToLowerInvariant();

            if (SupportedExtensions.TryGetValue(extension, out var contentType))
            {
                var requestedFile = Path.Combine(env.WebRootPath, context.Request.Path.Value!.TrimStart('/'));

                if (!File.Exists(requestedFile))
                {
                    var defaultFile = Path.Combine(env.WebRootPath, "assets", $"default{extension}");

                    if (File.Exists(defaultFile))
                    {
                        context.Response.StatusCode = 200;
                        context.Response.ContentType = contentType;
                        context.Response.Headers.CacheControl = "public, max-age=2592000";
                        context.Response.Headers.Expires = DateTime.UtcNow.AddMonths(1).ToString("R");
                        await context.Response.SendFileAsync(defaultFile);
                        return;
                    }

                    logger.LogWarning(
                        "Asset not found: {RequestPath}, but no default fallback exists at: {DefaultFile}",
                        context.Request.Path,
                        defaultFile);
                }
            }
        }

        await next(context);
    }
}
