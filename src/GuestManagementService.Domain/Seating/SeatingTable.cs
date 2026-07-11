namespace GuestManagementService.Domain.Seating;

public sealed class SeatingTable
{
    public const int SeatCountMin = 1;
    public const int SeatCountMax = 20;

    private SeatingTable()
    {
    }

    private SeatingTable(
        Guid id,
        Guid seatingLayoutId,
        string name,
        TableShape shape,
        int seatCount,
        DateTimeOffset createdAt)
    {
        Id = id;
        SeatingLayoutId = seatingLayoutId;
        Name = name;
        Shape = shape;
        SeatCount = seatCount;
        IsFull = false;
        Rotation = 0;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid SeatingLayoutId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public TableShape Shape { get; private set; }

    public int SeatCount { get; private set; }

    public bool IsFull { get; private set; }

    // Floor-plan coordinates. Null until the table is placed on the plan.
    public double? PositionX { get; private set; }

    public double? PositionY { get; private set; }

    public double Rotation { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Rename(string name, DateTimeOffset now)
    {
        Name = NormalizeName(name);
        UpdatedAt = now.ToUniversalTime();
    }

    public void Reshape(TableShape shape, int seatCount, DateTimeOffset now)
    {
        EnsureSeatCountInRange(seatCount);
        Shape = shape;
        SeatCount = seatCount;
        UpdatedAt = now.ToUniversalTime();
    }

    public void SetFull(bool isFull, DateTimeOffset now)
    {
        IsFull = isFull;
        UpdatedAt = now.ToUniversalTime();
    }

    public static SeatingTable Create(
        Guid id,
        Guid seatingLayoutId,
        string name,
        TableShape shape,
        int seatCount,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Table id must not be empty.", nameof(id));
        }

        if (seatingLayoutId == Guid.Empty)
        {
            throw new ArgumentException("Seating layout id must not be empty.", nameof(seatingLayoutId));
        }

        var normalizedName = NormalizeName(name);
        EnsureSeatCountInRange(seatCount);

        return new SeatingTable(
            id,
            seatingLayoutId,
            normalizedName,
            shape,
            seatCount,
            createdAt.ToUniversalTime());
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Table name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static void EnsureSeatCountInRange(int seatCount)
    {
        if (seatCount is < SeatCountMin or > SeatCountMax)
        {
            throw new ArgumentOutOfRangeException(
                nameof(seatCount),
                seatCount,
                $"Seat count must be between {SeatCountMin} and {SeatCountMax}.");
        }
    }
}
