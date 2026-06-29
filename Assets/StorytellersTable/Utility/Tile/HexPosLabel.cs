
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class HexPosLabel : MonoBehaviour
{
    [SerializeField] private Canvas _canvas;
    [SerializeField] private TextMeshProUGUI _tmpUGUI;
    [SerializeField] private HexRenderer _parent;
    [SerializeField] private float _yOffset = 0.15f;
    [SerializeField] private float _fontSize = 0.55f;
    [SerializeField] private Vector2 _windowSize = new Vector2(1f, 2f);


    private void Awake()
    {
        _parent = null;
        _canvas = GetComponent<Canvas>();
        _canvas.AddComponent<TextMeshProUGUI>();
        
        _tmpUGUI = _canvas.GetComponentInChildren<TextMeshProUGUI>();
        Setup();
    }

    public void Setup()
    {
        _tmpUGUI.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        _tmpUGUI.GetComponent<RectTransform>().sizeDelta = _windowSize;
        _tmpUGUI.fontSize = _fontSize;
        _tmpUGUI.alignment = TextAlignmentOptions.Center;
    }

    // comment this out when im satisfied
    public void OnValidate()
    {
        if (Application.isPlaying && _parent != null)
        {
            Setup();
            UpdateOffset();
        }
    }

    /// <summary>
    /// Sets the parent HexRender transform for the label.
    /// </summary>
    /// <param name="parent"></param>
    public void SetParent(HexRenderer parent)
    {
        _parent = parent;
        this.transform.SetParent(parent.transform, true);
        UpdateOffset();
    }

    /// <summary>
    /// Updates the offset based on the parent HexRender's height.
    /// </summary>
    public void UpdateOffset()
    {
        // we do parent.height/2 bc the height is from top to bottom of the hex, we only want the center to top.
        _tmpUGUI.transform.position = _parent.transform.position + new Vector3(0f, _parent.height/2f + _yOffset, 0f);
    }

    public void SetText(HexCoord hexCoord)
    {
        _tmpUGUI.text = $"{hexCoord.q}\n{hexCoord.r}";
    }
}