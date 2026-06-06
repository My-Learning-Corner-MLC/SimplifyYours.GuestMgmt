using System.Text;

namespace GuestManagementService.Application.Guests;

public static class GuestNormalization
{
    public static string? NormalizeEmail(string? emailAddress)
    {
        return string.IsNullOrWhiteSpace(emailAddress)
            ? null
            : emailAddress.Trim().ToLowerInvariant();
    }

    public static string NormalizePhone(string phoneNumber)
    {
        var builder = new StringBuilder();

        foreach (var character in phoneNumber.Trim())
        {
            if (char.IsDigit(character))
            {
                builder.Append(character);
                continue;
            }

            if (character == '+' && builder.Length == 0)
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
