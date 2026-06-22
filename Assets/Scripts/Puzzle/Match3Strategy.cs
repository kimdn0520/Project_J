using System.Collections.Generic;
using UnityEngine;

namespace Framework.PuzzleSystem
{
    // 가장 기본적인 가로세로 3매치 룰
    public class Match3Strategy : IMatchStrategy<PuzzleTile>
    {
        public IEnumerable<List<PuzzleTile>> FindMatches(GridSystem<PuzzleTile> grid, int x, int y)
        {
            var result = new List<List<PuzzleTile>>();
            
            // 수평이랑 수직 각각 따로 체크함
            var horizontal = GetMatchList(grid, x, y, new Vector2Int(1, 0));
            var vertical = GetMatchList(grid, x, y, new Vector2Int(0, 1));

            // 3개 이상 모였을 때만 결과에 추가
            if (horizontal.Count >= 3) result.Add(horizontal);
            if (vertical.Count >= 3) result.Add(vertical);

            return result;
        }

        // 전체 그리드 스캔해서 터질 타일들 탐색
        public List<PuzzleTile> FindAllMatches(GridSystem<PuzzleTile> grid)
        {
            // 중복 방지용 HashSet
            var matchedNodes = new HashSet<PuzzleTile>();
            
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    // 하나씩 돌면서 매칭 그룹 찾기
                    foreach (var match in FindMatches(grid, x, y))
                    {
                        foreach (var node in match) matchedNodes.Add(node);
                    }
                }
            }
            return new List<PuzzleTile>(matchedNodes);
        }

        // 기준점에서 특정 방향으로 양방향 탐색
        private List<PuzzleTile> GetMatchList(GridSystem<PuzzleTile> grid, int x, int y, Vector2Int dir)
        {
            var origin = grid.GetNode(x, y);
            if (origin == null || !origin.isMatchable) return new List<PuzzleTile>();

            var list = new List<PuzzleTile> { origin };
            
            // 정방향/역방향 둘 다 뒤짐 (ex: 좌-우, 상-하)
            list.AddRange(SearchInDirection(grid, x, y, dir, origin.type));
            list.AddRange(SearchInDirection(grid, x, y, -dir, origin.type));
            
            return list;
        }

        // 방향 하나 잡고 쭉 전진하면서 같은 타입 찾기
        private List<PuzzleTile> SearchInDirection(GridSystem<PuzzleTile> grid, int x, int y, Vector2Int dir, PuzzleTile.TileType type)
        {
            var found = new List<PuzzleTile>();
            int curX = x + dir.x;
            int curY = y + dir.y;

            while (grid.IsValid(curX, curY))
            {
                var node = grid.GetNode(curX, curY);
                // 타입 같고 매칭 가능한 놈인지 체크
                if (node != null && node.isMatchable && node.type == type)
                {
                    found.Add(node);
                    curX += dir.x;
                    curY += dir.y;
                }
                else break;
            }
            return found;
        }
    }
}
