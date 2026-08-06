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
            if (IsNull(reference))
            {
                throw new ArgumentNullException(Decorate(typeof(T).ToString(), context));
            }
            return reference;
        }

        public static T CheckNotNull<T>(T reference, string message, UnityEngine.MonoBehaviour context)
        {
            if (IsNull(reference))
            {
                throw new ArgumentNullException(Decorate(message, context));
            }
            return reference;
        }

        // Can find OrNull Extension Method (and others) here: https://github.com/adammyhre/Unity-Utils
        private static bool IsNull<T>(T reference)
        {
            if (reference is UnityEngine.Object obj)
                return obj.OrNull() == null;

            return reference is null;
        }

        //Only called when a check actually fails - walking the hierarchy costs ~5.5us and ~350 bytes, and these
        //checks live in Awake/Start on every component we spawn, so it must never run on the success path.
        private static string Decorate(string message, UnityEngine.MonoBehaviour context)
        {
            if (context.OrNull() != null && context.gameObject.OrNull() != null)
                return $"'{message}' (Hierarchy Path: '{context.gameObject.GetHierarchyPathAsString()}')";

            return message;
        }

        public static void CheckState(bool expression)
        {
            CheckState(expression, null);
        }

        public static void CheckState(bool expression, string messageTemplate, params object[] messageArgs)
        {
            if (expression)
            {
                return;
            }

            CheckState(false, string.Format(messageTemplate, messageArgs));
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
