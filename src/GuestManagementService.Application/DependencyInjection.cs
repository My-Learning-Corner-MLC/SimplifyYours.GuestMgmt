using FluentValidation;
using GuestManagementService.Application.Authorization;
using GuestManagementService.Application.Common.Logging;
using GuestManagementService.Application.Common.Validation;
using GuestManagementService.Application.Guests;
using GuestManagementService.Application.Guests.Birthday;
using GuestManagementService.Application.Guests.Wedding;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GuestManagementService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CurrentUserPipelineBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestLoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Guest metadata mappers, one per event type. Adding a new event type only requires
        // registering its IGuestMetadataMapper here — GuestMetadataMapperFactory discovers it
        // automatically via the IEnumerable<IGuestMetadataMapper> injection.
        services.AddScoped<IGuestMetadataMapper, WeddingGuestMetadataMapper>();
        services.AddScoped<IGuestMetadataMapper, BirthdayGuestMetadataMapper>();
        services.AddScoped<IGuestMetadataMapperFactory, GuestMetadataMapperFactory>();

        return services;
    }
}
