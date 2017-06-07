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

    private string directory = "Presets\\";

    [OSCProperty]
    public List<string> fileNames;

    // Use this for initialization
    void Start () {
       // init();
        fileNames = new List<string>();
        Directory.CreateDirectory(directory);
        ReadFileList();
        LoadTempFile();
        Debug.Log("Dictionnary size : " + ControllableMaster.RegisteredControllables.Count);
    }

    private void LoadTempFile()
    {
        fileName = SceneManager.GetActiveScene().name + "_preset.tmp";
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
            if (substrings[1] != SceneManager.GetActiveScene().name + "_preset.tmp")
            {
                var subsubstrings = substrings[1].Split('_');
                if(subsubstrings[0] == SceneManager.GetActiveScene().name)
                    fileNames.Add(substrings[1]);
            }
        }
        RaiseEventValueChanged("fileNames");
    }

    [OSCMethod]
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
        foreach (var item in ControllableMaster.RegisteredControllables)
        {
            lines.Add(JsonUtility.ToJson(item.Value.getData()));
        }
        File.WriteAllLines(directory + fileName, lines.ToArray());
        if (debugOSC)
            Debug.Log("Saved in " + directory + fileName);
        ReadFileList();
    }

    [OSCMethod]
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
            ControllableData cData = JsonUtility.FromJson<ControllableData>(line);

            //Don't load IOManager
            if (cData.dataID == this.id || cData.dataID == null) continue;
            if (!ControllableMaster.RegisteredControllables.ContainsKey(cData.dataID))
            {
                if (debugOSC)
                    Debug.Log(cData.dataID + " not registered in ControllableMaster");
                continue;
            }

            ControllableMaster.RegisteredControllables[cData.dataID].loadData(cData);
        }


        //string line;
        //var file = new StreamReader(directory + fileName);
        //while ((line = file.ReadLine()) != null)
        //{
        //    var substrings = line.Split(',');
        //    substrings[0] = "";
        //    var names = substrings[1].Split(':');
        //    var objectName = names[1].Replace("\"", "");

        //    //Don't load IOManager
        //    if (objectName == this.id) continue;

        //    if (!ControllableMaster.RegisteredControllables.ContainsKey(objectName))
        //    {
        //        if (debugOSC)
        //            Debug.Log(objectName + " not registered in ControllableMaster");
        //        continue;
        //    }

        //    for (var i = 2; i < substrings.Length; i++)
        //    {
        //        var propInfoInFile = substrings[i].Split(':');
        //        var propertyNameInFile = propInfoInFile[0].Replace("\"", "");

        //        foreach (var propInObject in ControllableMaster.RegisteredControllables[objectName].Properties)
        //        {
        //            if (propInObject.Key == propertyNameInFile)
        //            {
        //                propInfoInFile[1] = propInfoInFile[1].Replace("}", "");
        //                if (debugOSC)
        //                    Debug.Log("Setting " + propInObject.Key + " with " + propInfoInFile[1]);

        //                //TODO Vector3
        //                var objs = new List<object> {propInfoInFile[1]};

        //                ControllableMaster.RegisteredControllables[objectName]
        //                    .setFieldProp(propInObject.Value, propertyNameInFile, objs);
        //            }
        //        }

        //    }
        //}
        //file.Close();



        if (debugOSC)
            Debug.Log("Done.");
    }

    void OnApplicationQuit()
    {
        Debug.Log("Dictionnary size : " + ControllableMaster.RegisteredControllables.Count);
        fileName = SceneManager.GetActiveScene().name + "_preset.tmp";
        save();
    }
}
