using System.Collections.Generic;
using UnityEngine;

namespace MagicShapeMultiply
{
    public static class ComponentUtils
    {
        public static List<T> GetComponentsInAllChildren<T>(this GameObject gameObject, List<T> componentList = null)
        {
            return gameObject.transform.GetComponentsInAllChildren(componentList);
        }

        public static List<T> GetComponentsInAllChildren<T>(this Transform transform, List<T> componentList = null)
        {
            componentList ??= new List<T>();
            foreach (Transform t in transform)
            {
                T[] components = t.GetComponents<T>();
                foreach (T component in components)
                {
                    if (component != null)
                    {
                        componentList.Add(component);
                    }
                }
                t.GetComponentsInAllChildren(componentList);
            }
            return componentList;
        }
    }
}
