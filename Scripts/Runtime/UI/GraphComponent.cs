using System;
using RoyTheunissen.Graphing.UI;
using UnityEngine;

namespace RoyTheunissen.Graphing
{
    /// <summary>
    /// Lets you use a graph by placing a component in your UI rather than creating a graph through the service and
    /// having it be drawn automatically. This gives you more control of the registration flow / the way it's
    /// visualized in the canvas.
    /// </summary>
    public sealed class GraphComponent : MonoBehaviour
    {
        [Serializable]
        public sealed class LineSettings
        {
            [SerializeField] private string name = "Value";
            public string Name => name;

            [SerializeField] private Color color = Color.green;
            public Color Color => color;
        }
        
        [Header("Settings")]
        [SerializeField] private LineSettings[] lineSettings = new LineSettings[1];
        
        [Header("Dependencies")]
        [SerializeField] private GraphUI graphUI;

        private bool didCacheGraph;
        private Graph cachedGraph;
        public Graph Graph
        {
            get
            {
                CacheGraph();
                return cachedGraph;
            }
        }

        private void Awake()
        {
            graphUI.Initialize(Graph);
        }

        private void CacheGraph()
        {
            if (didCacheGraph)
                return;
            
            didCacheGraph = true;
            cachedGraph = new Graph(lineSettings[0].Name, lineSettings[0].Color);
        }

        private void OnValidate()
        {
            // There should always be at least one line.
            if (lineSettings.Length < 1)
                lineSettings = new LineSettings[1];
        }
    }
}
