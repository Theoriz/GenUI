using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class IOManager : Controllable
{
    private string directory = "Log\\";

    [OSCProperty("fileNames")]
    public List<string> fileNames;

    // Use this for initialization
    void Start () {
        init();
        id = gameObject.name;
        fileNames = new List<string>();
        ReadFileList();
        controllableMaster.Register(GetComponent<IOManager>());
    }

    private void ReadFileList()
    {
        foreach (var t in Directory.GetFiles(directory))
        {
            var substrings = t.Split('\\');
            fileNames.Add(substrings[1]);
        }
    }

    [OSCMethod("save")]
    public void save()
    {
        var date = DateTime.Today.Day + "-" + DateTime.Today.Month + "-" + DateTime.Today.Year + "_" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second;
        var fileName = directory + "MR_" + date + ".txt";
        var file = File.OpenWrite(fileName);
        file.Close();
        var lines = new List<string>();
        foreach (var item in controllableMaster.RegisteredControllables)
        {
            lines.Add(JsonUtility.ToJson(item.Value));
        }
        File.WriteAllLines(fileName, lines.ToArray());
        Debug.Log("Saved in " + fileName);
        ReadFileList();
    }

    [OSCMethod("load")]
    public void load()
    {
        Debug.Log("Loading " + fileNames.Last());
        string line;
        var file = new StreamReader(directory + fileNames.Last());
        while ((line = file.ReadLine()) != null)
        {
            var substrings = line.Split(',');
            substrings[0] = "";
            var names = substrings[1].Split(':');
            var objectName = names[1].Replace("\"", "");

            if (objectName == this.gameObject.name) continue;
            if (!controllableMaster.RegisteredControllables.ContainsKey(objectName)) continue;


            for (var i = 2; i < substrings.Length; i++)
            {
                var propInfoInFile = substrings[i].Split(':');
                var propertyNameInFile = propInfoInFile[0].Replace("\"", "");
                    
                foreach (var propInObject in controllableMaster.RegisteredControllables[objectName].Properties)
                {
                    if (propInObject.Key == propertyNameInFile)
                    {
                        Debug.Log("Setting " + propInObject.Key + " with " + propInfoInFile[1]);

                        var objs = new List<object> {propInfoInFile[1]};

                        controllableMaster.RegisteredControllables[objectName]
                            .setFieldProp(propInObject.Value, propertyNameInFile, objs);
                    }
                }
                    
            }
        }
        file.Close();
        Debug.Log("Done.");
    }
}
