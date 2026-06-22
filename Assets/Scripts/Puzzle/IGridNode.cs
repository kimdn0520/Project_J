namespace Framework.PuzzleSystem
{
    // 그리드에 들어가는 모든 오브젝트용 인터페이스
    public interface IGridNode
    {
        int X { get; }
        int Y { get; }
        
        // 데이터랑 뷰 좌표 맞출 때 사용
        void SetCoordinate(int x, int y);
    }
}
