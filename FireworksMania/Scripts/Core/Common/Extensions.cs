using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace FireworksMania.Core.Common
{
    public static class Extensions
    {
        //Reused between calls as every firework runs this once in Awake, and loading a big blueprint means
        //thousands of them in a row - main thread only, same as everything else touching ParticleSystems
        private static readonly List<ParticleSystem>    _particleSystemsBuffer = new List<ParticleSystem>();
        private static readonly HashSet<ParticleSystem> _subEmittersBuffer     = new HashSet<ParticleSystem>();
        private static readonly List<string>            _unloopedPathsBuffer   = new List<string>();

        /// <summary>
        /// Turns off looping on every particle system in the effect that would otherwise keep it alive forever.
        /// <para>
        /// One-shot fireworks (cakes, fountains, shells, ...) decide they are done by waiting for
        /// <see cref="ParticleSystem.IsAlive(bool)"/> with children included. A looping system anywhere in the
        /// effect therefore never lets that wait complete: the effect is never stopped, the firework never
        /// despawns, and its particle systems keep ticking for the rest of the session
        /// (https://github.com/Laumania/FireworksMania/issues/2325).
        /// </para>
        /// <para>
        /// Sub-emitters are deliberately left looping. They are driven by the particles of the system that owns
        /// them rather than emitting on their own, so looping is a normal authoring choice there - most vanilla
        /// cakes rely on it for trails and smoke - and it never keeps the effect alive. Un-looping those would
        /// change how the effect plays, which is exactly what this must not do.
        /// </para>
        /// <para>
        /// Never call this on a fuse or a thruster effect: those are meant to loop for as long as they burn and
        /// are stopped explicitly by their own behavior.
        /// </para>
        /// <para>
        /// Looping is turned off on every single instance, but it is only logged once per item and effect
        /// (https://github.com/Laumania/FireworksMania/issues/2359), and never on a dedicated server - see
        /// <see cref="ContentWarnings"/>.
        /// </para>
        /// </summary>
        /// <param name="itemName">
        /// Optional name of the item the effect belongs to, used to group the log. Only needed when the effect
        /// has been cloned out of its own prefab and placed under something else - a shell effect loaded into a
        /// mortar tube would otherwise be blamed on the tube. Everything else resolves it from the effect itself.
        /// </param>
        public static void DisableEndlessLooping(this ParticleSystem particleSystem, string itemName = null)
        {
            if (particleSystem == null)
                return;

            particleSystem.GetComponentsInChildren(true, _particleSystemsBuffer);

            _subEmittersBuffer.Clear();
            foreach (var system in _particleSystemsBuffer)
            {
                var subEmittersModule = system.subEmitters;
                if (subEmittersModule.enabled == false)
                    continue;

                for (int i = 0; i < subEmittersModule.subEmittersCount; i++)
                {
                    var subEmitter = subEmittersModule.GetSubEmitterSystem(i);
                    if (subEmitter != null)
                        _subEmittersBuffer.Add(subEmitter);
                }
            }

            _unloopedPathsBuffer.Clear();
            foreach (var system in _particleSystemsBuffer)
            {
                var mainModule = system.main;
                if (mainModule.loop == false || _subEmittersBuffer.Contains(system))
                    continue;

                mainModule.loop = false;
                _unloopedPathsBuffer.Add(GetPathInsideEffect(particleSystem.transform, system.transform));
            }

            if (_unloopedPathsBuffer.Count > 0 && ContentWarnings.AreLogged)
                WarnAboutTurnedOffLooping(particleSystem, itemName);

            _particleSystemsBuffer.Clear();
            _subEmittersBuffer.Clear();
            _unloopedPathsBuffer.Clear();
        }

        /// <summary>
        /// Logs the systems in <see cref="_unloopedPathsBuffer"/> as one warning, and only the first time this
        /// effect of this item is seen. The message used to be one warning per particle system per hierarchy
        /// path, which meant every single placed firework with the problem logged again - hundreds of identical
        /// paragraphs telling the reader nothing the first one didn't.
        /// </summary>
        private static void WarnAboutTurnedOffLooping(ParticleSystem effect, string itemName)
        {
            //The effect is the same content no matter where it ends up in the scene, so the item it belongs to is
            //what identifies it - not the hierarchy path, which differs for every instance and every parent
            if (string.IsNullOrEmpty(itemName))
                itemName = ContentWarnings.ResolveItemName(effect);

            if (ContentWarnings.ShouldLog(nameof(DisableEndlessLooping), itemName, effect.gameObject.name) == false)
                return;

            //Hardcoded English on purpose - this tells whoever built the content which particle system to fix,
            //and it has to match what they can search for in the log
            var count = _unloopedPathsBuffer.Count;
            var paths = string.Join(", ", _unloopedPathsBuffer.Select(path => $"'{path}'"));

            Debug.LogWarning($"Turned off 'Looping' on {count} particle system{(count == 1 ? "" : "s")} in '{itemName}': {paths} - a firework effect that loops never finishes, so the firework would never disappear and its particle systems would keep running for the rest of the session. Please turn off 'Looping' on the listed particle systems in the mod project, or wire them up as sub-emitters if they are meant to keep emitting. This is only logged once - 'Looping' is still turned off on every instance you place.", effect.gameObject);
        }

        /// <summary>
        /// Path from the effect root down to <paramref name="target"/>, e.g. "Effect/Explosion_1". Deliberately
        /// not the full hierarchy path: that one carries whatever the effect currently hangs under, which is
        /// noise to a modder looking for the particle system inside their own prefab.
        /// </summary>
        private static string GetPathInsideEffect(Transform effectRoot, Transform target)
        {
            //Has to stop here rather than fall through to the walk below - the effect root does have a parent
            //(the firework it sits on), and walking that far up is exactly what this avoids
            if (target == effectRoot)
                return effectRoot.name;

            var path    = target.name;
            var current = target.parent;

            while (current != null && current != effectRoot)
            {
                path    = $"{current.name}/{path}";
                current = current.parent;
            }

            return $"{effectRoot.name}/{path}";
        }

        public static void SetRandomSeed(this ParticleSystem particleSystem, uint randomSeed, float time = 0f)
        {
            if (particleSystem == null)
                return;

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (particleSystem.useAutoRandomSeed)
            {
                particleSystem.useAutoRandomSeed = false;
                particleSystem.randomSeed        = randomSeed;
            }

            foreach (Transform childTransform in particleSystem.transform)
            {
                var foundParticleInChild = childTransform.GetComponent<ParticleSystem>();
                if(foundParticleInChild != null)
                    SetRandomSeed(foundParticleInChild, ++randomSeed);
            }

            if (time > 0f)
            {
                if (particleSystem.gameObject.activeSelf == false)
                    Debug.Log($"ParticleSystem '{particleSystem.gameObject.name}' is not active why simulation to sync between server and client will not work. Make sure to set it to active before calling this method", particleSystem.gameObject);

                //Debug.Log($"Simulate particle system '{particleSystem.gameObject.name}' by time '{time}'");
                particleSystem.Simulate(time, true, true);
            }
        }

        public static string ToDelimitedString<T>(this IEnumerable<T> sequence)
        {
            if (sequence == null)
                return "null";
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("(");
            bool flag = true;
            foreach (T obj in sequence)
            {
                if (!flag)
                    stringBuilder.Append(", ");
                flag = false;
                stringBuilder.Append((object)obj);
            }
            stringBuilder.Append(")");
            return stringBuilder.ToString();
        }
    }
}
