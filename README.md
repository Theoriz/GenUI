# GenUI
Automatic OSC Control for Unity.

This plugins allows you to simply create a UI for your application, exposing script sliders, inputfield and method. This UI is also fully controllable via OSC.

![Demo](https://github.com/Theoriz/GenUI-Demo/blob/master/gif/genui.gif) 

Example Unity project can be found at : https://github.com/theoriz/genui-demo

## What's inside ?
You can expose bool, int, float, string, List and Vector3 properties. It is possible to define a range for int and float in order to get a slider in the UI by adding the Range metadata, otherwise you would get an inputfield. Method appear as button in UI so you can't give it arguments but this is possible using OSC.

## How to use ?
1. Drop the prefab "GenUI" in your game.
2. Create a new script inheriting from "Controllable". It will be the interface for the script you want to control.
3. Add to this script every attributes you want to control with UI/OSC and the metadata "[OSCProperty]. You can use booleans to use or not the UI, presets, etc. Just be sure that your attributs have the same name as the one in the script you want to control.
4. Add the metadata "[OSCMethod] above methods you want to expose.
6. Run !

<details><summary>**CONTROLLABLE EXAMPLE**</summary>
<p>

```C++
public class MyScriptControllable : Controllable {

	// Expose variables from MyScript to OSC by creating OSCProperties with the name of those variables
	[OSCProperty]
	public int intParameter;

	[OSCProperty]
	public float floatParameter;
	
	[OSCProperty][Range(0,1)]
	public float floatParameterWithRange;
	
	[OSCProperty(isInteractible = false)]
	public bool readOnlyBoolParameter;

	//Create OSC methods to call methods from myScript
	[OSCMethod]
	public void MyOSCMethod() {
		(TargetScript as MyScript).MyScriptMethod();
	}
}
```

</p>
</details>

## OSC Control
To access a property or launch a method you have to use its address.
For example : "/OCF/id/method" or "/OCF/id/floatProperty/ 1.5" by default the id corresponds to the script type name but this can be changed by setting the public variable ID in your script extending "Controllable".

You can also get your own OSC messages by connecting to MessageAvailable event in OSCMaster. This event will be triggered for every OSC message which doesn't start by /OCF/.

## Presets
This plugin comes with a preset system, you can save the state of a "'Controllable" script. It saves each property to a file that can be loaded later so that you can create differents settings for your script. To use it, simply click on "Save preset" then select the wanted preset inside the dropdown menu and press "Load Preset".
It is also possible to load a specific file via the OSC method "LoadPresetWithName" giving it as argument :
  - fileName (string) : case sensitive;
  - duration (float) : tween duration in seconds;
  - tweenType (string) : "EaseInOut", "EaseIn", "EaseOut" or "Linear" if you want to tween the current value to preset's one;
  

## Expose a List
To expose a string list you have to create a index string variable which will be used by the dropdown mennu as an index. It will allows you to know which element of the list is selected. Simply specify [OSCProperty(TargetList=yourListName)].

# Dev
Unity 2018.1.8f1
OCF : http://github.com/theoriz/OCF

