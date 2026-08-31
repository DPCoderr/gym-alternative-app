using Microsoft.Extensions.Options;

namespace GymAlternatief.Api.Infrastructure.Configuration;

public static class OptionsServiceCollectionExtensions
{
    public static OptionsBuilder<TOptions> AddValidatedOptions<TOptions, TValidator>(
        this IServiceCollection services,
        IConfigurationSection section)
        where TOptions : class
        where TValidator : class, IValidateOptions<TOptions>
    {
        services.AddSingleton<IValidateOptions<TOptions>, TValidator>();

        return services
            .AddOptions<TOptions>()
            .Bind(section)
            .ValidateOnStart();
    }
}
