using System.Collections.Generic;
using UnityEngine;

namespace CarMatchClone.Board
{
    public class PathfindingService : MonoBehaviour
    {
        [SerializeField] private Board _board;

        private static readonly Vector2Int[] _directions =
        {
            Vector2Int.right,
            Vector2Int.left,
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        private void Start()
        {
            RecalculateReachability(_board);
        }

        public void RecalculateReachability(Board board)
        {
            var exitSet = new HashSet<Vector2Int>(board.ExitPositions);
            foreach (var cell in board.GetAllCells())
            {
                if (cell.Occupant != null)
                    cell.Occupant.IsReachable = HasPathToExit(cell.Position, board, exitSet);
            }
        }

        // Dış çağrılar için (ör. ilerideki Booster sistemi)
        public bool HasPathToExit(Vector2Int start, Board board)
        {
            return HasPathToExit(start, board, new HashSet<Vector2Int>(board.ExitPositions));
        }

        private bool HasPathToExit(Vector2Int start, Board board, HashSet<Vector2Int> exitSet)
        {
            var openList = new List<Node>();
            var closedSet = new HashSet<Vector2Int>();

            openList.Add(new Node(start, 0, MinExitDistance(start, exitSet)));

            while (openList.Count > 0)
            {
                int bestIndex = GetLowestFIndex(openList);
                var current = openList[bestIndex];
                openList.RemoveAt(bestIndex);

                if (closedSet.Contains(current.Position))
                    continue;
                closedSet.Add(current.Position);

                foreach (var dir in _directions)
                {
                    var neighborPos = current.Position + dir;

                    if (closedSet.Contains(neighborPos)) continue;

                    if (exitSet.Contains(neighborPos))
                        return true;

                    var cell = board.GetCell(neighborPos);
                    if (cell == null || !cell.IsWalkable) continue;

                    openList.Add(new Node(
                        neighborPos,
                        current.G + 1,
                        MinExitDistance(neighborPos, exitSet)));
                }
            }

            return false;
        }

        private static int MinExitDistance(Vector2Int pos, HashSet<Vector2Int> exits)
        {
            int min = int.MaxValue;
            foreach (var exit in exits)
            {
                int d = ManhattanDistance(pos, exit);
                if (d < min) min = d;
            }
            return min;
        }

        private static int GetLowestFIndex(List<Node> list)
        {
            int best = 0;
            for (int i = 1; i < list.Count; i++)
            {
                if (list[i].F < list[best].F ||
                    (list[i].F == list[best].F && list[i].H < list[best].H))
                    best = i;
            }
            return best;
        }

        private static int ManhattanDistance(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        private class Node
        {
            public readonly Vector2Int Position;
            public readonly int G;
            public readonly int H;
            public int F => G + H;

            public Node(Vector2Int position, int g, int h)
            {
                Position = position;
                G = g;
                H = h;
            }
        }
    }
}
