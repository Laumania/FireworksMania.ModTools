using UnityEngine;

namespace FireworksMania.Core.Utilities
{
    /// <summary>
    /// Combined bounds of every active MeshFilter and enabled SkinnedMeshRenderer under a root, in the
    /// root's own (unscaled) local space - for callers that size or center an object by what is actually
    /// visible rather than by where its pivot happens to sit, which on a firework is wherever it was
    /// authored. The spawn palm ghost and the hand-carry miniature both measure themselves with this (#1864).
    /// </summary>
    public static class MeshBoundsUtility
    {
        /// <returns>False when there was nothing visible to measure.</returns>
        public static bool TryGetLocalBounds(Transform root, out Bounds localBounds)
        {
            localBounds = default;
            var hasBounds = false;

            foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (meshFilter.sharedMesh != null)
                    EncapsulateMesh(meshFilter.sharedMesh.bounds, root.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix, ref localBounds, ref hasBounds);
            }

            //A skinned mesh is measured at rest, where its renderer sits - the same shape the spawn ghosts
            //copy - and never from SkinnedMeshRenderer.bounds, which are padded import bounds that follow
            //the root bone. Mod items have skinned bodies: measured by MeshFilters alone, the Hell Yeah
            //Mod's cats were just their fuse (#2857). Disabled ones are skipped like the ghost skips them.
            foreach (var skinnedMeshRenderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (skinnedMeshRenderer.enabled && skinnedMeshRenderer.sharedMesh != null)
                    EncapsulateMesh(skinnedMeshRenderer.sharedMesh.bounds, root.worldToLocalMatrix * skinnedMeshRenderer.transform.localToWorldMatrix, ref localBounds, ref hasBounds);
            }

            return hasBounds;
        }

        /// <summary>
        /// Grows <paramref name="bounds"/> to take in a mesh's own bounding box carried through
        /// <paramref name="meshToTarget"/>, into whatever space the caller measures in - the root's local
        /// space here, world space for the mount and mortar fit (FireworkMountRules, #2857).
        /// </summary>
        internal static void EncapsulateMesh(Bounds meshBounds, Matrix4x4 meshToTarget, ref Bounds bounds, ref bool hasBounds)
        {
            //All eight corners, because a rotated part's axis-aligned box has to be re-derived
            for (var corner = 0; corner < 8; corner++)
            {
                var cornerLocal = meshBounds.center + Vector3.Scale(meshBounds.extents, new Vector3(
                    (corner & 1) == 0 ? -1f : 1f,
                    (corner & 2) == 0 ? -1f : 1f,
                    (corner & 4) == 0 ? -1f : 1f));
                var cornerInTarget = meshToTarget.MultiplyPoint3x4(cornerLocal);

                if (hasBounds == false)
                {
                    bounds    = new Bounds(cornerInTarget, Vector3.zero);
                    hasBounds = true;
                }
                else
                    bounds.Encapsulate(cornerInTarget);
            }
        }
    }
}
