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
            if (_instance != null && _instance != this)
            {
                Destroy(_instance.gameObject);
                return;
            }
            _instance = this;
        }

        public T Get<T>()
        {
            var cacheKey = typeof(T).FullName;
            if (_cache.TryGetValue(cacheKey, out var foundInCache))
            {
                if(foundInCache != null)
                {
                    //Debug.Log($"[{nameof(DependencyResolver)}] Resolved '{cacheKey}' from cache");
                    return (T)foundInCache;
                }
                else
                    _cache.Remove(cacheKey);
            }

            var foundImplementation = GameObject.FindObjectsOfType<MonoBehaviour>(true).OfType<T>().FirstOrDefault();
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
