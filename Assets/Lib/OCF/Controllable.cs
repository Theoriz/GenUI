using UnityEngine;
using System.Collections;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;

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
    public bool debugOSC;
    public Dictionary<string,FieldInfo> Properties;
    public Dictionary<string, MethodInfo> Methods;

    public delegate void ValueChangedEvent(string name);
    public event ValueChangedEvent valueChanged;


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
                Properties.Add(info.Name,info);
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

        if (id == "") id = gameObject.name;
        ControllableMaster.Register(this);
    }


    void OnDestroy()
    {
        ControllableMaster.UnRegister(this);
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

        if(debugOSC) Debug.Log("OSC Field IN TYPE : " + typeString +" " + values[0].ToString());

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
            if (values.Count >= 2) info.SetValue(this, new Vector2(getFloat(values[0]), getFloat(values[1])));
        }
        else if (typeString == "UnityEngine.Vector3")
        {
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

    protected void RaiseEventValueChanged(string property)
    {
        if (valueChanged != null) valueChanged(property);
    }


    public void setMethodProp(MethodInfo info, string property, List<object> values)
    {

        object[] parameters = new object[info.GetParameters().Length];

        if(debugOSC) Debug.Log("Set Method, num expected parameters : " + parameters.Length);

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
            data.nameList.Add(p.Name);
            data.valueList.Add(p.GetValue(this).ToString());
        }
        
        return data;
    }

    public void loadData(ControllableData data)
    {
       int index = 0;
       foreach (string dn in data.nameList)
        {
            List<object> values = new List<object>();
            values.Add(getObjectForValue(Properties[dn].FieldType.ToString(), data.valueList[index]));
            setFieldProp(Properties[dn], dn, values);
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
