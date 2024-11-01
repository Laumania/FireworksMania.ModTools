namespace FireworksMania.Core.Behaviors
{
    public interface IDestructible
    {
        void ApplyDamage(float damage);
        bool IsDestroyed { get; }
    }
}
