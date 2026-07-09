using System.Collections.Generic;
using UnityEngine;
using CarMatchClone.Core.Events;

namespace CarMatchClone.Board
{
    public class PathfindingService : MonoBehaviour
    {
        [SerializeField] private Board _board;
        [SerializeField] private CellEventChannel _onCellVacatedChannel;
        [SerializeField] private VoidEventChannel _onBoardStateChangedChannel;
        [SerializeField] private bool _debugLogging;

        private static readonly Vector2Int[] _directions =
        {
            Vector2Int.right,
            Vector2Int.left,
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        private void Start()
        {
            if (_board == null) { Debug.LogError("[PathfindingService] Board referansı atanmamış."); return; }
            RecalculateReachability(_board);
        }

        private void OnEnable()
        {
            if (_onCellVacatedChannel == null)
                Debug.LogWarning("[PathfindingService] OnCellVacatedChannel atanmamış — dinamik güncelleme çalışmaz.");
            else
                _onCellVacatedChannel.Subscribe(HandleCellVacated);

            if (_onBoardStateChangedChannel == null)
                Debug.LogWarning("[PathfindingService] OnBoardStateChangedChannel atanmamış — reveal/spawn sonrası güncelleme çalışmaz.");
            else
                _onBoardStateChangedChannel.Subscribe(HandleBoardStateChanged);
        }

        private void OnDisable()
        {
            _onCellVacatedChannel?.Unsubscribe(HandleCellVacated);
            _onBoardStateChangedChannel?.Unsubscribe(HandleBoardStateChanged);
        }

        private void HandleCellVacated(GridCell cell) => RecalculateReachability(_board);

        private void HandleBoardStateChanged() => RecalculateReachability(_board);

        public void RecalculateReachability(Board board)
        {
            var exitSet = new HashSet<Vector2Int>(board.ExitPositions);

            if (_debugLogging)
            {
                var walkLog = new System.Text.StringBuilder("[PathfindingService] Walkability: ");
                foreach (var cell in board.GetAllCells())
                    walkLog.Append($"({cell.Position.x},{cell.Position.y})={cell.IsWalkable}  ");
                Debug.Log(walkLog.ToString());
            }

            foreach (var cell in board.GetAllCells())
            {
                if (cell.Occupant != null)
                {
                    bool reachable = HasPathToExit(cell.Position, board, exitSet);
                    cell.Occupant.IsReachable = reachable;
                    if (_debugLogging)
                        Debug.Log($"[PathfindingService] {cell.Position} → IsReachable: {reachable}");
                }
            }
        }

        // Dış çağrılar için (ör. ilerideki Booster sistemi)
        public bool HasPathToExit(Vector2Int start, Board board)
        {
            return HasPathToExit(start, board, new HashSet<Vector2Int>(board.ExitPositions));
        }

        // CarMover'ın tween için kullandığı path: başlangıç HARİÇ, exit DAHİL.
        public List<Vector2Int> GetPathToExit(Vector2Int start, Board board)
        {
            var exitSet = new HashSet<Vector2Int>(board.ExitPositions);
            var openList = new List<Node>();
            var closedSet = new HashSet<Vector2Int>();

            openList.Add(new Node(start, 0, MinExitDistance(start, exitSet), null));

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
                    {
                        var path = new List<Vector2Int> { neighborPos };
                        var node = current;
                        while (node.Parent != null)
                        {
                            path.Add(node.Position);
                            node = node.Parent;
                        }
                        path.Reverse();
                        return path;
                    }

                    var cell = board.GetCell(neighborPos);
                    if (cell == null || !cell.IsWalkable) continue;

                    openList.Add(new Node(
                        neighborPos,
                        current.G + 1,
                        MinExitDistance(neighborPos, exitSet),
                        current));
                }
            }

            return null;
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
            public readonly Node Parent;

            public Node(Vector2Int position, int g, int h, Node parent = null)
            {
                Position = position;
                G = g;
                H = h;
                Parent = parent;
            }
        }
    }
}
