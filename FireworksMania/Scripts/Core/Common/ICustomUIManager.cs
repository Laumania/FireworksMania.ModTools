using UnityEngine;

namespace FireworksMania.Core.Common
{
    public interface ICustomUIManager
    {
        //Todo: Refactor this to be much simpler, it's way to complext as it is now.
        void ShowCanvas(Canvas canvas); //Todo: Consider just sending originalParent
        void HideCanvas(Canvas canvas);
        void RegisterCanvas(Canvas canvas, Transform originalParent);
        void UnregisterCanvas(Canvas canvas);
    }
}
