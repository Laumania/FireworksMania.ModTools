> DISCLAIMER: THIS ENTIRE REPOSITORY IS WORK-IN-PROGRESS - SO BARE WITH ME :)
> 
> KEEP AN EYE ON THIS REPO AND MY [YOUTUBE CHANNEL](https://www.youtube.com/laumania) TO GET UPDATED ON WHEN THIS WILL BE AVAILABLE FOR EVERYBODY.

# Fireworks Mania - Mod Tools
This is the place to start when creating mods for Fireworks Mania.

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

To install that specific version, go to the [Unity Archive](https://unity3d.com/get-unity/download/archive) and click the green "Unity Hub" button on that version. This should make Unity Hub download this version.

![image](https://user-images.githubusercontent.com/1378458/138602725-e61fd662-a607-4618-873d-a533e5043ab8.png)

## 3. Create empty Unity project
Fireworks Mania mods are build from inside Unity and you therefore need a empty 3D Unity project. This is (initially) exactly as you would do if you were going to create your own game, however, when building mods there are a few things that are different, which we will get to a bit later.


![image](https://user-images.githubusercontent.com/1378458/138611162-2917e479-285b-4192-b640-2fd0a10abbb9.png)

One important thing to know when naming your project is that you can have multiple mods within a single Unity project. This means that you might want to name the Unity project something more common like, "YourNickNameFireworksManiaMods", or "YourNickName.FireworksMania.Mods" etc. These are just suggestions and you should name the Unity project something that make sense to you and how you structure things. The name of the Unity project have no impact on mod name or anything.


## 4. Install dependencies

### Git
If you get an error saying something with "Git" when you try to add some of the below dependencies, its because you don't have Git installed on your computer.
You can get git from here, install that and restart Unity and Unity Hub: https://git-scm.com/


### Fireworks Mania Mod Tools
Package Manager, git: https://github.com/Laumania/FireworksMania.ModTools.git?path=Assets

### UniTask
Package Manager, git: https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask

See details on UniTasks Github page https://github.com/Cysharp/UniTask#upm-package

### DOTween
Package Manager, My Assets, Search for DOTween, Install
DOTween (https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676)

## 5. Create mod
![image](https://user-images.githubusercontent.com/1378458/133001208-db4187e8-e6d5-40cf-8504-24639e493286.png)
