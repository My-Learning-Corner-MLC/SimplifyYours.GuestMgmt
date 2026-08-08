using GuestManagementService.Domain.Guests;
using GuestManagementService.Domain.Seating;

namespace GuestManagementService.Application.Seating;

// Shared by GetSeatingLayoutQueryHandler and ApplyAssignmentsBatchCommandHandler, both of
// which need to turn a loaded SeatingLayout + the event's guests into the same
// SeatingLayoutDetails/SeatingSummaryDetails shape.
public static class SeatingLayoutProjector
{
    public static SeatingLayoutDetails Project(SeatingLayout layout, IReadOnlyCollection<Guest> guests)
    {
        var guestNamesById = guests.ToDictionary(guest => guest.Id, guest => $"{guest.FirstName} {guest.LastName}");

        var tables = layout.Tables
            .Select(table => SeatingTableDetails.From(table, layout.Assignments, guestNamesById))
            .ToList();
        var areas = layout.Areas.Select(SeatingAreaDetails.From).ToList();
        var seatCount = tables.Sum(table => table.SeatCount);
        // Seats reserved for a party's accompanying attendees aren't a real guest, so they
        // don't count toward "seated" (and mustn't reduce "floating").
        var seatedCount = layout.Assignments.Count(assignment => !assignment.IsReservedForParty);
        var floatingCount = guests.Count - seatedCount;

        var summary = new SeatingSummaryDetails(tables.Count, seatCount, seatedCount, floatingCount);
        return new SeatingLayoutDetails(layout.Id, layout.EventId, tables, areas, summary);
    }
}
