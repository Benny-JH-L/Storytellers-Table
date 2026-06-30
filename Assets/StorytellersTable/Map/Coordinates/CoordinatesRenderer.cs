
using System.Collections.Generic;
using StorytellersTable.Core.Data;
using UnityEngine;
using TMPro;

namespace StorytellersTable.Renderer
{
    /// <summary>
    /// Handles axial (hex) coordinate visuals.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class CoordinatesRenderer : MonoBehaviour
    {
        [SerializeField] private Canvas canvas; // canvas that renders the hex tile positions of TextMeshProUGUI
        [SerializeField] private float _yOffset = 0.15f;
        [SerializeField] private float _fontSize = 0.55f;
        [SerializeField] private Vector2 _windowSize = new Vector2(1f, 2f);

        [SerializeField] private bool showLabels; // toggle label visibility

        // Stores the labels at a given hex coordinate
        private Dictionary<HexCoord, TextMeshProUGUI> _hexLabels;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            showLabels = true;
            _hexLabels = new();
        }

        //comment this out when im satisfied
        public void OnValidate()
        {
            if (Application.isPlaying)
            {
                foreach (var pair in _hexLabels)
                {
                    TextMeshProUGUI tmpGUI = pair.Value;
                    tmpGUI.GetComponent<RectTransform>().sizeDelta = _windowSize;  // Update window size
                    tmpGUI.fontSize = _fontSize;
                    Vector3 pos = tmpGUI.transform.position;
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

            // Create the label
            TextMeshProUGUI tmpGUI = new GameObject($"Hex{tileData.hexCoord.ToString()}", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            tmpGUI.transform.SetParent(this.transform, true);

            // Set the labels position in the world
            Vector3 pos = StorytellersTable.Campaign.Modes.MapEditMode.GetPositionFromAxial(hexCoord); // ensure correct position is used
            pos.y += _yOffset + (tileData.height / 2f) + tileData.yPos;   // offset y based on tile data
            tmpGUI.transform.position = pos;
            tmpGUI.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // other set up
            tmpGUI.GetComponent<RectTransform>().sizeDelta = _windowSize;
            tmpGUI.fontSize = _fontSize;
            tmpGUI.alignment = TextAlignmentOptions.Center;

            // Set text
            tmpGUI.text = $"{hexCoord.q}\n{hexCoord.r}";

            tmpGUI.enabled = showLabels;
            _hexLabels[hexCoord] = tmpGUI;
        }

        /// <summary>
        /// Removes label based on TileData's hex coordinate.
        /// </summary>
        /// <param name="tileData"></param>
        public void RemoveLabel(TileData tileData)
        {
            if (_hexLabels.TryGetValue(tileData.hexCoord, out TextMeshProUGUI tmpGUI))
            {
                _hexLabels.Remove(tileData.hexCoord);
                Destroy(tmpGUI.gameObject);
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
