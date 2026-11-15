using System.Runtime.CompilerServices;

//The rocket launch hold on Thruster and RocketBehavior is internal - this folder ships to modders as source with the Mod
//Tools, and whatever they can see they can come to depend on - but its rule is exactly what the EditMode tests pin down
//(#3287). Harmless in the Mod Tools, where no assembly by that name exists.
[assembly: InternalsVisibleTo("FireworksMania.Tests.EditMode")]
