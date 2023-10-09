//#define NO_GRADIENTS
// #define GRADIENT_FOR_SINGLE_LINES_ONLY

using System.Collections.Generic;
using RoyTheunissen.Graphing.Utilities;
using UnityEngine;

#if URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif // #if URP

namespace RoyTheunissen.Graphing.UI
{
    /// <summary>
    /// Responsible for drawing the lines of a graph. This part uses GL instead of the canvas because it's way faster.
    /// </summary>
    public sealed class GraphLineVisualizer : MonoBehaviour
    {
        private const float GradientUnderLineOpacity = 0.075f;
        
        [SerializeField] private new Camera camera;
        
        [SerializeField] private Material material;
        [SerializeField] private GraphCanvasVisualizer graphCanvasVisualizer;
        [SerializeField] private Color gridColor = new Color(0.25f, 0.25f, 0.25f, 0);
        [SerializeField] private Color axisColor = new Color(0.5f, 0.5f, 0.5f, 0);
        
#if URP
        private void Awake()
        {
            // Make sure this camera is of type Overlay.
            UniversalAdditionalCameraData thisCamerasAdditionalCameraData = camera.GetUniversalAdditionalCameraData();
            thisCamerasAdditionalCameraData.renderType = CameraRenderType.Overlay;
        }

        private void OnEnable()
        {
            RenderPipelineManager.endCameraRendering += EndCameraRendering;

            // Make sure we add this camera to the camera stack.
            UniversalAdditionalCameraData additionalCameraData =
                Camera.main.GetComponent<UniversalAdditionalCameraData>();
            if (additionalCameraData == null)
                additionalCameraData = Camera.main.gameObject.AddComponent<UniversalAdditionalCameraData>();
            additionalCameraData.cameraStack.Add(camera);
        }

        private void EndCameraRendering(ScriptableRenderContext scriptableRenderContext, Camera camera)
        {
            OnPostRender();
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= EndCameraRendering;
        }
#endif // URP

        private void OnPostRender()
        {
            if (!material)
            {
                Debug.LogError("Please Assign a material on the inspector");
                return;
            }

            foreach (KeyValuePair<Graph, GraphUI> graphUiPair in graphCanvasVisualizer.GraphUis)
            {
                DrawGraphLines(graphUiPair.Key, graphUiPair.Value);
            }
        }

        private void DrawGraphLines(Graph graph, GraphUI ui)
        {
            ui.DataUi.ForEachHorizontalLine(DrawHorizontalLine);
            ui.DataUi.ForEachVerticalLine(DrawVerticalLine);

            foreach (GraphLine line in graph.Lines)
            {
                DrawGraphValueLine(graph, ui.DataUi, line);
            }
        }

        private static readonly List<Vector3> tempLineVertexPairs = new List<Vector3>();
        private static readonly List<Vector4> tempUnderLineGradientQuadVertices = new List<Vector4>();
        
        private void DrawGraphValueLine(Graph graph, GraphDataUI dataUi, GraphLine line)
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
                float thresholdValue = line.Points[line.Points.Count - 1].value;
                Vector2 posLeft = dataUi.GetNormalizedPosition(graph.TimeStart, thresholdValue);
                Vector2 posRight = dataUi.GetNormalizedPosition(graph.TimeEnd, thresholdValue);
                tempLineVertexPairs.Add(NormalizedGraphPositionToViewPosition(dataUi, posLeft));
                tempLineVertexPairs.Add(NormalizedGraphPositionToViewPosition(dataUi, posRight));
            }
            else
            {
                for (int i = 1; i < pointCount; i++)
                {
                    if (line.Points[i].time < graph.TimeStart)
                        continue;

                    if (line.Points[i].time > graph.TimeEnd)
                        return;

                    if (line.Mode == GraphLine.Modes.VerticalLineAtEveryPoint)
                    {
                        Vector2 posTop = dataUi.GetNormalizedPosition(line.Points[i].time, graph.ValueMax);
                        Vector2 posBottom = dataUi.GetNormalizedPosition(line.Points[i].time, graph.ValueMin);
                        tempLineVertexPairs.Add(NormalizedGraphPositionToViewPosition(dataUi, posBottom));
                        tempLineVertexPairs.Add(NormalizedGraphPositionToViewPosition(dataUi, posTop));
                        continue;
                    }

                    Vector2 posPrev = dataUi.GetNormalizedPosition(line.Points[i - 1].time, line.Points[i - 1].value);
                    Vector2 pos = dataUi.GetNormalizedPosition(line.Points[i].time, line.Points[i].value);

                    Vector3 posPrevGraph = NormalizedGraphPositionToViewPosition(dataUi, posPrev);
                    Vector3 posCurrentGraph = NormalizedGraphPositionToViewPosition(dataUi, pos);

                    if (drawGradientUnderLine)
                    {
                        Vector3 posPrevAtBottomGraph =
                            NormalizedGraphPositionToViewPosition(dataUi, posPrev.WithY(0.0f));
                        Vector3 posCurrentAtBottomGraph =
                            NormalizedGraphPositionToViewPosition(dataUi, pos.WithY(0.0f));
                        
                        tempUnderLineGradientQuadVertices.Add(posPrevAtBottomGraph.WithW(0.0f));
                        tempUnderLineGradientQuadVertices.Add(posPrevGraph.WithW(1.0f));
                        tempUnderLineGradientQuadVertices.Add(posCurrentGraph.WithW(1.0f));
                        tempUnderLineGradientQuadVertices.Add(posCurrentAtBottomGraph.WithW(0.0f));
                    }

                    tempLineVertexPairs.Add(posPrevGraph);
                    tempLineVertexPairs.Add(posCurrentGraph);
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
            DrawLine(
                dataUi, new Vector2(0.0f, y), new Vector2(1.0f, y), value.Approximately(0.0f) ? axisColor : gridColor);
        }

        private void DrawVerticalLine(GraphDataUI dataUi, float time)
        {
            float x = dataUi.GetNormalizedPosition(time, 0.0f).x;
            DrawLine(dataUi, new Vector2(x, 0.0f), new Vector2(x, 1.0f), gridColor);
        }

        private Vector2 NormalizedGraphPositionToViewPosition(GraphDataUI dataUi, Vector2 graphPosition)
        {
            // Graph to screen
            Vector2 screenPosition = dataUi.GetScreenSpacePosition(graphPosition);
            
            // Screen to view
            return new Vector2(screenPosition.x / Screen.width, screenPosition.y / Screen.height);
        }

        /// <summary>
        /// Draw a line on a graph UI between two normalized graph-space positions.
        /// </summary>
        private void DrawLine(GraphDataUI dataUi, Vector2 from, Vector2 to, Color color)
        {
            from = NormalizedGraphPositionToViewPosition(dataUi, from);
            to = NormalizedGraphPositionToViewPosition(dataUi, to);
            DrawLine(from, to, color);
        }
        
        private void StartDrawingQuads(Color color)
        {
            GL.PushMatrix();
            material.SetPass(0);
            GL.LoadOrtho();

            GL.Begin(GL.QUADS);
            GL.Color(color);
        }
        
        private void StopDrawingQuads()
        {
            GL.End();

            GL.PopMatrix();
        }

        private void StartDrawingLines(Color color)
        {
            GL.PushMatrix();
            material.SetPass(0);
            GL.LoadOrtho();

            GL.Begin(GL.LINES);
            GL.Color(color);
        }
        
        private void StopDrawingLines()
        {
            GL.End();

            GL.PopMatrix();
        }

        /// <summary>
        /// Draw a line between two view-space positions.
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
    }
}
