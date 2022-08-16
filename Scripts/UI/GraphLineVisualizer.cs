using System.Collections.Generic;
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
        [SerializeField] private new Camera camera;
        
        [SerializeField] private Material material;
        [SerializeField] private GraphCanvasVisualizer graphCanvasVisualizer;
        [SerializeField] private Color gridColor = new Color(0.25f, 0.25f, 0.25f, 0);
        [SerializeField] private Color axisColor = new Color(0.5f, 0.5f, 0.5f, 0);
        
#if URP
        private void OnEnable()
        {
            RenderPipelineManager.endCameraRendering += EndCameraRendering;

            // Make sure we add this camera to the camera stack.
            UniversalAdditionalCameraData additionalCameraData =
                Camera.main.GetOrAddComponent<UniversalAdditionalCameraData>();
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

        private static readonly List<Vector3> tempVertexPairs = new List<Vector3>();
        private void DrawGraphValueLine(Graph graph, GraphDataUI dataUi, GraphLine line)
        {
            Color color = line.Color;
            Color lineColor = new Color(color.r, color.g, color.b, color.a / 2);
            int lineCount = line.Points.Count;
            
            tempVertexPairs.Clear();

            for (int i = 1; i < lineCount; i++)
            {
                if (line.Points[i].time < graph.TimeStart)
                    continue;
                
                if (line.Points[i].time > graph.TimeEnd)
                    return;
                
                Vector2 posPrev = dataUi.GetNormalizedPosition(line.Points[i - 1].time, line.Points[i - 1].value);
                Vector2 pos = dataUi.GetNormalizedPosition(line.Points[i].time, line.Points[i].value);

                tempVertexPairs.Add(
                    NormalizedGraphPositionToViewPosition(dataUi, posPrev),
                    NormalizedGraphPositionToViewPosition(dataUi, pos));
            }

            // Draw the whole line in one go, this is the fastest.
            DrawLine(tempVertexPairs, lineColor);
        }

        private void DrawHorizontalLine(GraphDataUI dataUi, float value)
        {
            float y = dataUi.GetNormalizedPosition(0.0f, value).y;
            DrawLine(dataUi, new Vector2(0.0f, y), new Vector2(1.0f, y), value.Equal(0.0f) ? axisColor : gridColor);
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

        private void StartDrawing(Color color)
        {
            GL.PushMatrix();
            material.SetPass(0);
            GL.LoadOrtho();

            GL.Begin(GL.LINES);
            GL.Color(color);
        }
        
        private void StopDrawing()
        {
            GL.End();

            GL.PopMatrix();
        }

        /// <summary>
        /// Draw a line between two view-space positions.
        /// </summary>
        private void DrawLine(Vector2 from, Vector2 to, Color color)
        {
            StartDrawing(color);
            GL.Vertex(from);
            GL.Vertex(to);
            StopDrawing();
        }
        
        /// <summary>
        /// Draw a line between a list of paired positions. A line segment is drawn between every successive pair.
        /// </summary>
        private void DrawLine(List<Vector3> pairedPositions, Color color)
        {
            StartDrawing(color);
            for (int i = 0; i < pairedPositions.Count; i++)
            {
                GL.Vertex(pairedPositions[i]);
            }
            StopDrawing();
        }
    }
}
