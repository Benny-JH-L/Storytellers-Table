
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using Assets.StorytellersTable.Core.Map;

namespace StorytellersTable.Renderer
{
    /// <summary>
    /// Handles axial (hex) coordinate visuals.
    /// </summary>
    //[RequireComponent(typeof(Canvas))]
    public class CoordinatesRenderer : MonoBehaviour
    {
        //[SerializeField] private Canvas canvas; // canvas that renders the hex tile positions of TextMeshProUGUI
        [SerializeField] private float _yOffset = 0.25f;
        [SerializeField] private float _fontSize = 5f;
        [SerializeField] private Vector2 _windowSize = new Vector2(1f, 2f);

        [SerializeField] private bool showLabels; // toggle label visibility

        [Range(15, 20)]
        [SerializeField] private int renderDistance = 15;    // radius to render labels relative to camera rig

        // Stores the labels at a given hex coordinate
        private Dictionary<HexCoord, TextMeshPro> _hexLabels;

        private void Awake()
        {
            //canvas = GetComponent<Canvas>();
            showLabels = true;
            _hexLabels = new();
        }

        private void Update()
        {
            // Filter labels to display
            HexCoord camRigHexCoord = HexMath.WorldToAxial(Singleton.Instance.cameraController.transform.position);
            HashSet<HexCoord> tmpSet = new();
            tmpSet.Add(camRigHexCoord);
            HexMath.GetHexRingArea(camRigHexCoord, renderDistance, tmpSet);

            Vector3 camForward = Camera.main.transform.forward;
            // Update each label
            foreach ((HexCoord hexCoord, TextMeshPro tmp) in _hexLabels)
            {
                if (!tmpSet.Contains(hexCoord))
                {
                    tmp.enabled = false;
                    continue;
                }

                tmp.transform.forward = camForward;
                tmp.enabled = true;
            }
        }

        //comment this out when im satisfied
        public void OnValidate()
        {
            if (Application.isPlaying)
            {
                foreach (var pair in _hexLabels)
                {
                    TextMeshPro tmp = pair.Value;
                    tmp.GetComponent<RectTransform>().sizeDelta = _windowSize;  // Update window size
                    tmp.fontSize = _fontSize;
                    Vector3 pos = tmp.transform.position;
                    //pos.y = pos.y + (pos.y - _yOffset);
                    //tmpGUI.transform.position = pos;
                }
            }
        }

        /// <summary>
        /// Adds a label based on TileData's hex coordinate, and elevation.
        /// </summary>
        /// <param name="tileData"></param>
        public void AddLabel(TileData tileData)
        {
            HexCoord hexCoord = tileData.hexCoord;

            // check for existing label at hexcoord
            if (_hexLabels.TryGetValue(hexCoord, out var label))
            {
                Vector3 oldPos = label.transform.position;
                Vector3 newPos = label.transform.position;
                newPos.y = ComputeLabelOffset(tileData);

                newPos = (newPos.y > oldPos.y ? newPos : oldPos);   // take only the greater

                label.transform.position = newPos;
                return;
            }

            // Create the label
            TextMeshPro tmp = new GameObject($"Hex{tileData.hexCoord.ToString()}", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            tmp.transform.SetParent(this.transform, true);

            // Set the labels position in the world
            Vector3 pos = HexMath.GetPositionFromAxial(hexCoord); // ensure correct position is used
            pos.y += ComputeLabelOffset(tileData);
            tmp.transform.position = pos;
            //tmp.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            
            Vector3 camFwd = Camera.main.transform.forward;
            tmp.transform.forward = camFwd;

            // other set up
            tmp.rectTransform.sizeDelta = _windowSize;
            tmp.enableAutoSizing = false;
            tmp.fontSize = _fontSize;
            tmp.alignment = TextAlignmentOptions.Center;

            // Set text
            //tmpGUI.text = $"{hexCoord.q}\n{hexCoord.r}";
            tmp.text = $"{hexCoord.q}\n{hexCoord.r}\n{hexCoord.ToCube().s}";

            tmp.enabled = showLabels;
            _hexLabels[hexCoord] = tmp;
        }
        private float ComputeLabelOffset(TileData tileData)
        {
            //return (_yOffset + tileData.mapLayer.Y() + (Singleton.Instance.height / 2f));
            return (_yOffset + tileData.mapLayer.Y());
        }

        /// <summary>
        /// Removes label based on TileData's hex coordinate.
        /// </summary>
        /// <param name="tileData"></param>
        public void RemoveLabel(TileData tileData)
        {
            if (_hexLabels.TryGetValue(tileData.hexCoord, out TextMeshPro tmp))
            {
                _hexLabels.Remove(tileData.hexCoord);
                Destroy(tmp.gameObject);
            }
        }

        /// <summary>
        /// Toggles labels.
        /// </summary>
        [ContextMenu("Toggle Labels")]
        public void ToggleLabels()
        {
            showLabels = !showLabels;
            foreach (var pair in _hexLabels)
            {
                pair.Value.enabled = showLabels;
            }
        }

        /// <summary>
        /// Clears all labels.
        /// </summary>
        public void ClearLabels()
        {
            foreach (var pair in _hexLabels)
                Destroy(pair.Value.gameObject);
            
            _hexLabels.Clear();
        }
    }
}
