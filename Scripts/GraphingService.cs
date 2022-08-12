using System.Collections.Generic;
using RoyTheunissen.Scaffolding.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoyTheunissen.Graphing
{
    /// <summary>
    /// Responsible for drawing graphs, which is very useful for debugging.
    /// </summary>
    public sealed class GraphingService : MonoBehaviour, IDefaultService
    {
        private readonly Dictionary<string, Graph> graphsByName = new Dictionary<string, Graph>();
        public Dictionary<string, Graph> GraphsByName => graphsByName;

        private bool didLoadGraphingScene;

        public delegate void GraphAddedHandler(GraphingService graphingService, Graph graph);
        public event GraphAddedHandler GraphAddedEvent;
        
        public delegate void GraphRemovedHandler(GraphingService graphingService, Graph graph);
        public event GraphRemovedHandler GraphRemovedEvent;

        private void Awake()
        {
            LoadGraphingScene();
        }

        private void LoadGraphingScene()
        {
            if (didLoadGraphingScene)
                return;

            didLoadGraphingScene = true;
            
#if UNITY_EDITOR || ENABLE_GRAPHS
            SceneManager.LoadScene("Graphing", LoadSceneMode.Additive);
#endif // UNITY_EDITOR || ENABLE_GRAPHS
        }

        public void Add(Graph graph)
        {
            LoadGraphingScene();
            
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
