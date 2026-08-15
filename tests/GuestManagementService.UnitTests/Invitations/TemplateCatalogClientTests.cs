using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using GuestManagementService.Application.Abstractions.Invitations;
using GuestManagementService.Application.Invitations.GetInvitation;
using GuestManagementService.Application.Invitations.SubmitRsvp;
using GuestManagementService.Infrastructure.Invitations;
using Microsoft.Extensions.Logging.Abstractions;

namespace GuestManagementService.UnitTests.Invitations;

/// <summary>
/// AC 11 / B2: <see cref="ITemplateCatalogClient"/> must be called from exactly one place — the
/// authenticated save path — and never from anything reachable by an anonymous caller. Verified
/// here by reflection rather than by inspection: none of the anonymous-path handlers may declare a
/// dependency on it at all, so a future change cannot silently wire it in.
/// </summary>
/// <remarks>
/// The render-path resolver (<c>ResolveInvitationRenderQueryHandler</c>) lands in the B3-B6 PR and
/// is covered by the same assertion there — this PR only has the two anonymous handlers that
/// already exist on <c>feature-invitation-page-rsvp</c>.
/// </remarks>
public sealed class TemplateCatalogClientIsolationTests
{
    [Theory]
    [InlineData(typeof(GetInvitationQueryHandler))]
    [InlineData(typeof(SubmitRsvpCommandHandler))]
    public void AnonymousPathHandler_NeverDependsOnTheTemplateCatalogClient(Type handlerType)
    {
        var dependsOnCatalog = handlerType
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(ctor => ctor.GetParameters())
            .Any(parameter => parameter.ParameterType == typeof(ITemplateCatalogClient));

        Assert.False(
            dependsOnCatalog,
            $"{handlerType.Name} must never depend on ITemplateCatalogClient — it sits on the "
            + "anonymous render/RSVP path, and a template-management-service outage must never be "
            + "able to take it down.");
    }
}

public sealed class TemplateCatalogClientTests
{
    private static readonly Guid TemplateId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public async Task GetTemplateAsync_OnSuccess_ReturnsTheParsedEntry()
    {
        var handler = new FakeHandler((request, cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = TemplateId,
                    name = "Marigold",
                    eventType = "wedding",
                    version = 3,
                    htmlContent = "<html><body></body></html>",
                    cssContent = "body{}",
                    jsContent = (string?)null,
                }),
            }));

        var client = CreateClient(handler);

        var result = await client.GetTemplateAsync(TemplateId, CancellationToken.None);

        Assert.Equal(TemplateFetchStatus.Found, result.Status);
        Assert.Equal(TemplateId, result.Template!.Id);
        Assert.Equal(3, result.Template.Version);
        Assert.Equal("<html><body></body></html>", result.Template.HtmlContent);
    }

    [Fact]
    public async Task GetTemplateAsync_On404_ReturnsNotFound()
    {
        var handler = new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var result = await CreateClient(handler).GetTemplateAsync(TemplateId, CancellationToken.None);

        Assert.Equal(TemplateFetchStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetTemplateAsync_OnServerError_ReturnsUnavailable()
    {
        var handler = new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var result = await CreateClient(handler).GetTemplateAsync(TemplateId, CancellationToken.None);

        Assert.Equal(TemplateFetchStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task GetTemplateAsync_WhenTheHostIsUnreachable_ReturnsUnavailableRatherThanThrowing()
    {
        var handler = new FakeHandler((_, _) => throw new HttpRequestException("connection refused"));

        var result = await CreateClient(handler).GetTemplateAsync(TemplateId, CancellationToken.None);

        Assert.Equal(TemplateFetchStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task GetTemplateAsync_WhenTheRequestTimesOut_ReturnsUnavailableRatherThanThrowing()
    {
        var handler = new FakeHandler((_, _) => throw new TaskCanceledException("timed out"));

        var result = await CreateClient(handler).GetTemplateAsync(TemplateId, CancellationToken.None);

        Assert.Equal(TemplateFetchStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task GetTemplateAsync_WhenTheBodyIsUnreadable_ReturnsUnavailable()
    {
        var handler = new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not json"),
            }));

        var result = await CreateClient(handler).GetTemplateAsync(TemplateId, CancellationToken.None);

        Assert.Equal(TemplateFetchStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task GetTemplateAsync_RequestsTheExpectedPath()
    {
        Uri? requestedUri = null;
        var handler = new FakeHandler((request, _) =>
        {
            requestedUri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        await CreateClient(handler).GetTemplateAsync(TemplateId, CancellationToken.None);

        Assert.NotNull(requestedUri);
        Assert.Contains($"templates/{TemplateId:D}", requestedUri!.ToString(), StringComparison.Ordinal);
    }

    private static TemplateCatalogClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://template-management.internal/") };

        return new TemplateCatalogClient(httpClient, NullLogger<TemplateCatalogClient>.Instance);
    }

    private sealed class FakeHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            respond(request, cancellationToken);
    }
}
