using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyTheunissen.Graphing
{
    /// <summary>
    /// A graph to debug continuously changing values.
    /// </summary>
    public class Graph
    {
        private static readonly Color[] DefaultLineColours =
            { Color.green, Color.red, Color.blue, Color.yellow, Color.cyan, Color.magenta, Color.white };
        
        public string Name => DefaultLine.Name;
        
        public Color Color => DefaultLine.Color;

        private float duration;
        public float Duration => duration;
        
        private bool shouldCullOldPoints = true;
        public bool ShouldCullOldPoints
        {
            get => shouldCullOldPoints;
            set => shouldCullOldPoints = value;
        }

        private float startTime;
        
        private bool isPaused;
        public bool IsPaused
        {
            get => isPaused;
            set => isPaused = value;
        }

        private float valueMin;
        private float valueMax = 1.0f;
        public float ValueMin => valueMin;
        public float ValueMax => valueMax;

        private bool isRegistered;
        public bool IsRegistered
        {
            get => isRegistered;
            
            [Obsolete("Please use UpdateRegistration or Register/Unregister instead.")]
            set => UpdateRegistration(value);
        }

        public GraphLine DefaultLine => lines[0];

        private readonly List<GraphLine> lines = new List<GraphLine>();
        private readonly Dictionary<string, GraphLine> linesByName = new Dictionary<string, GraphLine>();
        public List<GraphLine> Lines => lines;

        public int LineCount => lines.Count;
        
        public float TimeStart => Mathf.Max(startTime, GraphTime - duration);
        public float TimeEnd => GraphTime;

        private float graphTime;
        public float GraphTime => graphTime;

        public delegate void LineAddedHandler(Graph graph, GraphLine line);
        public event LineAddedHandler LineAddedEvent;

        public Graph(
            string name, Color color, Func<float> valueGetter = null,
            GraphLine.Modes mode = GraphLine.Modes.ContinuousLine, float duration = 3.0f)
        {
            this.duration = duration;

            startTime = GraphTime;

            // Create a default graph line. You can add others if you wish.
            AddLine(name, color, mode, valueGetter);

            // Auto-register.
            // NOTE: As of 0.1.0 graphs no longer auto-register themselves in the constructor. If you want to create
            // an auto-registered graph, use Graph.Create. This is to better support non-throwaway graphs that are
            // part of some debugging GUI in the game. If you'd prefer it to work the way it did before,
            // you must enable the VALUE_GRAPHING_GRAPH_CONSTRUCTOR_AUTO_REGISTERS scripting define symbol.
#if VALUE_GRAPHING_GRAPH_CONSTRUCTOR_AUTO_REGISTERS
            UpdateRegistration(true);
#endif // VALUE_GRAPHING_GRAPH_CONSTRUCTOR_AUTO_REGISTERS
        }

        public Graph(string name, Func<float> valueGetter = null, GraphLine.Modes mode = GraphLine.Modes.ContinuousLine)
            : this(name, GetDefaultColorForLine(0), valueGetter, mode)
        {
        }

        public void InternalCleanup()
        {
            Unregister();

            for (int i = lines.Count - 1; i >= 0; i--)
            {
                lines[i].PointAddedEvent -= HandlePointAddedEvent;
            }
            lines.Clear();
            linesByName.Clear();
        }
        
        public Graph UpdateRegistration(bool value)
        {
            if (value)
                Register();
            else
                Unregister();
            return this;
        }

        public Graph Register()
        {
            if (isRegistered)
                return this;
            
            isRegistered = true;
            GraphingService.Instance.Add(this);
            return this;
        }
        
        public Graph Unregister()
        {
            if (!isRegistered)
                return this;
            
            isRegistered = false;
            GraphingService.Instance.Remove(this);
            return this;
        }
        
        public Graph SetShouldCullOldPoints(bool shouldCullOldPoints)
        {
            this.shouldCullOldPoints = shouldCullOldPoints;
            return this;
        }

        public Graph SetIsPaused(bool isPaused)
        {
            this.isPaused = isPaused;
            return this;
        }

        public Graph SetDuration(float duration)
        {
            this.duration = duration;
            return this;
        }
        
        public Graph SetMinValue(float minValue)
        {
            valueMin = minValue;
            return this;
        }
        
        public Graph SetMaxValue(float maxValue)
        {
            valueMax = maxValue;
            return this;
        }
        
        public Graph SetRange(float minValue, float maxValue)
        {
            valueMin = minValue;
            valueMax = maxValue;
            return this;
        }

        public GraphLine AddLine(string name, Color color, 
            GraphLine.Modes mode = GraphLine.Modes.ContinuousLine, Func<float> valueGetter = null)
        {
            GraphLine line = new GraphLine(this, lines.Count, name, color, mode, valueGetter);
            lines.Add(line);
            linesByName.Add(name, line);

            line.PointAddedEvent += HandlePointAddedEvent;

            LineAddedEvent?.Invoke(this, line);

            return line;
        }

        private static Color GetDefaultColorForLine(int index)
        {
            return DefaultLineColours[index % DefaultLineColours.Length];
        }
        
        private Color GetDefaultColorForNewLine()
        {
            return GetDefaultColorForLine(lines.Count);
        }

        public GraphLine AddLine(string name)
        {
            return AddLine(name, GetDefaultColorForNewLine());
        }

        public GraphLine GetLine(
            string name, Color color,
            GraphLine.Modes mode = GraphLine.Modes.ContinuousLine, Func<float> valueGetter = null)
        {
            bool didExist = linesByName.TryGetValue(name, out GraphLine line);
            return didExist ? line : AddLine(name, color, mode, valueGetter);
        }

        public GraphLine GetLine(string name)
        {
            return GetLine(name, GetDefaultColorForNewLine());
        }
        
        public GraphLine GetLine(int index)
        {
            if (index < 0 || index >= lines.Count)
                return null;
            
            return lines[index];
        }

        private void HandlePointAddedEvent(GraphLine graphLine, float value)
        {
            if (value < valueMin)
                valueMin = value;
            if (value > valueMax)
                valueMax = value;
        }

        private void CullOldPoints()
        {
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i].CullPointsBefore(GraphTime - duration);
            }
        }

        private GraphLine TryGetLineForValueAdding(int lineIndex)
        {
            if (lineIndex < 0)
            {
                Debug.LogWarning($"Tried to add value to invalid line index {lineIndex} for graph '{Name}'");
                return null;
            }

            // If we didn't have a line yet for the specified index, make one now.
            if (lineIndex >= lines.Count)
            {
                for (int i = lines.Count; i <= lineIndex; i++)
                {
                    AddLine($"Value #{i + 1}");
                }
            }

            return lines[lineIndex];
        }

        public Graph AddValue(float value, int lineIndex = 0)
        {
            GraphLine graphLine = TryGetLineForValueAdding(lineIndex);
            graphLine?.AddValue(value);
            
            return this;
        }
        
        public Graph AddValue(float value, string name)
        {
            return GetLine(name).AddValue(value).Graph;
        }

        public Graph AddValue(bool value, int lineIndex = 0)
        {
            GraphLine graphLine = TryGetLineForValueAdding(lineIndex);
            graphLine?.AddValue(value);
            
            return this;
        }
        
        public Graph AddValue(bool value, string name)
        {
            return GetLine(name).AddValue(value).Graph;
        }
        
        public Graph SetThreshold(float value, int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= lines.Count)
            {
                Debug.LogWarning($"Tried to add value to invalid line index {lineIndex} for graph '{Name}'");
                return this;
            }
            
            lines[lineIndex].SetThreshold(value);
            return this;
        }
        
        public void Update()
        {
            if (!IsPaused)
                graphTime += Time.deltaTime;
            
            foreach (GraphLine line in lines)
            {
                line.Update();
            }
            
            if (shouldCullOldPoints)
                CullOldPoints();
        }

        /// <summary>
        /// Gets an existing global graph or creates one if it didn't exist.
        /// Automatically visualized by the graph service.
        /// </summary>
        public static Graph Get(string name)
        {
            return Get(name, GetDefaultColorForLine(0));
        }

        /// <summary>
        /// Gets an existing global graph or creates one if it didn't exist.
        /// Automatically visualized by the graph service.
        /// </summary>
        public static Graph Get(string name, Color color, Func<float> valueGetter = null, 
            GraphLine.Modes mode = GraphLine.Modes.ContinuousLine, float duration = 3.0f)
        {
            bool didExist = GraphingService.Instance.GraphsByName.TryGetValue(name, out Graph graph);
            if (!didExist)
                graph = CreateGlobal(name, color, valueGetter, mode, duration);
            return graph;
        }

        /// <summary>
        /// Create a new globally registered graph. Automatically visualized by the graph service.
        /// </summary>
        [Obsolete("Graph.Create has been renamed to Graph.CreateGlobal for clarity.")]
        public static Graph Create(string name, Color color, Func<float> valueGetter = null, 
            GraphLine.Modes mode = GraphLine.Modes.ContinuousLine, float duration = 3.0f)
        {
            return CreateGlobal(name, color, valueGetter, mode, duration).Register();
        }
        
        /// <summary>
        /// Create a new globally registered graph. Automatically visualized by the graph service.
        /// </summary>
        public static Graph CreateGlobal(string name, Color color, Func<float> valueGetter = null, 
            GraphLine.Modes mode = GraphLine.Modes.ContinuousLine, float duration = 3.0f)
        {
            return new Graph(name, color, valueGetter, mode, duration).Register();
        }

        public static void SetIsPausedForAll(bool isPaused)
        {
            foreach (KeyValuePair<string, Graph> nameGraphPair in GraphingService.Instance.GraphsByName)
            {
                nameGraphPair.Value.IsPaused = isPaused;
            }
        }

        public GraphLine this[int index] => GetLine(index);
        public GraphLine this[string name] => GetLine(name);
    }

    public static class GraphExtensions
    {
        // Made this an extension method so that you don't have to do a null check on it first.
        public static void Cleanup(this Graph graph)
        {
            graph?.InternalCleanup();
        }
    }
}
