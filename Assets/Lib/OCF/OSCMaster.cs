using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using UnityOSC;


public class OSCMaster : MonoBehaviour
{
    OSCServer server;
    public int port = 6000;
    public bool debugMessage;

    Controllable[] controllables;

    public delegate void ValueUpdateReadyEvent(string target, string property, List<object> objects);
    public event ValueUpdateReadyEvent valueUpdateReady;

    // Use this for initialization
    void Awake()
    {
        server = new OSCServer(port);
        server.PacketReceivedEvent += packetReceived;
        server.Connect();
    }
    void packetReceived(OSCPacket p)
    {

        if (p.IsBundle())
        {
            foreach (OSCMessage m in p.Data)
            {
                processMessage(m);
            }
        }else processMessage((OSCMessage)p);
        Debug.Log("Packet processed");
    }

    void processMessage(OSCMessage m)
    {

        string[] addSplit = m.Address.Split(new char[] { '/' });

        if (addSplit.Length != 3)
        {
            if (debugMessage) Debug.LogWarning("Message " + m.Address + " is not a valid control address.");
            return;
        }

        string target = addSplit[1];
        string property = addSplit[2];

        if (debugMessage) Debug.Log("Message received for Target : " + target + ", property = " + property);

        ControllableMaster.UpdateValue(target, property, m.Data);
    }

    // Update is called once per frame
    void Update()
    {
      //  Debug.Log("update");
        server.Update();
    }


    void OnDestroy()
    {
        server.Close();
    }
}
