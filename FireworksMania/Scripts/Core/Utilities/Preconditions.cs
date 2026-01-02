using System;

namespace FireworksMania.Core.Utilities
{
    //Source https://gist.github.com/adammyhre/82d495ab99e2c59a19362119b2d43194
    public class Preconditions
    {
        Preconditions() { }

        [Obsolete("Use CheckNotNull(reference, context) instead", true)]
        public static T CheckNotNull<T>(T reference)
        {
            return CheckNotNull(reference, typeof(T).ToString(), null);
        }

        [Obsolete("Use CheckNotNull(reference, message, context) instead", true)]
        public static T CheckNotNull<T>(T reference, string message)
        {
            return CheckNotNull(reference, message, null);
        }

        public static T CheckNotNull<T>(T reference, UnityEngine.MonoBehaviour context)
        {
            return CheckNotNull(reference, typeof(T).ToString(), context);
        }

        public static T CheckNotNull<T>(T reference, string message, UnityEngine.MonoBehaviour context)
        {
            if(context.OrNull() != null && context.gameObject.OrNull() != null)
            {
                message = $"'{message}' (Hierarchy Path: '{context.gameObject.GetHierarchyPathAsString()}')";
            }

            // Can find OrNull Extension Method (and others) here: https://github.com/adammyhre/Unity-Utils
            if (reference is UnityEngine.Object obj && obj.OrNull() == null)
            {
                throw new ArgumentNullException(message);
            }
            if (reference is null)
            {
                throw new ArgumentNullException(message);
            }
            return reference;
        }

        public static void CheckState(bool expression)
        {
            CheckState(expression, null);
        }

        public static void CheckState(bool expression, string messageTemplate, params object[] messageArgs)
        {
            CheckState(expression, string.Format(messageTemplate, messageArgs));
        }

        public static void CheckState(bool expression, string message)
        {
            if (expression)
            {
                return;
            }

            throw message == null ? new InvalidOperationException() : new InvalidOperationException(message);
        }
    }
}
