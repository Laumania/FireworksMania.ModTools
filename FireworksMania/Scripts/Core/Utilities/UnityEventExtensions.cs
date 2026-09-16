using System;
using UnityEngine;
using UnityEngine.Events;

namespace FireworksMania.Core.Utilities
{
    public static class UnityEventExtensions
    {
        /// <summary>
        /// Invokes a serialized <see cref="UnityEvent"/> without letting a throwing listener abort the
        /// caller.
        ///
        /// These events are part of the modding surface, so anyone can attach a listener to them, and
        /// UMod routes mod listeners through its own bridge. A plain <c>Invoke()</c> lets an exception
        /// from any one of those listeners propagate straight out of the calling method - and because
        /// every one of these invokes sits *before* the game logic it announces, that logic is then
        /// silently skipped. Observed for real: a mod listener threw a NullReferenceException inside
        /// UMod's own bridge and the explosion applied no force, no ignition and no destruction, which
        /// stops a chain reaction dead and looks like an occasional dud firework.
        ///
        /// Swallowing is deliberate. A broken mod listener must not be able to break core gameplay, and
        /// the error is still logged with the offending object attached so it can be tracked down.
        /// </summary>
        public static void InvokeSafe(this UnityEvent unityEvent, UnityEngine.Object context, string eventName)
        {
            if (unityEvent == null)
                return;

            try
            {
                unityEvent.Invoke();
            }
            catch (Exception exception)
            {
                //Hardcoded English on purpose - this is a modder-facing diagnostic, not player chrome.
                //The path is written into the message rather than left to the context object: clicking
                //the log line only pings the object while the game is still running, and these turn up
                //in player logs where there is nothing to click.
                Debug.LogError($"A listener on '{eventName}' of '{DescribeContext(context)}' threw an exception. It has been ignored so the rest of the logic still runs, but whatever is listening is broken and should be fixed.\n{exception}", context);
            }
        }

        /// <summary>
        /// The full hierarchy path of whatever raised the event, so a modder can tell which item of
        /// theirs it was. Never throws and never returns null - it runs inside a catch block, and losing
        /// the exception itself to a failure in here would be far worse than a vague name.
        /// </summary>
        private static string DescribeContext(UnityEngine.Object context)
        {
            try
            {
                if (ReferenceEquals(context, null))
                    return "<no context>";

                //Unity's own ==, which is also true for a DESTROYED object while the managed reference is
                //still alive. Nothing on it can be read any more, but that it went away is itself a clue.
                if (context == null)
                    return "<destroyed object>";

                var gameObject = context as GameObject;
                if (gameObject == null && context is Component component)
                    gameObject = component.gameObject;

                //A ScriptableObject or any other asset has no place in a hierarchy, so its name is all there is
                if (gameObject == null)
                    return context.name;

                return gameObject.GetHierarchyPathAsString();
            }
            catch (Exception)
            {
                return "<unknown context>";
            }
        }
    }
}
