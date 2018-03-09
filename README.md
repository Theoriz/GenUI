# Unity-OSCControlFramework
Automatic OSC Control Framework for Unity
This plugins allows you to simply create a UI for your application, exposing script sliders, inputfield and method. This UI is also fully controllable via OSC.

## What works ?
You can expose bool, int, float and string property. It is possible to define a range for int and float in order to get a slider in the UI by adding the Range metadata, otherwise you would get an inputfield. Method appear as button in UI so you can't give it arguments but this is possible using OSC. Finally you can expose string list which will be displayed with a dropdown menu.

## Presets
This plugin comes with a preset system, you can save the state of a "'Controllable" script. It saves each property to a file that can be loaded later so that you can create differents settings for your script. To use it, simply click on "Save preset" then select the wanted preset inside the dropdown menu and press "Load Preset".
It is also possible to load a specific file via the OSC method "LoadPresetWithName" giving it as argument :
  - fileName (string) : case sensitive;
  - duration (float) : tween duration in seconds;
  - tweenType (string) : "EaseInOut", "EaseIn", "EaseOut" or "Linear" if you want to tween the current value to preset's one;

## How to use ?
1. Drop the prefab "OSCControlFramework" in your game.
2. Create a new script inheriting from "Controllable".
3. Add the metadata "[OSCProperty] above attributs you want to expose. You can use booleans to use or no the UI, presets, etc.
4. Add the metadata "[OSCMethod] above methods you want to expose.
5. Run !

## Expose a List
To expose a string list you have to create a index variable which will be used by the dropdown mennu as an index. It will allows you to know which element of the list is selected. Simply specify [OSCProperty(TargetList=yourListName)].

## OSC Control
To access a property or launch a method you have to use its address.
For example : "/OCF/id/method" or "/OCF/id/floatProperty/ 1.5" by default the id corresponds to the gameObject name but this can be changed by setting the public variable ID in your script extending "Controllable".

You can also get your own OSC messages by connecting to MessageAvailable event in OSCMaster. This event will be triggered for every OSC message which doesn't start by /OCF/.

## Issues
Sometimes UI won't show up or will be destroy if you load a new scene while playing. To prevent this you have to change the script execution order and set UIMaster as the first script to be executed.
