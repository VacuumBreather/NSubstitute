using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NSubstitute.Core
{
    internal static class Extensions
    {
        /// <summary>
        /// Checks if the instance can be used when a <paramref name="type"/> is expected.
        /// </summary>
        public static bool IsCompatibleWith(this object instance, Type type)
        {
            var requiredType = type.IsByRef ? type.GetElementType() : type;
            return instance == null ? TypeCanBeNull(requiredType) : requiredType.IsInstanceOfType(instance);
        }

        /// <summary>
        /// Join the <paramref name="strings"/> using <paramref name="separator"/>.
        /// </summary>
        public static string Join(this IEnumerable<string> strings, string separator)
        {
            return string.Join(separator, strings);
        }

        public static bool IsDelegate(this Type type)
        {
            // From CLR via C# (see full answer here: https://stackoverflow.com/a/4833071/2009373)
            // > The System.MulticastDelegate class is derived from System.Delegate,
            // > which is itself derived from System.Object.
            // > The reason why there are two delegate classes is historical and unfortunate;
            // > there should be just one delegate class in the FCL.
            // > Sadly, you need to be aware of both of these classes
            // > because even though all delegate types you create have MulticastDelegate as a base class,
            // > you'll occasionally manipulate your delegate types by using methods defined by the Delegate class
            // > instead of the MulticastDelegate class.
            //
            // Basically, MulticastDelegate and Delegate mean the same, but using MulticastDelegate for base check
            // is slightly faster, as internally type.BaseType is walked in a loop and MulticastDelegate is reached faster.
            return type.GetTypeInfo().IsSubclassOf(typeof(MulticastDelegate));
        }

        public static MethodInfo GetInvokeMethod(this Type type)
        {
            return type.GetMethod("Invoke");
        }

        private static bool TypeCanBeNull(Type type)
        {
            return !type.GetTypeInfo().IsValueType || Nullable.GetUnderlyingType(type) != null;
        }

        public static string GetNonMangledTypeName(this Type type)
        {
            // Handle simple case without invoking more complex logic.
            if (!type.GetTypeInfo().IsGenericType && !type.IsNested)
            {
                return type.Name;
            }

            Type[] genericTypeArguments = type.GetGenericArguments();
            int alreadyHandledGenericArgumentsCount = 0;
            var resultTypeName = new StringBuilder();

            void AppendTypeNameRecursively(Type currentType)
            {
                Type declaringType = currentType.DeclaringType;
                if (declaringType != null)
                {
                    AppendTypeNameRecursively(declaringType);
                    resultTypeName.Append("+");
                }

                resultTypeName.Append(GetTypeNameWithoutGenericArity(currentType));

                // When you take the generic type arguments for a nested type, the type arguments from parent types
                // are included as well. We don't want to include them again, so simply skip all the already
                // handled arguments.
                // Notice, we expect generic type arguments order to always be parent to child, left to right.
                string[] ownGenericArguments = genericTypeArguments
                    .Take(currentType.GetGenericArguments().Length)
                    .Skip(alreadyHandledGenericArgumentsCount)
                    .Select(t => t.GetNonMangledTypeName())
                    .ToArray();

                if (ownGenericArguments.Length == 0)
                {
                    return;
                }

                alreadyHandledGenericArgumentsCount += ownGenericArguments.Length;

                resultTypeName.Append("<");
                resultTypeName.Append(ownGenericArguments.Join(", "));
                resultTypeName.Append(">");
            }

            AppendTypeNameRecursively(type);
            return resultTypeName.ToString();

            string GetTypeNameWithoutGenericArity(Type t)
            {
                var tn = t.Name;
                var indexOfBacktick = tn.IndexOf('`');
                // For nested generic types the back stick symbol might be missing.
                return indexOfBacktick > -1 ? tn.Substring(0, indexOfBacktick) : tn;
            }
        }

        /// <summary>
        /// Tries to cast sequence to array first before making a new array sequence.
        /// </summary>
        public static T[] AsArray<T>(this IEnumerable<T> sequence)
        {
            return sequence is T[] array ? array : sequence.ToArray();
        }
    }
}