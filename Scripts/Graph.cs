using System;
using System.Collections.Generic;
using RoyTheunissen.Scaffolding.Services;
using UnityEngine;

namespace RoyTheunissen.Graphing
{
    /// <summary>
    /// A graph to debug continuously changing values.
    /// </summary>
    public class Graph
    {
        private const string DummyGraphName = "_Dummy";

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
                
                if (graphingService.Exists)
                {
                    if (value)
                        graphingService.Reference.Add(this);
                    else
                        graphingService.Reference.Remove(this);
                }
            }
        }

        private GraphLine DefaultLine => lines[0];

        private readonly List<GraphLine> lines = new List<GraphLine>();
        public List<GraphLine> Lines => lines;
        
        public float TimeStart => Mathf.Max(startTime, Time.time - duration);
        public float TimeEnd => Time.time;

        private bool IsDummy => Name == DummyGraphName;
        
        private static readonly Graph dummyGraph = new Graph(DummyGraphName);

        private static ServiceReference<GraphingService> graphingService = new ServiceReference<GraphingService>();
        
        public delegate void LineAddedHandler(Graph graph, GraphLine line);
        public event LineAddedHandler LineAddedEvent;

        public Graph(string name, Color color, Func<float> valueGetter = null, float duration = 3.0f)
        {
            this.duration = duration;
            
            startTime = Time.time;
            
            // Create a default graph line. You can add others if you wish.
            AddLine(name, color, valueGetter);
            
            // Auto-register.
            if (!IsDummy)
                IsRegistered = true;
        }

        public Graph(string name, Func<float> valueGetter = null) : this(name, Color.green, valueGetter)
        {
        }
        
        public void InternalCleanup()
        {
            IsRegistered = false;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                lines[i].PointAddedEvent -= HandlePointAddedEvent;
            }
        }

        public GraphLine AddLine(string name, Color color, Func<float> valueGetter = null)
        {
            GraphLine line = new GraphLine(lines.Count, name, color, valueGetter);
            lines.Add(line);
            
            line.PointAddedEvent += HandlePointAddedEvent;
            
            LineAddedEvent?.Invoke(this, line);
            
            return line;
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

        public void AddValue(float value, int lineIndex = 0)
        {
            if (lineIndex < 0 || lineIndex >= lines.Count)
            {
                Debug.LogWarning($"Tried to add value to invalid line index {lineIndex} for graph '{Name}'");
                return;
            }
            
            lines[lineIndex].AddValue(value);
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
            bool didExist = graphingService.Reference.GraphsByName.TryGetValue(name, out Graph graph);
            if (!didExist)
                graph = Create(name);
            return graph;
        }

        public static Graph Create(string name, Color color, Func<float> valueGetter = null, float duration = 3.0f)
        {
            return Create(true, name, color, valueGetter, duration);
        }

        public static Graph Create(bool isGraphEnabled, string name, Color color, Func<float> valueGetter = null, float duration = 3.0f)
        {
            return !isGraphEnabled ? dummyGraph : new Graph(name, color, valueGetter, duration);
        }

        public static Graph Create(string name, Func<float> valueGetter = null)
        {
            return Create(true, name, valueGetter);
        }

        public static Graph Create(bool isGraphEnabled, string name, Func<float> valueGetter = null)
        {
            return !isGraphEnabled ? dummyGraph : new Graph(name, valueGetter);
        }
    }
    
    public class GraphLine
    {
        private int index;
        public int Index => index;

        private string name;
        public string Name => name;

        private Color color;
        public Color Color => color;
        
        private List<GraphPoint> points = new List<GraphPoint>();
        public List<GraphPoint> Points => points;

        private Func<float> valueGetter;

        public delegate void PointAddedHandler(GraphLine graphLine, float value);
        public event PointAddedHandler PointAddedEvent;

        public GraphLine(int index, string name, Color color, Func<float> valueGetter = null)
        {
            this.index = index;
            this.name = name;
            this.color = color;
            this.valueGetter = valueGetter;
        }

        public GraphLine SetValueGetter(Func<float> valueGetter)
        {
            this.valueGetter = valueGetter;
            return this;
        }
        
        public void AddValue(float value)
        {
            points.Add(new GraphPoint(Time.time, value));
            PointAddedEvent?.Invoke(this, value);
        }

        public void CullPointsBefore(float time)
        {
            // Remove points that are too far big to show up anyway.
            for (int i = points.Count - 1; i >= 0; i--)
            {
                if (points[i].time < time)
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
