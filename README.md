> DISCLAIMER: THIS ENTIRE REPOSITORY AND MOD SUPPORT IS STILL UNDER DEVELOPMENT SO ALL SHOULD BE SEEN AS WORK-IN-PROGRESS.
> 
> KEEP AN EYE ON THIS REPO AND MY [YOUTUBE CHANNEL](https://www.youtube.com/laumania) TO GET UPDATED ON WHEN THIS WILL BE AVAILABLE FOR EVERYBODY.

# Fireworks Mania - Mod Tools
This is the place to start when creating mods for [Fireworks Mania](https://store.steampowered.com/app/1079260/Fireworks_Mania__An_Explosive_Simulator/).

# Prerequisites
To be able to build mods for Fireworks Mania there are a few prerequisites, at least it will be pretty hard to do if you don't know anything about the below.

- Basic knowledge about [Unity](https://unity.com)
  - If you want to get started I recommend Imphenzia's ["LEARN UNITY - The Most BASIC TUTORIAL I'll Ever Make"](https://www.youtube.com/watch?v=pwZpJzpE2lQ)
- Basic 3D modeling skills ([Blender](https://www.blender.org) is the tool I use, but there are others. As long as they can export to .fbx I think it's all fine)
  - If you want to get started I recommend Imphenzia's ["Learn Low Poly Modeling in Blender"](https://www.youtube.com/watch?v=1jHUY3qoBu8) if you are all new to Blender. 

Keep in mind that there are tons of good tutorials out there for basic Unity and Blender, so go check out what YouTube have to offer, the two recommendation are just one place to start.

**NO CODING SKILLS NEEDED**

It's very important to note, because this seems to be the biggest problem for most people, that you do **NOT** need to be able to code to build a mod - it can all be done via the Unity editor.

So none-coders, fear not :)


# Getting started
## 1. Getting Unity Hub
Go and get the [Unity Hub](https://unity3d.com/get-unity/download) (not the Beta version!)

When Unity Hub is installed on your machine, you are ready to install "Unity" (also called "Unity Editor").

## 2. Getting Unity Editor
To avoid unintended behavior and issues you always need the specific version of Unity that the Mod Tools are build with.

Luckily, when you have the Unity Hub installed, it's very easy to install the specific version you want and you can have multiple versions installed at the same time.

> As the Mod Tools gets updated, the target Unity version will also be updated. You can go back here and see what version you need. Alternatively the Mod Tools will tell you what version you need when you try to build your mod.

[You can see the version you need in the ModToolsSettings file on the highlighted line here](https://github.com/Laumania/FireworksMania.ModTools/blob/58a86d3d521b8451016bb59fbf89225f07890761/Assets/UMod/Resources/Editor/ModToolsSettings.asset#L29). 

![image](https://user-images.githubusercontent.com/1378458/138910145-e5900b45-8a0e-4b2c-b3fb-dc2822d5ccfd.png)

To install that specific version, go to the [Unity Archive](https://unity3d.com/get-unity/download/archive) and click the green "Unity Hub" button on that version. This should make Unity Hub download this version.

![image](https://user-images.githubusercontent.com/1378458/138602725-e61fd662-a607-4618-873d-a533e5043ab8.png)

## 3. Create empty Unity project
Fireworks Mania mods are build from inside Unity and you therefore need a empty 3D Unity project. This is (initially) exactly as you would do if you were going to create your own game, however, when building mods there are a few things that are different, which we will get to a bit later.


![image](https://user-images.githubusercontent.com/1378458/138611162-2917e479-285b-4192-b640-2fd0a10abbb9.png)

### Naming your Unity Project
One important thing to know when naming your project is that you can have multiple mods within a single Unity project. 

This means that you might want to name the Unity project something more generic like, *"YourNickNameFireworksManiaMods"*, *YourNickName.FireworksMania.Mods"* etc. 

These are just suggestions and you should name the Unity project something that make sense to you and how you structure things. The name of the Unity project have no impact on actual mod(s) name(s).

At this point you should now have an empty Unity project that should look something like this:
![image](https://user-images.githubusercontent.com/1378458/138737260-cd972920-94cd-4eeb-940f-41fe5002c736.png)

## 4. Install dependencies
> As of writing, Oct. 2021, you sadly have to install these dependencies manually as there are no way to automatically include these dependencies in the Mod Tools package.
> Fear not, it looks much more complicated than it really is.

### DOTween
Because DOTween is an Unity Asset Store package, you need to make sure it have it added to "My Assets" to install it in Unity.

Go to [DOTween in a browser here](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676), be sure you are logged in and click to "Add to My Assets" button. If the blue button says "Open in Unity" you already have it in your assets and you can move on to next step.

![image](https://user-images.githubusercontent.com/1378458/138900846-8c105138-d955-4ad9-84c5-ffd82cc282f9.png)

Now go to the Package Manager in the Unity Editor: Window -> Package Manager

![image](https://user-images.githubusercontent.com/1378458/138901793-177e0102-e32d-453d-9c12-7f3e9bc9a5bc.png)

Select "My Assets".

![image](https://user-images.githubusercontent.com/1378458/138901891-eb2392bc-b1e5-4326-aec4-9543173b91f6.png)

Search for "DOTween", select it and press the Download button.

![image](https://user-images.githubusercontent.com/1378458/138902089-a3024171-8ba4-4420-bada-7fddd4a3c437.png)

Once it's downloaded the download button change to an import button, press that to import the package into your Unity project.

An new window will pop up showing you the files in this package, press the Import button and wait for it to complete the import.

![image](https://user-images.githubusercontent.com/1378458/138902812-b01f4de1-9ca3-4f94-be34-b8bc55094f77.png)

Follow the instructions in the DOTween guide.

![image](https://user-images.githubusercontent.com/1378458/138903155-66622b26-d539-4ca3-a815-5d178066a1a9.png)

![image](https://user-images.githubusercontent.com/1378458/138903200-3b911624-f50a-430a-af66-7de77bd8fc31.png)

![image](https://user-images.githubusercontent.com/1378458/138903272-84fac192-7a7b-43dd-a876-f36fd19c5489.png)

![image](https://user-images.githubusercontent.com/1378458/138903389-4d59344c-6fa0-4283-bf30-2eda33978fe9.png)

![image](https://user-images.githubusercontent.com/1378458/138903461-87af2c19-61bf-4c8a-b860-fcf949e8336d.png)

You now have DOTween installed. You won't use it directly and you will not see any changes in the Unity Editor, its purely a dependency that the Fireworks Mania Mod Tools need to be able to build the project and thereby build the mods later on.

### UniTask
This dependency needs to be installed a little different, which is actually easier.

Go to the Package Manager again, click the little + button and select that you want to add an git package.

![image](https://user-images.githubusercontent.com/1378458/138905085-66314ab7-da3e-47a0-a418-3a974cb713b2.png)

Paste the following url in and press Add: https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask

![image](https://user-images.githubusercontent.com/1378458/138905271-262a098e-85f5-43a8-b336-25759917fd4e.png)

> Troubleshooting: If you get an error saying something with "Git" when you try to add some of the below dependencies, its because you don't have Git installed on your computer. 
> You can get git from here, install that and restart Unity and Unity Hub: https://git-scm.com/

![image](https://user-images.githubusercontent.com/1378458/138905359-08064f6f-63b8-4b4e-bc8a-65b047fdc084.png)

After the installation, you should be able to see the package in the list of "Packages: In Project" and remember to clear the search field too.

![image](https://user-images.githubusercontent.com/1378458/138905779-3a250d0b-5fbb-4ad0-822e-127e74096469.png)

If it looks similar to the above, you are done installing UniTask and can continue.

If you want to know more about UniTask, see details on UniTasks Github page https://github.com/Cysharp/UniTask#upm-package

Just as a little check to make sure you are all good before moving on, it's a good idea at this point to press the little Play button to run your current project. No errors should show up in the console and it should look similar to this.

![image](https://user-images.githubusercontent.com/1378458/138906746-b06d03c3-c800-4e79-ab26-4c0ac49a7d54.png)

Remember to press Play again to stop it running before continuing :)

## 5. Install Fireworks Mania Mod Tools
So with all the dependencies installed and a Unity Editor being able to "Play" without errors, you are ready to continue.

Now we need to install the actual Fireworks Mania Mod Tools. The installation is exactly as we did before with the git package.

![image](https://user-images.githubusercontent.com/1378458/138907247-a5e8d7d9-6318-4997-87b4-526839aa3301.png)

Paste in this url and hit Add: https://github.com/Laumania/FireworksMania.ModTools.git

![image](https://user-images.githubusercontent.com/1378458/138907336-b6292fe8-2c10-413e-b18a-7f282f9f1405.png)

![image](https://user-images.githubusercontent.com/1378458/138907625-188d9548-4fc3-4fd0-bd21-8663755ab91a.png)

Once it's done installing you might see a few errors in the console.

I am contantly working on trying to remove errors like this that you shouldn't worry about, however it's not always possible.

In many cases the errors are actually trying to help you with stuff you forgot, so please read them and see if it sounds right.

![image](https://user-images.githubusercontent.com/1378458/138907936-92101674-d704-4d14-a0d2-1e6199eda40c.png)

In this case they don't and you can see that by hitting the Clear button to remove all in the console and then press Play in the Unity Editor.

![image](https://user-images.githubusercontent.com/1378458/138908469-92b43605-70b3-4dde-8f3f-a6d4254170c2.png)

You should see something like this, with no errors. If the erros keep coming, it is an indicator that something is actually wrong, so click the error and see what it selects and read the message.

![image](https://user-images.githubusercontent.com/1378458/138908645-7d96c0ae-e49f-4faf-b34b-e5ad5aaa833a.png)


## 5. Create mod
![image](https://user-images.githubusercontent.com/1378458/133001208-db4187e8-e6d5-40cf-8504-24639e493286.png)
