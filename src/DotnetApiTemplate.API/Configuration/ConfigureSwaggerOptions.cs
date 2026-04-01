using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace DotnetApiTemplate.API.Configuration
{
    public class ConfigureSwaggerOptions : IConfigureNamedOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;
        private readonly string _version;

        public ConfigureSwaggerOptions(
            IApiVersionDescriptionProvider provider)
        {
            _provider = provider;
            var assembly = Assembly.GetEntryAssembly();
            var attribute = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            _version = attribute?.InformationalVersion ?? "1.0.0";
        }

        /// <summary>
        /// Configure each API discovered for Swagger Documentation
        /// </summary>
        /// <param name="options"></param>
        public void Configure(SwaggerGenOptions options)
        {
            // add swagger document for every API version discovered
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(
                    description.GroupName,
                    CreateVersionInfo(description, _version));
            }
        }

        /// <summary>
        /// Configure Swagger Options. Inherited from the Interface
        /// </summary>
        /// <param name="name"></param>
        /// <param name="options"></param>
        public void Configure(string? name, SwaggerGenOptions options)
        {
            Configure(options);
        }

        /// <summary>
        /// Create information about the version of the API
        /// </summary>
        /// <param name="desc"></param>
        /// <param name="version"></param>
        /// <returns>Information about the API</returns>
        private static OpenApiInfo CreateVersionInfo(ApiVersionDescription desc, string version)
        {
            var info = new OpenApiInfo()
            {
                Title = "Api Template",
                Version = desc.ApiVersion.ToString(),
                Description = $"<p><strong>Version: </strong>{version}</p>"
            };

            if (desc.IsDeprecated)
            {
                info.Description += " This API version has been deprecated. Please use one of the new APIs available from the explorer.";
            }

            return info;
        }
    }
}
