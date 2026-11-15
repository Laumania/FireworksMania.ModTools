using System.Runtime.CompilerServices;

//GameSoundDefinitionValidator is internal - this folder ships to modders as source with the Mod Tools, and whatever
//they can see they can come to depend on - but its rules are exactly what the EditMode tests pin down (#1073).
//Harmless in the Mod Tools, where no assembly by that name exists.
[assembly: InternalsVisibleTo("FireworksMania.Tests.EditMode")]

//EntityClassificationValidator is shared with the game's own report of fireworks with no classification (#1497).
//Just as harmless: the Mod Tools have no FireworksMania.Editor assembly either.
[assembly: InternalsVisibleTo("FireworksMania.Editor")]
