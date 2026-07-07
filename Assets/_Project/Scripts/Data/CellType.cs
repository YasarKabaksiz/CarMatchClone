namespace CarMatchClone.Data
{
    public enum CellType
    {
        CarSlot, // Car spawned here; isWalkable = false while occupied
        Empty    // Walkable gap within the shape; no car spawned
    }
}
