namespace FireworksMania.Core.Behaviors
{
    /// <summary>
    /// What a destructible actually is, for achievements. Authored deliberately rather than inferred:
    /// nothing else on these prefabs identifies them - they carry no tag, no layer and no definition,
    /// and FlammableBehavior is absent from every House_Destructible prefab, so it is not a marker.
    /// </summary>
    public enum DestructibleKind
    {
        None = 0,
        Mailbox = 1,
        Car = 2,
        House = 3,
        //Landmarks rather than categories: one-shot achievements, not counted stats. Assigned by hand
        //to the specific gas-station and fireworks-store prefabs, never to a building category.
        GasStation = 4,
        FireworksStore = 5,
    }
}
