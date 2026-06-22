using System;
using Framework.PuzzleSystem;

namespace Framework.Utils
{
    // 입력 중복 방지용 busy 상태 토글
    public struct BusyScope : IDisposable
    {
        private Action<bool> _busySetter;
        
        public BusyScope(Action<bool> busySetter)
        {
            _busySetter = busySetter;
            _busySetter(true);
        }

        public void Dispose() => _busySetter(false);
    }

    // 스왑 트랜잭션 관리. Commit 안 되면 Dispose 시점에 원상복구함.
    public class SwapTransaction : IDisposable
    {
        private GridSystem<PuzzleTile> _grid;
        private PuzzleTile _t1, _t2;
        private bool _isCommitted;

        public SwapTransaction(GridSystem<PuzzleTile> grid, PuzzleTile t1, PuzzleTile t2)
        {
            _grid = grid;
            _t1 = t1;
            _t2 = t2;
            
            // 생성과 동시에 일단 스왑
            _grid.SwapNodes(_t1.X, _t1.Y, _t2.X, _t2.Y);
            _isCommitted = false;
        }

        // 매칭 성공 시 호출해서 롤백 방지
        public void Commit() => _isCommitted = true;

        public void Dispose()
        {
            if (!_isCommitted)
            {
                // Commit 안됐으면 다시 원위치
                _grid.SwapNodes(_t1.X, _t1.Y, _t2.X, _t2.Y);
            }
        }
    }
}
