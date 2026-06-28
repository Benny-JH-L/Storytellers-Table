
using System.Collections.Generic;
using UnityEngine;

public struct Face
{
    public List<Vector3> vertices { get; private set; }
    public List<int> triangles { get; private set; }    // index of triangles
    public List<Vector2> uvs { get; private set; }

    public Face(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        this.uvs = uvs;
    }
};


[RequireComponent (typeof(MeshFilter))]
[RequireComponent (typeof(MeshRenderer))]
[RequireComponent (typeof(MeshCollider))]
public class HexRenderer : MonoBehaviour
{
    // Registry for flyweight mesh caching
    private static readonly Dictionary<(float innerSize, float outerSize, float height, bool isFlatTopped), Mesh> _meshRegistry = new();

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private HexPosLabel _hexPosLabel;

    [Header("Hex")]
    [SerializeField] private Material _material;    // only for debugging, will be removed
    public float innerSize;     // size of the inner hexagon (set to 0 for a normal solid hexagon)
    public float outerSize;     // size of the outer hexagon
    public float height;
    public bool isFlatTopped;


    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        GameObject tmp = new GameObject("label", typeof(HexPosLabel));
        _hexPosLabel = tmp.GetComponent<HexPosLabel>();
        _hexPosLabel.SetParent(this);
    }

    //private void OnEnable()   // causing erros bc it is called when outerRadius,inner radius and height are 0, only use when doing single tile viewing and editing
    //{
    //    DrawMesh();
    //}

    //comment this out when im satisfied
    //public void OnValidate()
    //{
    //    // Note: When a new HexRenderer is made, this is called first before any initialization takes place, so only use this when 
    //    // testing out tile settings so you see real time adjustments
    //    if (Application.isPlaying)
    //        DrawMesh();
    //}

    /// <summary>
    /// Generates the mesh and updates the MeshFilter, MeshRenderer, and hexPosLabel.
    /// </summary>
    public void DrawMesh()
    {
        //Debug.Log($"outer={outerSize} inner={innerSize} height={height} flatTopper={isFlatTopped}");

        /*
         * Instead of generating a mesh for each hex renderer instance, we can reuse the same mesh if they have the same
         * inner size, outer size, height, and is flat topped or not.
         */
        var meshKey = (innerSize, outerSize, height, isFlatTopped);

        // Try to get the cached mesh with key
        if (!_meshRegistry.TryGetValue(meshKey, out Mesh sharedMesh) || sharedMesh == null)
        {
            // create the new mesh for the registry
            sharedMesh = new Mesh();
            sharedMesh.name = $"Shared_HexMesh_{innerSize}_{outerSize}_{height}_{isFlatTopped}";

            List<Face> faces = _DrawFaces();
            _CombineFaces(sharedMesh, faces);

            // add the generated mesh to the registry cache
            _meshRegistry[meshKey] = sharedMesh;
        }
        //Debug.Log($"Verts: {_mesh.vertexCount}  Tris: {_mesh.triangles.Length / 3}");

        // update mesh filter
        _meshFilter.sharedMesh = sharedMesh;

        _hexPosLabel.UpdateOffset();
    }

    /// <summary>
    /// Draws and returns `faces` for the hexagon's, top, bottom, inner, and outer sides
    /// </summary>
    /// <returns></returns>
    private List<Face> _DrawFaces()
    {
        List<Face> faces = new List<Face>();

        // Top faces
        for (int point = 0; point < 6; point++)
            faces.Add(_CreateFace(innerSize, outerSize, height / 2f, height / 2f, point));

        // Bottom faces
        for (int point = 0; point < 6; point++)
            faces.Add(_CreateFace(innerSize, outerSize, -height / 2f, -height / 2f, point, true));

        // Outer faces, for sides on the outer hexagon
        for (int point = 0; point < 6; point++)
            faces.Add(_CreateFace(outerSize, outerSize, height / 2f, -height / 2f, point, true));

        // Inner faces, for sides on the inner hexagon
        for (int point = 0; point < 6; point++)
            faces.Add(_CreateFace(innerSize, innerSize, height / 2f, -height / 2f, point));

        return faces;
    }

    /// <summary>
    /// Computes the vertices, triangle indices, and uvs for a face.
    /// </summary>
    /// <param name="innerRad"></param>
    /// <param name="outerRad"></param>
    /// <param name="heightA"></param>
    /// <param name="heightB"></param>
    /// <param name="point"></param>
    /// <param name="reverse"></param>
    /// <returns>Return's a Face with vertices, triangle indices, and uvs.</returns>
    private Face _CreateFace(float innerRad, float outerRad, float heightA, float heightB, int point, bool reverse = false)
    {
        Vector3 pointA = GetPoint(innerRad, heightB, point);
        Vector3 pointB = GetPoint(innerRad, heightB, (point < 5) ? point + 1 : 0);  // we filter the point index here so that our last face triangle connects properly to the first
        Vector3 pointC = GetPoint(outerRad, heightA, (point < 5) ? point + 1 : 0);  // we filter the point index here so that our last face triangle connects properly to the first
        Vector3 pointD = GetPoint(outerRad, heightA, point);

        List<Vector3> vertices = new List<Vector3>() {pointA, pointB, pointC, pointD};
        List<int> triangles = new List<int>() { 0, 1, 2, 2, 3, 0}; // draws two triangles. Triangle 1: 0-1-2, Triangle 2: 2-3-0 (ie draws a quad)
        List<Vector2> uvs = new List<Vector2>() { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)}; // one for each point
        
        if (reverse)
            vertices.Reverse();

        return new Face(vertices, triangles, uvs);
    }

    /// <summary>
    /// helper function for CreateFace(). Calculates the position of points.
    /// </summary>
    /// <param name="size"></param>
    /// <param name="height"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    protected Vector3 GetPoint(float size, float height, int index)
    {
        float angle_deg = isFlatTopped ? (60 * index) : (60 * index) - 30;
        float angle_rad = Mathf.PI / 180f * angle_deg;  // radians
        return new Vector3((size * Mathf.Cos(angle_rad)), height, (size * Mathf.Sin(angle_rad)));
    }

    /// <summary>
    /// Get all the disjointed faces and flatten their vertices and UVs into single, global lists.
    /// Because all vertices are combined into one massive list, we must offset the local
    /// triangle indices so they point to the correct vertex position in the global array.
    /// 
    /// See function implementation for example.
    /// </summary>
    private void _CombineFaces(Mesh targetMesh, List<Face> faces)
    {
        targetMesh.Clear(); // ensure the mesh clear before adding to it
        /*
         * Get all the disjointed faces and flatten their vertices and UVs into single, global lists.
         * Because all vertices are combined into one massive list, we must offset the local 
         * triangle indices so they point to the correct vertex position in the global array.
         * 
         * Ex. 
         * face[0].vertices = {A, B, C, D} (Size: 4) -> Local triangles: 0, 1, 2, 2, 3, 0 -> ie A, B, C, C, D, A
         * face[1].vertices = {E, F, G, H} (Size: 4) -> Local triangles: 0, 1, 2, 2, 3, 0 -> ie E, F, G, G, H, E
         * 
         * Combined vertices = {A, B, C, D, E, F, G, H...} (Total Size: 24 faces * 4 = 96 vertices)
         * Combined uvs      = {...} (Total Size: 96)
         * 
         * Combined triangles = {
         * 0, 1, 2, 2, 3, 0,   // Face 0 (Offset 0): Draws 2 triangles in Clockwise order
         * 4, 5, 6, 6, 7, 4,   // Face 1 (Offset 4): Points to E, F, G, H
         * 8, 9, 10, 10, 11, 8 // Face 2 (Offset 8)...
         * }
        */
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < faces.Count; i++)
        {
            // Add the vertices
            vertices.AddRange(faces[i].vertices);  // get the face's vertices and add it to the list
            uvs.AddRange(faces[i].uvs);            // of those vertices get and add their uvs.

            // Offset the triangles
            int offset = (4 * i);
            foreach (int triangle in faces[i].triangles)
                triangles.Add(triangle +  offset);
        }

        targetMesh.vertices = vertices.ToArray();
        targetMesh.triangles = triangles.ToArray();
        targetMesh.uv = uvs.ToArray();
        targetMesh.RecalculateNormals();
    }

    public void SetMaterial(Material newMaterial)
    {
        _meshRenderer.sharedMaterial = newMaterial;
        //_material = newMaterial;
    }

    /// <summary>
    /// Set the displayed hex (axial) coordinate, q, r of the tile.
    /// </summary>
    /// <param name="hexCoord"></param>
    public void SetHexText(HexCoord hexCoord)
    {
        _hexPosLabel.SetText(hexCoord);
    }


}
