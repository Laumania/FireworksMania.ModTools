namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    /// <summary>
    /// Server-side attribution anchor: "which player's ignition chain lit this object". Explosion
    /// effects resolve it with GetComponentInParent, so implementors must sit on (or above) the
    /// object the explosion effects are parented under. First real causer wins; see IgnitionCauser.
    /// </summary>
    public interface IIgnitionCauserCarrier
    {
        ulong IgnitionCauserClientId { get; }
        void TrySetIgnitionCauser(ulong causerClientId);
        void ResetIgnitionCauser();
    }
}
