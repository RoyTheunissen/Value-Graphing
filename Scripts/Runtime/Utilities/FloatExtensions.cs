using UnityEngine;

namespace RoyTheunissen.Graphing.Utilities
{
    public static class FloatExtensions
    {
        // These methods break my naming convention of methods always having to be verbs but their
        // use is unambiguous and the whole point of these methods is to make floating point based 
        // comparisons more compact and readable.
        public static bool Approximately(this float a, float b)
        {
            return Mathf.Approximately(a, b);
        }

        public static bool Equal(this float a, float b)
        {
            return a.Approximately(b);
        }

        public static bool EqualOrGreater(this float a, float b)
        {
            return Equal(a, b) || a > b;
        }

        public static bool EqualOrSmaller(this float a, float b)
        {
            return Equal(a, b) || a < b;
        }

        public static float GetFraction(this float value, float start, float end)
        {
            if (start.Equal(end))
                return 0.0f;

            return Mathf.Clamp01((value - start) / (end - start));
        }
    }
}
