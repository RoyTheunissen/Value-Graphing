using System;
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
        private readonly List<Graph> graphs = new List<Graph>();
        public List<Graph> Graphs => graphs;

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
            
            graphs.Add(graph);
            
            GraphAddedEvent?.Invoke(this, graph);
        }
        
        public void Remove(Graph graph)
        {
            graphs.Remove(graph);
            
            GraphRemovedEvent?.Invoke(this, graph);
        }
        
#if UNITY_EDITOR || ENABLE_GRAPHS
        private void Update()
        {
            foreach (Graph graph in graphs)
            {
                graph.Update();
            }
        }
#endif // UNITY_EDITOR || ENABLE_GRAPHS
    }
}
