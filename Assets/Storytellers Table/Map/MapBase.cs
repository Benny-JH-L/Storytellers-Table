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
    public Material material;

    [Header("Other")]
    string mapID;
    Dictionary<HexCoord, GameObject> _tiles;

    // shared stuff: convert mouse pixel pos -> hex, etc... 


    public MapBase(string id)
    {
        outerSize = 1f;
        innerSize = 0f;
        height = 1f;
        mapID = id;
    }

    public abstract void Setup();

    private void Awake()
    {
        Setup();
    }

    private void OnEnable()
    {
        //Debug.Log("yo1");
        LayoutMap();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            //Debug.Log("yo2");
            LayoutMap();
        }
    }

    public void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame) // will prolly be relocated
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                HexCoord hexCoord = WorldToAxial(hit.point);
                Debug.Log($"Hit Point: {hit.point} | Computed Axial: {hexCoord}");

                // To verify it maps to an actual tile object in your dictionary:
                if (_tiles != null && _tiles.TryGetValue(hexCoord, out GameObject hitTile))
                {
                    Debug.Log($"Successfully clicked on object: {hitTile.name} at Key: {hexCoord}");
                }
                else
                {
                    Debug.LogWarning($"Hit registered at {hexCoord}, but no matching tile key was found in the dictionary.");
                }

                // need to give the hex renderer a collider (cause u can't click on it kek)... and need to get the axial coordainte then based on that i need to 
                // find the position on the world map for it, then place down a tile there.
                // (i dont think i need to do this): also may need to edit how layout map creates the map, so it conforms to an axial coordinate system
            }
        }
    }

    private void LayoutMap()
    {
        if (_tiles == null)
            _tiles = new Dictionary<HexCoord, GameObject>();

        ClearTiles();

        for (int y = 0; y < mapSize.y; y++)
        {
            for (int x = 0; x < mapSize.x; x++)
            {
                // determine the true axial coodinates based on grid orientation
                int q = isFlatTopped ? x : x - (y + (y & 1)) / 2;
                int r = isFlatTopped ? y - (x + (x & 1)) / 2 : y;
                HexCoord hexCoord = new HexCoord(q, r);

                GameObject tile = new GameObject($"Hex ({hexCoord.q},{hexCoord.r})", typeof(HexRenderer));
                tile.transform.position = GetPositionForHexFromCoordinate(new Vector2Int(x, y));

                HexRenderer hexRenderer = tile.GetComponent<HexRenderer>();
                hexRenderer.outerSize = outerSize;
                hexRenderer.innerSize = innerSize;
                hexRenderer.height = height;
                hexRenderer.isFlatTopped = isFlatTopped;
                hexRenderer.SetMaterial(material);
                hexRenderer.DrawMesh();
                hexRenderer.SetHexText(hexCoord);

                tile.transform.SetParent(transform, true);   

                // store the tile
                _tiles[hexCoord] = tile;
            }
        }
    }

    /// <summary>
    /// Clears and destroys all map tiles on this map
    /// </summary>
    private void ClearTiles()
    {
        foreach (KeyValuePair<HexCoord, GameObject> pair in _tiles)
        {
            Destroy(pair.Value);
        }
        _tiles.Clear();
    }

    /// <summary>
    /// Computes the position for the hex tile based on row and col
    /// </summary>
    /// <param name="coordinate"></param>
    /// <returns></returns>
    private Vector3 GetPositionForHexFromCoordinate(Vector2Int coordinate)
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
        float worldZ = worldPos.z;

        if (!isFlatTopped)
        {
            // Pointy-top matrix inversion transformation
            fracQ = (Mathf.Sqrt(3f) / 3f * worldX - 1f / 3f * -worldZ) / size;
            fracR = (2f / 3f * -worldZ) / size;
        }
        else
        {
            // Flat-top matrix inversion transformation
            fracQ = (2f / 3f * worldX) / size;
            fracR = (-1f / 3f * worldX + MathF.Sqrt(3f) / 3f * -worldZ) / size;
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

    /// <summary>
    /// Convert from axial Hex coordinate to cube coordinate
    /// </summary>
    /// <param name="hexCoord"></param>
    /// <returns></returns>
    public CubeCoord AxialToCube(HexCoord hexCoord)
    {
        return new CubeCoord(hexCoord.q, hexCoord.r);
    }

    /// <summary>
    /// Convert from cube coordinate to axial hex coordinate
    /// </summary>
    /// <param name="cubeCoord"></param>
    /// <returns></returns>
    public HexCoord CubeToAxial(CubeCoord cubeCoord)
    {
        return new HexCoord(cubeCoord.q, cubeCoord.r);
    }

}
