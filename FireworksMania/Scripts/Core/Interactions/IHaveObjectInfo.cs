using UnityEngine;

namespace FireworksMania.Core.Interactions
{
    public interface IHaveObjectInfo
    {
        string Name           { get; }
        GameObject GameObject { get; }
    }
}
