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
            
            [SerializeField] private GraphLine.Modes mode = GraphLine.Modes.ContinuousLine;
            public GraphLine.Modes Mode => mode;
            
            [SerializeField] private float threshold;
            public float Threshold => threshold;
        }
        
        [Header("Settings")]
        [SerializeField] private LineSettings[] lineSettings = new LineSettings[1];

        [SerializeField] private float rangeMin;
        [SerializeField] private float rangeMax = 1.0f;
        
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
            
            // Create the graph / the initial line.
            cachedGraph = new Graph(lineSettings[0].Name, lineSettings[0].Color, null, lineSettings[0].Mode);
            cachedGraph.SetRange(rangeMin, rangeMax);
            if (lineSettings[0].Mode == GraphLine.Modes.Threshold)
                cachedGraph.GetLine(0).SetThreshold(lineSettings[0].Threshold);
            
            // Create any additional lines.
            for (int i = 1; i < lineSettings.Length; i++)
            {
                cachedGraph.AddLine(lineSettings[i].Name, lineSettings[i].Color, lineSettings[i].Mode);
                
                if (lineSettings[i].Mode == GraphLine.Modes.Threshold)
                    cachedGraph.GetLine(i).SetThreshold(lineSettings[i].Threshold);
            }
        }

        private void OnValidate()
        {
            // There should always be at least one line.
            if (lineSettings.Length < 1)
                lineSettings = new LineSettings[1];

            // Made sure the minimum range is always smaller than the maximum range.
            float range1 = rangeMin;
            float range2 = rangeMax;
            rangeMin = Mathf.Min(range1, range2);
            rangeMax = Mathf.Max(range1, range2);
        }
    }
}
