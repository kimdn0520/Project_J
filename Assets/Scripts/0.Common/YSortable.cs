using UnityEngine;

namespace Core
{
    /// <summary>
    /// Dynamic Y-Sorting component for 2D Top-Down games.
    /// Adjusts SpriteRenderer sortingOrder dynamically based on Y position.
    /// Objects lower on the screen (smaller Y) will be rendered in front of objects higher on the screen.
    /// Supports parent-relative sorting for items placed on top of furniture (e.g. Memo paper on Desk).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class YSortable : MonoBehaviour
    {
        [Header("Sorting Settings")]
        [SerializeField] private int baseOrder = 5000;
        [SerializeField] private float yOffset = 0f;
        [SerializeField] private bool isStatic = false;

        [Header("Parent Relative Sorting (For Desk Items, Memos, etc.)")]
        [SerializeField] private bool followParentYSort = false;
        [SerializeField] private int orderOffsetFromParent = 1;

        private SpriteRenderer spriteRenderer;
        private YSortable parentYSort;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (followParentYSort && transform.parent != null)
            {
                parentYSort = transform.parent.GetComponentInParent<YSortable>();
            }
        }

        private void Start()
        {
            UpdateSortingOrder();
        }

        private void LateUpdate()
        {
            if (!isStatic || followParentYSort)
            {
                UpdateSortingOrder();
            }
        }

        public void UpdateSortingOrder()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer == null) return;

            if (followParentYSort)
            {
                if (parentYSort == null && transform.parent != null)
                {
                    parentYSort = transform.parent.GetComponentInParent<YSortable>();
                }

                if (parentYSort != null)
                {
                    spriteRenderer.sortingOrder = parentYSort.GetSortingOrder() + orderOffsetFromParent;
                    return;
                }
            }

            // Calculate sorting order based on Y position (multiplied by 100 for precision)
            float sortY = transform.position.y + yOffset;
            spriteRenderer.sortingOrder = baseOrder - Mathf.RoundToInt(sortY * 100f);
        }

        public int GetSortingOrder()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            return spriteRenderer != null ? spriteRenderer.sortingOrder : 0;
        }

        public void SetStatic(bool bStatic)
        {
            isStatic = bStatic;
        }

        public void SetYOffset(float offset)
        {
            yOffset = offset;
        }

        public void SetFollowParent(bool follow, int offsetFromParent = 1)
        {
            followParentYSort = follow;
            orderOffsetFromParent = offsetFromParent;
        }
    }
}
