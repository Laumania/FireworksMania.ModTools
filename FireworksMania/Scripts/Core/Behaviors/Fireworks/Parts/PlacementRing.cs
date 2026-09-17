using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    /// <summary>
    /// The green ring the Spawn and Physics Tools draw on every mortar tube and rack socket the held item fits.
    /// It is that opening's trigger sphere (#2911): centered on the sphere, facing the way the sphere's object
    /// faces and as big as its radius. A creator places and sizes the ring by placing and sizing the sphere, and
    /// a rack of any shape gets rings that follow it.
    ///
    /// It used to be worked out from the shape of the thing instead - a rack's flat top face, a tube's fuse
    /// pivot - and a rack whose sleeves fan out along an arch has no flat top face, so its outer rings hung in
    /// the air half a meter past its ends.
    ///
    /// The tools and the editor gizmo both read the ring from here, so what a creator sees in the editor is
    /// what a player gets in the game.
    /// </summary>
    internal static class PlacementRing
    {
        //The shape of PlacementGhostRingMesh, as fractions of the radius a ring is scaled to: a band between
        //these two radii, rising from the opening up to its thickness. The gizmo draws exactly this band, and
        //PlacementRingTests holds the mesh to the same numbers, so the editor and the game cannot drift apart.
        public const float InnerEdge = 0.78f;
        public const float OuterEdge = 1.10f;
        public const float Thickness = 0.1f;

        public static Pose GetPose(SphereCollider sphere) =>
            new Pose(sphere.transform.TransformPoint(sphere.center), sphere.transform.rotation);

        //Sized the way Unity sizes the sphere itself, by the largest axis of its scale
        public static float GetRadius(SphereCollider sphere)
        {
            var scale = sphere.transform.lossyScale;

            return sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }

#if UNITY_EDITOR
        private static readonly Color RingGizmoColor = new Color(0.35f, 1f, 0.3f, 0.9f);

        //Both edges of the band, where it meets the opening and at its top - the ring the tools put in the game
        public static void DrawGizmo(Pose pose, float radius)
        {
            if (radius <= 0f)
                return;

            var facing = pose.rotation * Vector3.up;
            var top    = pose.position + facing * (radius * Thickness);

            UnityEditor.Handles.color = RingGizmoColor;
            UnityEditor.Handles.DrawWireDisc(pose.position, facing, radius * InnerEdge, 2f);
            UnityEditor.Handles.DrawWireDisc(pose.position, facing, radius * OuterEdge, 2f);
            UnityEditor.Handles.DrawWireDisc(top,           facing, radius * InnerEdge, 2f);
            UnityEditor.Handles.DrawWireDisc(top,           facing, radius * OuterEdge, 2f);
        }
#endif
    }
}
