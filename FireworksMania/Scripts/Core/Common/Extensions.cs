using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace FireworksMania.Core.Common
{
    public static class Extensions
    {
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
    }
}
