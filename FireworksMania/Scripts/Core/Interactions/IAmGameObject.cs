using System;
using UnityEngine;

namespace FireworksMania.Core.Interactions
{
    [Obsolete("This interface is obsolete, replace with IAmGameObject", true)]
    public interface IHaveObjectInfo : IAmGameObject
    {
    }

    public interface IAmGameObject
    {
        string Name           { get; }
        GameObject GameObject { get; }
    }
}
