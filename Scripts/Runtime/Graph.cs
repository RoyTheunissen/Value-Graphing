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
            AddLine(name, color, valueGetter, mode);

            // Auto-register.
            IsRegistered = true;
        }

        public Graph(string name, Func<float> valueGetter = null, GraphLine.Modes mode = GraphLine.Modes.ContinuousLine)
            : this(name, Color.green, valueGetter, mode)
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

        public GraphLine AddLine(
            string name, Color color, Func<float> valueGetter = null,
            GraphLine.Modes mode = GraphLine.Modes.ContinuousLine)
        {
            GraphLine line = new GraphLine(this, lines.Count, name, color, valueGetter, mode);
            lines.Add(line);
            linesByName.Add(name, line);

            line.PointAddedEvent += HandlePointAddedEvent;

            LineAddedEvent?.Invoke(this, line);

            return line;
        }

        public GraphLine GetLine(
            string name, Color color, Func<float> valueGetter = null,
            GraphLine.Modes mode = GraphLine.Modes.ContinuousLine)
        {
            bool didExist = linesByName.TryGetValue(name, out GraphLine line);
            return didExist ? line : AddLine(name, color, valueGetter, mode);
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
            if (lineIndex < 0 || lineIndex >= lines.Count)
            {
                Debug.LogWarning($"Tried to add value to invalid line index {lineIndex} for graph '{Name}'");
                return this;
            }
            
            lines[lineIndex].AddValue(value);
            return this;
        }
        
        public Graph AddValue(bool value, int lineIndex = 0)
        {
            return AddValue(value ? 1.0f : 0.0f, lineIndex);
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

#if UNITY_EDITOR || ENABLE_GRAPHS
        public void Update()
        {
            foreach (GraphLine line in lines)
            {
                line.Update();
            }
            
            CullOldPoints();
        }
#endif // UNITY_EDITOR || ENABLE_GRAPHS

        public static Graph Get(string name)
        {
            return Get(name, Color.green);
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
    
    public class GraphLine
    {
        public enum Modes
        {
            ContinuousLine,
            VerticalLineAtEveryPoint,
            Threshold,
        }
        
        private Graph graph;
        public Graph Graph => graph;

        private int index;
        public int Index => index;

        private string name;
        public string Name => name;

        private Color color;
        public Color Color => color;

        private Modes mode;
        public Modes Mode => mode;

        private List<GraphPoint> points = new List<GraphPoint>();
        public List<GraphPoint> Points => points;

        private Func<float> valueGetter;

        public delegate void PointAddedHandler(GraphLine graphLine, float value);
        public event PointAddedHandler PointAddedEvent;

        public GraphLine(
            Graph graph, int index, string name, Color color, Func<float> valueGetter = null, Modes mode = Modes.ContinuousLine)
        {
            this.graph = graph;
            this.index = index;
            this.name = name;
            this.color = color;
            this.valueGetter = valueGetter;
            this.mode = mode;
        }

        public GraphLine SetValueGetter(Func<float> valueGetter)
        {
            this.valueGetter = valueGetter;
            return this;
        }
        
        public GraphLine SetMode(Modes mode)
        {
            this.mode = mode;
            return this;
        }
        
        public GraphLine AddValue(float value)
        {
            points.Add(new GraphPoint(Time.time, value));
            PointAddedEvent?.Invoke(this, value);
            return this;
        }

        public GraphLine SetThreshold(float value)
        {
            if (Mode == Modes.Threshold)
            {
                points.Clear();
                AddValue(value);
                return this;
            }
            
            Debug.LogWarning($"Trying to set threshold of line '{name}' " +
                             $"but we're not a threshold line, we are a {mode} line.");
            return this;
        }

        public void CullPointsBefore(float time)
        {
            // Remove points that wouldn't be visible anyway.
            for (int i = points.Count - 1; i >= 0; i--)
            {
                // NOTE: If there is a point after this one and that one ISN'T before the specified time, don't cull
                // it yet, because then this current point is useful for drawing a line from 0 to the first value.
                // The point would theoretically end up to the left of the graph (because it's smaller than MinTime)
                // but this is clamped anyway. This check prevents a gap forming at the left of the first valud point.
                bool hasNextPoint = i + 1 < points.Count;
                bool nextPointIsTooOld = hasNextPoint && points[i + 1].time < time;
                if (points[i].time < time && nextPointIsTooOld)
                    points.RemoveAt(i);
            }
        }

        public void Update()
        {
#if UNITY_EDITOR || ENABLE_GRAPHS
            if (valueGetter != null)
            {
                float currentValue = valueGetter();
                AddValue(currentValue);
            }
#endif // UNITY_EDITOR || ENABLE_GRAPHS
        }
    }

    public struct GraphPoint
    {
        public float time;
        public float value;

        public GraphPoint(float time, float value)
        {
            this.time = time;
            this.value = value;
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
