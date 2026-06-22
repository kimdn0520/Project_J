using UnityEngine;

public class GridLineRenderer : MonoBehaviour
{
    [SerializeField] private Grid grid;
    [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private int gridSizeRange = 100; // 그릴 범위 그리드 개수

    private Material lineMaterial;

    private void Awake()
    {
        if (grid == null) grid = GetComponent<Grid>();
        CreateLineMaterial();
    }

    private void CreateLineMaterial()
    {
        if (lineMaterial != null) return;
        
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader != null)
        {
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    private void OnRenderObject()
    {
        if (Camera.current != Camera.main) return;
        if (grid == null) return;

        CreateLineMaterial();
        if (lineMaterial == null) return;

        lineMaterial.SetPass(0);

        GL.Begin(GL.LINES);
        GL.Color(lineColor);

        // 세로 격자선 그리기
        for (int x = -gridSizeRange; x <= gridSizeRange; x++)
        {
            Vector3 start = grid.CellToWorld(new Vector3Int(x, -gridSizeRange, 0));
            Vector3 end = grid.CellToWorld(new Vector3Int(x, gridSizeRange, 0));
            GL.Vertex3(start.x, start.y, 0);
            GL.Vertex3(end.x, end.y, 0);
        }

        // 가로 격자선 그리기
        for (int y = -gridSizeRange; y <= gridSizeRange; y++)
        {
            Vector3 start = grid.CellToWorld(new Vector3Int(-gridSizeRange, y, 0));
            Vector3 end = grid.CellToWorld(new Vector3Int(gridSizeRange, y, 0));
            GL.Vertex3(start.x, start.y, 0);
            GL.Vertex3(end.x, end.y, 0);
        }

        GL.End();
    }
}
