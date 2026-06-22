using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Framework.Utils;

namespace Framework.PuzzleSystem
{
    public class PuzzleController : MonoBehaviour
    {
        #region Inspector Settings
        [Header("Grid Config")]
        [SerializeField, Range(3, 10)] private int _width = 6;
        [SerializeField, Range(3, 15)] private int _height = 8;
        
        [Header("Prefabs")]
        [SerializeField] private PuzzleTile _tilePrefab;
        [SerializeField] private PuzzleTile _iceTilePrefab;
        [SerializeField] private Transform _gridParent;
        #endregion

        private GridSystem<PuzzleTile> _grid;
        private IMatchStrategy<PuzzleTile> _strategy;
        
        private bool _isProcessing = false;
        private PuzzleTile _selectedTile;

        private List<UniTask> _animationTasks = new List<UniTask>();

        private async void Start()
        {
            // 시스템 초기 세팅. 3매치 전략 사용.
            _grid = new GridSystem<PuzzleTile>(_width, _height);
            _strategy = new Match3Strategy();
            
            // 그리드 데이터 바뀌면 연출 태스크에 자동 추가
            _grid.OnNodeChanged += (x, y, tile) => 
            {
                if (tile != null) _animationTasks.Add(tile.MoveToTargetAsync());
            };
            
            await InitializeGrid();
        }

        #region Core Gameplay
        // 리스트에 쌓인 모든 애니메이션 끝날 때까지 대기
        private async UniTask WaitForAnimations()
        {
            if (_animationTasks.Count == 0) return;
            
            var tasks = _animationTasks.ToList();
            _animationTasks.Clear();
            await UniTask.WhenAll(tasks);
        }

        // 보드 초기화. 시작하자마자 터지는 거 없게 반복해서 체크함.
        private async UniTask InitializeGrid()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++) SpawnTile(x, y, true);
            }
            await WaitForAnimations();

            while (true)
            {
                var matches = _strategy.FindAllMatches(_grid);
                if (matches.Count == 0) break;

                foreach (var node in matches)
                {
                    _grid.SetNode(node.X, node.Y, null);
                    Destroy(node.gameObject);
                    SpawnTile(node.X, node.Y, true);
                }
                await WaitForAnimations();
            }
        }

        // 타일 스폰. 10% 확률로 얼음 블록 섞어줌.
        private void SpawnTile(int x, int y, bool isInitial = false)
        {
            // TODO: 나중에 기획 쪽이랑 확률 밸런스 조정 필요
            bool spawnIce = Random.value < 0.1f;
            var prefab = spawnIce && _iceTilePrefab != null ? _iceTilePrefab : _tilePrefab;

            var tile = Instantiate(prefab, _gridParent);
            var type = (PuzzleTile.TileType)Random.Range(0, 5);
            tile.Init(x, y, type);
            
            // 처음엔 제자리, 리필일 땐 위에서 떨어지게
            tile.transform.localPosition = isInitial 
                ? tile.GetTargetPosition() 
                : new Vector3(x, _height, 0);

            _grid.SetNode(x, y, tile);
        }

        private void Update()
        {
            if (_isProcessing) return;

            // 마우스 클릭 감지
            if (UnityEngine.InputSystem.Mouse.current?.leftButton.wasPressedThisFrame == true)
            {
                HandleInput();
            }
        }

        // 클릭한 위치 계산해서 타일 선택/스왑 처리
        private void HandleInput()
        {
            var mousePos2D = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            var worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos2D.x, mousePos2D.y, 10f));
            
            int x = Mathf.RoundToInt(worldPos.x);
            int y = Mathf.RoundToInt(worldPos.y);

            if (!_grid.IsValid(x, y)) return;

            var clickedTile = _grid.GetNode(x, y);
            if (clickedTile == null) return;

            // 못 움직이는 타일(얼음 등)은 처음 선택 불가
            if (!clickedTile.isMovable && _selectedTile == null) return;

            if (_selectedTile == null)
            {
                _selectedTile = clickedTile;
                _selectedTile.Select(true);
            }
            else
            {
                if (_selectedTile == clickedTile)
                {
                    _selectedTile.Select(false);
                    _selectedTile = null;
                }
                else
                {
                    // 인접한 타일인지 확인 후 스왑 실행
                    bool isNeighbor = Mathf.Abs(_selectedTile.X - clickedTile.X) + Mathf.Abs(_selectedTile.Y - clickedTile.Y) == 1;
                    
                    if (clickedTile.isMovable && isNeighbor)
                    {
                        _selectedTile.Select(false);
                        ProcessSwap(_selectedTile, clickedTile).Forget();
                        _selectedTile = null;
                    }
                    else
                    {
                        // 다른 타일 클릭하면 선택 교체
                        _selectedTile.Select(false);
                        if (clickedTile.isMovable)
                        {
                            _selectedTile = clickedTile;
                            _selectedTile.Select(true);
                        }
                        else _selectedTile = null;
                    }
                }
            }
        }

        // 실제 스왑 로직. Transaction 패턴 써서 매칭 안되면 자동 롤백함.
        private async UniTaskVoid ProcessSwap(PuzzleTile t1, PuzzleTile t2)
        {
            using (new BusyScope(state => _isProcessing = state))
            {
                using (var swap = new SwapTransaction(_grid, t1, t2))
                {
                    await WaitForAnimations();

                    if (await ProcessMatches())
                    {
                        swap.Commit(); // 매칭 성공 시 데이터 확정
                    }
                }
                // Undo 연출까지 기다려줌
                await WaitForAnimations();
            }
        }
        #endregion

        #region Match & Cascade Logic
        // 터지는 로직 루프. 매칭 -> 파괴 -> 낙하 -> 리필 순서.
        private async UniTask<bool> ProcessMatches()
        {
            bool hasMatch = false;
            while (true)
            {
                var matches = _strategy.FindAllMatches(_grid);
                if (matches.Count == 0) break;

                hasMatch = true;

                // 주변 기믹 블록들한테 터졌다고 알림
                foreach (var m in matches) NotifyNeighbors(m.X, m.Y);

                // 파괴 애니메이션 실행
                await UniTask.WhenAll(matches.Select(m => m.PlayDestroyAnim()));
                foreach (var m in matches) _grid.SetNode(m.X, m.Y, null);

                // 얼음 블록 같은 거 HP 다 닳았는지 체크
                await CheckSpecialBlocks();
                // 빈칸 채우기
                await HandleFalling();
                await HandleRefill();
            }
            return hasMatch;
        }

        // 상하좌우 인접 타일에 타격 알림
        private void NotifyNeighbors(int x, int y)
        {
            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];

                if (_grid.IsValid(nx, ny))
                {
                    _grid.GetNode(nx, ny)?.OnMatchAdjacent();
                }
            }
        }

        // 특수 블록들 상태 확인해서 파괴 처리
        private async UniTask CheckSpecialBlocks()
        {
            var tasks = new List<UniTask>();
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_grid.GetNode(x, y) is IceTile ice && ice.CurrentHP <= 0)
                    {
                        _grid.SetNode(x, y, null);
                        tasks.Add(ice.PlayDestroyAnim());
                    }
                }
            }
            if (tasks.Count > 0) await UniTask.WhenAll(tasks);
        }

        // 빈 공간 위에서 타일들 떨어뜨리기
        private async UniTask HandleFalling()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_grid.GetNode(x, y) == null)
                    {
                        for (int k = y + 1; k < _height; k++)
                        {
                            var upperNode = _grid.GetNode(x, k);
                            if (upperNode != null && upperNode.isMovable)
                            {
                                _grid.SwapNodes(x, y, x, k);
                                break;
                            }
                        }
                    }
                }
            }
            await WaitForAnimations();
        }

        // 맨 윗줄 빈 곳들 새로 채우기
        private async UniTask HandleRefill()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_grid.GetNode(x, y) == null) SpawnTile(x, y);
                }
            }
            await WaitForAnimations();
        }
        #endregion
    }
}
