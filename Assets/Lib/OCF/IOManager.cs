using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IOManager : Controllable
{
    private string fileName;
    private bool loaded;

    private string directory = "Presets\\";

    [OSCProperty("fileNames")]
    public List<string> fileNames;

    // Use this for initialization
    void Start () {
       // init();
        id = gameObject.name;
        fileNames = new List<string>();
        Directory.CreateDirectory(directory);
        ReadFileList();
        controllableMaster.Register(GetComponent<IOManager>());
    }

    void Update()
    {
        if (!loaded)
        {
            loaded = true;
            LoadTempFile();
        }
    }
    private void LoadTempFile()
    {
        fileName = "preset.tmp";
        if (File.Exists(directory + fileName))
        {
            load();   
        }
    }

    public void ReadFileList()
    {
        fileNames.Clear();
        foreach (var t in Directory.GetFiles(directory))
        {
            var substrings = t.Split('\\');
            if (substrings[1] != "preset.tmp")
            {
                var subsubstrings = substrings[1].Split('_');
                if(subsubstrings[0] == SceneManager.GetActiveScene().name)
                    fileNames.Add(substrings[1]);
            }
        }
        RaiseEventValueChanged("fileNames");
    }

    [OSCMethod("savePreset")]
    public void savePreset()
    {
        var date = DateTime.Today.Day + "-" + DateTime.Today.Month + "-" + DateTime.Today.Year + "_" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second;
        fileName = SceneManager.GetActiveScene().name + "_" + date + ".txt";
        save();
    }

    private void save()
    {
        //create file
        var file = File.OpenWrite(directory + fileName);
        file.Close();
        var lines = new List<string>();
        foreach (var item in controllableMaster.RegisteredControllables)
        {
            lines.Add(JsonUtility.ToJson(item.Value));
        }
        File.WriteAllLines(directory + fileName, lines.ToArray());
        if (debugOSC)
            Debug.Log("Saved in " + directory + fileName);
        ReadFileList();
    }

    [OSCMethod("loadPreset")]
    public void loadPreset()
    {
        fileName = fileNames.Last();
        if(File.Exists(directory + fileName))
            load();
        else
        {
            Debug.LogWarning("File " + fileName + " doesn't exist !");
            ReadFileList();
        }
    }

    private void load()
    {
        if (debugOSC)
            Debug.Log("Loading " + directory + fileName);

        string line;
        var file = new StreamReader(directory + fileName);
        while ((line = file.ReadLine()) != null)
        {
            var substrings = line.Split(',');
            substrings[0] = "";
            var names = substrings[1].Split(':');
            var objectName = names[1].Replace("\"", "");
            if (objectName == this.gameObject.name) continue;
            if (!controllableMaster.RegisteredControllables.ContainsKey(objectName))
            {
                if (debugOSC)
                    Debug.Log("Nothing registered in ControllableMaster");
                continue;
            }

            for (var i = 2; i < substrings.Length; i++)
            {
                var propInfoInFile = substrings[i].Split(':');
                var propertyNameInFile = propInfoInFile[0].Replace("\"", "");
                
                foreach (var propInObject in controllableMaster.RegisteredControllables[objectName].Properties)
                {
                    if (propInObject.Key == propertyNameInFile)
                    {
                        propInfoInFile[1] = propInfoInFile[1].Replace("}", "");
                        if (debugOSC)
                            Debug.Log("Setting " + propInObject.Key + " with " + propInfoInFile[1]);

                        //TODO Vector3
                        var objs = new List<object> {propInfoInFile[1]};

                        controllableMaster.RegisteredControllables[objectName]
                            .setFieldProp(propInObject.Value, propertyNameInFile, objs);
                    }
                }
                    
            }
        }
        file.Close();
        if (debugOSC)
            Debug.Log("Done.");
    }

    void OnApplicationQuit()
    {
        fileName = "preset.tmp";
        save();
    }
}
