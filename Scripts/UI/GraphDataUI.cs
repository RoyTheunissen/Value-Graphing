using RoyTheunissen.Scaffolding.Pooling;
using UnityEngine;

namespace RoyTheunissen.Graphing.UI
{
    /// <summary>
    /// Visualizes the actual data of a graph using UI components.
    /// </summary>
    public sealed class GraphDataUI : MonoBehaviour 
    {
        [Header("Grid Line Values")]
        [SerializeField] private RectTransform gridLineValuesContainer;
        [SerializeField] private GridLineValueUI gridLineValueUiPrefab;
        [SerializeField] private RectTransform gridLinesContainer;

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

        private Rect cachedGridScreenSpaceRect;
        private bool didCacheGridScreenSpaceRect;
        private Rect GridScreenSpaceRect
        {
            get
            {
                if (!didCacheGridScreenSpaceRect)
                {
                    didCacheGridScreenSpaceRect = true;
                    
                    Vector3 worldMin = new Vector3(GridLinesCorners[0].x, GridLinesCorners[0].y, GridLinesCorners[0].z);
                    Vector3 worldMax = new Vector3(GridLinesCorners[2].x, GridLinesCorners[2].y, GridLinesCorners[2].z);
            
                    Vector3 canvasMin = canvas.worldCamera.WorldToScreenPoint(worldMin);
                    Vector3 canvasMax = canvas.worldCamera.WorldToScreenPoint(worldMax);

                    cachedGridScreenSpaceRect = new Rect(
                        canvasMin.x, canvasMin.y, canvasMax.x - canvasMin.x, canvasMax.y - canvasMin.y);
                }
                return cachedGridScreenSpaceRect;
            }
        }

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

        private void OnDestroy()
        {
            horizontalGridLineValues.Cleanup();
        }

        private void LateUpdate()
        {
            UpdateGrid();
        }

        public Vector2 GetNormalizedPosition(float time, float value)
        {
            return new Vector2(
                time.GetFraction(graph.TimeStart, graph.TimeEnd), value.GetFraction(graph.ValueMin, graph.ValueMax));
        }

        public Vector2 GetScreenSpacePosition(Vector2 normalizedGridPosition)
        {
            Rect gridScreenSpaceRect = GridScreenSpaceRect;
            return new Vector2(
                gridScreenSpaceRect.xMin + gridScreenSpaceRect.width * normalizedGridPosition.x,
                gridScreenSpaceRect.yMin + gridScreenSpaceRect.height * normalizedGridPosition.y);
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
