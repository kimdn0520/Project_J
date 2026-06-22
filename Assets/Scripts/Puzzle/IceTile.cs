using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Framework.PuzzleSystem
{
    // 특수기믹: 몇 번 때려야 깨지는 얼음 블록. 안 움직임.
    public class IceTile : PuzzleTile
    {
        [SerializeField] private int _hp = 2;
        
        public override void Init(int x, int y, TileType type)
        {
            base.Init(x, y, type);
            
            // 얼음이라 고정 상태
            isMovable = false;
            isMatchable = false;
            
            UpdateVisual();
        }

        public override void OnHit()
        {
            _hp--;
            
            if (_hp <= 0)
            {
                // 파괴 처리는 컨트롤러가 체크해서 실행함
            }
            else
            {
                // 맞았을 때 살짝 흔드는 연출
                PlayHitAnim().Forget();
                UpdateVisual();
            }
        }

        public int CurrentHP => _hp;

        // 남은 HP에 따라서 투명도 조절
        private void UpdateVisual()
        {
            float alpha = 0.4f + (_hp * 0.2f);
            _renderer.color = new Color(0.5f, 0.8f, 1f, alpha);
        }

        private async UniTaskVoid PlayHitAnim()
        {
            await transform.DOShakePosition(0.15f, 0.1f, 10, 90, false, true)
                .ToUniTask();
        }
    }
}
