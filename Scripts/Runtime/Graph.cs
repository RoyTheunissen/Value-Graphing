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

        private float startTime;

        private float valueMin;
        private float valueMax = 1.0f;
        public float ValueMin => valueMin;
        public float ValueMax => valueMax;

        private bool isRegistered;
        public bool IsRegistered
        {
            get => isRegistered;
            set
            {
                if (isRegistered == value)
                    return;
                
                isRegistered = value;
                
                if (value)
                    GraphingService.Instance.Add(this);
                else
                    GraphingService.Instance.Remove(this);
            }
        }

        private GraphLine DefaultLine => lines[0];

        private readonly List<GraphLine> lines = new List<GraphLine>();
        private readonly Dictionary<string, GraphLine> linesByName = new Dictionary<string, GraphLine>();
        public List<GraphLine> Lines => lines;
        
        public float TimeStart => Mathf.Max(startTime, Time.time - duration);
        public float TimeEnd => Time.time;

        public delegate void LineAddedHandler(Graph graph, GraphLine line);
        public event LineAddedHandler LineAddedEvent;

        public Graph(
            string name, Color color, Func<float> valueGetter = null,
            GraphLine.Modes mode = GraphLine.Modes.ContinuousLine, float duration = 3.0f)
        {
            this.duration = duration;

            startTime = Time.time;

            // Create a default graph line. You can add others if you wish.
            AddLine(name, color, mode, valueGetter);

            // Auto-register.
            IsRegistered = true;
        }

        public Graph(string name, Func<float> valueGetter = null, GraphLine.Modes mode = GraphLine.Modes.ContinuousLine)
            : this(name, GetDefaultColorForLine(0), valueGetter, mode)
        {
        }

        public void InternalCleanup()
        {
            IsRegistered = false;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                lines[i].PointAddedEvent -= HandlePointAddedEvent;
            }
            lines.Clear();
            linesByName.Clear();
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
                lines[i].CullPointsBefore(Time.time - duration);
            }
        }

        public Graph AddValue(float value, int lineIndex = 0)
        {
            if (lineIndex < 0)
            {
                Debug.LogWarning($"Tried to add value to invalid line index {lineIndex} for graph '{Name}'");
                return this;
            }

            // If we didn't have a line yet for the specified index, make one now.
            if (lineIndex >= lines.Count)
            {
                for (int i = lines.Count; i <= lineIndex; i++)
                {
                    AddLine($"Value #{i + 1}");
                }
            }
            
            lines[lineIndex].AddValue(value);
            return this;
        }
        
        public Graph AddValue(float value, string name)
        {
            return GetLine(name).AddValue(value).Graph;
        }

        public Graph AddValue(bool value, int lineIndex = 0)
        {
            return AddValue(value ? 1.0f : 0.0f, lineIndex);
        }
        
        public Graph AddValue(bool value, string name)
        {
            return AddValue(value ? 1.0f : 0.0f, name);
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
            foreach (GraphLine line in lines)
            {
                line.Update();
            }
            
            CullOldPoints();
        }

        public static Graph Get(string name)
        {
            return Get(name, GetDefaultColorForLine(0));
        }

        /// <summary>
        /// Gets an existing line or creates one if it didn't exist.
        /// </summary>
        public static Graph Get(string name, Color color, Func<float> valueGetter = null, 
            GraphLine.Modes mode = GraphLine.Modes.ContinuousLine, float duration = 3.0f)
        {
            bool didExist = GraphingService.Instance.GraphsByName.TryGetValue(name, out Graph graph);
            if (!didExist)
                graph = Create(name, color, valueGetter, mode, duration);
            return graph;
        }

        /// <summary>
        /// Create a new line.
        /// </summary>
        public static Graph Create(string name, Color color, Func<float> valueGetter = null, 
            GraphLine.Modes mode = GraphLine.Modes.ContinuousLine, float duration = 3.0f)
        {
            return new Graph(name, color, valueGetter, mode, duration);
        }
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
