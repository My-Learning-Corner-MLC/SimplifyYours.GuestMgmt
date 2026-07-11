using GuestManagementService.Domain.Seating;

namespace GuestManagementService.Application.Seating;

public static class SeatingParsing
{
    public static bool TryParseShape(string? value, out TableShape shape)
    {
        return Enum.TryParse(value, ignoreCase: true, out shape) && Enum.IsDefined(shape);
    }
}
