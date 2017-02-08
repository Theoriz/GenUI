using UnityEngine;
using System.Collections;
using System.Reflection;
using System;
using System.Collections.Generic;

public class OSCControllable : MonoBehaviour {


    public string oscName;
    public bool debugOSC;
    public List<KeyValuePair<string,FieldInfo>> oscProperties;
    public List<KeyValuePair<string, MethodInfo>> oscMethods;

    void init()
    {
        //PROPERTIES
        oscProperties = new List<KeyValuePair<string, FieldInfo>>();
        
        Type t = GetType();
        FieldInfo[] objectFields = t.GetFields(BindingFlags.Instance | BindingFlags.Public);
        for (int i = 0; i < objectFields.Length; i++)
        {
            FieldInfo info = objectFields[i];
            OSCProperty attribute = Attribute.GetCustomAttribute(info, typeof(OSCProperty)) as OSCProperty;
            if (attribute != null)
            {
                oscProperties.Add(new KeyValuePair<string, FieldInfo>(attribute.address,info));
            }
        }

        //METHODS

        oscMethods = new List<KeyValuePair<string, MethodInfo>>();

        MethodInfo[] methodFields = t.GetMethods(BindingFlags.Instance | BindingFlags.Public);

        for (int i = 0; i < methodFields.Length; i++)
        {
            MethodInfo info = methodFields[i];
            OSCMethod attribute = Attribute.GetCustomAttribute(info, typeof(OSCMethod)) as OSCMethod;
            if (attribute != null)
            {
                oscMethods.Add(new KeyValuePair<string, MethodInfo>(attribute.address, info));
            }          
        }
    }



    public void setProp(string property, List<object> values)
    {
        
        if (oscProperties == null || oscMethods == null) init();

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



   public void setFieldProp(FieldInfo info, string property, List<object> values)
   { 
        string typeString = info.FieldType.ToString();

        if(debugOSC) Debug.Log("OSC Field IN TYPE : " + typeString +" " + values[0].ToString());

        // if we detect any attribute print out the data.

        if (typeString == "System.Single")
        {
            if(values.Count >= 1) info.SetValue(this, getFloat(values[0]));
        }else if(typeString == "System.Boolean")
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
            if (values.Count >= 4) info.SetValue(this, new Color(getFloat(values[0]), getFloat(values[1]), getFloat(values[2]), getFloat(values[3])));
            else if(values.Count >= 3) info.SetValue(this, new Color(getFloat(values[0]), getFloat(values[1]), getFloat(values[2]),1));
        }
        else if (typeString == "System.String")
        {
           // Debug.Log("String received : " + values.ToString());
            info.SetValue(this, values[0].ToString());
        }
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
        if (t == typeof(string)) return float.Parse((string)value);
        if (t == typeof(bool)) return (bool)value ? 1 : 0;
        
        return float.NaN;
    }

    public int getInt(object value)
    {
        Type t = value.GetType();
        if (t == typeof(float)) return (int)((float)value);
        if (t == typeof(int)) return (int)value;
        if (t == typeof(string)) return int.Parse((string)value);
        if (t == typeof(bool)) return (bool)value ? 1 : 0;

        return 0;
    }

    public bool getBool(object value)
    {
        Type t = value.GetType();
        if (t == typeof(float)) return (float)value >= 1;
        if (t == typeof(int)) return (int)value >= 1;
        if (t == typeof(string)) return int.Parse((string)value) >= 1;
        if (t == typeof(bool)) return (bool)value;

        return false;
    }

    public FieldInfo getPropInfoForAddress(string address)
    {
        foreach(KeyValuePair<string, FieldInfo> p in oscProperties)
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
        foreach (KeyValuePair<string, MethodInfo> p in oscMethods)
        {
            if (p.Key == address)
            {
                return p.Value;
            }
        }

        return null;
    }

    public virtual void Start() { }
    public virtual void Update() { }

}
