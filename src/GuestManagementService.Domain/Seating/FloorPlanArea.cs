namespace GuestManagementService.Domain.Seating;

// Room elements placed alongside tables on the floor plan: presets (stage, dance floor,
// bar, entrance, buffet, cake) and free-form custom areas (photo booth, gift table, ...).
public sealed class FloorPlanArea
{
    public const double MinDimension = 0.1;
    public const double MaxDimension = 100_000;
    // Capacity is optional (null = "not tracked"); when specified it must be a positive count.
    public const int CapacityMin = 1;
    public const int CapacityMax = 1000;

    private FloorPlanArea()
    {
    }

    private FloorPlanArea(
        Guid id,
        Guid seatingLayoutId,
        string name,
        AreaKind kind,
        AreaShape shape,
        double width,
        double height,
        string? color,
        int? capacity,
        DateTimeOffset createdAt)
    {
        Id = id;
        SeatingLayoutId = seatingLayoutId;
        Name = name;
        Kind = kind;
        Shape = shape;
        Width = width;
        Height = height;
        Color = color;
        Capacity = capacity;
        Rotation = 0;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid SeatingLayoutId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public AreaKind Kind { get; private set; }

    public AreaShape Shape { get; private set; }

    public double Width { get; private set; }

    public double Height { get; private set; }

    // Floor-plan coordinates. Null until the area is placed on the plan (mirrors SeatingTable).
    public double? PositionX { get; private set; }

    public double? PositionY { get; private set; }

    public double Rotation { get; private set; }

    public string? Color { get; private set; }

    public int? Capacity { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static FloorPlanArea Create(
        Guid id,
        Guid seatingLayoutId,
        string name,
        AreaKind kind,
        AreaShape shape,
        double width,
        double height,
        string? color,
        int? capacity,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Area id must not be empty.", nameof(id));
        }

        if (seatingLayoutId == Guid.Empty)
        {
            throw new ArgumentException("Seating layout id must not be empty.", nameof(seatingLayoutId));
        }

        var normalizedName = NormalizeName(name);
        EnsureDimensionInRange(width, nameof(width));
        EnsureDimensionInRange(height, nameof(height));
        EnsureCapacityInRange(capacity);

        return new FloorPlanArea(
            id,
            seatingLayoutId,
            normalizedName,
            kind,
            shape,
            width,
            height,
            NormalizeColor(color),
            capacity,
            createdAt.ToUniversalTime());
    }

    public void Update(
        string name,
        AreaKind kind,
        AreaShape shape,
        double width,
        double height,
        string? color,
        int? capacity,
        DateTimeOffset now)
    {
        EnsureDimensionInRange(width, nameof(width));
        EnsureDimensionInRange(height, nameof(height));
        EnsureCapacityInRange(capacity);

        Name = NormalizeName(name);
        Kind = kind;
        Shape = shape;
        Width = width;
        Height = height;
        Color = NormalizeColor(color);
        Capacity = capacity;
        UpdatedAt = now.ToUniversalTime();
    }

    public void Move(double positionX, double positionY, double rotation, DateTimeOffset now)
    {
        PositionX = positionX;
        PositionY = positionY;
        Rotation = rotation;
        UpdatedAt = now.ToUniversalTime();
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Area name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static string? NormalizeColor(string? color)
    {
        return string.IsNullOrWhiteSpace(color) ? null : color.Trim();
    }

    private static void EnsureDimensionInRange(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < MinDimension || value > MaxDimension)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"{parameterName} must be between {MinDimension} and {MaxDimension}.");
        }
    }

    private static void EnsureCapacityInRange(int? capacity)
    {
        if (capacity is < CapacityMin or > CapacityMax)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                $"Capacity must be between {CapacityMin} and {CapacityMax}.");
        }
    }
}
