using TMPro;
using UnityEngine;

namespace RoyTheunissen.Graphing.UI
{
    /// <summary>
    /// Displays the value of a corresponding horizontal line.
    /// </summary>
    public sealed class GridLineValueUI : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TMP_Text text;

        public void UpdatePosition(float position, float value)
        {
            text.text = value.ToString("0.0");
            
            rectTransform.anchorMin = new Vector2(rectTransform.anchorMin.x, position);
            rectTransform.anchorMax = new Vector2(rectTransform.anchorMax.x, position);
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, 0.0f);
        }
    }
}
