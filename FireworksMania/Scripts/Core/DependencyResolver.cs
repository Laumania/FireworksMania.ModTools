using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FireworksMania.Core
{
    public class DependencyResolver : MonoBehaviour
    {
        private IDictionary<string,object> _cache = new Dictionary<string,object>();

        public void Awake()
        {
            //Claim, and no early return: the duplicate branch destroys the OTHER instance and keeps THIS
            //one, so returning left the static pointing at the destroyed one - and every Get<T> then ran
            //against its stale cache instead of this instance's empty one (#2390).
            //This is SceneSingleton.Claim spelled out - that lives in the game assembly, which Core can't
            //reference (#2902). DependencyResolverTests pins this copy to the same behavior.
            if (_instance != null && _instance != this)
            {
                //Destroy throws outside play mode, and the EditMode tests drive this branch
                if (Application.isPlaying)
                    Destroy(_instance.gameObject);
                else
                    DestroyImmediate(_instance.gameObject);
            }

            _instance = this;
        }

        public T Get<T>()
        {
            var cacheKey = typeof(T).FullName;
            if (_cache.TryGetValue(cacheKey, out var foundInCache))
            {
                if(foundInCache != null && (foundInCache as UnityEngine.Object) != null && ReferenceEquals(foundInCache, null) == false)
                {
                    //Debug.Log($"[{nameof(DependencyResolver)}] Resolved '{cacheKey}' from cache");
                    return (T)foundInCache;
                }
                else
                    _cache.Remove(cacheKey);
            }

            var foundImplementation = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).OfType<T>().FirstOrDefault();
            if(foundImplementation != null) 
            {
                //Debug.Log($"[{nameof(DependencyResolver)}] Added '{cacheKey}' to cache");
                _cache.Add(cacheKey, foundImplementation);
                return foundImplementation;
            }

            //Debug.Log($"[{nameof(DependencyResolver)}] Unable to resolve '{cacheKey}'");
            return default;
        }

        private static DependencyResolver _instance;
        public static DependencyResolver Instance => _instance;
    }
}
