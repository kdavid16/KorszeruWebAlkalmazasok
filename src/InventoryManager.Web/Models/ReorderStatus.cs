namespace InventoryManager.Web.Models;

public enum ReorderStatus
{
    Pending = 1,   // Automatikusan generálva / Jóváhagyásra vár
    Ordered = 2,   // Beszerző jóváhagyta, megrendelve a beszállítótól
    Received = 3,  // Beérkezett és raktárba vételezve
    Cancelled = 4  // Elutasítva / Lemondva
}
