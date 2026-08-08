namespace GuestManagementService.Application.Seating;

// Shared by the single and batch table-position validators. The room canvas is
// px-based and unbounded in practice, but a generous cap rejects clearly bad input
// (NaN/Infinity already fail IsFinite; this guards against absurd magnitudes).
public static class SeatingPositionBounds
{
    public const double MinCoordinate = -100_000;
    public const double MaxCoordinate = 100_000;
}
