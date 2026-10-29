using System.Collections.Generic;
using FireworksMania.Core.Utilities;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    /// <summary>
    /// An item's shape as a rack socket sees it: its visible parts and its colliders, each measured upright,
    /// so a socket can ask how wide just the bottom of it is - the part that goes down the bore (#3046).
    ///
    /// Measured whole, a rocket is as wide as its head and bounced off every socket its stick fits. Only the
    /// bore has to fit, and a rack's bore is under 10 cm deep. Within it a mesh that sits wholly inside is
    /// measured as it looks. A mesh that runs on up out of the bore can not say how wide it is at the bottom -
    /// the Mod Tools rocket template is stick and head in one mesh - so the colliders reaching into the bore
    /// stand in for it: they are what the author said the body is, and what physically enters the tube.
    ///
    /// Everything is read from transforms, mesh bounds and collider settings rather than from Renderer or
    /// Collider bounds, so it measures a prefab asset (the SpawnTool's item) exactly like a live object.
    /// Mesh vertices are never read: shipped meshes are not readable.
    /// </summary>
    public sealed class MountFitProfile
    {
        //Slack for a part that ends exactly at the bore's top
        private const float BoreEdgeTolerance = 0.001f;

        private readonly List<Bounds> _visualParts   = new List<Bounds>();
        private readonly List<Bounds> _colliderParts = new List<Bounds>();
        private          Bounds       _visualBounds;

        private MountFitProfile() { }

        /// <summary>
        /// Measures <paramref name="item"/> as if it stood upright, whatever way it is turned right now.
        /// Returns null when it has nothing visible to measure.
        /// </summary>
        public static MountFitProfile Measure(GameObject item)
        {
            var profile   = new MountFitProfile();
            var root      = item.transform;
            var toUpright = Matrix4x4.Scale(root.lossyScale) * root.worldToLocalMatrix;

            //The same parts CalculateUprightRendererBounds measures, so the bottom here is the bottom the socket
            //seats the item on: every MeshRenderer, and every enabled skinned mesh at rest (#2857)
            foreach (var meshRenderer in item.GetComponentsInChildren<MeshRenderer>())
            {
                if (meshRenderer.TryGetComponent<MeshFilter>(out var meshFilter) && meshFilter.sharedMesh != null)
                    profile.AddVisualPart(meshFilter.sharedMesh.bounds, toUpright * meshFilter.transform.localToWorldMatrix);
            }

            foreach (var skinnedMeshRenderer in item.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (skinnedMeshRenderer.enabled && skinnedMeshRenderer.sharedMesh != null)
                    profile.AddVisualPart(skinnedMeshRenderer.sharedMesh.bounds, toUpright * skinnedMeshRenderer.transform.localToWorldMatrix);
            }

            if (profile._visualParts.Count == 0)
                return null;

            foreach (var collider in item.GetComponentsInChildren<Collider>())
            {
                if (collider.isTrigger == false && collider.enabled && TryGetUprightColliderBounds(collider, toUpright * collider.transform.localToWorldMatrix, out var colliderBounds))
                    profile._colliderParts.Add(colliderBounds);
            }

            return profile;
        }

        /// <summary>
        /// Size of what lies within <paramref name="boreDepth"/> of the item's bottom. A depth of zero or less
        /// means the socket has no depth to go by, and the whole item is measured.
        /// </summary>
        internal Vector3 FootprintWithinDepth(float boreDepth)
        {
            if (boreDepth <= 0f)
                return _visualBounds.size;

            var boreTop        = _visualBounds.min.y + boreDepth;
            var footprint      = default(Bounds);
            var hasFootprint   = false;
            var leavesTheBore  = false;
            var colliderInBore = false;

            for (int i = 0; i < _visualParts.Count; i++)
            {
                var visualPart = _visualParts[i];
                if (visualPart.min.y > boreTop)
                    continue;

                if (visualPart.max.y <= boreTop + BoreEdgeTolerance)
                    Encapsulate(ref footprint, ref hasFootprint, visualPart);
                else
                    leavesTheBore = true;
            }

            for (int i = 0; i < _colliderParts.Count; i++)
            {
                if (_colliderParts[i].min.y <= boreTop)
                {
                    Encapsulate(ref footprint, ref hasFootprint, _colliderParts[i]);
                    colliderInBore = true;
                }
            }

            //Nothing to stand in for a mesh that leaves the bore, so it is measured as it looks - an item is
            //never waved through for want of a collider
            if (leavesTheBore && colliderInBore == false)
            {
                for (int i = 0; i < _visualParts.Count; i++)
                {
                    if (_visualParts[i].min.y <= boreTop)
                        Encapsulate(ref footprint, ref hasFootprint, _visualParts[i]);
                }
            }

            return hasFootprint ? footprint.size : _visualBounds.size;
        }

        private void AddVisualPart(Bounds meshBounds, Matrix4x4 meshToUpright)
        {
            var partBounds    = default(Bounds);
            var hasPartBounds = false;
            MeshBoundsUtility.EncapsulateMesh(meshBounds, meshToUpright, ref partBounds, ref hasPartBounds);

            if (_visualParts.Count == 0)
                _visualBounds = partBounds;
            else
                _visualBounds.Encapsulate(partBounds);

            _visualParts.Add(partBounds);
        }

        private static bool TryGetUprightColliderBounds(Collider collider, Matrix4x4 colliderToUpright, out Bounds uprightBounds)
        {
            uprightBounds = default;
            var hasBounds = false;

            switch (collider)
            {
                case BoxCollider box:
                    MeshBoundsUtility.EncapsulateMesh(new Bounds(box.center, box.size), colliderToUpright, ref uprightBounds, ref hasBounds);
                    return true;

                case MeshCollider meshCollider when meshCollider.sharedMesh != null:
                    MeshBoundsUtility.EncapsulateMesh(meshCollider.sharedMesh.bounds, colliderToUpright, ref uprightBounds, ref hasBounds);
                    return true;

                case SphereCollider sphere:
                {
                    //PhysX scales a sphere by its largest axis
                    var radius = sphere.radius * MaxAxisScale(colliderToUpright, Vector3.right, Vector3.up, Vector3.forward);
                    uprightBounds = new Bounds(colliderToUpright.MultiplyPoint3x4(sphere.center), Vector3.one * (radius * 2f));
                    return true;
                }

                case CapsuleCollider capsule:
                {
                    //Two spheres at the ends of the segment: PhysX scales the height by the capsule's own axis and
                    //the radius by the larger of the other two
                    var axis      = capsule.direction == 0 ? Vector3.right : capsule.direction == 2 ? Vector3.forward : Vector3.up;
                    var sideA     = capsule.direction == 0 ? Vector3.up    : Vector3.right;
                    var sideB     = capsule.direction == 2 ? Vector3.up    : Vector3.forward;
                    var axisWorld = colliderToUpright.MultiplyVector(axis);
                    var radius    = capsule.radius * MaxAxisScale(colliderToUpright, sideA, sideB, sideB);
                    var halfSpine = Mathf.Max(capsule.height * 0.5f * axisWorld.magnitude - radius, 0f);
                    var center    = colliderToUpright.MultiplyPoint3x4(capsule.center);
                    var spine     = axisWorld.normalized * halfSpine;
                    var diameter  = Vector3.one * (radius * 2f);

                    uprightBounds = new Bounds(center + spine, diameter);
                    uprightBounds.Encapsulate(new Bounds(center - spine, diameter));
                    return true;
                }

                default:
                    return false;
            }
        }

        private static float MaxAxisScale(Matrix4x4 matrix, Vector3 axisA, Vector3 axisB, Vector3 axisC)
        {
            return Mathf.Max(matrix.MultiplyVector(axisA).magnitude, Mathf.Max(matrix.MultiplyVector(axisB).magnitude, matrix.MultiplyVector(axisC).magnitude));
        }

        private static void Encapsulate(ref Bounds bounds, ref bool hasBounds, Bounds part)
        {
            if (hasBounds)
                bounds.Encapsulate(part);
            else
            {
                bounds    = part;
                hasBounds = true;
            }
        }
    }
}
