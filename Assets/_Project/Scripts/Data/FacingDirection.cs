using UnityEngine;

namespace CarMatchClone.Data
{
    public enum FacingDirection { Up, Down, Left, Right }

    internal static class FacingDirectionExtensions
    {
        public static Vector2Int ToVector(this FacingDirection dir)
        {
            switch (dir)
            {
                case FacingDirection.Up:    return new Vector2Int( 0,  1);
                case FacingDirection.Down:  return new Vector2Int( 0, -1);
                case FacingDirection.Left:  return new Vector2Int(-1,  0);
                case FacingDirection.Right: return new Vector2Int( 1,  0);
                default:                   return Vector2Int.zero;
            }
        }
    }
}
