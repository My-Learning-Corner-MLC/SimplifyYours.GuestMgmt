using GuestManagementService.Application.Abstractions.Common;
using GuestManagementService.Application.Abstractions.EventReferences;
using GuestManagementService.Application.Abstractions.Guests;
using GuestManagementService.Application.Abstractions.Invitations;
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

        services.AddSingleton<IInvitationTokenGenerator, Guests.InvitationTokenGenerator>();
        services.AddSingleton<IInvitationRenderer>(_ => new Invitations.ScribanInvitationRenderer(configuration));
        services.AddSingleton<IInvitationLinkBuilder>(_ => new Guests.InvitationLinkBuilder(configuration));

        // Typed client for template-management-service's catalog. Called from exactly one place —
        // the authenticated save path — never from the anonymous render path. See
        // ITemplateCatalogClient's remarks for why that boundary matters.
        services.AddHttpClient<ITemplateCatalogClient, Invitations.TemplateCatalogClient>((sp, client) =>
        {
            var baseUrl = configuration["TemplateManagement:BaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException(
                    "Configuration value 'TemplateManagement:BaseUrl' is required to reach the "
                    + "template catalog.");
            }

            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddScoped<IGuestRepository, EfCoreGuestRepository>();
        services.AddScoped<IEventReferenceRepository, EfCoreEventReferenceRepository>();
        services.AddScoped<IEventInvitationSettingsRepository, EfCoreEventInvitationSettingsRepository>();
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
