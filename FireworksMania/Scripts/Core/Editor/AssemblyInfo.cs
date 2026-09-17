using System.Runtime.CompilerServices;

//GameSoundDefinitionValidator is internal - this folder ships to modders as source with the Mod Tools, and whatever
//they can see they can come to depend on - but its rules are exactly what the EditMode tests pin down (#1073).
//Harmless in the Mod Tools, where no assembly by that name exists.
[assembly: InternalsVisibleTo("FireworksMania.Tests.EditMode")]
