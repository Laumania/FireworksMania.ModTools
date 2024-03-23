using System;
using System.Linq;
using UnityEngine;
using IFuseConnectionTool = FireworksMania.Core.Tools.IFuseConnectionTool;

namespace FireworksMania.Core.Common
{
    [Obsolete("This isn't really working anymore in multiplayer - if you run into this reach out to me (Laumania) as I'm in doubt how many use this.", true)]
    public class GameReferenceManager : MonoBehaviour
    {
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            else
            {
                _instance = this;
            }
        }

        private void Start()
        {
            _fuseConnectionTool = FindObjectsOfType<MonoBehaviour>(true).OfType<IFuseConnectionTool>().SingleOrDefault();
        }

        public IFuseConnectionTool FuseConnectionTool => _fuseConnectionTool;
        private IFuseConnectionTool _fuseConnectionTool;

        private static GameReferenceManager _instance;
        public static GameReferenceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GameObject.FindObjectOfType<GameReferenceManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning($"Unable to find gameobject of type '{nameof(GameReferenceManager)}'");
                    }
                }

                return _instance;
            }
        }
    }
}
