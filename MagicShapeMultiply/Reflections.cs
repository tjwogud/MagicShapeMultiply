using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace MagicShapeMultiply
{
    public static class Reflections
    {
        private readonly static Dictionary<(Type, string), Delegate> fieldGetters = new Dictionary<(Type, string), Delegate>();
        private readonly static Dictionary<(Type, string), Delegate> fieldSetters = new Dictionary<(Type, string), Delegate>();

        private readonly static Dictionary<(Type, string), Delegate> propertyGetters = new Dictionary<(Type, string), Delegate>();
        private readonly static Dictionary<(Type, string), Delegate> propertySetters = new Dictionary<(Type, string), Delegate>();

        public static object Get(this Type type, string name, object instance = null)
        {
            if (fieldGetters.TryGetValue((type, name), out Delegate v1))
                return v1.DynamicInvoke(instance);
            if (propertyGetters.TryGetValue((type, name), out Delegate v2))
                return v2.DynamicInvoke(instance);
            FieldInfo field = AccessTools.Field(type, name);
            if (field != null)
                return CreateFieldGetter(type, field).DynamicInvoke(instance);
            PropertyInfo property = AccessTools.Property(type, name);
            if (property != null)
                return CreatePropertyGetter(type, property).DynamicInvoke(instance);
            return null;
        }

        public static T Get<T>(this Type type, string name, object instance = null)
        {
            return (T)Get(type, name, instance);
        }

        public static object Get(this object instance, string name)
        {
            return instance?.GetType().Get(name, instance);
        }

        public static T Get<T>(this object instance, string name)
        {
            return instance != null ? instance.GetType().Get<T>(name, instance) : default;
        }

        private static Delegate CreateFieldGetter(Type type, FieldInfo field)
        {
            var instanceExp = Expression.Parameter(type, "instance");
            var fieldExp = Expression.Field(field.IsStatic ? null : instanceExp, field);
            var getter = Expression.Lambda(fieldExp, instanceExp).Compile();
            fieldGetters[(type, field.Name)] = getter;
            return getter;
        }

        private static Delegate CreatePropertyGetter(Type type, PropertyInfo property)
        {
            MethodInfo getMethod = property.GetGetMethod(true) ?? throw new ArgumentException("getter does not exist!");
            var instanceExp = Expression.Parameter(type, "instance");
            var methodExp = Expression.Call(getMethod.IsStatic ? null : instanceExp, getMethod);
            var getter = Expression.Lambda(methodExp, instanceExp).Compile();
            fieldGetters[(type, property.Name)] = getter;
            return getter;
        }

        public static void Set(this Type type, string name, object value, object instance = null)
        {
            if (fieldSetters.TryGetValue((type, name), out Delegate v1))
                v1.DynamicInvoke(instance, value);
            if (propertySetters.TryGetValue((type, name), out Delegate v2))
                v2.DynamicInvoke(instance, value);
            FieldInfo field = AccessTools.Field(type, name);
            if (field != null)
                CreateFieldSetter(type, field).DynamicInvoke(instance, value);
            PropertyInfo property = AccessTools.Property(type, name);
            if (property != null)
                CreatePropertySetter(type, property).DynamicInvoke(instance, value);
        }

        public static void Set(this object instance, string name, object value)
        {
            instance?.GetType().Set(name, value, instance);
        }

        private static Delegate CreateFieldSetter(Type type, FieldInfo field)
        {
            var instanceExp = Expression.Parameter(type, "instance");
            var valueExp = Expression.Parameter(field.FieldType, "value");
            var fieldExp = Expression.Field(field.IsStatic ? null : instanceExp, field);
            var assignExp = Expression.Assign(fieldExp, valueExp);
            var setter = Expression.Lambda(assignExp, instanceExp, valueExp).Compile();
            fieldSetters[(type, field.Name)] = setter;
            return setter;
        }

        private static Delegate CreatePropertySetter(Type type, PropertyInfo property)
        {
            MethodInfo setMethod = property.GetSetMethod(true) ?? throw new ArgumentException("setter does not exist!");
            var instanceExp = Expression.Parameter(type, "instance");
            var valueExp = Expression.Parameter(property.PropertyType, "value");
            var methodExp = Expression.Call(setMethod.IsStatic ? null : instanceExp, setMethod, valueExp);
            var setter = Expression.Lambda(methodExp, instanceExp, valueExp).Compile();
            fieldSetters[(type, property.Name)] = setter;
            return setter;
        }

        public static object Method(this Type type, string name, object[] parameters = null, Type[] parameterTypes = null, Type[] genericTypes = null, object instance = null)
        {
            MethodInfo method = AccessTools.Method(type, name, parameterTypes, genericTypes);
            return method.Invoke(instance, parameters);
        }

        public static T Method<T>(this Type type, string name, object[] parameters = null, Type[] parameterTypes = null, Type[] genericTypes = null, object instance = null)
        {
            return (T)type.Method(name, parameters, parameterTypes, genericTypes, instance);
        }

        public static object Method(this object instance, string name, object[] parameters = null, Type[] parameterTypes = null, Type[] genericTypes = null)
        {
            return instance?.GetType().Method(name, parameters, parameterTypes, genericTypes, instance);
        }

        public static T Method<T>(this object instance, string name, object[] parameters = null, Type[] parameterTypes = null, Type[] genericTypes = null)
        {
            return instance != null ? instance.GetType().Method<T>(name, parameters, parameterTypes, genericTypes, instance) : default;
        }

        public static Type GetType(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(name);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
