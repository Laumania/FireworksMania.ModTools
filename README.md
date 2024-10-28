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

Coders, fear not either, you can do custom scripting 🤓

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

Paste in this url and hit Add: 
> https://github.com/Laumania/FireworksMania.ModTools.git

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

The "Mod Export Directory" is there you mod is eventually going to be placed everytime you build it. To make it easier and quicker to test, it make sense to set this directory to the local "Mods" folder of the game. 
This way, everytime you build the mod, all you have to do is restart the map in the game. The game will detect the mod have changed and reload it and you can test your changes.

As this "Mods" folder is placed in a location under your user on your PC, the path to it is unique to your machine and user.

Click the 3 dots to set the "Mod Export Directory".

![image](https://github.com/user-attachments/assets/fa568118-4f97-4d78-823c-c9bbed23f2cf)

This will open the normal file explorer. Copy/Paste the below into the adress bar in the top.

> %userprofile%\appdata\locallow\Laumania ApS\Fireworks mania\Mods

![image](https://github.com/user-attachments/assets/674e7b4e-7829-4853-921e-bb7c83c7c161)

Hit Enter and you will get to the folder we want.
![image](https://github.com/user-attachments/assets/a43c94d3-cc78-4629-967f-b3c8c5fdf3ae)

Click the "Select folder" button and the correct path will be set as the "Mod Export Directory".

![image](https://github.com/user-attachments/assets/749ea929-6322-46e4-85dc-cd547b8d24a1)

On the "Build" tab, set the "Optimize for" to "File Size". 
> This is very important! If you do not do this your mod will take up more space on players harddrive, take longer to download and take longer to load into the game. So do this for your own and all other players sake :)

![image](https://user-images.githubusercontent.com/1378458/139145086-9d16d5d8-083e-40cf-a87d-79dbe1675781.png)

After this you can close the "Export Settings" window and continue.

## Create an EntityDefinition
Now we are starting to hit Fireworks Mania specific stuff, EntityDefinition - what is that?

> An EntityDefinition is basically metadata that describes any object you can spawn in Fireworks Mania. It holds data such as the name, icon, prefab to spawn etc.

All items in the inventory in Fireworks Mania is basically an "EntityDefinition".

Let us create our first "EntityDefinition" of the type "FireworkEntityDefinition", which holds metadata about a specific type of entity, namely "fireworks". 

We of course create this in our "Definitions" folder.

![image](https://github.com/user-attachments/assets/bf435ce6-b12c-486c-9c1c-a7f1141894a9)

As you will see in a moment, it is always a good idea to name your EntityDefinitions something unique, as the name of the file will also be used as the "EntityDefinitionId" that will be used to uniquely identify this specific firework that you are creating.

I therefore recommend naming it after this schema:

*YourNickname_EntityType_NameOfTheItem*

In my sample here I call it: *Laumania_Cake_TutorialCake*

![image](https://github.com/user-attachments/assets/d8e6f05d-5130-4d40-81ed-ac26a6732b3d)

### Entity Definition Id
You will see some "errors" in the console now - these are here to help you.

![image](https://github.com/user-attachments/assets/11542db4-7e60-485b-97a2-4bccce70ced1)

You can see the first one says something about you need to update the Id.

If you select the EntityDefinition you can see what fields it have in the Inspector.

![image](https://github.com/user-attachments/assets/46f556b0-8706-4a19-98e8-b0b7b42a92eb)

Looking closer we see that Id the error is talking about.

![image](https://github.com/user-attachments/assets/bb49bb20-276e-43ec-b559-5f13c8281ba8)

For a newly created EntityDefinition, you can see this field it set to: INSERT UNIQUE DEFINITION ID

> You CAN put in your own id here, but I recommend using the context menu method I have added that give it an Id that match the filename.

Right click in the top of the Inspector.

![image](https://github.com/user-attachments/assets/05222f38-d755-42d8-a765-c912aaf63ddd)

![image](https://github.com/user-attachments/assets/1144e3d5-d2e4-4d4b-8db1-471c42b12bfe)

> One important thing to note here is that this EntityDefinitionId is used, among other things, to save in blueprint files. Therefore, once your mod has been released the first time - **do not change this id** - as you will break users of your mods blueprints.

### Entity Definition Type
The next error says something about EntityDefinitionType that is missing. It is because an FireworkEntityDefinition needs to have a type.

![image](https://github.com/user-attachments/assets/d4f3e599-8fd5-4c51-b22f-ef43f902d8f5)

As we know we are building a cake let us pick the Cake type.

> EntityDefinitionType is the one that determine under what category/type the firework will show up in the Inventory in game.

Click this little round thingy to select a type.

![image](https://github.com/user-attachments/assets/ce46d46a-ef99-4f5f-8bb4-3c087101f0ae)

If you window looks like this:
 
![image](https://github.com/user-attachments/assets/38e74e58-b34e-49fa-96db-521c3cca04c8)

You need to click the little eye icon to toggle on assets from packages, as these types comes as part of the Fireworks Mania Mod Tools in a package.

![image](https://github.com/user-attachments/assets/d5a8f882-b20a-4264-8ace-5c923ae5a098)

![image](https://github.com/user-attachments/assets/608671bb-59b5-4feb-9088-87ea494f07dc)

### Entity Definition Prefab
Now we have a definition with an unique Id and an Entity Definition Type. 

However, when you set the type, you get another error, saying something about missing a Prefab Game Object.

![image](https://github.com/user-attachments/assets/cdcec0bf-fad1-4a30-9afa-049f139b5b7d)

This is the [prefab](https://docs.unity3d.com/Manual/Prefabs.html) that will be spawned in game when a player spawns your firework. Therefore, this prefab is your actual firework with logic to act as a cake in this case. It have the 3d model, cake behavior, effect, fuse, sound etc. 

Creating and modifying this prefab is where the majority of your time will be spend as a typical firework mod creator.

For now however, as this is a getting started guide, we will keep it simple so you get a basic idea of how a mod it put together, without going into the details of creating particle effect, setup the various fireworks behavior etc. We will get to that later.

Luckily, its very easy to get started, as the Mod Tools comes with some templates you can use to start from.

So right click in the Hierarchy and create an Cake_Template.
![image](https://github.com/user-attachments/assets/52ae02a8-4b9f-4db4-afe1-91e42fe84ad2)

To the left you see the Cake_Template instance you just created (we will make that into an prefab in a bit as thats what we actually need) and to the right you see all the components on that game object because it's select. Don't look to much at the right for now, we get to that later.
![image](https://github.com/user-attachments/assets/40743b72-7c15-4b5b-bf3a-ac4a09798772)

Now lets rename the instance in the Hierarchy to what we want our prefab to be named.

Once its renamed, drag and drop it to the "Prefabs" folder in your mods folder.

![image](https://github.com/user-attachments/assets/9e57f7fc-a3c2-4eab-acc6-3fd136e4b351)

You will now see the instance in the Hierarchy turned blue, because it's now an instance of an actual prefab and you now have the prefab in your "Prefabs" folder too.

![image](https://github.com/user-attachments/assets/62da0c6f-a436-4036-878d-272bd0e416bd)

At this point you delete the prefab instance in the hierarchy, as you want to make changes directly to the original prefab and not to the prefab instance.

![image](https://github.com/user-attachments/assets/4eac1675-ea33-48d9-89e1-c74dc6b50b09)

Now we want to edit the prefab, you do that by double clicking in.

![image](https://github.com/user-attachments/assets/5bbd63c5-de00-4c5d-88bf-f9d11203edb7)

You now have the prefab open in edit mode and by selecting the top gameobject (root node) you can setup the last part of the prefab. 

What you need to do here is set the "Entity Definition" to be the "Laumania_Cake_TutorialCake" we created earlier. This tells the prefab which entity definition it belongs too, you don't have to understand why now, just know that its needed for the game to work properly.

![image](https://github.com/user-attachments/assets/419f2cab-1554-4728-a6f5-30770c8b3513)

Press CTRL + S to save the changes on the prefab. It is recommended to have "Auto save" turned off, as it can in some cases slow down Unity a lot.

![image](https://github.com/user-attachments/assets/53d753ec-17fd-49b4-a46c-35c6909c2119)

Now the prefab knows which FireworksEntityDefinition it belongs to, now we need to tell the "Laumania_Cake_TutorialCake" EntityDefinition which prefab it should spawn when selected in the Inventory in game. So the relationship goes both ways.

Select the "Laumania_Cake_TutorialCake" (the EntityDefinition) to make it show its details in the Inspector and then drag the "Laumania_Cake_TutorialCakePrefab" to the "Prefab Game Object" field.

![image](https://github.com/user-attachments/assets/bb80d5cd-a942-4c3c-8ceb-ab9664e3db79)
(Alternative you can also click the little circle next to the field end select the prefab there - the result is the same)

### Entity Definition Name
Our FireworkEntityDefinition also needs a name, which is the one showing up in the Inventory.

So let us give it a name.

![image](https://github.com/user-attachments/assets/f98b83e3-16ec-4bba-95f2-e1d743d85e72)

### Entity Definition Icon
For the firework to look good in the Inventory we also need to provide a preview icon. 

All you need to do is to right click the prefab in the Project window.
![image](https://github.com/user-attachments/assets/1025f08d-f334-47d6-832c-48a35c2f9f1e)

As you can see there are different options and you can test out which one you like the best. Everytime you click, a new icon is generated next to the prefab, if one is exiting there it will overwrite it. So very easy to test things out.

![image](https://github.com/user-attachments/assets/b91b45ce-7501-40fd-a9cd-75452e20552f)

This looks good so lets go with that one. The icon starts out expanded, but you can click the little arrow to collapse it, so it's not too confusing.
![image](https://github.com/user-attachments/assets/f090664f-7676-46cb-b8c3-bdd98e87e139)

You can decide to leave the icon next to the prefab, but we also made an folder called "Icons" earlier you can put it in. Drag and drop the icon file to move it. 

Its up to you how you want to organize your project. My advise is to be consistant. Do the same thing the same way, everytime, to avoid confusing your future self.

Now you just select the "Laumania_Cake_TutorialCake" (the EntityDefinition) and drag in the generated icon to the "Icon" field.

![image](https://github.com/user-attachments/assets/89c9a153-5b85-4d10-8906-c3eb3d644ab3)




## Build Mod
With all the above setup building the mod is the easy part.

![image](https://github.com/user-attachments/assets/56ed74d4-734f-459c-9c52-eb7cf2382a60)

Unity will spend some time building and you should see this in the console when it is done.

![image](https://github.com/user-attachments/assets/29c20f6a-7ffa-4176-9089-3e42fbd02ed5)

At the same time the export directory will open and you should see your mod in the Mods folder of Fireworks Mania.

![image](https://github.com/user-attachments/assets/0d801476-63ff-4b25-bc59-dffd11db4633)

Your mod is successfully build - let us try it out!


## Testing Your Mod In Game
With your mod, or mods, in the "Mods" folder of the game, the mods will attempt to load on the load of a map in the game.

In most cases you want to test out your mod in a Singleplayer game, to avoid others joining your host, as they will not be able to see your creation from the mod as they do not have it installed. For others to get access to your game it need to be put on mod.io, you will find a video about that in the YouTube playlist mentioned in the beginning.

So let us start up a Singleplayer game. Lets test it out in the Town map. All map should work for this. When the map is loaded open the Inventory and find your firework.

Here you should see your first mod cake.

![image](https://github.com/user-attachments/assets/7fbac6ae-9d02-4678-9ffa-45b3d4c99f30)

Now select it and spawn it and see it working.

![20241028183205_1](https://github.com/user-attachments/assets/af7dabcc-1d79-421d-b5b1-ff92353a51e8)

![20241028183218_1](https://github.com/user-attachments/assets/f5837b63-2b1b-492a-a541-d18472a9d029)

![20241028183221_2](https://github.com/user-attachments/assets/0c070ba1-0d99-46a8-b705-509efe44b7cc)

Congrats - you have created your first Fireworks Mania Mod!

# What's next?
So far you only scratched the surface of modding for Fireworks Mania and to be frank, all the above was the boring setup part. 

Now the fun begins where you can make your own effects, maps, characters etc.

This written "Getting started guide" stop here and the rest you need to find in the YouTube Playlist mentioned in [Video Tutorials](#video-tutorials), as it is much easier to explain and show in videos.

Love to see what awesome stuff you are going to create 🤓


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



















