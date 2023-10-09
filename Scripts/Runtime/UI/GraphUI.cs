using System.Collections.Generic;
using UnityEngine;

namespace RoyTheunissen.Graphing.UI
{
    /// <summary>
    /// Responsible for managing various components that visualize a single graph using canvas UI elements.
    /// </summary>
    public sealed class GraphUI : MonoBehaviour
    {
        [SerializeField] private RectTransform headerContainer;
        [SerializeField] private LineInfoUI lineInfoUiPrefab;
        
        [SerializeField] private GraphDataUI graphDataUi;
        public GraphDataUI DataUi => graphDataUi;

        private Graph graph;

        private Dictionary<GraphLine, LineInfoUI> lineInfoUisByLine = new Dictionary<GraphLine, LineInfoUI>();

        public void Initialize(Graph graph)
        {
            this.graph = graph;
            
            CreateLineInfoUiForPreExistingLines();

            graph.LineAddedEvent += HandleLineAddedEvent;

            graphDataUi.Initialize(graph);
        }

        private void Update()
        {
            // The graphing service is now a pure C# class and does not get an Update call.
            // Let's update the graphs as they are being visualized instead.
            graph.Update();
        }

        public void Cleanup()
        {
            graph.LineAddedEvent -= HandleLineAddedEvent;
            
            Destroy(gameObject);
        }

        private void CreateLineInfoUiForPreExistingLines()
        {
            foreach (GraphLine line in graph.Lines)
            {
                CreateUiForLine(line);
            }
        }

        private void HandleLineAddedEvent(Graph graph, GraphLine line)
        {
            CreateUiForLine(line);
        }

        private void CreateUiForLine(GraphLine line)
        {
            LineInfoUI lineInfoUi = Instantiate(lineInfoUiPrefab, headerContainer);
            lineInfoUi.Initialize(line);
            
            lineInfoUisByLine.Add(line, lineInfoUi);
        }
    }
}
