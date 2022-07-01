using UnityEngine;

public class SafeArea : MonoBehaviour
{
    public RectTransform panel;
    Rect lastSafeArea = new Rect(0, 0, 0, 0);

    void ApplySafeArea(Rect area)
    {
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = Vector2.zero;

        var anchorMin = area.position;
        var anchorMax = area.position + area.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;
        panel.anchorMin = anchorMin;
        panel.anchorMax = anchorMax;

        lastSafeArea = area;
    }

    void Update()
    {
        if (panel == null) { return; }

        Rect safeArea = Screen.safeArea;

        if (safeArea != lastSafeArea)
        {
            ApplySafeArea(safeArea);
        }
    }
}