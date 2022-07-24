using HarmonyLib;
using System;
using System.Reflection;

namespace MagicShapeMultiply
{
    internal static class ReflectionUtils
    {
        public static object Get(this object obj, string name)
        {
            return obj.GetType().Get(name, obj);
        }

        public static object Get(this Type type, string name, object instance = null)
        {
            FieldInfo field = AccessTools.Field(type, name);
            if (field != null)
                return field.GetValue(instance);
            else
            {
                PropertyInfo property = AccessTools.Property(type, name);
                if (property != null)
                    return property.GetValue(instance);
                else
                    throw new ArgumentException($"No Field or Property named {name} in {type}!");
            }
        }

        public static T Get<T>(this object obj, string name)
        {
            return obj.GetType().Get<T>(name, obj);
        }

        public static T Get<T>(this Type type, string name, object instance = null)
        {
            object rtn = type.Get(name, instance);
            if (rtn != null && rtn.GetType().IsAssignableFrom(typeof(T)))
                return (T)rtn;
            else
                return default;
        }



        public static void Set(this object obj, string name, object value)
        {
            obj.GetType().Set(name, value, obj);
        }

        public static void Set(this Type type, string name, object value, object instance = null)
        {
            FieldInfo field = AccessTools.Field(type, name);
            if (field != null)
                field.SetValue(instance, value);
            else
            {
                PropertyInfo property = AccessTools.Property(type, name);
                if (property != null)
                    property.SetValue(instance, value);
                else
                    throw new ArgumentException($"No Field or Property named {name} in {type}!");
            }
        }


        public static object Method(this object obj, string name, object[] args = null, Type[] argTypes = null)
        {
            return obj.GetType().Method(name, args, argTypes, obj);
        }

        public static object Method(this Type type, string name, object[] args = null, Type[] argTypes = null, object instance = null)
        {
            if (argTypes == null && args != null)
            {
                argTypes = new Type[args.Length];
                for (int i = 0; i < args.Length; i++)
                    argTypes[i] = args[i].GetType();
            }
            return AccessTools.Method(type, name, argTypes).Invoke(instance, args);
        }

        public static T Method<T>(this object obj, string name, object[] args = null, Type[] argTypes = null)
        {
            return obj.GetType().Method<T>(name, args, argTypes);
        }

        public static T Method<T>(this Type type, string name, object[] args = null, Type[] argTypes = null, object instance = null)
        {
            object rtn = type.Method(name, args, argTypes, instance);
            if (rtn.GetType().IsAssignableFrom(typeof(T)))
                return (T)rtn;
            else
                return default;
        }
    }
}
