namespace FireworksMania.Core.Persistence
{
    public interface ISaveableComponent
    {
        CustomEntityComponentData CaptureState();
        void RestoreState(CustomEntityComponentData customComponentData);

        string SaveableComponentTypeId { get; }
    }
}
