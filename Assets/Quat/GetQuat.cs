using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System;
using System.Threading;

public class GetQuat : MonoBehaviour {

    public GUIText gui;
    //定义基本信息  
    public string portName = "COM8";
    public int baudRate = 115200;
    public Parity parity = Parity.None;
    public int dataBits = 8;
    public StopBits stopBits = StopBits.One;

    int[] data = new int[6];//用于存储6位数据  
    SerialPort sp = null;
    Thread dataReceiveThread;

    string message = "";

    void Start()
    {
        OpenPort();
        dataReceiveThread = new Thread(new ThreadStart(DataReceiveFunction));
        dataReceiveThread.Start();
    }

    void Update()
    {
        string str = "";
        for (int i = 0; i < data.Length; i++)
        {
            str += data[i].ToString() + " ";
        }
        gui.text = str;
        Debug.Log(str);
    }

    public void OpenPort()
    {
        sp = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
        sp.ReadTimeout = 400;
        try
        {
            sp.Open();
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
    }


    public void ClosePort()
    {
        try
        {
            sp.Close();
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
    }

    public void WriteData(string dataStr)
    {
        if (sp.IsOpen)
        {
            sp.Write(dataStr);
        }

    }


    void OnApplicationQuit()
    {
        ClosePort();
    }


    void DataReceiveFunction()
    {
        byte[] buffer = new byte[128];
        int bytes = 0;
        //定义协议  
        int flag0 = 0xFF;
        int flag1 = 0xAA;
        int index = 0;//用于记录此时的数据次序  
        while (true)
        {
            if (sp != null && sp.IsOpen)
            {
                try
                {
                    bytes = sp.Read(buffer, 0, buffer.Length);
                    for (int i = 0; i < bytes; i++)
                    {

                        if (buffer[i] == flag0 || buffer[i] == flag1)
                        {
                            index = 0;//次序归0   
                            continue;
                        }
                        else
                        {
                            if (index >= data.Length) index = data.Length - 1;//理论上不应该会进入此判断，但是由于传输的误码，导致数据的丢失，使得标志位与数据个数出错  
                            data[index] = buffer[i];//将数据存入data中  
                            index++;
                        }

                    }
                }
                catch (Exception ex)
                {
                    if (ex.GetType() != typeof(ThreadAbortException))
                    {
                        Debug.Log(ex.Message);
                    }
                }
            }
            Thread.Sleep(10);
        }
    }


    void OnGUI()
    {
        message = GUILayout.TextField(message);
        if (GUILayout.Button("Send Message"))
        {
            WriteData(message);
        }
        string by = "XX AA 03 31 20 51 00 00";
        if (GUILayout.Button("Send", GUILayout.Height(50)))
        {
            WriteData(by);
        }
    }
}
