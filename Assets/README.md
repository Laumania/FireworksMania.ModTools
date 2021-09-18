# Change log
List of changes for each version of the Fireworks Mania Mod Tools.

## 2021.9.2
- Refactored FuseSmall and FuseMedium to use a new setup for FuseConnectionPoint logic

## 2021.9.1
- Changed CakeBehavior to only have one particle effect as we dont' need a list, as multiple particle systems can be just be put under one anyway
- Changed Thruster to have a single particle system too, for same reasons as above
- Removed RequiredComponent attribute on RocketBehavior, as it's not support very good by UMod, so replaced it with some OnValidate() logic that attempts to do the same
- Upgraded to UMod 2.8.4
- Upgraded to Unity 2021.1.20f1
- Enabled a setting to allow the game to try and load mods build with older versions. It might fail, but I assume it will also work in many cases and I think it's better to attempt to load a mod in the game, then proactively avoid loading mods build with older version each time the game/Unity is updated. Time will tell how this works out


