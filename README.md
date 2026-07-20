# GenUI
Generative UI and OSC Control for Unity.

This plugin allows you to simply create a UI for your application, exposing script sliders, input fields and methods. This UI is also fully controllable via OSC.

![Demo](https://github.com/Theoriz/GenUI-Demo/blob/master/gif/genui.gif) 

## Installation

Add the following line to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.theoriz.unityosc": "https://github.com/Theoriz/UnityOSC.git",
    "com.theoriz.ocf": "https://github.com/Theoriz/OCF.git",
    "com.theoriz.genui": "https://github.com/Theoriz/GenUI.git"
  }
}
```

Or in the Unity Editor, go to **Window > Package Manager > + > Add package from git URL** and enter:

```
https://github.com/Theoriz/UnityOSC.git
```
then
```
https://github.com/Theoriz/OCF.git
```
then
```
https://github.com/Theoriz/GenUI.git
```

## Default Shortcuts

- F1 : Toggle the UI.
- PageUp / PageDown, or Ctrl + Plus/Minus (numpad included) : Scale up/down the UI, only when the UI is visible.
- Ctrl + Left/Right/Up/Down arrow : Move the UI, only when the UI is visible.
- F2 : Reset the UI, only when the UI is visible.

Scaling is ignored while you are typing in an input field.

## Setup
1. In the toolbar go to Theoriz -> GenUI -> Add GenUI to Scene. Or add the GenUI prefab from the Samples folder manually. 
2. Generate controllables for the scripts you want to control. You can use the manual or automatic workflow below.
3. Run the scene, press F1 to toggle the UI.

### Automatic Controllable Generation (Easy - Recommended)

1. In your MonoBehaviour script, add the [OSCExposed] attribute to the fields, properties and methods you want to expose to the UI and OSC.

> [!TIP]
> You can set some fields or properties as read only by using [OSCExposed(readOnly = true)].

> [!WARNING]
> Do not reuse a name that "Controllable" already declares — `Save`, `id`, `debug` and `name` are the ones most often hit. The generated Controllable inherits from "Controllable", so a member of the same name shadows the real one and breaks it. The generator refuses these and tells you which member to rename. See [Reserved names](https://github.com/Theoriz/OCF#reserved-names) in the OCF documentation.

2. On the script component of your script in your scene, click on the three dots on the top right and choose Add Controllable. It will prompt you to generate a Controllable script, click Generate.

> [!TIP]
> You can also generate a controllable directly from the project window by right-clicking on a script and choosing Generate Controllable Script.

3. Once the Controllable is generated and compiled, click on the three dots on the top right and choose Add Controllable again. This time it should add the Controllable component and set it up automatically.

### Manual Controllable Generation (Advanced - More Options)

Writing the Controllable yourself is the only way to reach the [OSCProperty] options that have no [OSCExposed] equivalent — `targetList`, `enumName`, `includeInPresets` and `showInUI`. (`readOnly` needs no hand-written mirror: use [OSCExposed(readOnly = true)] and the generator forwards it.) They are documented in [OSCProperty options](https://github.com/Theoriz/OCF#oscproperty-options) in the OCF documentation.

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
- Color

Enums are also supported, and render as a dropdown — see [Exposing an enum](https://github.com/Theoriz/OCF#exposing-an-enum) in the OCF documentation.

Exposing a type that is not in this list logs a warning and draws no widget.

The Header, Range, and Tooltip attributes are also supported in Controllables.

You can also expose methods. Methods without parameters will show as a button in the UI, methods with parameters will not show in the UI but are still exposed to OSC control.

## OSC Control
To access a property or launch a method, use its address.

For example : "/OCF/id/method" or "/OCF/id/floatProperty 1.5". By default the id corresponds to the script type name, but this can be changed by setting the public variable `id` on your script extending "Controllable".

> [!TIP]
> You can copy the OSC Control Address of any exposed parameter in the UI directly by right clicking on the parameter value.

To handle your own OSC messages — anything not addressed to /OCF/ — subscribe to the receiver directly :

```C#
using UnityOSC;

OSCMaster.Receivers["myReceiver"].messageReceived += (OSCMessage m) => Debug.Log(m.Address);
```

## Presets
This plugin comes with a preset system, you can save the state of a "Controllable" script. It saves each property to a file that can be loaded later so that you can create different settings for your script. To use it, simply click on "Save", then select the wanted preset inside the dropdown menu and press "Load".

Each panel has "Save", "Save As", "Load" and "Show" buttons, and the GenUI panel has "Save All", "Save As All" and "Load All" to apply the same action to every controllable at once.

It is also possible to load a specific file via the OSC method "LoadWithName", giving it the case-sensitive file name as its argument :

```
/OCF/id/LoadWithName "myPreset.pst"
```

## Expose a List
To expose a string list you have to create an index string variable which will be used by the dropdown menu as an index. It will allow you to know which element of the list is selected. Simply specify [OSCProperty(targetList = "yourListName")].

See [Exposing a list](https://github.com/Theoriz/OCF#exposing-a-list) in the OCF documentation for a full example.

# Dev
OCF : http://github.com/theoriz/OCF


