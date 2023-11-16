using System;
using UnityEngine;

namespace FireworksMania.Core.Definitions
{
    [CreateAssetMenu(fileName = "New Map Definition", menuName = "Fireworks Mania/Definitions/Map Definition")]
    public class MapDefinition : ScriptableObject
    {
        [Header("General")]
        [Tooltip("Name of the map. Used to display in map selection UI")]
        [SerializeField]
        private string _mapName = "Untitled Map";

        [Tooltip("Description of the map")]
        [SerializeField]
        private string _description = "";

        [SerializeField]
        private Sprite[] _thumbnails;

        [SerializeField]
        [Tooltip("Exact name of the scene in your mod holds the map. Important, name your scene something unique")]
        private string _sceneName;

        [Header("Game Settings")]
        [SerializeField]
        private GameSettings _gameSettings;


        [Header("Environment Settings")]
        [SerializeField]
        private TimeSettings _timeSettings;
        [SerializeField]
        private LightingSettings _lightingSettings;
        [SerializeField]
        private SkySettings _skySettings;
        //[SerializeField]
        //private AudioSettings _audioSettings;
        [SerializeField]
        private WeatherSettings _weatherSettings;


        public string SceneName                      => _sceneName;
        public string Description                    => _description;
        public string MapName                        => _mapName;
        public Sprite[] Thumbnails                   => _thumbnails;
        public LightingSettings LightingSettings     => _lightingSettings;
        public SkySettings SkySettings               => _skySettings;
        //public AudioSettings AudioSettings           => _audioSettings;
        public TimeSettings TimeSettings             => _timeSettings;
        public WeatherSettings WeatherSettings       => _weatherSettings;
        public GameSettings GameSettings             => _gameSettings;
    }

    [Serializable]
    public struct GameSettings
    {
        [SerializeField]
        [Tooltip("Set the Y coordinate for where the 'ObjectCatcher' should be positioned in game. The 'ObjectCatcher' is responsible for catching the player and respawn, once the player hits it. This is normally place not too far below ground level in the map. Its also responsible for catching and destroying gameobjects falling over the edge of the map. In most cases you don't want to change this, if you map is placed at the normal Y=0 level.")]
        private Common.SerializableNullable<int> _objectCatcherDepth;

        public Common.SerializableNullable<int> ObjectCatcherDepth => _objectCatcherDepth;
    }

    [Serializable]
    public struct LightingSettings
    {
        [SerializeField]
        private Common.SerializableNullable<AnimationCurve> _ambientIntensityCurve;
        [SerializeField]
        private Common.SerializableNullable<Gradient> _ambientSkyColorGradient;

        [SerializeField]
        private Common.SerializableNullable<AnimationCurve> _sunIntensityCurve;
        [SerializeField]
        private Common.SerializableNullable<AnimationCurve> _moonIntensityCurve;

        public Common.SerializableNullable<AnimationCurve> AmbientIntensityCurve => _ambientIntensityCurve;
        public Common.SerializableNullable<Gradient> AmbientSkyColorGradient     => _ambientSkyColorGradient;
        public Common.SerializableNullable<AnimationCurve> SunIntensityCurve     => _sunIntensityCurve;
        public Common.SerializableNullable<AnimationCurve> MoonIntensityCurve    => _moonIntensityCurve;
    }

    [Serializable]
    public struct SkySettings
    {
        [SerializeField]
        private Common.SerializableNullable<AnimationCurve> _intensityCurve;
        [SerializeField]
        private Common.SerializableNullable<float> _intensity;

        public Common.SerializableNullable<AnimationCurve> IntensityCurve => _intensityCurve;
        public Common.SerializableNullable<float> Intensity => _intensity; 
    }

    [Serializable]
    public struct AudioSettings
    {
        [SerializeField]
        private Common.SerializableNullable<AudioClip> _ambientDayClip;
        [SerializeField]
        private Common.SerializableNullable<AnimationCurve> _ambientDayVolumeCurve;

        [SerializeField]
        private Common.SerializableNullable<AudioClip> _ambientNightClip;
        [SerializeField]
        private Common.SerializableNullable<AnimationCurve> _ambientNightVolumeCurve;


        public Common.SerializableNullable<AudioClip> AmbientDayClip               => _ambientDayClip;
        public Common.SerializableNullable<AnimationCurve> AmbientDayVolumeCurve   => _ambientDayVolumeCurve;
        public Common.SerializableNullable<AudioClip> AmbientNightClip             => _ambientNightClip;
        public Common.SerializableNullable<AnimationCurve> AmbientNightVolumeCurve => _ambientNightVolumeCurve;
    }

    [Serializable]
    public struct TimeSettings
    {
        [Tooltip("Set the initial time of day in hours. (12.5 = 12:30)")]
        [SerializeField]
        private Common.SerializableNullable<float> _startTimeOfDay;
        
        public Common.SerializableNullable<float> StartTimeOfDay => _startTimeOfDay;
    }

    [Serializable]
    public struct WeatherSettings
    {
        //[SerializeField]
        //private SerializableNullable<bool> _volumetricClouds;
        [SerializeField]
        private Common.SerializableNullable<WeatherPresetType> _startWeather;

        //[Header("Weather Preset Settings")]
        //[SerializeField]
        //private WeatherSettingPreset _clearSky;
        //[SerializeField]
        //private WeatherSettingPreset _cloudy;
        //[SerializeField]
        //private WeatherSettingPreset _foggy;
        //[SerializeField]
        //private WeatherSettingPreset _rain;
        //[SerializeField]
        //private WeatherSettingPreset _snow;

        //public SerializableNullable<bool> VolumetricClouds          => _volumetricClouds;
        public Common.SerializableNullable<WeatherPresetType> StartWeather => _startWeather;

        //public WeatherSettingPreset ClearSky => _clearSky;
        //public WeatherSettingPreset Cloudy   => _cloudy;
        //public WeatherSettingPreset Foggy    => _foggy;
        //public WeatherSettingPreset Rain     => _rain;
        //public WeatherSettingPreset Snow     => _snow;
    }

    [Serializable]
    public struct WeatherSettingPreset
    {
        [Header("Wind Settings - EXPERIMENTAL")]
        [SerializeField]
        [Tooltip("Will be clamped between -1.0 and 1.0")]
        private Common.SerializableNullable<float> _windSpeed;
        [SerializeField]
        [Tooltip("Will be clamped between -1.0 and 1.0")]
        private Common.SerializableNullable<float> _windTurbulence;
        [SerializeField]
        [Tooltip("Will be clamped between 0 and 1.0")]
        private Common.SerializableNullable<float> _windDirectionX;
        [SerializeField]
        [Tooltip("Will be clamped between 0 and 1.0")]
        private Common.SerializableNullable<float> _windDirectionY;


        [Header("Fog Settings - EXPERIMENTAL")]
        [SerializeField]
        [Tooltip("Will be clamped between 0.0 and 1.0")]
        private Common.SerializableNullable<float> _fogDensity1;

        [SerializeField]
        [Tooltip("Will be clamped between 0.0 and 1.0")]
        private Common.SerializableNullable<float> _fogDensity2;


        public Common.SerializableNullable<float> WindSpeed      => _windSpeed;
        public Common.SerializableNullable<float> WindTurbulence => _windTurbulence;
        public Common.SerializableNullable<float> WindDirectionX => _windDirectionX;
        public Common.SerializableNullable<float> WindDirectionY => _windDirectionY;
        public Common.SerializableNullable<float> FogDensity1    => _fogDensity1;
        public Common.SerializableNullable<float> FogDensity2    => _fogDensity2;
    }

    public enum WeatherPresetType
    {
        ClearSky = 0,
        Cloudy = 1,
        Foggy = 2,
        Rain = 3,
        Snow = 4,
        DarkCloudy = 5
    }
}
