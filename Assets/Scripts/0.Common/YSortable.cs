using UnityEngine;

namespace Core
{
    /// <summary>
    /// Dynamic Y-Sorting component for 2D Top-Down games.
    /// Adjusts SpriteRenderer sortingOrder dynamically based on Y position.
    /// Objects lower on the screen (smaller Y) will be rendered in front of objects higher on the screen.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class YSortable : MonoBehaviour
    {
        [Header("Sorting Settings")]
        [SerializeField] private int baseOrder = 5000;
        [SerializeField] private float yOffset = 0f;
        [SerializeField] private bool isStatic = false;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            UpdateSortingOrder();
        }

        private void LateUpdate()
        {
            if (!isStatic)
            {
                UpdateSortingOrder();
            }
        }

        public void UpdateSortingOrder()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                // Calculate sorting order based on Y position (multiplied by 100 for precision)
                float sortY = transform.position.y + yOffset;
                spriteRenderer.sortingOrder = baseOrder - Mathf.RoundToInt(sortY * 100f);
            }
        }

        public void SetStatic(bool bStatic)
        {
            isStatic = bStatic;
        }

        public void SetYOffset(float offset)
        {
            yOffset = offset;
        }
    }
}
