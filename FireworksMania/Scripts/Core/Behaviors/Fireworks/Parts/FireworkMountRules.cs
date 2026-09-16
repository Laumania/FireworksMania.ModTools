using FireworksMania.Core.Utilities;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    /// <summary>
    /// Pure decision rules for what a FireworkMountPoint seats, keeps holding, rejects and releases.
    /// Kept free of scene state so the rules are covered by EditMode tests (FireworkMountRulesTests).
    /// </summary>
    public static class FireworkMountRules
    {
        //"Roughly fit" (#2288): PreloadedTubes/RomanCandles have no EntityDiameterDefinition, so the
        //renderer bounds footprint is measured against the socket diameter with a little slack
        public const float FitTolerance = 1.1f;

        public static bool FitsDiameter(Vector3 boundsSize, float allowedDiameter)
        {
            if (allowedDiameter <= 0f)
                return false;

            var maxAllowedSize = allowedDiameter * FitTolerance;
            return boundsSize.x <= maxAllowedSize && boundsSize.z <= maxAllowedSize;
        }

        //Blueprint loads restore seated fireworks kinematic inside the sockets' triggers before the
        //rack re-registers them - a kinematic candidate is never a throw/drop, so it must never seat
        //(or bounce) via the trigger path
        public static bool CanMount(bool isSocketOccupied, bool isFirework, bool isIgnited, bool isPickedUp, bool isAlreadySeated, bool isMissingSpawnedNetworkObject, bool isKinematic)
        {
            return isSocketOccupied              == false &&
                   isFirework                    == true  &&
                   isIgnited                     == false &&
                   isPickedUp                    == false &&
                   isAlreadySeated               == false &&
                   isMissingSpawnedNetworkObject == false &&
                   isKinematic                   == false;
        }

        public static bool ShouldRelease(bool seatedEntityExists, bool isKinematic, bool isPickedUp)
        {
            if (seatedEntityExists == false)
                return true;

            //Rockets flip themselves dynamic at launch and the PhysicsTool does the same when grabbing,
            //while ObjectPickup keeps things kinematic but stamps IsPickedUp - all three mean "let go"
            return isKinematic == false || isPickedUp == true;
        }

        public static bool ShouldRejectWithForce(bool canMountIgnoringFit, bool fitsDiameter, bool isKinematic, bool isIgnited, bool isPickedUp)
        {
            if (isKinematic)
                return false;

            //A carried item is not a failed placement - it is a player holding something at the socket
            //while they line it up. Bouncing it out of their hands, with the reject sound, in the middle
            //of that is nonsense. ObjectPickup never reached here because it carries things KINEMATIC
            //and so takes the early out above; the PhysicsTool carries them dynamic, so the moment
            //IsPickedUp started making CanMount say no, every firework carried up to a socket got
            //rejected on the spot - "it snaps, plays the reject sound and pops straight out" (#2471)
            if (isPickedUp)
                return false;

            //An ignited firework is leaving under its own power - a launching rocket sweeps
            //through its own and neighboring tube triggers, and bouncing it (plus the reject
            //sound) at liftoff would be pure nonsense
            if (isIgnited)
                return false;

            return canMountIgnoringFit == false || fitsDiameter == false;
        }

        /// <summary>
        /// Combined bounds of the GameObject's MeshRenderers and enabled SkinnedMeshRenderers, measured
        /// with its rotation reset, so the footprint is comparable no matter how the object is currently
        /// tumbling. Shared by mount points, mortar tubes and the spawn tool. Returns null when there is
        /// nothing to measure.
        /// </summary>
        public static Bounds? CalculateUprightRendererBounds(GameObject targetGameObject)
        {
            var originalRotation                = targetGameObject.transform.rotation;
            targetGameObject.transform.rotation = Quaternion.identity;

            try
            {
                var meshRenderers   = targetGameObject.GetComponentsInChildren<MeshRenderer>();
                var hasBounds       = meshRenderers.Length > 0;
                var resultingBounds = hasBounds ? meshRenderers[0].bounds : default;
                for (int i = 1; i < meshRenderers.Length; i++)
                    resultingBounds.Encapsulate(meshRenderers[i].bounds);

                //A skinned body at rest, where its renderer sits - never SkinnedMeshRenderer.bounds, which
                //are padded import bounds that follow the root bone. Measured by MeshRenderers alone, the
                //Hell Yeah Mod's cats were just their fuse: The Kitty seated itself in a 4.6 cm rack socket
                //and UIA Cat went down a 2-inch mortar (#2857). Disabled ones are skipped, as the ghost does.
                foreach (var skinnedMeshRenderer in targetGameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (skinnedMeshRenderer.enabled && skinnedMeshRenderer.sharedMesh != null)
                        MeshBoundsUtility.EncapsulateMesh(skinnedMeshRenderer.sharedMesh.bounds, skinnedMeshRenderer.transform.localToWorldMatrix, ref resultingBounds, ref hasBounds);
                }

                return hasBounds ? resultingBounds : (Bounds?)null;
            }
            finally
            {
                targetGameObject.transform.rotation = originalRotation;
            }
        }
    }
}
