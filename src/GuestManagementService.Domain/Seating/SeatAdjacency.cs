namespace GuestManagementService.Domain.Seating;

// Seat-index adjacency for the "reserve seats around a dropped seat" search. Round and
// Square tables lay seats out in index order around a ring (see the frontend's
// computeSeatPositions), so index N-1 is adjacent to index 0. Long tables render as two
// independent rows — indices [0, ceil(seatCount/2)) on top, the rest on the bottom — where
// the last top seat and the first bottom seat sit on opposite sides of the table, not next
// to each other, so adjacency does not wrap between the two rows.
public static class SeatAdjacency
{
    // Picks `requiredCount` contiguous free seats anchored at anchorSeatIndex, expanding
    // outward one step at a time, alternating sides so seats are claimed as evenly as
    // possible on both sides of the anchor. Each direction stops the moment it hits an
    // occupied seat, a row boundary (Long tables), or a seat already claimed by the other
    // direction (possible on a fully-free ring when requiredCount uses every seat). Returns
    // null when fewer than requiredCount contiguous free seats are reachable from the
    // anchor. The anchor is always the first element of the result.
    public static IReadOnlyList<int>? FindContiguousFreeSeats(
        TableShape shape,
        int seatCount,
        int anchorSeatIndex,
        int requiredCount,
        IReadOnlySet<int> occupiedSeatIndexes)
    {
        if (requiredCount <= 0)
        {
            return [];
        }

        if (requiredCount > seatCount || occupiedSeatIndexes.Contains(anchorSeatIndex))
        {
            return null;
        }

        var isCircular = shape != TableShape.Long;
        var (rowStart, rowEnd) = RowBounds(shape, seatCount, anchorSeatIndex);

        var chosen = new List<int> { anchorSeatIndex };
        int leftStep = 0, rightStep = 0;
        bool leftOpen = true, rightOpen = true;
        var preferRight = true;

        while (chosen.Count < requiredCount && (leftOpen || rightOpen))
        {
            // Alternate sides each step so seats are claimed evenly on both sides of the
            // anchor; fall back to whichever side is still open once the other closes.
            var goRight = preferRight ? rightOpen : !leftOpen;
            preferRight = !preferRight;

            var step = goRight ? ++rightStep : ++leftStep;
            var candidate = goRight ? anchorSeatIndex + step : anchorSeatIndex - step;
            var seatIndex = ResolveSeatIndex(candidate, isCircular, seatCount, rowStart, rowEnd);

            if (seatIndex is null || occupiedSeatIndexes.Contains(seatIndex.Value) || chosen.Contains(seatIndex.Value))
            {
                if (goRight)
                {
                    rightOpen = false;
                }
                else
                {
                    leftOpen = false;
                }

                continue;
            }

            chosen.Add(seatIndex.Value);
        }

        return chosen.Count >= requiredCount ? chosen : null;
    }

    private static int? ResolveSeatIndex(int candidate, bool isCircular, int seatCount, int rowStart, int rowEnd)
    {
        if (isCircular)
        {
            return ((candidate % seatCount) + seatCount) % seatCount;
        }

        return candidate >= rowStart && candidate < rowEnd ? candidate : null;
    }

    // Long tables only: which row (top or bottom) the anchor seat falls in, matching the
    // frontend's computeLongPositions split (top row gets the extra seat when seatCount is odd).
    private static (int Start, int End) RowBounds(TableShape shape, int seatCount, int anchorSeatIndex)
    {
        if (shape != TableShape.Long)
        {
            return (0, seatCount);
        }

        var topCount = (seatCount + 1) / 2;
        return anchorSeatIndex < topCount ? (0, topCount) : (topCount, seatCount);
    }
}
