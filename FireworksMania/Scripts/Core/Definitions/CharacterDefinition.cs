using UnityEngine;

namespace FireworksMania.Core.Definitions
{
    [CreateAssetMenu(fileName = "New Character Definition", menuName = "Fireworks Mania/Definitions/Character Definition")]
    public class CharacterDefinition : ScriptableObject
    {
        [Tooltip("Global unique definition id for this character definition.")]
        [SerializeField]
        private string _id;

        [Tooltip("Human readable name of this character model.")]
        [SerializeField]
        private string _name;

        [Tooltip("Prefab with the character prefab.")]
        [SerializeField]
        private GameObject _characterPrefab;

        [Tooltip("The Humanoid Avatar this character use for animations.")]
        [SerializeField]
        private Avatar _characterAvatar;

        public GameObject CharacterPrefab => _characterPrefab;
        public Avatar CharacterAvatar     => _characterAvatar;
        public string Id                  => _id;
        public string Name                => _name;
    }
}
