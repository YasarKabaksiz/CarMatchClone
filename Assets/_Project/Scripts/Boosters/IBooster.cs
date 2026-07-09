using CarMatchClone.Core;

namespace CarMatchClone.Boosters
{
    // true → booster etki etti, stok azaltılır.
    // false → booster hiçbir şey yapmadı, stok HARCANMAZ.
    public interface IBooster
    {
        bool Execute(CarMatchClone.Board.Board board, GameState state);
    }
}
