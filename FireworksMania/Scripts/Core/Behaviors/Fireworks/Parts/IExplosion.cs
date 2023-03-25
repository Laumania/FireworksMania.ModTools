namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    public interface IExplosion
    {
        void Explode();
        bool IsExploding { get; }
    }
}