using UnityEngine;

namespace RoyTheunissen.Graphing
{
    /// <summary>
    /// Quickly shows off how to make a graph.
    /// </summary>
    public class GraphingExample : MonoBehaviour
    {
        private Graph complexGraph;
        
        private void Awake()
        {
            complexGraph = Graph.CreateGlobal("Sine Wave", Color.green);
            complexGraph.AddLine("Cosine Wave", Color.red);
        }

        private void OnDestroy()
        {
            complexGraph.Cleanup();
        }

        private void Update()
        {
            // If you want a neat and tidy complex graph with multiple lines, this is the best way to do that.
            complexGraph.AddValue(Mathf.Sin(Time.time * Mathf.PI));
            complexGraph.AddValue(Mathf.Cos(Time.time * Mathf.PI), 1);
            
            // If you just want to quickly look at a value, this is the fastest way to do that.
            // This will not get cleaned up when the scene is exited, so you should only do this if you're debugging
            // a value, and then remove the graphing code before you commit your work.
            Graph.Get("Inline Graph Test").AddValue(Mathf.Repeat(Time.time, 1.0f));
        }
    }
}
