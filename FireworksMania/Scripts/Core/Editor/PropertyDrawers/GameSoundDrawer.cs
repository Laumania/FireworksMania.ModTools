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
            var gameSoundNames = AssetDatabaseHelper.FindAssetsByType<GameSoundNameCollection>()
                                                    .SelectMany(collection => collection.Sounds);

            var definitionNames = AssetDatabaseHelper.FindAssetsByType<GameSoundDefinition>()
                                                     .Where(definition => definition != null)
                                                     .Select(definition => definition.name);

            _selectableSoundItems = BuildSelectableSoundItems(gameSoundNames, definitionNames);
        }

        //A sound is listed once. The game's own sounds are in a GameSoundNameCollection and usually also a
        //GameSoundDefinition in the project, so those go under "Fireworks Mania" and only the rest - a mod's own
        //definitions - under "Others" (#3024). Ignoring case, because MasterAudio looks sound groups up that way.
        internal static List<string> BuildSelectableSoundItems(IEnumerable<string> gameSoundNames, IEnumerable<string> definitionNames)
        {
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var items     = new List<string>();

            foreach (var soundName in gameSoundNames)
            {
                if (seenNames.Add(soundName))
                    items.Add("Fireworks Mania/" + soundName);
            }

            foreach (var soundName in definitionNames)
            {
                if (seenNames.Add(soundName))
                    items.Add("Others/" + soundName);
            }

            items.Sort(StringComparer.OrdinalIgnoreCase);
            return items;
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
