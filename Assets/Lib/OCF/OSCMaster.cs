using UnityEngine;
using System.Collections;
using UnityOSC;


public class OSCMaster : MonoBehaviour {

    OSCServer server;
    public int port = 6000;
    public bool debugMessage;

    OSCControllable[] controllables;
    
	// Use this for initialization
	void Awake () {
        server = new OSCServer(port);
        server.PacketReceivedEvent += packetReceived;
        server.Connect();

        controllables = FindObjectsOfType<OSCControllable>();
        foreach(OSCControllable c in controllables)
        {
            Debug.Log("Add controllable : " + c.oscName);
        }
	}

    void packetReceived(OSCPacket p)
    {

        OSCMessage m = (OSCMessage)p;
        
        string[] addSplit = m.Address.Split(new char[] { '/' });

        if (addSplit.Length != 3)
        {
            if (debugMessage) Debug.LogWarning("Message " + m.Address + " is not a valid control address.");
            return;
        }

        string target = addSplit[1];
        string property = addSplit[2];

        if(debugMessage) Debug.Log("Message received for Target : " + target + ", property = " + property);

        OSCControllable c = getControllableForID(target);
        if (c == null) return;
        
        
        c.setProp(property, m.Data);
    }

    OSCControllable getControllableForID(string id)
    {
        foreach(OSCControllable c in controllables)
        {
            if (c.oscName == id) return c;
        }
        return null;
    }
	
	// Update is called once per frame
	void Update () {
        Debug.Log("update");
        server.Update();
	}


    void OnDestroy()
    {
        server.Close();
    }
}
