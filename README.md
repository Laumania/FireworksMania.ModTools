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
Ok lets get started boys and girls - read on to get started.


## 1. Getting Unity
So the first thing you need is Unity. 

> If you have no idea how Unity works, I will suggest you find some getting started tutorial on YouTube (Insert link?).

Go and get the "Unity Hub": https://unity3d.com/get-unity/download

When Unity Hub installed on your machine you can install you are ready to install Unity.

> I'm not sure how this will work in the future yet when it comes to different versions of Unity, for now you need a specific version.

### Specific Unity Version
To avoid unintended behavior and issues you always need the specific version of Unity that the Mod Tools are build with. [You can see the version you need here](https://github.com/Laumania/FireworksMania.ModTools/blob/059e7a83ae0a3f7b293aa1e1c297d4a4e2109a66/ProjectSettings/ProjectVersion.txt#L1). 
Go to this link and click the green "Unity Hub" button on that version. This should make Unity Hub download this version.
https://unity3d.com/get-unity/download/archive

## 2. Create empty Unity project
![image](https://user-images.githubusercontent.com/1378458/133001075-917e2258-838f-4051-9221-02f48a73323f.png)


## 3. Install dependencies

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

## 4. Create mod
![image](https://user-images.githubusercontent.com/1378458/133001208-db4187e8-e6d5-40cf-8504-24639e493286.png)
