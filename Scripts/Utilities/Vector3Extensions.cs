using UnityEngine;

namespace RoyTheunissen.Graphing.Utilities
{
    public static class Vector3Extensions
    {
        public static Vector3 WithX(this Vector3 v, float x)
        {
            return new Vector3(x, v.y, v.z);
        }

        public static Vector3 WithY(this Vector3 v, float y)
        {
            return new Vector3(v.x, y, v.z);
        }

        public static Vector3 WithZ(this Vector3 v, float z)
        {
            return new Vector3(v.x, v.y, z);
        }
        
        public static Vector4 WithW(this Vector3 v, float w)
        {
            return new Vector4(v.x, v.y, v.z, w);
        }
    }
}
