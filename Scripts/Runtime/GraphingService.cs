using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoyTheunissen.Graphing
{
    /// <summary>
    /// Responsible for drawing graphs, which is very useful for debugging.
    /// </summary>
    public sealed class GraphingService
    {
        private readonly Dictionary<string, Graph> graphsByName = new Dictionary<string, Graph>();
        public Dictionary<string, Graph> GraphsByName => graphsByName;

        private bool didLoadGraphingScene;

        // Will use a singleton pattern for now as this package is needed for projects that use dependency injection
        // instead of the service locator pattern.
        private static bool didCacheInstance;
        private static GraphingService instance;
        public static GraphingService Instance
        {
            get
            {
                if (!didCacheInstance)
                {
                    instance = new GraphingService();
                    didCacheInstance = true;
                }
                return instance;
            }
        }

        public delegate void GraphAddedHandler(GraphingService graphingService, Graph graph);
        public event GraphAddedHandler GraphAddedEvent;
        
        public delegate void GraphRemovedHandler(GraphingService graphingService, Graph graph);
        public event GraphRemovedHandler GraphRemovedEvent;

        public void Load()
        {
            LoadGraphingSetup();
        }

        private void LoadGraphingSetup()
        {
            if (didLoadGraphingScene)
                return;

            didLoadGraphingScene = true;
            
#if UNITY_EDITOR || ENABLE_GRAPHS
            GameObject rendererPrefab = Resources.Load<GameObject>("Graphing/Graphs Renderer");
            GameObject rendererInstance = Object.Instantiate(rendererPrefab);
            Object.DontDestroyOnLoad(rendererInstance);
#endif // UNITY_EDITOR || ENABLE_GRAPHS
        }

        public void Add(Graph graph)
        {
            LoadGraphingSetup();
            
            graphsByName.Add(graph.Name, graph);
            
            GraphAddedEvent?.Invoke(this, graph);
        }
        
        public void Remove(Graph graph)
        {
            graphsByName.Remove(graph.Name);
            
            GraphRemovedEvent?.Invoke(this, graph);
        }
        
#if UNITY_EDITOR || ENABLE_GRAPHS
        private void Update()
        {
            foreach (KeyValuePair<string, Graph> kvp in graphsByName)
            {
                kvp.Value.Update();
            }
        }
#endif // UNITY_EDITOR || ENABLE_GRAPHS
    }
}