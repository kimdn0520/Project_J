using System;

namespace Framework.PuzzleSystem
{
    // 2차원 배열 데이터 관리용.
    public class GridSystem<T> where T : class, IGridNode
    {
        private T[,] _nodes;
        private int _width;
        private int _height;

        // 데이터 바뀌면 뷰 쪽에 알려주는 용도
        public event Action<int, int, T> OnNodeChanged;

        public GridSystem(int width, int height)
        {
            _width = width;
            _height = height;
            _nodes = new T[width, height];
        }

        // 인덱서 추가해서 grid[x, y] 로 바로 쓸 수 있게 함
        public T this[int x, int y]
        {
            get => GetNode(x, y);
            set => SetNode(x, y, value);
        }

        public void SetNode(int x, int y, T node)
        {
            if (!IsValid(x, y)) return;
            _nodes[x, y] = node;
            node?.SetCoordinate(x, y);
            
            OnNodeChanged?.Invoke(x, y, node);
        }

        public T GetNode(int x, int y) => IsValid(x, y) ? _nodes[x, y] : null;

        // 두 타일 데이터 위치 바꿀 때
        public void SwapNodes(int x1, int y1, int x2, int y2)
        {
            T node1 = GetNode(x1, y1);
            T node2 = GetNode(x2, y2);

            _nodes[x1, y1] = node2;
            _nodes[x2, y2] = node1;

            node1?.SetCoordinate(x2, y2);
            node2?.SetCoordinate(x1, y1);

            OnNodeChanged?.Invoke(x1, y1, node2);
            OnNodeChanged?.Invoke(x2, y2, node1);
        }

        // 범위 안인지 체크
        public bool IsValid(int x, int y) => x >= 0 && x < _width && y >= 0 && y < _height;
        
        public int Width => _width;
        public int Height => _height;
    }
}
