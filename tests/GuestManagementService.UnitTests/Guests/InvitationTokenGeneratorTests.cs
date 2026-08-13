using GuestManagementService.Infrastructure.Guests;

namespace GuestManagementService.UnitTests.Guests;

public sealed class InvitationTokenGeneratorTests
{
    private readonly InvitationTokenGenerator _generator = new();

    [Fact]
    public void Generate_ProducesA128BitBase64UrlToken()
    {
        var token = _generator.Generate();

        // 16 bytes base64-encoded is 24 characters including two '=' padding characters, so an
        // unpadded token is exactly 22.
        Assert.Equal(22, token.Length);
        Assert.DoesNotContain('=', token);
    }

    [Fact]
    public void Generate_ProducesUrlSafeCharactersOnly()
    {
        // The token goes straight into a URL path, so '+' and '/' must have been translated.
        for (var i = 0; i < 500; i++)
        {
            var token = _generator.Generate();

            Assert.DoesNotContain('+', token);
            Assert.DoesNotContain('/', token);
            Assert.All(token, c => Assert.True(
                char.IsLetterOrDigit(c) || c == '-' || c == '_',
                $"Unexpected character '{c}' in token."));
        }
    }

    [Fact]
    public void Generate_DoesNotCollideAcrossManyTokens()
    {
        // Not a proof of unpredictability — that comes from using RandomNumberGenerator — but it
        // does catch a generator accidentally rewired to something constant or sequential.
        var tokens = new HashSet<string>();

        for (var i = 0; i < 10_000; i++)
        {
            Assert.True(tokens.Add(_generator.Generate()), "Generated a duplicate invitation token.");
        }

        Assert.Equal(10_000, tokens.Count);
    }
}
