using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Framework.PuzzleSystem
{
    public class PuzzleTile : MonoBehaviour, IGridNode
    {
        #region Enums & Consts
        public enum TileType { Red, Blue, Green, Yellow, Purple }
        #endregion

        #region Data Fields
        public TileType type;

        public int X { get; private set; }
        public int Y { get; private set; }

        [Header("Settings")]
        public bool isMovable = true;      // 장애물 체크용
        public bool isMatchable = true;    // 매칭 대상 여부

        [SerializeField] protected SpriteRenderer _renderer;
        #endregion

        // 초기 세팅
        public virtual void Init(int x, int y, TileType type)
        {
            this.X = x;
            this.Y = y;
            this.type = type;
            
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            
            _renderer.color = type switch
            {
                TileType.Red => Color.red,
                TileType.Blue => Color.blue,
                TileType.Green => Color.green,
                TileType.Yellow => Color.yellow,
                TileType.Purple => new Color(0.5f, 0, 0.5f),
                _ => Color.white
            };
        }

        #region Interface Impls
        public void SetCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vector3 GetTargetPosition() => new Vector3(X, Y, 0);
        #endregion

        #region Interaction & Events
        public virtual void OnHit() { }
        public virtual void OnMatchAdjacent() => OnHit();
        #endregion

        #region Animations
        // 타일 이동 연출
        public async UniTask MoveToTargetAsync(float duration = 0.2f)
        {
            await transform.DOLocalMove(GetTargetPosition(), duration)
                .SetEase(Ease.OutQuad)
                .ToUniTask();
        }

        // 파괴 연출
        public async UniTask PlayDestroyAnim()
        {
            await transform.DOScale(Vector3.zero, 0.15f)
                .SetEase(Ease.InBack)
                .ToUniTask();

            if (this != null && gameObject != null)
                Destroy(gameObject);
        }
        
        // 선택 시 살짝 키우기
        public void Select(bool isSelected)
        {
            float targetScale = isSelected ? 1.15f : 1.0f;
            transform.DOScale(targetScale, 0.1f).SetEase(Ease.OutBack);
        }
        #endregion
    }
}
