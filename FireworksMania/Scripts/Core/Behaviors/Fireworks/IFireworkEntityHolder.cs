namespace FireworksMania.Core.Behaviors.Fireworks
{
    /// <summary>
    /// Marker for entities that hold other firework entities - mortars, firework mount racks and
    /// the like. A holder can never be loaded, swallowed or mounted into another holder: no
    /// mortars inside mortars, no racks inside mortars, no racks mounted on racks.
    /// </summary>
    public interface IFireworkEntityHolder
    {
    }
}
