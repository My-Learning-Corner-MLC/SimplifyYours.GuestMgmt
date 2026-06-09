using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Contracts.IntegrationEvents;
using GuestManagementService.Infrastructure.Messaging;
using GuestManagementService.Infrastructure.Persistence;
using GuestManagementService.Infrastructure.Persistence.Inbox;
using GuestManagementService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimplifyYours.Event.Abstractions;
using SimplifyYours.Event.Consumer;

namespace GuestManagementService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GuestManagementServiceDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("GuestManagementServiceDb");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'GuestManagementServiceDb' is required to use Guest Management service persistence.");
            }

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IGuestRepository, EfCoreGuestRepository>();
        services.AddScoped<IEventReferenceRepository, EfCoreEventReferenceRepository>();
        services.AddScoped<IEventInboxStore, GuestManagementInboxStore>();
        services.AddScoped<IIntegrationEventHandler<EventReferencePayload>, EventReferenceIntegrationEventHandler>();
        services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();
        services.AddSimplifyYoursEventConsumer(options =>
        {
            options.BootstrapServers = configuration["Kafka:BootstrapServers"];
            options.GroupId = configuration["Kafka:GroupId"];
            options.MaxHandleAttempts = configuration.GetValue("Kafka:MaxHandleAttempts", 5);
            options.DeadLetterTopicSuffix = configuration["Kafka:DeadLetterTopicSuffix"] ?? ".dlq";

            options.Subscribe<EventReferencePayload>(
                configuration["Kafka:EventReferenceTopic"],
                ["EventCreated", "EventUpdated", "EventDeleted"]);
        });

        return services;
    }
}
