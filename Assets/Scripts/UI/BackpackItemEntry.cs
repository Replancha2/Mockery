using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class BackpackItemEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Color normalColor  = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color hoverColor   = Color.white;

    private TextMeshProUGUI _label;
    private System.Action   _onClick;

    public void Init(string text, System.Action onClick)
    {
        _label   = GetComponentInChildren<TextMeshProUGUI>();
        _onClick = onClick;

        if (_label != null)
        {
            _label.text  = text;
            _label.color = normalColor;
        }
    }

    public void OnPointerEnter(PointerEventData _)
    {
        if (_label != null) _label.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData _)
    {
        if (_label != null) _label.color = normalColor;
    }

    public void OnPointerClick(PointerEventData _) => _onClick?.Invoke();
}
