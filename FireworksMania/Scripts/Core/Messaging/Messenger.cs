/*
 * Advanced C# messenger by Ilya Suzdalnitski. V1.0
 * 
 * Based on Rod Hyde's "CSharpMessenger" and Magnus Wolffelt's "CSharpMessenger Extended".
 * Altered by Laumania to fit Fireworks Mania.
 * 
 * Features:
 	* Prevents a MissingReferenceException because of a reference to a destroyed message handler.
 	* Option to log all messages
 	* Extensive error detection, preventing silent bugs
 * 
 * Usage examples:
 	1. Messenger.AddListener<GameObject>("prop collected", PropCollected);
 	   Messenger.Broadcast<GameObject>("prop collected", prop);
 	2. Messenger.AddListener<float>("speed changed", SpeedChanged);
 	   Messenger.Broadcast<float>("speed changed", 0.5f);
 * 
 * Messenger cleans up its evenTable automatically upon loading of a new level.
 * 
 * Don't forget that the messages that should survive the cleanup, should be marked with Messenger.MarkAsPermanent(string)
 * Documentation here: http://wiki.unity3d.com/index.php/Advanced_CSharp_Messenger
 */

//#define LOG_ALL_MESSAGES
//#define LOG_ADD_LISTENER
//#define LOG_BROADCAST_MESSAGE
//#define REQUIRE_LISTENER

using System;
using System.Collections.Generic;
using UnityEngine;

namespace FireworksMania.Core.Messaging
{
    public delegate void Callback<T>(T arg);

    /// <summary>
    /// Messenger used to hook up listeners for messages and for broadcasting messages across components
    /// </summary>
    public static class Messenger
    {
        #region Internal variables

        private static Dictionary<string, Delegate> _eventTable = new Dictionary<string, Delegate>();

        //Message handlers that should never be removed, regardless of calling Cleanup
        private static List<string> _permanentMessages = new List<string>();
        #endregion

        #region Helper methods
        //Marks a certain message as permanent.
        public static void MarkAsPermanent<T>()
        {
            var eventId = GetEventId<T>();
#if LOG_ALL_MESSAGES
            Debug.Log("Messenger MarkAsPermanent \t\"" + eventId + "\"");
#endif

            _permanentMessages.Add(eventId);
        }

        private static string GetEventId<T>()
        {
            return typeof(T).FullName;
        }


        public static void Cleanup()
        {
#if LOG_ALL_MESSAGES
		Debug.Log("MESSENGER Cleanup. Make sure that none of necessary listeners are removed.");
#endif

            List<string> messagesToRemove = new List<string>();

            foreach (KeyValuePair<string, Delegate> pair in _eventTable)
            {
                bool wasFound = false;

                foreach (string message in _permanentMessages)
                {
                    if (pair.Key == message)
                    {
                        wasFound = true;
                        break;
                    }
                }

                if (!wasFound)
                    messagesToRemove.Add(pair.Key);
            }

            foreach (string message in messagesToRemove)
            {
                _eventTable.Remove(message);
            }
        }

        public static void PrintEventTable()
        {
            Debug.Log("\t\t\t=== MESSENGER PrintEventTable ===");

            foreach (KeyValuePair<string, Delegate> pair in _eventTable)
            {
                Debug.Log("\t\t\t" + pair.Key + "\t\t" + pair.Value);
            }

            Debug.Log("\n");
        }
        #endregion

        #region Message logging and exception throwing
        private static void OnListenerAdding(string eventType, Delegate listenerBeingAdded)
        {
#if LOG_ALL_MESSAGES || LOG_ADD_LISTENER
		Debug.Log("MESSENGER OnListenerAdding \t\"" + eventType + "\"\t{" + listenerBeingAdded.Target + " -> " + listenerBeingAdded.Method + "}");
#endif

            if (!_eventTable.ContainsKey(eventType))
            {
                _eventTable.Add(eventType, null);
            }

            Delegate d = _eventTable[eventType];
            if (d != null && d.GetType() != listenerBeingAdded.GetType())
            {
                throw new ListenerException(string.Format("Attempting to add listener with inconsistent signature for event type {0}. Current listeners have type {1} and listener being added has type {2}", eventType, d.GetType().Name, listenerBeingAdded.GetType().Name));
            }
        }

        private static void OnListenerRemoving(string eventType, Delegate listenerBeingRemoved)
        {
#if LOG_ALL_MESSAGES
		Debug.Log("MESSENGER OnListenerRemoving \t\"" + eventType + "\"\t{" + listenerBeingRemoved.Target + " -> " + listenerBeingRemoved.Method + "}");
#endif

            if (_eventTable.ContainsKey(eventType))
            {
                Delegate d = _eventTable[eventType];

                if (d == null)
                {
                    throw new ListenerException(string.Format("Attempting to remove listener with for event type \"{0}\" but current listener is null.", eventType));
                }
                else if (d.GetType() != listenerBeingRemoved.GetType())
                {
                    throw new ListenerException(string.Format("Attempting to remove listener with inconsistent signature for event type {0}. Current listeners have type {1} and listener being removed has type {2}", eventType, d.GetType().Name, listenerBeingRemoved.GetType().Name));
                }
            }
            else
            {
#if LOG_ALL_MESSAGES
                throw new ListenerException(string.Format("Attempting to remove listener for type \"{0}\" but Messenger doesn't know about this event type.", eventType));
#endif
            }
        }

        private static void OnListenerRemoved(string eventType)
        {
            if (_eventTable.ContainsKey(eventType) && _eventTable[eventType] == null)
            {
                _eventTable.Remove(eventType);
            }
        }

        private static void OnBroadcasting(string eventType)
        {
#if REQUIRE_LISTENER
            if (!eventTable.ContainsKey(eventType))
            {
                throw new BroadcastException(string.Format("Broadcasting message \"{0}\" but no listener found. Try marking the message with Messenger.MarkAsPermanent.", eventType));
            }
#endif
        }

        private static BroadcastException CreateBroadcastSignatureException(string eventType)
        {
            return new BroadcastException(string.Format("Broadcasting message \"{0}\" but listeners have a different signature than the broadcaster.", eventType));
        }

        public class BroadcastException : Exception
        {
            public BroadcastException(string msg)
                : base(msg)
            {
            }
        }

        public class ListenerException : Exception
        {
            public ListenerException(string msg)
                : base(msg)
            {
            }
        }
#endregion

        #region AddListener

        /// <summary>
        /// Registers a listener for the specific message type T.
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="handler"></param>
        public static void AddListener<T>(Callback<T> handler)
        {
            var eventId = GetEventId<T>();
            OnListenerAdding(eventId, handler);

            var currentDelegate = _eventTable[eventId];
            if (currentDelegate != null)
            {
                var currentInvocationList = currentDelegate.GetInvocationList();
                foreach (var currentDelegateInvokation in currentInvocationList)
                {
                    if (currentDelegateInvokation == null)
                        continue;

                    if (currentDelegateInvokation.Target == handler.Target)
                    {
                        if (currentDelegateInvokation.Method == handler.Method)
                        {
                            Debug.Log($"Messenger listener '{eventId}' with Target: '{handler.Target}' and Method: '{handler.Method}' already registred. Skipping this one");
                            return; //If we already this Target and Method hooked up we don't want to add it again
                        }
                    }
                }
            }

            _eventTable[eventId] = (Callback<T>)_eventTable[eventId] + handler;

#if LOG_ALL_MESSAGES || LOG_ADD_LISTENER
		Debug.Log("MESSENGER OnListenerAdded \t\"" + eventId + "\"\t{" + handler.Target + " -> " + handler.Method + "}");
#endif
        }

        #endregion

        #region RemoveListener

        /// <summary>
        /// Removes a listener for the specific message type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        //Single parameter
        public static void RemoveListener<T>(Callback<T> handler)
        {
            var eventId = GetEventId<T>();
            OnListenerRemoving(eventId, handler);

            if (_eventTable.ContainsKey(eventId))
                _eventTable[eventId] = (Callback<T>)_eventTable[eventId] - handler;

            OnListenerRemoved(eventId);
        }
        #endregion

        #region Broadcast

        /// <summary>
        /// Broadcast an message of type T to all registered listener for that message type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arg1"></param>
        //Single parameter
        static public void Broadcast<T>(T arg1)
        {
            var eventId = GetEventId<T>();

#if LOG_ALL_MESSAGES || LOG_BROADCAST_MESSAGE
		Debug.Log("MESSENGER\t" + System.DateTime.Now.ToString("hh:mm:ss.fff") + "\t\t\tInvoking \t\"" + eventId + "\"");
#endif
            OnBroadcasting(eventId);

            Delegate d;
            if (_eventTable.TryGetValue(eventId, out d))
            {
                Callback<T> callback = d as Callback<T>;

                if (callback != null)
                {
                    callback(arg1);
                }
                else
                {
                    throw CreateBroadcastSignatureException(eventId);
                }
            }
        }

        internal static void AddListener<T>()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
