namespace InventoryManager.Web.Models;

public enum MovementType
{
    Inbound = 1,   // Beérkezés (beszállítás, raktári bevét)
    Outbound = 2,  // Kiadás (értékesítés, selejtezés)
    Adjustment = 3 // Leltári korrekció
}
