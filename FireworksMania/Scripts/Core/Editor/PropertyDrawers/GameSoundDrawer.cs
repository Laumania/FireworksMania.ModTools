using System;
using System.Collections.Generic;
using System.Linq;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Definitions;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Events;
using AssetDatabaseHelper = FireworksMania.Core.Editor.Helpers.AssetDatabaseHelper;

namespace FireworksMania.Core.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(GameSoundAttribute))]
    public class GameSoundDrawer : PropertyDrawer
    {
        private List<string> _selectableSoundItems;
        private HashSet<string> _soundOptions = new HashSet<string>();
        private string temp;


        public GameSoundDrawer()
        {
            PopulateFromGameSoundCollections();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (temp != null)
            {
                property.stringValue = temp;
            }
            Rect button = new Rect(position.x + 200, position.y, position.width - 200, position.height);

            GUI.Label(position, label);
            //Debug.Log("Current Property = " + property.stringValue);

            if (GUI.Button(button, property.stringValue, EditorStyles.popup))
            {
                StringListSearchProvider provider = ScriptableObject.CreateInstance<StringListSearchProvider>();
                provider.setItems(_selectableSoundItems.ToArray());
                provider.setCallback((x) => { temp = x; });
                SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), provider);
            }
        }


        private void PopulateFromGameSoundCollections()
        {
            _soundOptions.Clear();

            var soundCollections = AssetDatabaseHelper.FindAssetsByType<SoundCollection>();

            foreach (var foundGameSoundCollectionItem in soundCollections)
            {
                foreach (var soundItem in foundGameSoundCollectionItem.Sounds)
                {
                    if (_soundOptions.Contains(soundItem) == false)
                        _soundOptions.Add("Fireworks Mania/" +soundItem);
                }
            }

            var gameSoundDefinitions = AssetDatabaseHelper.FindAssetsByType<GameSoundDefinition>();
            foreach (var gameSoundDef in gameSoundDefinitions)
            {
                if (gameSoundDef != null)
                {
                    if (_soundOptions.Contains(gameSoundDef.name) == false)
                        _soundOptions.Add("Others/"+gameSoundDef.name);
                }
            }

            _selectableSoundItems = _soundOptions.ToList();
            _selectableSoundItems.Sort(StringComparer.OrdinalIgnoreCase);
        }
    }

    public class StringListSearchProvider : ScriptableObject, ISearchWindowProvider
    {

        private string[] listItems;
        private UnityAction<string> onSetIndexCallback;

        public void setCallback(UnityAction<string> callback)
        {
            onSetIndexCallback = callback;
        }

        public void setItems(string[] items)
        {
            listItems = items;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> searchlist = new List<SearchTreeEntry>();
            searchlist.Add(new SearchTreeGroupEntry(new GUIContent("Game Sounds"), 0));

            List<string> groups = new List<string>();
            foreach (string item in listItems)
            {
                string[] entryTitle = item.Split('/');
                string groupName = "";
                for (int i = 0; i < entryTitle.Length - 1; i++)
                {
                    groupName += entryTitle[i];
                    if (!groups.Contains(groupName))
                    {
                        searchlist.Add(new SearchTreeGroupEntry(new GUIContent(entryTitle[i]), i + 1));
                        groups.Add(groupName);
                    }
                    groupName += "/";
                }

                SearchTreeEntry entry = new SearchTreeEntry(new GUIContent(entryTitle.Last()));
                entry.level = entryTitle.Length;
                entry.userData = entryTitle.Last();
                searchlist.Add(entry);
            }

            return searchlist;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            onSetIndexCallback?.Invoke((string)SearchTreeEntry.userData);
            return true;
        }

    }
}
