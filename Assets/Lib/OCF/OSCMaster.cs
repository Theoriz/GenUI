using System;
using UnityEngine;
using System.Collections.Generic;
using UnityOSC;


public class OSCMaster : Controllable
{
    public static OSCMaster instance;

    OSCServer server;

    [OSCProperty]
    public int localPort = 6000;


    [OSCProperty(isInteractible = false)] public bool isConnected;

    public bool logIncoming;
    public bool logOutgoing;

    OSCClient client;
   
    Controllable[] controllables;

    public delegate void ValueUpdateReadyEvent(string target, string property, List<object> objects);
    public event ValueUpdateReadyEvent valueUpdateReady;

    public delegate void MessageAvailable(OSCMessage message);
    public event MessageAvailable messageAvailable;

    // Use this for initialization
    public override void Awake()
    {
        instance = this;

        usePanel = true;
        base.Awake();

        Connect();

        client = new OSCClient(System.Net.IPAddress.Loopback, 0, false);

    }

    public void Connect()
    {
        Debug.Log("Connecting to port " + localPort);
        try
        {
            if(server != null)
                server.Close();

            server = new OSCServer(localPort);
            server.PacketReceivedEvent += packetReceived;
        
            server.Connect();
            isConnected = true;
        }
        catch (Exception e)
        {
            Debug.Log("Error with port " + localPort);
            isConnected = false;
        }
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

        //First addSplit is null because of /OCF/...
        if (addSplit[1] != "OCF") //.Length != 3)
        {
            if(messageAvailable != null)
                messageAvailable(m); //propagate the message
             //if (logIncoming) Debug.LogWarning("Message " + m.Address + " is not a valid control address.");
            //return;
        }
        else //Starts with /OCF/ so it's control
        {
            string target = addSplit[2];
            string property = addSplit[3];

            if (logIncoming) Debug.Log("Message received for Target : " + target + ", property = " + property);
            ControllableMaster.UpdateValue(target, property, m.Data);
        }
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();

        if (localPort != server.LocalPort)
        {
            Connect();
        }

        server.Update();
    }

    public static void sendMessage(OSCMessage m, string host, int port)
    {
        if (instance.logOutgoing)
        {
            string args = "";
            for (int i = 0; i < m.Data.Count; i++) args += (i > 0 ? ", " : "") + m.Data[i].ToString();
            Debug.Log("Sending " + m.Address + " : "+args);
        }

        instance.client.SendTo(m, host, port);
    }


    void OnApplicationQuit()
    {
        server.Close();
    }
}
