# Changelog
All notable changes to Fireworks Mania Mod Tools will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

[How to upgrade Mod Tools package](#how-to-upgrade-mod-tools-package)

## [v2024.3.2] - 2024-3-23 (CLOSED BETA)
Target Unity version: [v2022.3.19f1](https://unity3d.com/unity/whats-new/2022.3.19)

>**IMPORTANT**: I haven't spend time not breaking previous version of shells and mortars. So the ones made with previous version won't work. Delete them and start over if the fastest way to continue.

### Added
- Added "rightclick Template" for easier creation of your own UnwrappedShellFuse Prefab

### Changed
- Simplified how Shells and Mortars are structured so it's hopefully a bit easier to understand


## [v2024.3.1] - 2024-3-23 (CLOSED BETA)
Target Unity version: [v2022.3.19f1](https://unity3d.com/unity/whats-new/2022.3.19)

>**IMPORTANT**: As this is a beta and there is no upgrade guide yet, please use this version in a new Unity Project. Do not upgrade to this version in your existing mod project - for now.

In general there are a lot of changes in this version of the Mod Tools so I'm not listing all here, only the important parts.

### Added
- First draft of native Shell and Mortar support. I'll share a private video for you closed beta guys of how Shell and Mortars work in this version
- Added weather "Dark Cloudy" so it can now be set on MapDefinitions
- Added 3 Footstep Physics material, that can be used on ground colliders to change the footstep sound of players

### Changed
- I changed the namespace of the CharacterDefinition, which means character mods build with previous version won't work with v2024-3-1 of the game. Luckily it's easy you fix, just rebuild your character mod with this version of the Mod Tools and it will work again.



## [v2023.1.6] - 2023-1-25
Target Unity version: [v2021.3.16f1](https://unity3d.com/unity/whats-new/2021.3.16)

### Changed
- Upgraded to new Unity version (v2021.3.16f1), so remember to upgrade!
- A big change is how map mods are now loaded in the game. Lately I have changed the game to list custom maps differently to make it a lot faster. The reason it is faster now is that I no longer load the map mods for listing them, I am now using the mod.io meta data, like name and thumbnail. The 'downside' to that is that it means I don't have access to the MapDefinition metadata either, like thumbnail. Therefore, when loading local map mods (meaning mod from the /Mods/ folder directly, I just use a blue fallback thumbnail and the name of the mod I can get quick access to via UMod. In this version of the Mod Tools you can still set the thumbnails, but I will remove that in the future

### Fixed
- Fixed error where Unity events was not always working in the build mod, ex. events on the DayNightTriggerBehavior


## [v2022.12.3] - 2022-12-12
Target Unity version: [v2021.3.9f1](https://unity3d.com/unity/whats-new/2021.3.9)

> ### Upgrade steps for Mod Tools v2022.9.6 or earlier
> Backup your Unity project before you continue!
> 
> This update of the Mod Tools have the two dependencies DOTween and UniTask included. This makes it a lot easier to get started making mod, as you now only need to install the Fireworks Mania Mod Tools. However, for you who are upgrading you need to delete UniTask and DOTween from your Unity project for it to build again.
>
>
> 1. Upgrade to Unity [v2021.3.9f1](https://unity3d.com/unity/whats-new/2021.3.9)
> 
> 2. Update Fireworks Mania Mod Tools to latest version
> 
>    - Window > Package Manager > (Packages: "In Project" > Fireworks Mania Mod Tools > Press "Update" button  
>    - *You might get some errors and warning, but just continue, they should all go await after completing all the steps*
>   
> 3. Uninstall UniTask
>
>    - Window > Package Manager > (Packages: In Project > UniTask > Press "Remove" button
>   
> 4. Uninstall DOTween
>
>    - In Unity, right click and delete folder "/Assets/Plugins/Demigiant"
>    - In Unity, right click and delete file "/Assets/Resources/DOTweenSettings"
>
> 5. Close Unity and reopen
>    - You should now be able to build your mod(s) as normally.

### Added
- Added custom map support. Create an scene with a unique name in your mod, add the "PlayerSpawnLocationPrefab" from the Mod Tools to your scene, create an MapDefinition and fill out all the fields. Now build your mod and your map should show up in the map selection UI
- Added SunIntensityCurve and MoonIntensityCurve to MapDefinition 
- New setting on MapDefinitions for "ObjectCatcherDepth", see tooltip to figure out what it does
- Added back "Lighting Settings" and "Sky Settings" to the MapDefinition. I will make some videos at a later point to show what they do in game, as you cannot see the effect until your map is loaded in game
- Added Unity Events to Fuse and ExplosionPhysicsForceEffect components. This enables you to do various stuff when these events trigger, like showing/hiding an gameobject, call a method on a custom script etc.
- DayNightCycleTriggerBehavior - Behavior that fires Unity Events upon day and night changes so you can ex. turn on and off lights
  - *This is the behavior used on the streetlights in Town and City to turn them on/off* 
- Added some validations to when building the mod, so you get a warning if your scene includes a camera or a Directional light. It won't stop the build, just warn you in the console

### Changed
- Updated some of the "Effect" prefabs in an attempt to fix the "black" smoke that can happen from time to time
- Upgraded UMod to latest version (v2.9.2)
- Added a lot of "Environment" settings to MapDefinition so you have control over a lot more things
  - *There is currently a challenge with some of the weather settings, so currently in v2022.11.1 of the game, these have no effect*

### Fixed
- Fixed bug where DayNightCycleTriggerBehavior didn't trigger events at map load


## [Released]  [v2022.9.6] - 2022-09-30
**IMPORTANT**: Be sure to update Unity to version specifically [v2021.3.9f1](https://unity3d.com/unity/whats-new/2021.3.9) (click the "Install this version with Unity Hub." in the top) **BEFORE** upgrading the Mod Tools!

[Alternatively see how to install a specific version of Unity in general](https://www.youtube.com/watch?v=RU6fppWN74M)

### Added
- Added new SearchWidget UI for browsing GameSounds (No more endless scrolling when browsing for game sounds!)
- Added new StartUpPrefabDefinition. Add a prefab to this and the prefab will be instansiated upon mod load. This it meant to be used for Start/OnDestroy events and not visual objects etc.

### Changed
- Upgraded Unity version to v2021.3.9f1 - this means you have to upgrade the Unity version you build mods in to the same. (Remember to backup!)
- Updated "How to upgrade Mod Tools" section, as the new version of Unity supports updating git packages via the Package Manager UI.
- Updated the Changelog structure to be based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)

## [Released] [v2022.6.2]
- Added new property "Enabled" to IIgnitable, so if you are using this in custom code you need to recompile and implement it
- Added new settings in FireworksMania.Core.CoreSettings, right now it's possible to also set these in your custom scripts, please don't do that. I will change that later on I think, so its readonly. Anyway, if you have a custom firework, you might need to look into taking the "AutoDespawnFireworks" into consideration
- Disabled shadows on FuseStandardPrefab, as you don't really see it, so we might get that little extra performance for not having to render shadows for fuses


## [Released] [v2022.5.1]
- Added "IgnorePickupBehavior"
- Refactored the Messenger system. The old Messender is marked as depricated and the new one if found in the FireworksMania.Core.Messaging namespace. Go see the video on my channel about how the Messenger works: https://youtu.be/UhIaPy3pw14
- Added new messages (MessengerEventBlueprintStartLoading + MessengerEventBlueprintCompletedLoading) you can add a listener to to react upon Blueprint start loading and completed loaded
- Added new message (MessengerEventChangeUIMode) you can broadcast to change the "UI Mode"


## [Released] [v2022.4.2]
- Added optional settings on 'UseableBehavior" to turn on/off outline and interaction UI ('Use 'E')
- Upgraded from the quick patch version of UMod, to the real new v2.9.0 version of UMod
- Refactored the BaseFireworksBehavior to implement some more interfaces, that were previously implemented in each firework behavior, like RocketBehavior etc. I guess very few, if any, have made their own firework behavior, deriving from BaseFireworkBehavior, but if you have, please rebuild your mod, as you must likely can remove some of the methods, as they are now for of the BaseFireworkBehavior

## [Released] [v2022.4.1]
- Added "Hang time" settings to the RocketBehavior to adjust the time between fuse burns out to the rocket explode. In some cases, you don't want the random "hang time" that is per default applied. However, unless you have a specific reason to disable this randomness, you should leave it, as it's there to make things seem a bit more "real" and for performance reasons, so not all rockets goes off in the exact same frame
- Renamed all "MessengerEvents" in Core (Mod Tools) to be named similar to the ones in the native game. This will be a breaking change if a mod use these directly, however, as I haven't done much documentation for how to use this works, I assume not many are using these
- Added new "UseableBehavior". Putting this on an game object in your mod will make it show up as an "Useable" object for the player and the player can "click" it with the E button.
This behavior have the events OnBeginUse (triggered when E is pressed) and OnEndUse (triggered when E is released). You can in Unity then hook up these events to call what ever method on your game object to execute logic. Examples of this could be a button, open/close door etc
- Added new "PlaySoundBehavior" which can be used to play a sound from an object on Start, or used together with the "UseableBehavior" to trigger an sound when something is clicked
- Removed "Build & Run" option from the Mod Tools menu, as it wasn't working anyway
- This is not something new to the Mod Tools as such, but logic has been added to the game, that enables the player to pick up game objects that does not have the UseableBehavior on it and weighs under 20kg (Weight is set as the Mass in the Rigidbody component on the game object). Just something to be aware of, if you are creating something that logically shouldn't be possible to pick up, it needs to weigh more than 20 kg (Mass > 20)

## [Released] [v2021.12.1]
- Renamed "SmokeBomb" type to "Smoke" so we can put more than just smokebombs under it
- Added new type Fountains - not that we have that many yet, but it's a category by itself anyway

## [Released] [v2021.11.6]
- Nothing major, just upgraded the UMod version, shouldn't have any big impact

## [Released] [v2021.11.5]
- Added validations on Fuse so it will log an error in Unity if you are missing particle effect etc. on the Fuse
- Added "Random Pitch" to GameSoundDefinition so the game can add a random pitch to your custom sound to "fake" variations. Per default it is set to -0.1 and 0.1 which is what I also use default in the build in sounds and that seems to add a good variation, so for most of you, just leave it at that
- Added "Fade In/out" to GameSoundDefinition so you custom sound can have a fade in/out. In most cases this is not needed, for like explosions etc. however, for sounds that kind of "start" and "end" more fluent, it might come in handy, ex. could be some of the "Fire Machine" sound where they right now stops kind of "too fast", a little fade would be good here I think - play around with it


## [Released] [v2021.11.4]
- Changed the validation on the "IgnoreRigidbodies" to be a warning instead of an error, as the mod can actually work with this issue, it is just recommended to fix it


## [Released] [v2021.11.3]
- Added more validation to the ParticleSystemExplosion and ParticleSystemSound to help inform that you are missing an ParticleSystemObserver component, as they need that to be able to work
- Added validation to inform about if you have an empty/null item in yoru list of "ignore rigidbodies" on ExplosionPhysicsForceEffect, so you can either assign the fireworks own rigidbody to it, which you in most cases want, or remove the item for the list. Both are valid ways to fix this problem
- Renamed "Layers" to "Affected Layers" on ExplosionPhysicsForceEffect to be a bit descriptive. The tool tip is still all wrong, but it is an Unity bug which should be [fixed in a upcoming version](https://issuetracker.unity3d.com/issues/layermask-ignores-custom-tooltips-and-labels-in-the-inspector-when-using-the-tooltip-attribute-or-guicontent) 
- Added posibility to include "custom sounds" in your mod via the "Game Sound Definition". This means you can now include your own sounds in your mod and use them in all the scripts where you pick a sound from the drop down. First you need to drag in an audio file with the sound (typically .wav file) into your mod folder. Then you need to create an: Fireworks Mania -> Definitions -> Game Sound Definition. Then you drag in your audio clip from before into the game sound definition and set the various properties. Then you can select your sound from the drop down where you pick sounds


## [Released] [v2021.11.2]
- Fixed missing collider on the Dummy Fountain in the included ModSamples

## [Released] [v2021.10.3]
- Reorganized how the github repo for the package to remove all that it is not specifically a part of the pack. This also means the .git url have changed - the new .git path can be found in the new [README](https://github.com/Laumania/FireworksMania.ModTools#5-install-fireworks-mania-mod-tools)
- Updated Readme to have much more info on how to get started creating mods. It takes you through all the basic setup stuff and to build your first mod
- Changed logic in SaveableEntity so it now always tries to set the EntityDefinition by itself, if it finds an component that implements IHaveBaseEntityDefinition. This basically means that you do not have to set this anymore when creating a firework. Only for props this this needs to be set

## [Released] [v2021.10.2]
- Added possibility to create Firecracker, Fountains, PreloadedTube, RomanCandle, SmokeBomb, Whistler and Zipper
- Added "Dummy" prefabs as samples for all the above in the ModSamples folder. These are meant to be used as a starting point. Drag the Dummy prefab to the hierchy and Prefab-Unpack and create your own from there (I will do a video of this soon :))
- Upgraded UMod to 2.8.6 (There is still the bug that the old mods prior to v2021.10.1 have to rebuild with the latest version. However, after a long dialog with the UMod creator, it actually looks like this might be a Unity bug. It have been filed as a bug to Unity
- Removed the "locked" part of the EntityDefinitions, as the locked logic in the game is now done in another way
- Added Developer references to the Mod Tools to point to this repository
- Removed Muzzle and Trail materials from the Mod Tools as they were never meant to be there

## [Released] [v2021.10.1]
- Included missing materials for DummyCake
- Updated and optimized the DummyCake a bit
- Renamed some firework effect prefabs to have a prefix of FX_
- Added an ParticleLight prefab so we can use the same light in all our effects, making it easier if we need to make a common change at some point. See how it's used in the DummyCake
- Renamed some internal variable of EntityDefinitions, something that you shouldn't really see, but now you know

## [Released] [v2021.9.6]
- Added new "CakeBehavior" so you can now build cakes in your mods
- Added a DummyCake in the Mod Samples to see how it's put together, but as you will see, it works very much like RocketBehavior
- Updated the DummyRocket effect to be a bit more performant, discovered it wasn't using GPU materials places where it should
- Deleted the legacy FuseConnectionPointIndicatorPrefab as we don't need it anymore
- Removed the Fireworks Type "Featured" as modders shouldn't use that (plus this enture thing might change completely)
- Upgraded to UMod 2.8.5 - should fix the "Fuse reference bug"
- Renamed "FuseSmall" to "FuseStandardPrefab" in the Mod Samples
- Upgraded to use Unity v2021.1.22f1

## [Released] [v2021.9.2]
- Refactored FuseSmall and FuseMedium to use a new setup for FuseConnectionPoint logic

## [Released] [v2021.9.1]
- Changed CakeBehavior to only have one particle effect as we dont' need a list, as multiple particle systems can be just be put under one anyway
- Changed Thruster to have a single particle system too, for same reasons as above
- Removed RequiredComponent attribute on RocketBehavior, as it's not support very good by UMod, so replaced it with some OnValidate() logic that attempts to do the same
- Upgraded to UMod 2.8.4
- Upgraded to Unity 2021.1.20f1
- Enabled a setting to allow the game to try and load mods build with older versions. It might fail, but I assume it will also work in many cases and I think it's better to attempt to load a mod in the game, then proactively avoid loading mods build with older version each time the game/Unity is updated. Time will tell how this works out

## How to upgrade Mod Tools package

Make sure you have a [backup of your project before upgrading!](https://github.com/Laumania/FireworksMania.ModTools/tree/v2021.11.5#project-in-github--backup)

Unity > Window > Package Manager > (Packages: "In Project" > Fireworks Mania Mod Tools > Press "Update" button  
