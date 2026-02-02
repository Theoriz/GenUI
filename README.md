# GenUI
Automatic OSC Control for Unity.

This plugins allows you to simply create a UI for your application, exposing script sliders, inputfield and method. This UI is also fully controllable via OSC.

![Demo](https://github.com/Theoriz/GenUI-Demo/blob/master/gif/genui.gif) 

Example Unity project can be found at : https://github.com/theoriz/genui-demo

## Default Shortcuts

- F1 : Toggle the UI.
- PageUp / PageDown or Ctrl + Plus/Minus: Scale up/down the UI, only when the UI is visible.
- Ctrl + Left/Right/Up/Down arrow : Move the UI, only when the UI is visible.
- F2 : Reset the UI, only when the UI is visible.

## Setup
1. Add the "GenUI" prefab to your scene.
2. Generate controllables for the scripts you want to control. You can use the manual or automatic workflow below.
3. Run the scene, press F1 to toggle the UI.

### Automatic Controllable Generation (Easy - Recommended)

1. In your MonoBehaviour script, add the [OSCExposed] attribute to the fields, properties and methods you want to expose to the UI and OSC.

> [!TIP]
> You can set some fields or properties as read only by using [OSCExposed(readOnly = true)].

2. On the script component of your script in your scene, click on the three dots on the top right and choose Add Controllable. It will prompt you to generate a Controllable script, click Generate.

> [!TIP]
> You can also generate a controllable directly from the project window by right-clicking on a script and choosing Generate Controllable Script.

3. Once the Controllable is generated and compiled, click on the three dots on the top right and choose Add Controllable again. This time it should add the Controllable component and set it up automatically.

### Manual Controllable Generation (Advanced - More Options)

1. Create a new script inheriting from "Controllable". It will be the interface for the script you want to control.
2. For each field or property you want to control with UI/OSC, add a field in the Controllable with the [OSCProperty] attribute and **the exact same name** as the corresponding field or property in the script you want to control.
5. For each method you want to expose. Add a method in the Controllable with the [OSCMethod] attribute. Then call the method from the controlled script as shown in the example below.
6. Add the controllable script to a gameobject in your scene.
7. Link the TargetScript of the Controllable instance to the corresponding script component.
8. Set the desired bar color, it controls color of the panel bar of this controllable in the UI.
9. Set the desired ID, it controls the name of the panel in the UI, and the name used in the OSC address.

<details><summary>CONTROLLABLE EXAMPLE</summary>
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
	
	[OSCProperty(readOnly = true)]
	public bool readOnlyBoolParameter;

	//Create OSC methods to call methods from myScript
	[OSCMethod]
	public void MyOSCMethod() {
		(TargetScript as MyScript).MyScriptMethod();
	}

	//You can expose methods with arguments, but they will not show in the UI
	[OSCMethod]
	public void MyOSCMethodWithArgs(float arg0, int arg1, string arg2) {
		(TargetScript as MyScript).MyScriptMethodWithArgs(arg0, arg1, arg2);
	}
}
```

</p>
</details>

## Supported types
You can expose the following types :
- bool
- int
- float
- string
- Vector2
- Vector2Int
- Vector3
- Vector3Int
- Vector4
- Color

The Header, Range, and Tooltip attributes are also supported in Controllables.

You can also expose methods. Methods without parameters will show as a button in the UI, methods with parameters will not show in the UI but are still exposed to OSC control.

## OSC Control
To access a property or launch a method, use its address.

For example : "/OCF/id/method" or "/OCF/id/floatProperty/ 1.5" by default the id corresponds to the script type name but this can be changed by setting the public variable ID in your script extending "Controllable".

> [!TIP]
> You can copy the OSC Control Address of any exposed parameter in the UI directly by right clicking on the parameter value.

You can also get your own OSC messages by connecting to MessageAvailable event in OSCMaster. This event will be triggered for every OSC message which doesn't start by /OCF/.

## Presets
This plugin comes with a preset system, you can save the state of a "'Controllable" script. It saves each property to a file that can be loaded later so that you can create differents settings for your script. To use it, simply click on "Save preset" then select the wanted preset inside the dropdown menu and press "Load Preset".
It is also possible to load a specific file via the OSC method "LoadPresetWithName" giving it as argument :
  - fileName (string) : case sensitive;
  - duration (float) : tween duration in seconds;
  - tweenType (string) : "EaseInOut", "EaseIn", "EaseOut" or "Linear" if you want to tween the current value to preset's one;
  

## Expose a List
To expose a string list you have to create a index string variable which will be used by the dropdown mennu as an index. It will allows you to know which element of the list is selected. Simply specify [OSCProperty(targetList=yourListName)].

# Dev
OCF : http://github.com/theoriz/OCF


