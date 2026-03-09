using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
public class ShowTileMap : MonoBehaviour
{
    public Tilemap tilemap;
    public Camera targetCamera;
    public int marginCells = 2;
    public bool onlyDrawTiles = false;
    public float zOffset = 0f;

    private void Reset()
    {
        tilemap = GetComponent<Tilemap>();
    }

    private void OnDrawGizmos()
    {
        if (tilemap == null) return;

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        // Camera view bounds in world
        Vector3 w0 = cam.ViewportToWorldPoint(new Vector3(0f, 0f, cam.nearClipPlane));
        Vector3 w1 = cam.ViewportToWorldPoint(new Vector3(1f, 1f, cam.nearClipPlane));
        w0.z = 0f;
        w1.z = 0f;

        Vector3Int c0 = tilemap.WorldToCell(w0);
        Vector3Int c1 = tilemap.WorldToCell(w1);

        int xmin = Mathf.Min(c0.x, c1.x) - marginCells;
        int xmax = Mathf.Max(c0.x, c1.x) + marginCells;
        int ymin = Mathf.Min(c0.y, c1.y) - marginCells;
        int ymax = Mathf.Max(c0.y, c1.y) + marginCells;

        Grid grid = tilemap.layoutGrid;
        if (grid == null) return;

        Vector3 size = grid.cellSize;
        float z = transform.position.z + zOffset;

        for (int y = ymin; y <= ymax; y++)
        {
            for (int x = xmin; x <= xmax; x++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);

                if (onlyDrawTiles && !tilemap.HasTile(cell)) continue;

                Vector3 world = tilemap.CellToWorld(cell);

                Vector3 p0 = new Vector3(world.x, world.y, z);
                Vector3 p1 = new Vector3(world.x + size.x, world.y, z);
                Vector3 p2 = new Vector3(world.x + size.x, world.y + size.y, z);
                Vector3 p3 = new Vector3(world.x, world.y + size.y, z);

                Gizmos.DrawLine(p0, p1);
                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawLine(p2, p3);
                Gizmos.DrawLine(p3, p0);
            }
        }
    }
}
