# Fireworks Mania - Mod Tools
This is the place to start when creating mods for [Fireworks Mania](https://store.steampowered.com/app/1079260/Fireworks_Mania__An_Explosive_Simulator/).

# Video Tutorials
I have created a playlist with various videos about creating mods for Fireworks Mania:

[Mod Support | Fireworks Mania](https://www.youtube.com/playlist?list=PLfRfKfCcFo_tvABAdgGDM5GGlaSa9Smck)

# Prerequisites
To be able to build mods for Fireworks Mania there are a few prerequisites.

- Basic knowledge about [Unity](https://unity.com)
  - If you want to get started I recommend Imphenzia's ["LEARN UNITY - The Most BASIC TUTORIAL I'll Ever Make"](https://www.youtube.com/watch?v=pwZpJzpE2lQ)
- Basic 3D modeling skills ([Blender](https://www.blender.org) is the tool I use, but there are others. As long as they can export to .fbx I think it's all fine)
  - If you want to get started I recommend Imphenzia's ["Learn Low Poly Modeling in Blender"](https://www.youtube.com/watch?v=1jHUY3qoBu8) if you are all new to Blender. 

Keep in mind that there are tons of good tutorials out there for basic Unity and Blender stuff, so go check out what YouTube have to offer, the two recommendation are just one place to start.

It is very important to note that **NO CODING SKILLS ARE REQUIRED** to be able to build a mod - it can all be done via the Unity editor.

So, none-coders, fear not.

Coders, fear not either, you can do custom scripting.

# Getting started
## 1. Getting Unity Hub & Git
[Download and install Git](https://git-scm.com/) - Unity need this to be installed to be able to install the Mod Tools package later one, so you might as well install it right away.

[Download and install Unity Hub](https://unity.com/download) - The Unity Hub acts a a "launcher" and it's via this that you will install the actual Unity Editor.

After the two above have been installed, restart your PC and come back here.

## 2. Getting Unity Editor
To avoid unintended behavior and issues you always need the specific version of Unity that the Mod Tools are build with.

When you have the Unity Hub installed, it's easy to install the specific version you want. Also know that you can have multiple versions installed at the same time.

You find the current target Unity version in the [Change log](CHANGELOG.md).

If you don't have the given version installed, just click on the version number in the Change log. This will take you to Unity's website where you can click the "Install" button and it will open up in your Unity hub and install that version. Really easy!

![image](https://github.com/user-attachments/assets/d9fd49fa-61f9-4db2-9b1d-0bdfaa396ea9)

## 3. Create empty Unity project
Fireworks Mania mods are build from a Unity Project. So create an empty 3D (Build-in Render Pipeline) Unity project.

![image](https://github.com/user-attachments/assets/e721a480-262f-481c-96ce-82771318fc5d)


### Naming your Unity Project
One important thing to know when naming your project is that you can have multiple mods within a single Unity project. 

This means that you might want to name the Unity project something more generic like, *"YourNickNameFireworksManiaMods"*, *"YourNickName.FireworksMania.Mods"* or simply *"FireworksMania.Mods"*.

These are just suggestions and you should name the Unity project something that make sense to you and how you structure things. The name of the Unity project have no impact on actual mod(s) name(s).

At this point you should now have an empty Unity project that should look something like this:
![image](https://github.com/user-attachments/assets/99d54efa-c99e-44e1-81aa-dda7f5566140)

To make sure we are off to a good start and all is as it should, it's a good idea to enter Play-mode. Click the Play button in the top and make sure there are no errors in the console. Click the Play button again to exit play mode.
![image](https://github.com/user-attachments/assets/361acdd8-051b-491a-95f1-628da74d0c5d)



## 4. Install Fireworks Mania Mod Tools
Now we need to install the Fireworks Mania Mod Tools.

Go to Windows->Package Manager, click the little + button and select that you want to add an git package.

![image](https://github.com/user-attachments/assets/f045a7d3-30d8-4a17-8503-9279c57fde62)

Paste in this url and hit Add: https://github.com/Laumania/FireworksMania.ModTools.git

![image](https://github.com/user-attachments/assets/1d1bc3a5-bbad-49b6-af28-0301516983d7)

The installation can take some time, so let its do its work.

Once its done you might get this popup. Just click Yes and it should automatically restart the Unity Editor.
![image](https://github.com/user-attachments/assets/11c35d7f-8c03-4e72-92fa-8efe8f7eddb6)

You now have the Fireworks Mania Mod Tools installed and are ready to create your first mod!

![image](https://github.com/user-attachments/assets/3cbbb379-a7f3-4c80-b201-16f255ced006)

Close the Package Manager window and hit Play again to make sure we don't have any errors. Click Play again to exit Play mode, which is important, as we dont want to be in Play mode.

![image](https://github.com/user-attachments/assets/93b7c6b8-a695-40e4-a2da-79e1eb76281b)

# Create Your First Mod
This is the point where things start to get interesting.

As mentioned previously, it is important to remember that you can build multiple mods from this single Unity project.

## Create Mod & Folders
Lets first create an "Mods" folder where we can have all our mods in.

![image](https://github.com/user-attachments/assets/87ad59fa-4d7e-49ea-b2c2-a0120f9acf0c)

Then let us go and create a new mod.

![image](https://github.com/user-attachments/assets/b0462e0e-eafd-4e52-988a-661b19a9cec3)

![image](https://github.com/user-attachments/assets/9f1db1cd-af7c-4815-80cd-463a9c76ea94)

Now give the mod a good descriptive and unique name. It's always an good idea to prefix it with your nickname like I have done here.

Avoid spaces and special characters in the name.

Make sure to put the mod in "Assets/Mods" as shown. 

It is not a requirement, but the more structure you have on your things, the easier it is to find and navigate as the project grows.

![image](https://github.com/user-attachments/assets/dd999d23-6dd3-461b-9311-ba665b28f5d0)

You can think of this folder, "Laumania_TutorialMod_01" in this sample, as the mod itself. 

Simply put, everything you want to include in your mod needs to be inside this folder.

Now that we are in the mod folder, lets create some more folders that will help us better organize our files as we go.

> You can organize your mod as you want, but if you are in doubt I recommend using this folder structure for a start.

Right click and create the following folders:
- Definitions
- Icons
- Models
- Prefabs

![image](https://github.com/user-attachments/assets/ae3bddcb-fb9d-4dd6-9909-76ceea8fb330)

Now head to the Export Settings to setup metadata on your mod, some build options etc.

![image](https://github.com/user-attachments/assets/2e57daac-4627-45d8-8098-dfb7809bbb96)

Fill out the following fields under "Mod Information" with what fits your mod, the rest can be ignored as they are not really used in the game.

- Mod Name
- Mod Version
- Mod Author

![image](https://github.com/user-attachments/assets/11af3453-9ae3-4274-9436-aafeb757a450)

The "Mod Export Directory" can be set to exactly what you want, however, for easier test of your mod it is recommended that you set this to your local "Mods" folder for Fireworks Mania. This way you build your mod directly into the games mods folder and do not have to copy files each time.

To find the path press WIN + R to get the Run prompt up.

Put in: %userprofile%\appdata\locallow\Laumania ApS\Fireworks mania\Mods

And press Enter.

![image](https://user-images.githubusercontent.com/1378458/139145921-f7382bfc-71b1-4e84-9a87-d22297c02e28.png)

A new window will open in the correct location, copy the path from that and put it into the "Mod Export Directory".

![image](https://user-images.githubusercontent.com/1378458/139146085-5420792e-8587-4858-a341-e41be75a2bb9.png)

![image](https://github.com/user-attachments/assets/094b6426-aaaf-4dd8-a73f-0131defb7093)

On the "Build" tab, set the "Optimize for" to "File Size" to avoid your mods to be very large in size.

![image](https://user-images.githubusercontent.com/1378458/139145086-9d16d5d8-083e-40cf-a87d-79dbe1675781.png)

After this you can close the "Export Settings" window and continue.

## Create an EntityDefinition
Now we are starting to hit Fireworks Mania specific stuff, EntityDefinition - what is that?

> An EntityDefinition is basically metadata that describes any object you can spawn in Fireworks Mania. It holds data such as the name, icon, prefab to spawn etc.

All items in the inventory in Fireworks Mania is basically an "EntityDefinition".

Let us create our first "EntityDefinition" of the type "FireworkEntityDefinition", which holds metadata about a specific type of entity, namely "fireworks". 

We of course create this in our "Definitions" folder.

![image](https://user-images.githubusercontent.com/1378458/139088210-1c2f4197-59da-47dc-ad83-4a678146d5b4.png)

As you will see in a moment, it is always a good idea to name your EntityDefinitions something unique, as the name of the file will also be used as the "EntityDefinitionId" that will be used to uniquely identify this specific firework that you are creating.

I therefore recommend naming it after this schema:

*YourNickname_EntityType_NameOfTheItem*

You will find all the possible firework types in this folder:
![image](https://user-images.githubusercontent.com/1378458/140571230-807126d2-4ffd-4587-9409-055f25c457a0.png)


In my sample here I call it: *Laumania_Rocket_TutorialRocket*

![image](https://user-images.githubusercontent.com/1378458/139089176-ac89b969-a843-4ea8-bb6a-9bcbfa045538.png)

### Entity Definition Id
You will see some "errors" in the console now - these are here to help you.

![image](https://user-images.githubusercontent.com/1378458/139089415-9f065536-acd9-404e-a781-db2d7f846ea8.png)

You can see the first one says something about you need to update the Id.

If you select the EntityDefinition you can see what fields it have in the Inspector.

![image](https://user-images.githubusercontent.com/1378458/139089742-c8ca719b-ed2e-4aa3-b238-0e3c52d6313c.png)

Looking closer we see that Id the error is talking about.

![image](https://user-images.githubusercontent.com/1378458/139089828-98d75d83-ef54-4bed-9f21-fff46ab47612.png)

For a newly created EntityDefinition, you can see this field it set to: INSERT UNIQUE DEFINITION ID

> You CAN put in your own id here, but I recommend using the context menu method I have added that give it an Id that match the filename.

Right click in the top of the Inspector.

![image](https://user-images.githubusercontent.com/1378458/139090061-2abfdf99-385c-4e07-87fc-d3991c90d140.png)

![image](https://user-images.githubusercontent.com/1378458/139090412-096b6eeb-5c23-4715-ae94-a22b6569e8a7.png)

> One important thing to note here is that this EntityDefinitionId is used, among other things, to save in blueprint files. Therefore, once your mod has been released the first time - **do not change this id** - as you will break users of your mods blueprints.

### Entity Definition Type
The next error says something about EntityDefinitionType that is missing. It is because an FireworkEntityDefinition needs to have a type.

![image](https://user-images.githubusercontent.com/1378458/139092007-5dda56e1-3ff9-40f5-b99b-3868787eed6f.png)

As we know we are building a rocket let us pick the Rocket type.

> EntityDefinitionType is the one that determine under what category/type the firework will show up in the Inventory in game.

Click this little round thingy to select a type.

![image](https://user-images.githubusercontent.com/1378458/139092367-62e29a9f-f74f-486d-ba92-3718f69e37f3.png)

If you window looks like this:
 
![image](https://user-images.githubusercontent.com/1378458/139092429-e2a5b3ae-db6c-4fe8-aa6a-4052f9c236af.png)

You need to click the little eye icon to toggle on assets from packages, as these types comes as part of the Fireworks Mania Mod Tools in a package.

![image](https://user-images.githubusercontent.com/1378458/139092700-00311929-681d-427b-a0c3-c346efbeee6a.png)

### Entity Definition Prefab
Now we have a definition with an unique Id and an Entity Definition Type. 

However, when you set the type, you get another error, saying something about missing a Prefab Game Object.

![image](https://user-images.githubusercontent.com/1378458/139095883-805b70e5-5b1d-453b-b8cc-13bd2c7de7f4.png)

This is the [prefab](https://docs.unity3d.com/Manual/Prefabs.html) that will be spawned in game when a player spawns your firework. Therefore, this prefab is your actual firework with logic to act as a rocket in this case. It have the 3d model, rocket behavior, effect, fuse, sound etc. 

Creating and modifying this prefab is where the majority of your time will be spend as a typical firework mod creator.

For now however, as this is a getting started guide, we will keep it simple so you get a basic idea of how a mod it put together, without going into the details of creating particle effect, setup the various fireworks behavior etc. We will get to that later.

So to help you get started, I have included some "ModSamples" in the Fireworks Mania Mod Tools.

![image](https://user-images.githubusercontent.com/1378458/139133149-cb18c33b-ebcb-4b6b-b851-891144278131.png)

Find the prefab "Rocket_DummyRocket_Prefab".

As all these are inside a package you cannot copy or manipulate them here, which is as it should be. However, you can drag this prefab into your scene and from it create your own prefab.

First drag the prefab into the scene hierarchy.

![image](https://user-images.githubusercontent.com/1378458/139134286-520ea2c2-d501-4125-95a3-d8eec58fd288.png)

Then right click it, Prefab -> Unpack to unpack it from being an prefab fra the ModSamples.

![image](https://user-images.githubusercontent.com/1378458/139134419-0c5a2bc1-eeb1-4e81-8f70-a18053211956.png)

Now you can rename that gameobject in the hierarchy to fit your fireworks name.

![image](https://user-images.githubusercontent.com/1378458/139134599-3c7b2f82-7c80-41e1-9e1d-a3df9c8459cf.png)

Because it is no longer a prefab in the hierarchy, we can make it our own prefab by dragging it to the Prefabs folder in our mod's folder.

![image](https://user-images.githubusercontent.com/1378458/139134750-664001a0-9994-4f86-8a3e-1075ba8188db.png)

As it is now a prefab again, it is again showing up as blue in the hierarchy and you can see it in the Prefabs folder in your mod.

![image](https://user-images.githubusercontent.com/1378458/139135040-54edae2e-88ec-4715-b4cc-74ce128b22a5.png)

For now we can delete the game object (prefab instance) in the hierarchy to avoid by mistake make changes to that, instead of the actual prefab.

![image](https://user-images.githubusercontent.com/1378458/139139295-1c519819-27ed-4df8-b661-b57d84636f8c.png)

Instead, double click the prefab in the project window, to open up the prefab.

![image](https://user-images.githubusercontent.com/1378458/139139537-98b56d31-6a6a-44a4-841d-4f14aa87b0a9.png)

You now have the prefab open in edit mode and by selecting the top gameobject (root node) you can setup the last part of the prefab to fit to your FireworkEntityDefinition you created earlier.

![image](https://user-images.githubusercontent.com/1378458/139139782-7c0d18ff-59b5-4ab8-b251-fa7363c369e8.png)

Only thing we need to do here is to tell this prefabs RocketBehavior, which Entity Definition it is related to.

As you might have guessed, as we want this prefab to be related to our FireworkEntityDefinition from before.

So, again click the little round icon and select your definition.

![image](https://user-images.githubusercontent.com/1378458/139140050-af61b1e2-c505-4340-8a1c-fde06606f5f1.png)

![image](https://user-images.githubusercontent.com/1378458/139140092-d4dd3cf1-8546-4142-a31c-0448d369a0c4.png)

Now we need to go back to where we came from, the FireworkEntityDefinition from before, where we needed to provide it with an prefab. We have now created that prefab and can therefore assign it to the FireworkEntityDefinition.

Drag the prefab to the field in the Inspector.

![image](https://user-images.githubusercontent.com/1378458/139140686-ab690713-05f9-4b6e-9796-340b7abb4b51.png)


### Entity Definition Name
Our FireworkEntityDefinition also needs a name, which is the one showing up in the Inventory.

So let us give it a name.

![image](https://user-images.githubusercontent.com/1378458/139143631-9e0996cb-e476-4520-bcd4-05e65604a9d0.png)


### Entity Definition Icon
We also need to provide an icon which is used in the Inventory. In your own mod you would create your own icon and I will do a guide on how to do that later.

For now let us just use one of the icons from the ModSamples.

![image](https://user-images.githubusercontent.com/1378458/139143908-dfe541f8-8c4c-4b76-aa7a-7996255d977b.png)

## Build Mod
With all the above setup building the mod is the easy part.

![image](https://user-images.githubusercontent.com/1378458/139146350-44fdf6ff-5197-4b90-9e96-a12cd6922801.png)

Unity will spend some time building and you should see this in the console when it is done.

![image](https://user-images.githubusercontent.com/1378458/139146423-6e893d7d-9dc8-469a-b93d-5ba4d88c776c.png)

At the same time the export directory will open and you should see your mod in the Mods folder of Fireworks Mania.

![image](https://user-images.githubusercontent.com/1378458/139146509-e1e52e96-f5af-4096-882a-9c4f7043cac0.png)

Your mod is successfully build - let us try it out!


## Testing Your Mod In Game
With your mod, or mods, in the "Mods" folder of the game, the mods will attempt to load on the load of a map in the game.

So let us start up the game and to go the Flat map. All map should work for this. When the map is loaded open the Inventory and head over to the "Workshop" part.

Here you should see your first mod rocket.

![image](https://user-images.githubusercontent.com/1378458/139147106-8e53f90a-68f4-42fc-aece-eff7edf7be2a.png)

Now select it and spawn it and see it working.

![image](https://user-images.githubusercontent.com/1378458/139147195-b91c4243-e2d8-4b0f-9e08-c2c1a96a7606.png)

![image](https://user-images.githubusercontent.com/1378458/139147243-39be5c71-1067-466d-ae3f-37b713da9343.png)

Congrats - you have created your first Fireworks Mania Mod!

# Project in Github / Backup
This is not a requirement, but having your project in some sort of version control is essential.

If you are new to Github, Blender etc. and all seems overwhelming, I know it seems like another big thing you have to learn, but learning to use version control can save you so much time trying to figure out what broke your mod/game. Further more you are likely to try out a bit more risky things, because you know you can always just revert and get back to a version that worked.

There are many good videos out there, but here is one to get started, that can help you get started with getting your Unity project in Github.

[Using GitHub with Unity effectively! Improve your workflow!](https://www.youtube.com/watch?v=WH7qDUYHGK8)

Alternatively, if Github and source control is too complicated, there is also an asset like the one below that simply backup your Unity project for you locally.

https://assetstore.unity.com/packages/tools/utilities/zip-backup-71979


# Publish Your Mod
Once your mod is ready for the world, it is time to get it into the workshop in the game.

You do that by uploading your mod to mod.io here: https://fireworksmania.mod.io

We all look forward to try out your mod :D

# Troubleshooting
If you get a lot of bugs like missing FuseIndicator, Reimport All or restart Unity seems to fix the issue. Thanks guanaco0403.



















