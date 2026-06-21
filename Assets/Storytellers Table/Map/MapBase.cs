using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Resource: https://www.youtube.com/watch?v=EPaSmQ2vtek

public abstract class MapBase : MonoBehaviour
{
    [Header("Grid Settings")]
    public Vector2Int mapSize;

    [Header("Tile Settings")]
    public float outerSize = 1f;
    public float innerSize = 0f;
    public float height = 1f;
    public bool isFlatTopped;
    public Material material; // default material of map

    [Header("Other")]
    public string mapID;
    [SerializeField] private AdjacencyList _adjacencyList;

    // strictly for the Unity inspector
    [Header("OnValidate()")]
    [SerializeField] private bool toggleOnValidate;

    // shared stuff: convert mouse pixel pos -> hex, etc...
    public abstract void Setup();
    public abstract TileBase CreateTileDataInstance(HexCoord hexCoord);

    private void Awake()
    {
        Setup();
        toggleOnValidate = false;
    }

    private void OnEnable()
    {
        _LayoutMap();
    }

    private void OnValidate()
    {
        if (toggleOnValidate && Application.isPlaying)
        {
            // what ever you want...
        }
    }

    /// <summary>
    /// Rebuilds the map
    /// </summary>
    [ContextMenu("Rebuild Map")] // In the Unity inspector, right click the map script, and select this option
    public void RebuildMap()
    {
        Debug.Log($"Re building map of size q={mapSize.x}, r={mapSize.y}...");
        _LayoutMap();
    }

    [ContextMenu("Re Draw Hex Tile Mesh")] // In the Unity inspector, right click the map script, and select this 
    public void ReDrawTileMesh()
    {
        Debug.Log("Re drawing tile mesh...");
        _adjacencyList.ReDrawTileMesh();
    }

    public void Update()
    {
        // testing purposes only, will prolly be relocated
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                HexCoord hexCoord = WorldToAxial(hit.point);
                Debug.Log($"Hit Point: {hit.point} | Computed Axial: {hexCoord}");

                // To verify it maps to an actual tile object in your dictionary:
                if (_adjacencyList != null && _adjacencyList.TryGetValue(hexCoord, out GameObject hitTile))
                {
                    //Debug.Log($"Successfully clicked on object: {hitTile.name} at Key: {hexCoord}");
                    if (hitTile.TryGetComponent<TileComponent>(out TileComponent tileComp))
                    {
                        Debug.Log($"Clicked tile data type: {tileComp.GetData<TileBase>().GetType().Name} at coord: {tileComp.HexCoord}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Hit registered at {hexCoord}, but no matching tile key was found in the dictionary.");

                    // place a tile at an empty space
                    if (Keyboard.current.leftShiftKey.isPressed)
                    {
                        Debug.Log($"Placing new tile at {hexCoord}.");
                        GameObject newTile = _GenerateTile(hexCoord);
                    }

                }
            }
        }
    }

    /// <summary>
    /// Generates a map using q, and r. q is the length of the map, and r is the height of the map.
    /// </summary>
    private void _LayoutMap()
    {
        if (_adjacencyList == null)
            _adjacencyList = new AdjacencyList();

        ClearTiles();

        // Generate a clean rectangular bound using axial loops
        for (int r = 0; r < mapSize.y; r++)
        {
            // Calculate the row offset dynamically to slice a straight vertical edge
            int offset = Mathf.FloorToInt(r / 2f);

            for (int q = -offset; q < mapSize.x - offset; q++)
            {
                // Capture the exact true coordinates
                HexCoord hexCoord = new HexCoord(q, r);

                // If flat-topped, the coordinate mapping swaps columns/rows for the offset orientation
                if (isFlatTopped)
                {
                    int qFlat = r;
                    int offsetFlat = Mathf.FloorToInt(qFlat / 2f);
                    int rFlat = q;
                    hexCoord = new HexCoord(qFlat, rFlat + offsetFlat);
                }

                GameObject tile = _GenerateTile(hexCoord);
            }
        }
    }

    /// <summary>
    /// Generates a hex tile at the specified hex axial, q & r, coordinate, and places it at the converted world space position, 
    /// then adds it to the map's adjacency list.
    /// </summary>
    /// <param name="hexCoord"></param>
    /// <returns></returns>
    private GameObject _GenerateTile(HexCoord hexCoord)
    {
        // Create GameObject with HexRenderer and TileComponent
        GameObject tile = new GameObject($"Hex ({hexCoord.q},{hexCoord.r})", typeof(HexRenderer), typeof(TileComponent));
        tile.transform.position = _GetPositionFromAxial(hexCoord);

        // Set up HexRenderer
        HexRenderer hexRenderer = tile.GetComponent<HexRenderer>();
        hexRenderer.outerSize = outerSize;
        hexRenderer.innerSize = innerSize;
        hexRenderer.height = height;
        hexRenderer.isFlatTopped = isFlatTopped;
        hexRenderer.SetMaterial(material);
        hexRenderer.DrawMesh();
        hexRenderer.SetHexText(hexCoord);

        // Set up and bridge the tile data context
        TileBase tileData = CreateTileDataInstance(hexCoord);
        TileComponent tileComponent = tile.GetComponent<TileComponent>();
        tileComponent.Setup(hexCoord, tileData);

        // parent the tile to the map GameObject
        tile.transform.SetParent(transform, true);  

        // add the tile to the adjacency map
        _adjacencyList[hexCoord] = tile;

        return tile;
    }

    /// <summary>
    /// Computes the exact 3D world position from the hex coordinate using structural basis vector matrix transformations.
    /// This removes all floating point tracking gaps and anchors the origin natively at (0,0,0).
    /// </summary>
    private Vector3 _GetPositionFromAxial(HexCoord coord)
    {
        float xPosition = 0f;
        float zPosition = 0f;
        float size = outerSize;

        if (!isFlatTopped)
        {
            // Pointy-Topped Basis Matrix 
            xPosition = size * (Mathf.Sqrt(3f) * coord.q + Mathf.Sqrt(3f) / 2f * coord.r);
            zPosition = size * (3f / 2f * coord.r);
        }
        else
        {
            // Flat-Topped Basis Matrix
            xPosition = size * (3f / 2f * coord.q);
            zPosition = size * (Mathf.Sqrt(3f) / 2f * coord.q + Mathf.Sqrt(3f) * coord.r);
        }

        // Inverting the Z axis to maintain your layout structure starting from top-left progression
        return new Vector3(xPosition, 0f, -zPosition);
    }

    /// <summary>
    /// Clears and destroys all map tiles on this map
    /// </summary>
    public void ClearTiles()
    {
        _adjacencyList.Clear();
    }

    /// <summary>
    /// [DEPRECATED] Computes the position for the hex tile based on row and col
    /// </summary>
    /// <param name="coordinate"></param>
    /// <returns></returns>
    private Vector3 _GetPositionForHexFromCoordinate(Vector2Int coordinate)
    {
        int column = coordinate.x;
        int row = coordinate.y;
        float width, height, xPosition, yPosition;
        bool shouldOffset;
        float horizontalDistance, verticalDistance, offset, size = outerSize;

        if (!isFlatTopped)
        {
            shouldOffset = (row % 2) == 0;
            width = Mathf.Sqrt(3) * size;
            height = 2f * size;

            horizontalDistance = width;
            verticalDistance = height * (3f / 4f);

            offset = (shouldOffset) ? width / 2 : 0;

            xPosition = (column * horizontalDistance) + offset;
            yPosition = (row * verticalDistance);
        }
        else
        {
            shouldOffset = (column % 2) == 0;
            width = 2f * size;
            height = Mathf.Sqrt(3) * size;

            horizontalDistance = width * (3f / 4f);
            verticalDistance = height;

            offset = (shouldOffset) ? height / 2 : 0;
            xPosition = (column * horizontalDistance);
            yPosition = (row * verticalDistance) - offset;
        }

        return new Vector3(xPosition, 0, -yPosition); // since i want to layout the tiles from the top left, we inverse the z-position
    }

    /// <summary>
    /// Converts a 3D world position (using X and Y) into a discrete integer Axial HexCoord.
    /// </summary>
    /// <param name="worldPos"></param>
    /// <returns></returns>
    public HexCoord WorldToAxial(Vector3 worldPos)
    {
        float size = outerSize;
        float fracQ, fracR;
        float worldX = worldPos.x;
        float worldZ = -worldPos.z; // apply layout space restoration up front

        if (!isFlatTopped)
        {
            // Pointy-top matrix inversion transformation
            fracQ = (Mathf.Sqrt(3f) / 3f * worldX - 1f / 3f * worldZ) / size;
            fracR = (2f / 3f * worldZ) / size;
        }
        else
        {
            // Flat-top matrix inversion transformation
            fracQ = (2f / 3f * worldX) / size;
            fracR = (-1f / 3f * worldX + MathF.Sqrt(3f) / 3f * worldZ) / size;
        }

        // Convert to 3D cube coordinates to do robust rounding (ensuring q + r + s = 0)
        float fracS = -fracQ - fracR;

        int q = Mathf.RoundToInt(fracQ);
        int r = Mathf.RoundToInt(fracR);
        int s = Mathf.RoundToInt(fracS);

        // Calculate the rounding deltas
        float qDiff = Mathf.Abs(q - fracQ);
        float rDiff = Mathf.Abs(r - fracR);
        float sDiff = Mathf.Abs(s - fracS);

        // Re-adjust the axis with the largest rounding error to satisfy q + r + s = 0
        if (qDiff > rDiff && qDiff > sDiff)
        {
            q = -r - s;
        }
        else if (rDiff > sDiff)
        {
            r = -q - s;
        }
        // (If sDiff is largest, no adjustments to q or r are required)

        return new HexCoord(q, r);
    }

}
