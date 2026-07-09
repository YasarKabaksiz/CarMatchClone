namespace CarMatchClone.Data
{
    public enum CellType
    {
        CarSlot,       // Araç burada başlar; dolu iken isWalkable = false
        Empty,         // Geçilebilir boşluk; araç spawn olmaz
        Wall,          // Geçilemez sınır; isWalkable = false, araç yok
        LockedBox,     // Kilitli kutu; içinde gizli araç var (CellEntry.color), 4 komşudan biri boşalınca açılır
        GarageSpawner  // Garaj; önündeki hücre (facingDirection) boşalınca yeni araç üretir
    }
}
