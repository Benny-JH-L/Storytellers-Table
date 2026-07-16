
using StorytellersTable.Utility.Log;
using System.Collections.Generic;
using UnityEngine;

// Resource for basic idea: https://www.youtube.com/watch?v=EPaSmQ2vtek

namespace StorytellersTable.Renderer
{
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

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class HexRenderer : MonoBehaviour
    {
        // Registry for flyweight mesh caching
        private static readonly Dictionary<(float innerSize, float outerSize, float height, bool isFlatTopped), Mesh> _meshRegistry = new();

        // Cached Shader Property IDs for high-performance stringless lookups
        private static readonly int IsHighlightedProp = Shader.PropertyToID("_IsHighlighted");
        private static readonly int IsSelectedProp = Shader.PropertyToID("_IsSelected");
        private static readonly int IsGhostProp = Shader.PropertyToID("_IsGhost");
        private static readonly int RiseStartTimeProp = Shader.PropertyToID("_Rise_Start_Time");

        // Property block
        private MaterialPropertyBlock _materialPropertyBlock;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        // Cached state flags to prevent redundant GPU pipeline operations and state check roundtrips
        private bool _isHighlighted;
        private bool _isSelected;
        private bool _isGhost;

        [Header("Hex properties")]
        public float innerSize;     // size of the inner hexagon (set to 0 for a normal solid hexagon)
        public float outerSize;     // size of the outer hexagon
        public float height;        // TileData's height represent this

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        private void OnEnable()
        {
            _materialPropertyBlock = new MaterialPropertyBlock();

            _isHighlighted = false;
            _isSelected = false;
            _isGhost = false;
        }

        //comment this out when im satisfied
        public void OnValidate()
        {
            // Note: When a new HexRenderer is made, this is called first before any initialization takes place, so only use this when 
            // testing out tile settings so you see real time adjustments
            if (Application.isPlaying)
                DrawMesh();
        }

        #region Highlight & Selection & Ghost Visual states

        public void ToggleHighlight()
        {
            EnableHighlight(!_isHighlighted);
        }

        /// <summary>
        /// Toggles the hover/highlight visual state of the hex tile. 
        /// Uses MaterialPropertyBlocks to ensure GPU instancing and batching remain unbroken.
        /// </summary>
        /// <param name="isHighlighted">True to enable highlight, false to disable.</param>
        public void EnableHighlight(bool isHighlighted)
        {
            // return if we're setting the same state again
            if (_isHighlighted == isHighlighted) 
                return;

            _isHighlighted = isHighlighted;

            // Fetch the current block to preserve any other per-instance properties
            _meshRenderer.GetPropertyBlock(_materialPropertyBlock);

            if (_isHighlighted)
            {
                // Assign starting hover time
                _materialPropertyBlock.SetFloat(RiseStartTimeProp, Time.time);

                // Assign our specific state (1 for true, 0 for false)
                _materialPropertyBlock.SetFloat(IsHighlightedProp, 1f);

                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                //DebugOut.Log(this, "Hover start time: " + Time.time);
                #endif
            }
            else
            {
                // Reset highlight state and timer
                _materialPropertyBlock.SetFloat(RiseStartTimeProp, -999f);
                _materialPropertyBlock.SetFloat(IsHighlightedProp, 0f);
            }

            // Reapply the block to the renderer
            _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        public void ToggleSelectedVisual()
        {
            SetSelectedVisual(!_isSelected);
        }

        /// <summary>
        /// Toggles the selection visual state of the hex tile.
        /// Uses MaterialPropertyBlocks to ensure GPU instancing and batching remain unbroken.
        /// </summary>
        /// <param name="isSelected">True to enable selection outline/color, false to disable.</param>
        public void SetSelectedVisual(bool isSelected)
        {
            if (_isSelected == isSelected)
                return;
            
            _isSelected = isSelected;

            _meshRenderer.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetFloat(IsSelectedProp, isSelected ? 1f : 0f);
            _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        public void ToggleGhostVisual()
        {
            //_meshRenderer.GetPropertyBlock(_materialPropertyBlock);
            //float ghostVal = _materialPropertyBlock.GetFloat(IsGhostProp);
            //SetGhostVisual(ghostVal > 0 ? false : true);
            SetGhostVisual(!_isGhost);
        }

        /// <summary>
        /// Toggles the ghost visual state of the hex tile.
        /// Uses MaterialPropertyBlocks to ensure GPU instancing and batching remain unbroken.
        /// </summary>
        /// <param name="isGhost"></param>
        public void SetGhostVisual(bool isGhost)
        {
            if (_isGhost == isGhost)
                return;

            _isGhost = isGhost;

            _meshRenderer.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetFloat(IsGhostProp, isGhost ? 1f : 0f); // change the `IsGhost` value on the shader graph
            _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        #endregion

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
            var meshKey = (innerSize, outerSize, height, Singleton.Instance.isFlatTopped);

            // Try to get the cached mesh with key
            if (!_meshRegistry.TryGetValue(meshKey, out Mesh sharedMesh) || sharedMesh == null)
            {
                // create the new mesh for the registry
                sharedMesh = new Mesh();
                sharedMesh.name = $"Shared_HexMesh_{innerSize}_{outerSize}_{height}_{Singleton.Instance.isFlatTopped}";

                List<Face> faces = _DrawFaces();
                _CombineFaces(sharedMesh, faces);

                // add the generated mesh to the registry cache
                _meshRegistry[meshKey] = sharedMesh;
            }
            //Debug.Log($"Verts: {_mesh.vertexCount}  Tris: {_mesh.triangles.Length / 3}");

            // update mesh filter
            _meshFilter.sharedMesh = sharedMesh;

            //_hexPosLabel.UpdateOffset();
        }

        #region Face generation
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

            List<Vector3> vertices = new List<Vector3>() { pointA, pointB, pointC, pointD };
            List<int> triangles = new List<int>() { 0, 1, 2, 2, 3, 0 }; // draws two triangles. Triangle 1: 0-1-2, Triangle 2: 2-3-0 (ie draws a quad)
            List<Vector2> uvs = new List<Vector2>() { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) }; // one for each point

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
            float angle_deg = Singleton.Instance.isFlatTopped ? (60 * index) : (60 * index) - 30;
            float angle_rad = Mathf.PI / 180f * angle_deg;  // radians
            return new Vector3((size * Mathf.Cos(angle_rad)), height, (size * Mathf.Sin(angle_rad)));
        }
        #endregion

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
                    triangles.Add(triangle + offset);
            }

            targetMesh.vertices = vertices.ToArray();
            targetMesh.triangles = triangles.ToArray();
            targetMesh.uv = uvs.ToArray();
            targetMesh.RecalculateNormals();
        }

        public void SetMaterial(Material newMaterial)
        {
            _meshRenderer.sharedMaterial = newMaterial;
        }

        public Material GetMaterial()
        {
            return _meshRenderer.sharedMaterial;
        }

        /// <summary>
        /// Set the material shader of the MeshRenderer.
        /// </summary>
        /// <param name="shader"></param>
        public void SetMaterialShader(Shader shader)
        {
            if (_meshRenderer.sharedMaterial != null)
                _meshRenderer.sharedMaterial.shader = shader;
        }

        public MeshRenderer GetMeshRenderer()
        {
            return _meshRenderer;
        }

    }

}
