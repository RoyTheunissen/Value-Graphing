using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyTheunissen.Graphing
{
    /// <summary>
    /// One of possibly several lines in a graph. Visualizes points.
    /// </summary>
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
            if (valueGetter != null)
            {
                float currentValue = valueGetter();
                AddValue(currentValue);
            }
        }
    }
}