using System.Collections.Generic;
using RoyTheunissen.Scaffolding.Services;
using UnityEngine;

namespace RoyTheunissen.Graphing.UI
{
    /// <summary>
    /// Responsible for creating and destroying canvas-based visualizers for graphs.
    /// </summary>
    public sealed class GraphCanvasVisualizer : MonoBehaviour
    {
        [SerializeField] private RectTransform graphUiContainer;
        [SerializeField] private GraphUI graphUiPrefab;
        
        [SerializeField] private GameObject[] objectsThatShouldPersistBetweenScenes;
        
        private Dictionary<Graph, GraphUI> graphUis = new Dictionary<Graph, GraphUI>();
        public Dictionary<Graph, GraphUI> GraphUis => graphUis;

        private ServiceReference<GraphingService> graphingService = new ServiceReference<GraphingService>();

        private void Awake()
        {
            foreach (GameObject go in objectsThatShouldPersistBetweenScenes)
            {
                DontDestroyOnLoad(go);
            }
            
            CreateUiForPreExistingGraphs();

            graphingService.Reference.GraphAddedEvent += HandleGraphAddedEvent;
            graphingService.Reference.GraphRemovedEvent += HandleGraphRemovedEvent;
        }

        private void OnDestroy()
        {
            if (graphingService.Exists)
            {
                graphingService.Reference.GraphAddedEvent -= HandleGraphAddedEvent;
                graphingService.Reference.GraphRemovedEvent -= HandleGraphRemovedEvent;
            }
        }

        private void CreateUiForPreExistingGraphs()
        {
            foreach (KeyValuePair<string, Graph> kvp in graphingService.Reference.GraphsByName)
            {
                CreateUiForGraph(kvp.Value);
            }
        }

        private void HandleGraphAddedEvent(GraphingService graphingService, Graph graph)
        {
            CreateUiForGraph(graph);
        }

        private void CreateUiForGraph(Graph graph)
        {
            GraphUI graphUi = Instantiate(graphUiPrefab, graphUiContainer);
            graphUi.Initialize(graph);
            graphUis.Add(graph, graphUi);
        }

        private void HandleGraphRemovedEvent(GraphingService graphingService, Graph graph)
        {
            bool existed = graphUis.TryGetValue(graph, out GraphUI graphUi);
            if (!existed)
                return;

            graphUi.Cleanup();
            graphUis.Remove(graph);
        }
    }
}
