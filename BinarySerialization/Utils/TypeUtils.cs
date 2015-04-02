using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BinarySerialization.Utils
{
    internal static class TypeUtils
    {
        internal static ObjectType DetermineObjectType(Type type)
        {
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return ObjectType.Nullable;
            }
            if(type.IsPrimitive || type == typeof(decimal))
            {
                return ObjectType.Primitive;
            }
            if(type == typeof(string))
            {
                return ObjectType.String;
            }
            if(type.IsAssignableTo<IEnumerable>())
            {
                return ObjectType.Enumerable;
            }
            if(type.IsClass)
            {
                return ObjectType.Class;
            }
            if(type.IsValueType)
            {
                return ObjectType.Struct;
            }

            return ObjectType.Unsupported;
        }

        internal static Type GetEnumerableItemType(Type type)
        {
            var elementType = type.GetElementType();

            if(elementType == null) //non-array
            {
                var genericType = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
                if(genericType == typeof(IList<>) || genericType == typeof(List<>))
                {
                    elementType = type.GetGenericArguments().First();
                }
            }

            return elementType;
        }

        internal static bool IsSupportedElementType(Type elementType)
        {
            return elementType != typeof(object)
                                       && !elementType.IsAbstract
                                       && !elementType.IsInterface;
        }

        internal static IList CreateList(Type itemType)
        {
            var collectionType = typeof(List<>).MakeGenericType(itemType);
            return (IList)Activator.CreateInstance(collectionType);
        }

        internal static Array CreateArray(Type itemType, int length)
        {
            return Array.CreateInstance(itemType, length);
        }
    }
}