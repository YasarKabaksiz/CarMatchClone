namespace CarMatchClone.Data
{
    public enum CellType
    {
        CarSlot, // Car spawned here; isWalkable = false while occupied
        Empty,   // Walkable gap; no car spawned
        Wall     // Impassable boundary; isWalkable = false, no car spawned
    }
}
