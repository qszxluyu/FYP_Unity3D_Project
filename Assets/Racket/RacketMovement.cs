using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class RacketMovement : MonoBehaviour {

    //Parameters used for serial input
    private string WholeLine;
    string[] QAData;
    public bool MaxLoopControl = true;
    public int MaxLoopCount = 20000; //Set to maximum loop count to avoid initial while loop go to infinity

    //Parameters used for reset
    private Vector3 OriginalPosition;

    //Parameter used for main update
    public bool ShowDisp = true;
    private Quaternion Rotate;
    public float QuatX, QuatY, QuatZ, QuatW;
    public float AccelX, AccelY, AccelZ;
    private float LastAccelX, LastAccelY, LastAccelZ;
    public float SpeedX, SpeedY, SpeedZ;
    private float LastSpeedX, LastSpeedY, LastSpeedZ;
    public float DispX, DispY, DispZ;
    private float LastDispX, LastDispY, LastDispZ;

    //Parameters used for steady state offset
    public int SStateLength = 32;
    public float SStateAccelX, SStateAccelY, SStateAccelZ;

    //Parameter used for mechanical filter
    public float MagnitudeAccelThreshold = 0.25f;

    //Parameters used for averaging filter
    public bool UseAvgAccel = true;
    public int AvgSize = 8;
    private float SumAccelX, SumAccelY, SumAccelZ;
    public float AvgAccelX, AvgAccelY, AvgAccelZ;


    void Start()
    {

        UpdataSStateAccel();

        OriginalPosition = this.transform.position;

        ResetParameters();

        //Choose which test raw file to use

        //QuatTestLines = System.IO.File.ReadAllLines(@"..\FYP_Serial_Quat\Assets\Racket\MoveWithAccelZeroMean.txt");
        //QuatTestLines = System.IO.File.ReadAllLines(@"..\FYP_Serial_Quat\Assets\Racket\Random.txt");

    }


    void Update()
    {

        // Run factory self test and calibration routine
        if (Input.GetKey(KeyCode.T))
        {

            GameObject.Find("RacketPviot").GetComponent<SerialController>().SendSerialMessage("t");

            UpdataSStateAccel();

            ResetParameters();

        }

    }

    void FixedUpdate()
    {

        RealTimeSerialWithAvg();

    }

    void RealTimeSerialWithAvg()
    {

        //Update the LastAccel
        LastAccelX = AccelX;
        LastAccelY = AccelY;
        LastAccelZ = AccelZ;

        LastSpeedX = SpeedX;
        LastSpeedY = SpeedY;
        LastSpeedZ = SpeedZ;

        LastDispX = DispX;
        LastDispY = DispY;
        LastDispZ = DispZ;

        //Read a serial message and update Quat and Accel data
        WholeLine = GameObject.Find("RacketPviot").GetComponent<SerialController>().ReadSerialMessage();

        if (WholeLine != null)
        {
            QAData = WholeLine.Split(new[] { ',' });

            if (QAData.Length != 7)
            {
                //Invalid input, use the same data as the last update
                Debug.Log("Invalid input, keep last valid data.");
            }
            else
            {
                //Valid input, update raw Quat and Accel data
                float.TryParse(QAData[0], out QuatW);
                float.TryParse(QAData[1], out QuatX);
                float.TryParse(QAData[2], out QuatY);
                float.TryParse(QAData[3], out QuatZ);

                float.TryParse(QAData[4], out AccelX);
                float.TryParse(QAData[5], out AccelY);
                float.TryParse(QAData[6], out AccelZ);

            }
        }
        else
        {
            //Empty serial message, use the same data as the last update
            Debug.Log("Empty serial message, keep last valid data");
        }

        //Update the average
        SumAccelX = AvgAccelX * (AvgSize - 1) + AccelX;
        AvgAccelX = SumAccelX / AvgSize;
        SumAccelY = AvgAccelY * (AvgSize - 1) + AccelY;
        AvgAccelY = SumAccelY / AvgSize;
        SumAccelZ = AvgAccelZ * (AvgSize - 1) + AccelZ;
        AvgAccelZ = SumAccelZ / AvgSize;

        // Use a threshold to filter the mechanical noise
        float MagnitudeAccel = Mathf.Sqrt(AvgAccelX * AvgAccelX + AvgAccelY * AvgAccelY + AvgAccelZ * AvgAccelZ);

        if (MagnitudeAccel < MagnitudeAccelThreshold)
        {

            AccelX = 0;
            AccelY = 0;
            AccelZ = 0;

            SpeedX = 0;
            SpeedY = 0;
            SpeedZ = 0;

            DispX = 0;
            DispY = 0;
            DispZ = 0;        
            
        }
        else
        {
            //TODO:Use AvgAccel or Zero mean version Accel?
            AccelX = AvgAccelX - SStateAccelX;
            AccelY = AvgAccelY - SStateAccelY;
            AccelZ = AvgAccelZ - SStateAccelZ;

            //Apply the double integration
            SpeedX = LastSpeedX + LastAccelX * Time.fixedDeltaTime + (AccelX - LastAccelX) * Time.fixedDeltaTime / 2f;
            SpeedY = LastSpeedY + LastAccelY * Time.fixedDeltaTime + (AccelY - LastAccelY) * Time.fixedDeltaTime / 2f;
            SpeedZ = LastSpeedZ + LastAccelZ * Time.fixedDeltaTime + (AccelZ - LastAccelZ) * Time.fixedDeltaTime / 2f;

            DispX = LastDispX + LastSpeedX * Time.fixedDeltaTime + (SpeedX - LastSpeedX) * Time.fixedDeltaTime / 2f;
            DispY = LastDispY + LastSpeedY * Time.fixedDeltaTime + (SpeedY - LastSpeedY) * Time.fixedDeltaTime / 2f;
            DispZ = LastDispZ + LastSpeedZ * Time.fixedDeltaTime + (SpeedZ - LastSpeedZ) * Time.fixedDeltaTime / 2f;

        }

        Rotate = new Quaternion(-QuatX, -QuatZ, -QuatY, QuatW);

        //Apply the transform

        if (ShowDisp)
        {

            this.transform.Translate(SpeedX, SpeedZ, SpeedY, Space.Self);
            /*
            this.transform.Translate(Vector3.right * DispX);
            this.transform.Translate(Vector3.up * DispZ);
            this.transform.Translate(Vector3.forward * DispY);
            */

        }

        this.transform.rotation = Rotate;

    }

    void UpdataSStateAccel()
    {

        int SStateCounter = 0;
        int LoopCounter = 0;

        float TempSumAccelX = 0;
        float TempSumAccelY = 0;
        float TempSumAccelZ = 0;

        while (SStateCounter < SStateLength)
        {

            WholeLine = GameObject.Find("RacketPviot").GetComponent<SerialController>().ReadSerialMessage();

            if (WholeLine != null)
            {

                //Debug.Log(WholeLine);
                QAData = WholeLine.Split(new[] { ',' });
                
                if (QAData.Length == 7)
                {

                    float.TryParse(QAData[4], out AccelX);
                    float.TryParse(QAData[5], out AccelY);
                    float.TryParse(QAData[6], out AccelZ);

                    TempSumAccelX += AccelX;
                    TempSumAccelY += AccelY;
                    TempSumAccelZ += AccelZ;

                    SStateCounter++;

                }

            }

            LoopCounter++;
            if (MaxLoopControl && (LoopCounter >= MaxLoopCount))
            {
                break;
            }

        }

        if (MaxLoopControl && (LoopCounter >= MaxLoopCount))
        {
            Debug.LogWarning("Loop counter exceed limit, number of valid input for SState: " + SStateCounter.ToString());
        }

        SStateAccelX = TempSumAccelX / SStateLength;
        SStateAccelY = TempSumAccelY / SStateLength;
        SStateAccelZ = TempSumAccelZ / SStateLength;

    }

    void ResetParameters()
    {

        this.transform.position = OriginalPosition;

        SpeedX = 0;
        SpeedY = 0;
        SpeedZ = 0;

        DispX = 0;
        DispY = 0;
        DispZ = 0;

        LastAccelX = 0;
        LastAccelY = 0;
        LastAccelZ = 0;

        LastSpeedX = 0;
        LastSpeedY = 0;
        LastSpeedZ = 0;

        LastDispX = 0;
        LastDispY = 0;
        LastDispZ = 0;

        if (UseAvgAccel) {

            SumAccelX = 0;
            SumAccelY = 0;
            SumAccelZ = 0;

            //Initiate the AvgAccel   
            int IniIndex = 0;
            int LoopCounter = 0;

            while (IniIndex < AvgSize)
            {

                WholeLine = GameObject.Find("RacketPviot").GetComponent<SerialController>().ReadSerialMessage();

                if (WholeLine != null)
                {

                    QAData = WholeLine.Split(new[] { ',' });

                    if (QAData.Length == 7)
                    {

                        float.TryParse(QAData[4], out AccelX);
                        float.TryParse(QAData[5], out AccelY);
                        float.TryParse(QAData[6], out AccelZ);

                        SumAccelX += AccelX;
                        SumAccelY += AccelY;
                        SumAccelZ += AccelZ;

                        IniIndex++;

                    }

                }

                LoopCounter++;
                if (MaxLoopControl && (IniIndex > MaxLoopCount))
                {
                    break;
                }

                //IniIndex++;//For debug

            }

            if (MaxLoopControl && (IniIndex > MaxLoopCount))
            {
                Debug.LogWarning("Loop counter exceed limit, number of valid input for Avg: " + IniIndex.ToString());
            }

            AvgAccelX = SumAccelX / AvgSize;
            AvgAccelY = SumAccelY / AvgSize;
            AvgAccelZ = SumAccelZ / AvgSize;

        }

    }

}