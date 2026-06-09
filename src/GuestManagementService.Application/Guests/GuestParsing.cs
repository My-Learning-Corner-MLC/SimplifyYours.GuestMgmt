using GuestManagementService.Domain.Guests;

namespace GuestManagementService.Application.Guests;

internal static class GuestParsing
{
    public static bool TryParseGender(string? value, out Gender gender)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            gender = Gender.PreferNotToSay;
            return true;
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Equals("male", StringComparison.OrdinalIgnoreCase))
        {
            gender = Gender.Male;
            return true;
        }

        if (normalizedValue.Equals("female", StringComparison.OrdinalIgnoreCase))
        {
            gender = Gender.Female;
            return true;
        }

        if (normalizedValue.Equals("other", StringComparison.OrdinalIgnoreCase))
        {
            gender = Gender.Other;
            return true;
        }

        if (normalizedValue.Equals("preferNotToSay", StringComparison.OrdinalIgnoreCase))
        {
            gender = Gender.PreferNotToSay;
            return true;
        }

        gender = Gender.PreferNotToSay;
        return false;
    }

    public static string ToContractValue(Gender gender)
    {
        return gender switch
        {
            Gender.Male => "male",
            Gender.Female => "female",
            Gender.Other => "other",
            _ => "preferNotToSay"
        };
    }
}
