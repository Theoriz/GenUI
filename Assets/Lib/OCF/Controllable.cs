using UnityEngine;
using System.Collections;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine.SceneManagement;

[Serializable]
public class ControllableData
{
    public string dataID;

    public List<string> nameList;
    public List<string> valueList;

    public ControllableData()
    {
        nameList = new List<string>();
        valueList = new List<string>();
    }
}

public class Controllable : MonoBehaviour
{
    public string id;
    public string folder = "";
    public bool debug;
    public string targetDirectory;

    public Dictionary<string, FieldInfo> Properties;
    public Dictionary<string, MethodInfo> Methods;

    public delegate void ValueChangedEvent(string name);

    public event ValueChangedEvent valueChanged;

    [OSCProperty(TargetList = "presetList", IncludeInPresets = false)] public string currentPreset;

    public List<string> presetList;


    void Awake()
    {
        //PROPERTIES
        Properties = new Dictionary<string, FieldInfo>();

        Type t = GetType();
        FieldInfo[] objectFields = t.GetFields(BindingFlags.Instance | BindingFlags.Public);

        for (int i = 0; i < objectFields.Length; i++)
        {
            FieldInfo info = objectFields[i];
            OSCProperty attribute = Attribute.GetCustomAttribute(info, typeof(OSCProperty)) as OSCProperty;
            if (attribute != null)
            {
                Properties.Add(info.Name, info);
            }
        }

        //METHODS

        Methods = new Dictionary<string, MethodInfo>();

        MethodInfo[] methodFields = t.GetMethods(BindingFlags.Instance | BindingFlags.Public);

        for (int i = 0; i < methodFields.Length; i++)
        {
            MethodInfo info = methodFields[i];
            OSCMethod attribute = Attribute.GetCustomAttribute(info, typeof(OSCMethod)) as OSCMethod;
            if (attribute != null)
            {
                // Debug.Log("Added a new method : " + attribute.address);
                Methods.Add(info.Name, info);
            }
        }

        presetList = new List<string>();
        ReadFileList();


        if (string.IsNullOrEmpty(id)) id = gameObject.name;
        ControllableMaster.Register(this);

        if (presetList.Count > 1)
            currentPreset = presetList[1];
    }

    public void ReadFileList()
    {
        presetList.Clear();
        targetDirectory = "Presets/" + (folder.Length > 0?folder:SceneManager.GetActiveScene().name) + "/" + id + "/";
        Directory.CreateDirectory(targetDirectory);
        foreach (var t in Directory.GetFiles(targetDirectory))
        {
            var onlyFileName = t.Split('/').Last();
            presetList.Add(onlyFileName);
        }

        RaiseEventValueChanged("presetList");
    }

    [OSCMethod]
    public void SavePreset()
    {

        var date = DateTime.Today.Day + "-" + DateTime.Today.Month + "-" + DateTime.Today.Year + "_" +
                   DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second;
        var fileName = date + ".pst";
        targetDirectory = "Presets/" + (folder.Length > 0 ? folder : SceneManager.GetActiveScene().name) + "/" + id + "/";
        Debug.Log("Saving in " + targetDirectory + fileName + "...");
        //create file
        if (!Directory.Exists(targetDirectory)) Directory.CreateDirectory(targetDirectory);
        var file = File.OpenWrite(targetDirectory + fileName);
        file.Close();

        CallMeBeforeSave();
        File.WriteAllText(targetDirectory + fileName, JsonUtility.ToJson(this.getData()));

        if (debug)
            Debug.Log("Saved in " + targetDirectory + fileName);

        ReadFileList();
    }

    [OSCMethod]
    public void LoadPreset()
    {
        //if (debug)
            Debug.Log("Loading " + currentPreset);

        var file = new StreamReader(targetDirectory + currentPreset);
        ControllableData cData = JsonUtility.FromJson<ControllableData>(file.ReadLine());

        loadData(cData);
        DataLoaded();

        if (debug)
            Debug.Log("Done.");
    }

    //Override it if you want to do things after a load 
    public virtual void DataLoaded() { }
    //Override it if you want to do things before a preset save
    public virtual void CallMeBeforeSave()
    {

    }

    void OnDestroy()
    {
        ControllableMaster.UnRegister(this);
    }

    public FieldInfo getFieldInfoByName(string requestedName)
    {
        var objectFields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
        FieldInfo requestedField = null;
   
        foreach (var item in objectFields)
        {
            if(item.Name == requestedName)
                requestedField = item;
        }

        return requestedField;
    }

    protected void RaiseEventValueChanged(string property)
    {
        if (valueChanged != null) valueChanged(property);
    }

    public void setProp(string property, List<object> values)
    {

     //   if (Properties == null || Methods == null) init();

        FieldInfo info = getPropInfoForAddress(property);
        if (info != null)
        {
            setFieldProp(info, property, values);
            return;
        }

        MethodInfo mInfo = getMethodInfoForAddress(property);
        if (mInfo != null)
        {
            setMethodProp(mInfo, property, values);
            return;
        }
    }

    public void setFieldProp(FieldInfo info, string property, List<object> values, bool silent = false)
   {
        string typeString = info.FieldType.ToString();

        if(debug)
            Debug.Log("Setting attribut  " + property + " of type " + typeString +" with " + values.Count+" value(s) // "+values[0].ToString());
    
        // if we detect any attribute print out the data.

        if (typeString == "System.Single")
        {
            if(values.Count >= 1) info.SetValue(this, getFloat(values[0]));
        }
        else if(typeString == "System.Boolean")
        {
            if (values.Count >= 1) info.SetValue(this, getBool(values[0]));
        }
        else if(typeString == "System.Int32")
        {
            if (values.Count >= 1) info.SetValue(this, getInt(values[0]));
        } else if (typeString == "UnityEngine.Vector2")
        {
            if (values.Count == 1) info.SetValue(this, (Vector2)values[0]);
            if (values.Count >= 2) info.SetValue(this, new Vector2(getFloat(values[0]), getFloat(values[1])));
        }
        else if (typeString == "UnityEngine.Vector3")
        {
            if (values.Count == 1) info.SetValue(this, (Vector3)values[0]);
            if (values.Count >= 3) info.SetValue(this, new Vector3(getFloat(values[0]), getFloat(values[1]), getFloat(values[2])));
        }
        else if (typeString == "UnityEngine.Color")
        {
            if(values.Count == 1) info.SetValue(this, (Color)values[0]);
            else if (values.Count >= 4) info.SetValue(this, new Color(getFloat(values[0]), getFloat(values[1]), getFloat(values[2]), getFloat(values[3])));
            else if(values.Count >= 3) info.SetValue(this, new Color(getFloat(values[0]), getFloat(values[1]), getFloat(values[2]),1));
        }
        else if (typeString == "System.String")
        {
           // Debug.Log("String received : " + values.ToString());
            info.SetValue(this, values[0].ToString());
        }
       if (valueChanged != null && !silent) valueChanged(property);
    }

    public void setMethodProp(MethodInfo info, string property, List<object> values)
    {

        object[] parameters = new object[info.GetParameters().Length];

        if(debug) Debug.Log("Set Method, num expected parameters : " + parameters.Length);

        int valueIndex = 0;
        for(int i=0;i<parameters.Length;i++)
        {
            string typeString = info.GetParameters()[i].ParameterType.ToString();
            //Debug.Log("OSC IN Method, arg "+i+" TYPE : " + typeString + ", num values in OSC Message " + values.Count);

            if (typeString == "System.Single")
            {
                if (values.Count >= valueIndex + 1)
                {
                    parameters[i] = getFloat(values[valueIndex]);
                    valueIndex += 1;
                }
            }
            else if (typeString == "System.Boolean")
            {
                if (values.Count >= valueIndex + 1)
                {
                    parameters[i] = getBool(values[valueIndex]);
                    valueIndex += 1;
                }
            }
            else if (typeString == "System.Int32")
            {
                if (values.Count >= valueIndex + 1)
                {
                    parameters[i] = getInt(values[valueIndex]);
                    valueIndex += 1;
                }
            }
            else if (typeString == "UnityEngine.Vector2")
            {
                if (values.Count >= valueIndex + 2)
                {
                    parameters[i] = new Vector2(getFloat(values[valueIndex]), getFloat(values[valueIndex + 1]));
                    valueIndex += 2;
                }
            }
            else if (typeString == "UnityEngine.Vector3")
            {
                if (values.Count >= valueIndex + 3)
                {
                    parameters[i] = new Vector3(getFloat(values[valueIndex]), getFloat(values[valueIndex + 1]), getFloat(values[valueIndex + 2]));
                    valueIndex += 3;
                }
            }
            else if (typeString == "UnityEngine.Color")
            {
                if (values.Count >= valueIndex + 4)
                {
                    parameters[i] = new Color(getFloat(values[valueIndex + 0]), getFloat(values[valueIndex + 1]), getFloat(values[valueIndex + 2]), getFloat(values[valueIndex + 3]));
                    valueIndex += 4;
                }
                else if (values.Count >= i + 3)
                {
                    parameters[i] = new Color(getFloat(values[valueIndex + 0]), getFloat(values[valueIndex + 1]), getFloat(values[valueIndex + 2]), 1);
                    valueIndex += 3;
                }

            }
            else if (typeString == "System.String")
            {
                if (values.Count >= valueIndex + 1)
                {
                    parameters[i] = values[i].ToString();
                    valueIndex += 1;
                }
            }

        }

        info.Invoke(this, parameters);
    }

    public float getFloat(object value)
    {
        Type t = value.GetType();
        if (t == typeof(float)) return (float)value;
        if (t == typeof(int)) return (float)((int)value);
        if (t == typeof(string))
        {
            float result = 0;
            float.TryParse((string)value, out result);
            return result;
        }

        if (t == typeof(bool)) return (bool)value ? 1 : 0;

        return float.NaN;
    }

    public int getInt(object value)
    {
        Type t = value.GetType();
        if (t == typeof(float)) return (int)((float)value);
        if (t == typeof(int)) return (int)value;
        if (t == typeof(string))
        {
            int result = 0;
            int.TryParse((string)value, out result);
            return result;
        }
        if (t == typeof(bool)) return (bool)value ? 1 : 0;

        return 0;
    }

    public bool getBool(object value)
    {
        Type t = value.GetType();
        if (t == typeof(float)) return (float)value >= 1;
        if (t == typeof(int)) return (int)value >= 1;
        if (t == typeof(string))
        {
            string s = ((string) value).ToLower();
            if (s == "true" || s == "1")
                return true;

            if (s == "false" || s == "0")
                return false;

            int result = 0;
            int.TryParse((string)value, out result);

            return result >= 1;
        }
        if (t == typeof(bool)) return (bool)value;

        return false;
    }

    public FieldInfo getPropInfoForAddress(string address)
    {
        foreach(KeyValuePair<string, FieldInfo> p in Properties)
        {
            if(p.Key == address)
            {
                return p.Value;
            }
        }

        return null;
    }

    public MethodInfo getMethodInfoForAddress(string address)
    {
        foreach (KeyValuePair<string, MethodInfo> p in Methods)
        {
            if (p.Key == address)  return p.Value;
        }

        return null;
    }

    public object getData()
    {
        ControllableData data = new ControllableData();
        data.dataID = id;

        foreach (FieldInfo p in Properties.Values)
        {
            OSCProperty attribute = Attribute.GetCustomAttribute(p, typeof(OSCProperty)) as OSCProperty;
            if (attribute.IncludeInPresets)
            {
                Debug.Log("Attribute : " + p.Name + " of type " + p.FieldType + " is saved.");
                data.nameList.Add(p.Name);
                
                //Because a simple "toString" doesn't give the full value
                if (p.FieldType.ToString() == "UnityEngine.Vector3")
                {
                    data.valueList.Add(((Vector3) p.GetValue(this)).ToString("F8"));
                }
                else if (p.FieldType.ToString() == "System.Single")
                {
                    data.valueList.Add(((float)p.GetValue(this)).ToString("F8"));
                }
                else 
                    data.valueList.Add(p.GetValue(this).ToString());
            }
        }

        return data;
    }

    public void loadData(ControllableData data)
    {
       int index = 0;
       foreach (string dn in data.nameList)
        {
            List<object> values = new List<object>();
            FieldInfo info;
            if(Properties.TryGetValue(dn,out info))
            {
                values.Add(getObjectForValue(Properties[dn].FieldType.ToString(), data.valueList[index]));
                setFieldProp(Properties[dn], dn, values);
            }

            index++;
        }
    }

    /*
    Int32 fieldOffset = 0;(
    var assemblyName
        = new AssemblyName("MyDynamicAssembly");
    var assemblyBuilder
        = AppDomain.CurrentDomain.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run);
    var moduleBuilder
        = assemblyBuilder.DefineDynamicModule("MyDynamicModule");

    var myTypeBuilder = moduleBuilder.DefineType("data", TypeAttributes.Public);

    // add public fields to match the source object
    foreach (FieldInfo sourceField in Properties.Values)
    {
        FieldBuilder fieldBuilder
            = myTypeBuilder.DefineField(
                sourceField.Name,
                sourceField.FieldType,
                FieldAttributes.Public);
        fieldBuilder.SetOffset(fieldOffset);
        fieldOffset++;
    }

    // create the dynamic class
    Type dynamicType = myTypeBuilder.CreateType();

    // create an instance of the class
    newData = Activator.CreateInstance(dynamicType);

    // copy the values of the public fields of the
    // source object to the dynamic object
    foreach (FieldInfo sourceField in Properties.Values)
    {
        FieldInfo destField
            = newData.GetType().GetField(sourceField.Name);
        destField.SetValue(
            newData,
            sourceField.GetValue(this));
    }

    // give the new class to the caller for casting purposes
    outType = dynamicType;

    return newData;
    */




    object getObjectForValue(string typeString, string value)
    {
        if (typeString == "System.Single") return getFloat(value);
        if (typeString == "System.Boolean") return getBool(value);
        if (typeString == "System.Int32") return getInt(value);
        if (typeString == "UnityEngine.Vector2") return StringToVector2(value);
        if( typeString == "UnityEngine.Vector3") return StringToVector3(value);
        if (typeString == "UnityEngine.Color") return StringToColor(value);
        if (typeString == "System.String") return value;

        return null;
    }


    public static Color StringToColor(string sColor)
    {
        // Remove the parentheses
        if (sColor.StartsWith("RGBA(") && sColor.EndsWith(")"))
        {
            sColor = sColor.Substring(5, sColor.Length - 6);
        }

        // split the items
        string[] sArray = sColor.Split(',');

        // store as a Vector3
        Color result = new Color(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]),
            float.Parse(sArray[3])
        );


        return result;
    }

    public static Vector2 StringToVector2(string sVector)
    {
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector2 result = new Vector2(
            float.Parse(sArray[0]),
            float.Parse(sArray[1])
            );

        return result;
    }

    public static Vector3 StringToVector3(string sVector)
    {
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]));

        return result;
    }

}
