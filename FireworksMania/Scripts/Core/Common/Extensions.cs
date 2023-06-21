using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FireworksMania.Core.Common
{
    public static class Extensions
    {
        public static void SetRandomSeed(this ParticleSystem particleSystem, uint randomSeed)
        {
            if (particleSystem == null)
                return;

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (particleSystem.useAutoRandomSeed)
            {
                particleSystem.useAutoRandomSeed = false;
                particleSystem.randomSeed = randomSeed;
            }

            //for (int i = 0; i < particleSystem.subEmitters.subEmittersCount; i++)
            //{
            //    SetRandomSeed(particleSystem.subEmitters.GetSubEmitterSystem(i), ++randomSeed);
            //}

            foreach (Transform childTransform in particleSystem.transform)
            {
                var foundParticleInChild = childTransform.GetComponent<ParticleSystem>();
                if(foundParticleInChild != null)
                    SetRandomSeed(foundParticleInChild, ++randomSeed);
            }
        }
    }
}
