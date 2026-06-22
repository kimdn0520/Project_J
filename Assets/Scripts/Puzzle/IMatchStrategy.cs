using System.Collections.Generic;

namespace Framework.PuzzleSystem
{
    // 퍼즐 매칭 로직 인터페이스.
    public interface IMatchStrategy<T> where T : class, IGridNode
    {
        // 특정 좌표 기준으로 매칭된 덩어리들 찾기
        IEnumerable<List<T>> FindMatches(GridSystem<T> grid, int startX, int startY);
        
        // 보드 전체 탐색해서 터질 것들 한꺼번에 반환
        List<T> FindAllMatches(GridSystem<T> grid);
    }
}
