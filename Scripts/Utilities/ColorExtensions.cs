using UnityEngine;

namespace RoyTheunissen.Graphing.Utilities
{
    public static class ColorExtensions
    {
        public static Color WithR(this Color color, float r)
        {
            return new Color(r, color.g, color.b, color.a);
        }

        public static Color WithG(this Color color, float g)
        {
            return new Color(color.r, g, color.b, color.a);
        }

        public static Color WithB(this Color color, float b)
        {
            return new Color(color.r, color.g, b, color.a);
        }

        public static Color WithA(this Color color, float a)
        {
            return new Color(color.r, color.g, color.b, a);
        }

        public static Color WithValue(this Color color, float value)
        {
            return new Color(color.r * value, color.g * value, color.b * value, color.a);
        }
    }
}
