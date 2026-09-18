using GuestManagementService.Domain.Seating;

namespace GuestManagementService.Application.Seating;

public static class SeatingParsing
{
    public static bool TryParseShape(string? value, out TableShape shape)
    {
        return Enum.TryParse(value, ignoreCase: true, out shape) && Enum.IsDefined(shape);
    }

    public static bool TryParseBatchOpType(string? value, out SeatingBatchOpType opType)
    {
        return Enum.TryParse(value, ignoreCase: true, out opType) && Enum.IsDefined(opType);
    }

    public static bool TryParseAreaKind(string? value, out AreaKind kind)
    {
        return Enum.TryParse(value, ignoreCase: true, out kind) && Enum.IsDefined(kind);
    }

    public static bool TryParseAreaShape(string? value, out AreaShape shape)
    {
        return Enum.TryParse(value, ignoreCase: true, out shape) && Enum.IsDefined(shape);
    }
}
