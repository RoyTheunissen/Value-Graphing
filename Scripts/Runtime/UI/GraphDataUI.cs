//#define NO_GRADIENTS
//#define GRADIENT_FOR_SINGLE_LINES_ONLY

using System.Collections.Generic;
using RoyTheunissen.Graphing.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace RoyTheunissen.Graphing.UI
{
    /// <summary>
    /// Visualizes the actual data of a graph using UI components.
    /// </summary>
    public sealed class GraphDataUI : MonoBehaviour 
    {
        private const float GradientUnderLineOpacity = 0.075f;
        
        [Header("Grid Line Values")]
        [SerializeField] private RectTransform gridLineValuesContainer;
        [SerializeField] private GridLineValueUI gridLineValueUiPrefab;
        [SerializeField] private RectTransform gridLinesContainer;
        
        [Header("Value Lines")]
        [SerializeField] private RawImage linesArea;
        [SerializeField] private Material material;
        [SerializeField] private Color gridColor = new Color(0.25f, 0.25f, 0.25f, 0);
        [SerializeField] private Color axisColor = new Color(0.5f, 0.5f, 0.5f, 0);

        private const int TargetLineCountHorizontal = 6;
        private const int TargetLineCountVertical = 10;

        private Canvas canvas;
        
        private Graph graph;
        
        private Pool<GridLineValueUI> horizontalGridLineValues;
        
        private readonly Vector3[] cachedRectCorners = new Vector3[4];
        private bool didCacheGridLinesCorners;

        private Vector3[] GridLinesCorners
        {
            get
            {
                if (!didCacheGridLinesCorners)
                {
                    didCacheGridLinesCorners = true;
                    gridLinesContainer.GetWorldCorners(cachedRectCorners);
                }
                return cachedRectCorners;
            }
        }
        
        private RenderTexture linesRenderTexture;
        private Vector2 linesRenderTextureSizeCachedFor;

        public delegate void HorizontalLineHandler(GraphDataUI graphDataUi, float value);
        public delegate void VerticalLineHandler(GraphDataUI graphDataUi, float value);

        public void Initialize(Graph graph)
        {
            this.graph = graph;

            canvas = GetComponentInParent<Canvas>();
            
            horizontalGridLineValues = new Pool<GridLineValueUI>(
                () => Instantiate(gridLineValueUiPrefab, gridLineValuesContainer),
                glv => Destroy(glv.gameObject),
                null, null, TargetLineCountHorizontal * 2 + 1);
        }
        
        private void OnEnable()
        {
            TryCacheRenderTexture();
        }

        private void OnDisable()
        {
            RenderTexture.ReleaseTemporary(linesRenderTexture);
        }

        private void OnDestroy()
        {
            horizontalGridLineValues.Cleanup();
        }

        private void LateUpdate()
        {
            UpdateGrid();
            
            DrawGraphLines();
        }
        
        private void TryCacheRenderTexture()
        {
            Rect lineAreaRect = linesArea.rectTransform.rect;
            
            if (linesRenderTextureSizeCachedFor == lineAreaRect.size && linesRenderTexture != null)
                return;

            if (linesRenderTexture != null)
            {
                RenderTexture.ReleaseTemporary(linesRenderTexture);
                linesRenderTexture = null;
                linesArea.enabled = false;
            }
            
            if (lineAreaRect.size == Vector2.zero)
                return;
            
            linesRenderTexture = RenderTexture.GetTemporary(
                (int)lineAreaRect.width, (int)lineAreaRect.height, 0, RenderTextureFormat.ARGB32, 0);
            linesRenderTexture.autoGenerateMips = false;
            linesRenderTexture.filterMode = FilterMode.Point;
            linesRenderTextureSizeCachedFor = lineAreaRect.size;
            linesArea.texture = linesRenderTexture;
            linesArea.enabled = true;
        }
        
        private void DrawGraphLines()
        {
            TryCacheRenderTexture();
            
            // Only if we have a valid render texture.
            if (!linesArea.enabled)
                return;
            
            RenderTexture previousActiveRenderTexture = RenderTexture.active;
            RenderTexture.active = linesRenderTexture;
            
            StartDrawing();
            
            ForEachHorizontalLine(DrawHorizontalLine);
            ForEachVerticalLine(DrawVerticalLine);

            foreach (GraphLine line in graph.Lines)
            {
                DrawGraphValueLine(graph, line);
            }
            
            StopDrawing();
            
            RenderTexture.active = previousActiveRenderTexture;
        }
        
        private static readonly List<Vector3> tempLineVertexPairs = new List<Vector3>();
        private static readonly List<Vector4> tempUnderLineGradientQuadVertices = new List<Vector4>();
        
        private void DrawGraphValueLine(Graph graph, GraphLine line)
        {
            Color color = line.Color;
            Color lineColor = new Color(color.r, color.g, color.b, color.a / 2);
            int pointCount = line.Points.Count;

#if NO_GRADIENTS
            const bool drawGradientUnderLine = false;
#else
            bool drawGradientUnderLine = line.Mode != GraphLine.Modes.Threshold;
    #if GRADIENT_FOR_SINGLE_LINES_ONLY
            drawGradientUnderLine = drawGradientUnderLine && graph.Lines.Count == 1;
    #endif // GRADIENT_FOR_SINGLE_LINES_ONLY
#endif // NO_GRADIENTS
            
            if (drawGradientUnderLine)
                tempUnderLineGradientQuadVertices.Clear();
            
            tempLineVertexPairs.Clear();

            if (line.Mode == GraphLine.Modes.Threshold)
            {
                if (line.Points.Count > 0)
                {
                    float thresholdValue = line.Points[line.Points.Count - 1].value;
                    Vector2 posLeft = GetNormalizedPosition(graph.TimeStart, thresholdValue);
                    Vector2 posRight = GetNormalizedPosition(graph.TimeEnd, thresholdValue);
                    tempLineVertexPairs.Add(posLeft);
                    tempLineVertexPairs.Add(posRight);
                }
            }
            else
            {
                int startIndex = line.Mode == GraphLine.Modes.ContinuousLine ? 1 : 0;
                for (int i = startIndex; i < pointCount; i++)
                {
                    if (line.Points[i].time < graph.TimeStart)
                        continue;

                    if (line.Points[i].time > graph.TimeEnd)
                        return;

                    if (line.Mode == GraphLine.Modes.VerticalLines)
                    {
                        Vector2 posTop = GetNormalizedPosition(line.Points[i].time, graph.ValueMax);
                        Vector2 posBottom = GetNormalizedPosition(line.Points[i].time, graph.ValueMin);
                        tempLineVertexPairs.Add(posBottom);
                        tempLineVertexPairs.Add(posTop);
                        continue;
                    }

                    Vector3 posPrev = GetNormalizedPosition(line.Points[i - 1].time, line.Points[i - 1].value);
                    Vector3 pos = GetNormalizedPosition(line.Points[i].time, line.Points[i].value);

                    if (drawGradientUnderLine)
                    {
                        Vector3 posPrevAtBottomGraph = posPrev.WithY(0.0f);
                        Vector3 posCurrentAtBottomGraph = pos.WithY(0.0f);
                        
                        tempUnderLineGradientQuadVertices.Add(posPrevAtBottomGraph.WithW(0.0f));
                        tempUnderLineGradientQuadVertices.Add(posPrev.WithW(1.0f));
                        tempUnderLineGradientQuadVertices.Add(pos.WithW(1.0f));
                        tempUnderLineGradientQuadVertices.Add(posCurrentAtBottomGraph.WithW(0.0f));
                    }

                    tempLineVertexPairs.Add(posPrev);
                    tempLineVertexPairs.Add(pos);
                }
            }

            // Draw a cheeky gradient under the line because it helps with readability. But mostly it just looks great.
            if (drawGradientUnderLine)
                DrawQuads(tempUnderLineGradientQuadVertices, lineColor.WithA(lineColor.a * GradientUnderLineOpacity));
            
            // Draw the whole line in one go, this is the fastest.
            DrawLine(tempLineVertexPairs, lineColor);
        }

        private void DrawHorizontalLine(GraphDataUI dataUi, float value)
        {
            float y = dataUi.GetNormalizedPosition(0.0f, value).y;
            DrawLine(new Vector2(0.0f, y), new Vector2(1.0f, y), value.Approximately(0.0f) ? axisColor : gridColor);
        }

        private void DrawVerticalLine(GraphDataUI dataUi, float time)
        {
            float x = dataUi.GetNormalizedPosition(time, 0.0f).x;
            DrawLine(new Vector2(x, 0.0f), new Vector2(x, 1.0f), gridColor);
        }
        
        #region Drawing Internals

        private void StartDrawing()
        {
	        GL.PushMatrix();
	        material.SetPass(0);
	        GL.LoadOrtho();
	        
	        GL.Clear(true, true, Color.clear);
        }

        private void StopDrawing()
        {
	        GL.End();

	        GL.PopMatrix();
        }
        
        private void StartDrawingQuads(Color color)
        {
            GL.Begin(GL.QUADS);
            GL.Color(color);
        }
        
        private void StopDrawingQuads()
        {
        }

        private void StartDrawingLines(Color color)
        {
            GL.Begin(GL.LINES);
            GL.Color(color);
        }
        
        private void StopDrawingLines()
        {
        }

        /// <summary>
        /// Draw a line between two graph-space positions.
        /// </summary>
        private void DrawLine(Vector2 from, Vector2 to, Color color)
        {
            StartDrawingLines(color);
            GL.Vertex(from);
            GL.Vertex(to);
            StopDrawingLines();
        }
        
        /// <summary>
        /// Draw a line between a list of paired positions. A line segment is drawn between every successive pair.
        /// </summary>
        private void DrawLine(List<Vector3> pairedPositions, Color color)
        {
            StartDrawingLines(color);
            for (int i = 0; i < pairedPositions.Count; i++)
            {
                GL.Vertex(pairedPositions[i]);
            }
            StopDrawingLines();
        }
        
        /// <summary>
        /// Draw a line between a list of quad positions. A quad is drawn between every 4 points.
        /// </summary>
        private void DrawQuads(List<Vector4> corners, Color color)
        {
            StartDrawingQuads(color);
            for (int i = 0; i < corners.Count; i++)
            {
                GL.Color(color.WithA(color.a * corners[i].w));
                GL.Vertex(corners[i]);
            }
            StopDrawingQuads();
        }
        #endregion

        public Vector2 GetNormalizedPosition(float time, float value)
        {
            return new Vector2(
                time.GetFraction(graph.TimeStart, graph.TimeEnd), value.GetFraction(graph.ValueMin, graph.ValueMax));
        }

        private void UpdateGrid()
        {
            int horizontalLinesUsed = 0;
            void DrawHorizontalLine(GraphDataUI dataUi, float value)
            {
                float y = GetNormalizedPosition(0.0f, value).y;
                horizontalGridLineValues.AvailableObjects[horizontalLinesUsed].gameObject.SetActive(true);
                horizontalGridLineValues.AvailableObjects[horizontalLinesUsed].UpdatePosition(y, value);
                horizontalLinesUsed++;
            }
            
            // Draw values next to the horizontal lines that denote the value range.
            ForEachHorizontalLine(DrawHorizontalLine);
            
            // The unused visualizers can be disabled. This ensures minimal activation/deactivation.
            for (int i = horizontalLinesUsed; i < horizontalGridLineValues.AvailableObjects.Count; i++)
            {
                horizontalGridLineValues.AvailableObjects[i].gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// This strange structure is used because we want the logic of where the lines should be drawn to be defined
        /// here because it's needed for placing certain canvas elements there, but we need a different script
        /// (GraphLineVisualizer) to also know where those lines should go so it can draw lines in a more optimized way.
        /// </summary>
        public void ForEachHorizontalLine(HorizontalLineHandler handler)
        {
            // Prepare for the horizontal lines that denote the value range.
            float valueInterval = Mathf.Max(0.1f, (graph.ValueMax - graph.ValueMin) / TargetLineCountHorizontal);
            int horizontalLinesMax = Mathf.CeilToInt((graph.ValueMax - graph.ValueMin) / valueInterval) + 1;
            horizontalGridLineValues.EnsureCapacity(horizontalLinesMax);

            // Draw the axis.
            handler(this, 0.0f);

            // Draw the horizontal grid lines based on the min and max values.
            for (float v = valueInterval; v.EqualOrSmaller(graph.ValueMax); v += valueInterval)
                handler(this, v);
            for (float v = -valueInterval; v.EqualOrGreater(graph.ValueMin); v -= valueInterval)
                handler(this, v);
        }
        
        /// <summary>
        /// This strange structure is used because we want the logic of where the lines should be drawn to be defined
        /// here because it's needed for placing certain canvas elements there, but we need a different script
        /// (GraphLineVisualizer) to also know where those lines should go so it can draw lines in a more optimized way.
        /// </summary>
        public void ForEachVerticalLine(VerticalLineHandler handler)
        {
            float step = graph.Duration / TargetLineCountVertical;
            float timeStart = Mathf.Floor((graph.TimeEnd - graph.Duration) / step) * step;
            float timeEnd = graph.TimeEnd;
            for (float t = timeStart; t < timeEnd; t += step)
                handler(this, t);
        }
    }
}
