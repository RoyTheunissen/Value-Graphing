namespace RoyTheunissen.Graphing
{
    /// <summary>
    /// Single point on a graph's line.
    /// </summary>
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
}
