using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyTheunissen.Graphing.UI
{
    /// <summary>
    /// Responsible for visualizing the information of a graph line (color and name).
    /// </summary>
    public sealed class LineInfoUI : MonoBehaviour
    {
        [SerializeField] private Image lineColorImage;
        [SerializeField] private TMP_Text lineNameText;

        public void Initialize(GraphLine line)
        {
            lineColorImage.color = line.Color;
            lineNameText.text = line.Name;
        }

        public void Cleanup()
        {
            Destroy(gameObject);
        }
    }
}
