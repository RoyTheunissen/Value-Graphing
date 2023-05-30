using System.Collections.Generic;
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

        private void Awake()
        {
            foreach (GameObject go in objectsThatShouldPersistBetweenScenes)
            {
                DontDestroyOnLoad(go);
            }
            
            CreateUiForPreExistingGraphs();

            GraphingService.Instance.GraphAddedEvent += HandleGraphAddedEvent;
            GraphingService.Instance.GraphRemovedEvent += HandleGraphRemovedEvent;
        }

        private void OnDestroy()
        {
            GraphingService.Instance.GraphAddedEvent -= HandleGraphAddedEvent;
            GraphingService.Instance.GraphRemovedEvent -= HandleGraphRemovedEvent;
        }

        private void CreateUiForPreExistingGraphs()
        {
            foreach (KeyValuePair<string, Graph> kvp in GraphingService.Instance.GraphsByName)
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
