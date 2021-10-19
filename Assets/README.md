# Change log
List of changes for each version of the Fireworks Mania Mod Tools.

# How to upgrade Mod Tools
Unity is not currently able to update .git packages with the Package Manager (a feature tht should come in the future) - you have to do it manually.
To update a .git package, you simply just add the package again.

So just add the .git url again to the Mod Tools.

---

## v2021.10.1
- Included missing materials for DummyCake
- Updated and optimized the DummyCake a bit
- Renamed some firework effect prefabs to have a prefix of FX_
- Added an ParticleLight prefab so we can use the same light in all our effects, making it easier if we need to make a common change at some point. See how it's used in the DummyCake
- Renamed some internal variable of EntityDefinitions, something that you shouldn't really see, but now you know

## v2021.9.6
- Added new "CakeBehavior" so you can now build cakes in your mods
- Added a DummyCake in the Mod Samples to see how it's put together, but as you will see, it works very much like RocketBehavior
- Updated the DummyRocket effect to be a bit more performant, discovered it wasn't using GPU materials places where it should
- Deleted the legacy FuseConnectionPointIndicatorPrefab as we don't need it anymore
- Removed the Fireworks Type "Featured" as modders shouldn't use that (plus this enture thing might change completely)
- Upgraded to UMod 2.8.5 - should fix the "Fuse reference bug"
- Renamed "FuseSmall" to "FuseStandardPrefab" in the Mod Samples
- Upgraded to use Unity v2021.1.22f1

## v2021.9.2
- Refactored FuseSmall and FuseMedium to use a new setup for FuseConnectionPoint logic

## v2021.9.1
- Changed CakeBehavior to only have one particle effect as we dont' need a list, as multiple particle systems can be just be put under one anyway
- Changed Thruster to have a single particle system too, for same reasons as above
- Removed RequiredComponent attribute on RocketBehavior, as it's not support very good by UMod, so replaced it with some OnValidate() logic that attempts to do the same
- Upgraded to UMod 2.8.4
- Upgraded to Unity 2021.1.20f1
- Enabled a setting to allow the game to try and load mods build with older versions. It might fail, but I assume it will also work in many cases and I think it's better to attempt to load a mod in the game, then proactively avoid loading mods build with older version each time the game/Unity is updated. Time will tell how this works out


