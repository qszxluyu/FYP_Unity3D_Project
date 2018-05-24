using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class RacketMovement : MonoBehaviour {

    public float QuatX, QuatY, QuatZ, QuatW;

    public float AccelX, AccelY, AccelZ;

    private float LastAccelX, LastAccelY, LastAccelZ;

    public float SpeedX, SpeedY, SpeedZ;

    private float LastSpeedX, LastSpeedY, LastSpeedZ;

    public float DispX, DispY, DispZ;

    private float LastDispX, LastDispY, LastDispZ;

    public float MagnitudeAccelThreshold = 0.25f;

    private string[] QuatTestLines;

    private int LineIndex = 0;

    private Vector3 OriginalPosition;

    //Parameters used for Average
    public bool UseAvgAccel = true;

    public int AvgSize = 8;

    private float SumAccelX, SumAccelY, SumAccelZ;

    public float AvgAccelX, AvgAccelY, AvgAccelZ;

    void Start()
    {

        OriginalPosition = this.transform.position;

        ResetParameters();

        //Choose which test raw file to use

        //QuatTestLines = System.IO.File.ReadAllLines(@"..\FYP_Serial_Quat\Assets\Racket\MoveWithAccelZeroMean.txt");
        //QuatTestLines = System.IO.File.ReadAllLines(@"..\FYP_Serial_Quat\Assets\Racket\Random.txt");

        //QuatTests();

    }


    void Update()
    {

        // Run factory self test and calibration routine
        if (Input.GetKey(KeyCode.T))
        {

            GameObject.Find("RacketPviot").GetComponent<SerialController>().SendSerialMessage("t");

            ResetParameters();

        }

    }

    void FixedUpdate()
    {

        //WithoutAccel();

        //WithAccel();

        //RealTimeSerial();

        RealTimeSerialWithAvg();

    }

    void RealTimeSerial()
    {

        string WholeLine = GameObject.Find("RacketPviot").GetComponent<SerialController>().ReadSerialMessage();

        string[] QAData = WholeLine.Split(new[] { ',' });

        if (QAData.Length != 7)
        {
            Debug.Log("Invalid input, data abandoned.");
            return;
        }

        float.TryParse(QAData[0], out QuatW);
        float.TryParse(QAData[1], out QuatX);
        float.TryParse(QAData[2], out QuatY);
        float.TryParse(QAData[3], out QuatZ);

        float.TryParse(QAData[4], out AccelX);
        float.TryParse(QAData[5], out AccelY);
        float.TryParse(QAData[6], out AccelZ);

        // Use a threshold to determine the racket is moved or not
        float MagnitudeAccel = Mathf.Sqrt( AccelX*AccelX + AccelY*AccelY + AccelZ*AccelZ );

        if (MagnitudeAccel < MagnitudeAccelThreshold)
        {
            SpeedX = 0;
            SpeedY = 0;
            SpeedZ = 0;
        }
        else
        {
            SpeedX += AvgAccelX * Time.deltaTime;
            SpeedY += AvgAccelY * Time.deltaTime;
            SpeedZ += AvgAccelZ * Time.deltaTime;
        }

        Quaternion rotate = new Quaternion(-QuatX, -QuatZ, -QuatY, QuatW);

        //Apply the transform

        this.transform.Translate(SpeedX, SpeedZ, SpeedY, Space.Self);
        this.transform.rotation = rotate;

    }

    void RealTimeSerialWithAvg()
    {

        string WholeLine = GameObject.Find("RacketPviot").GetComponent<SerialController>().ReadSerialMessage();

        string[] QAData = WholeLine.Split(new[] { ',' });

        if (QAData.Length != 7)
        {
            Debug.Log("Invalid input, data abandoned.");
            return;
        }

        LastAccelX = AccelX;
        LastAccelY = AccelY;
        LastAccelZ = AccelZ;

        LastSpeedX = SpeedX;
        LastSpeedY = SpeedY;
        LastSpeedZ = SpeedZ;

        LastDispX = DispX;
        LastDispY = DispY;
        LastDispZ = DispZ;

        float.TryParse(QAData[0], out QuatW);
        float.TryParse(QAData[1], out QuatX);
        float.TryParse(QAData[2], out QuatY);
        float.TryParse(QAData[3], out QuatZ);

        float.TryParse(QAData[4], out AccelX);
        float.TryParse(QAData[5], out AccelY);
        float.TryParse(QAData[6], out AccelZ);

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
            AccelX = AvgAccelX;
            AccelY = AvgAccelY;
            AccelZ = AvgAccelZ;

            //Apply the double integration
            SpeedX = LastSpeedX + LastAccelX * Time.fixedDeltaTime + (AccelX - LastAccelX) * Time.fixedDeltaTime / 2f;
            SpeedY = LastSpeedY + LastAccelY * Time.fixedDeltaTime + (AccelY - LastAccelY) * Time.fixedDeltaTime / 2f;
            SpeedZ = LastSpeedZ + LastAccelZ * Time.fixedDeltaTime + (AccelZ - LastAccelZ) * Time.fixedDeltaTime / 2f;

            DispX = LastDispX + LastSpeedX * Time.fixedDeltaTime + (SpeedX - LastSpeedX) * Time.fixedDeltaTime / 2f;
            DispY = LastDispY + LastSpeedY * Time.fixedDeltaTime + (SpeedY - LastSpeedY) * Time.fixedDeltaTime / 2f;
            DispZ = LastDispZ + LastSpeedZ * Time.fixedDeltaTime + (SpeedZ - LastSpeedZ) * Time.fixedDeltaTime / 2f;

        }




        Quaternion rotate = new Quaternion(-QuatX, -QuatZ, -QuatY, QuatW);

        //Apply the transform

        this.transform.Translate(SpeedX, SpeedZ, SpeedY, Space.Self);
        /*
        this.transform.Translate(Vector3.right * DispX);
        this.transform.Translate(Vector3.up * DispZ);
        this.transform.Translate(Vector3.forward * DispY);
        */
        this.transform.rotation = rotate;

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

            while (IniIndex < AvgSize)
            {

                string WholeLine = GameObject.Find("RacketPviot").GetComponent<SerialController>().ReadSerialMessage();

                string[] QAData = WholeLine.Split(new[] { ',' });

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

            AvgAccelX = SumAccelX / AvgSize;
            AvgAccelY = SumAccelY / AvgSize;
            AvgAccelZ = SumAccelZ / AvgSize;

        }

    }

    void WithoutAccel()
    {

        string WholeLine = QuatTestLines[LineIndex];

        string[] QuatData = WholeLine.Split(new[] { ',' });

        if (QuatData.Length != 4)
        {
            Debug.Log("Misforned input on line " + LineIndex.ToString());

        }
        /*
        else
        {
            Debug.Log(QuatData[0] + ", " + QuatData[1] + ", " + QuatData[2] + ", " + QuatData[3]);

        }
        */

        float.TryParse(QuatData[0], out QuatX);
        float.TryParse(QuatData[1], out QuatY);
        float.TryParse(QuatData[2], out QuatZ);
        float.TryParse(QuatData[3], out QuatW);

        Quaternion rotate = new Quaternion(QuatX, QuatY, QuatZ, QuatW);

        //Apply the transform
        this.transform.rotation = rotate;

        LineIndex++;
        //Repeat the movement
        if (LineIndex >= QuatTestLines.Length)
        {
            LineIndex = 0;
        }

    }

    void WithAccel()
    {

        string WholeLine = QuatTestLines[LineIndex];

        string[] QAData = WholeLine.Split(new[] { ',' });

        if (QAData.Length !=7)
        {
            Debug.Log("Misforned input on line " + LineIndex.ToString());
        }
        /*
        else
        {
            Debug.Log(QAData[0] + ", " + QAData[1] + ", " + QAData[2] + ", " + QAData[3] + ", " + 
                      QAData[4] + ", " + QAData[5] + ", " + QAData[6]);
        }
        */

        float.TryParse(QAData[0], out QuatX);
        float.TryParse(QAData[1], out QuatY);
        float.TryParse(QAData[2], out QuatZ);
        float.TryParse(QAData[3], out QuatW);

        float.TryParse(QAData[4], out AccelX);
        float.TryParse(QAData[5], out AccelY);
        float.TryParse(QAData[6], out AccelZ);

        SpeedX += AccelX;
        SpeedY += AccelY;
        SpeedZ += AccelZ;

        Quaternion rotate = new Quaternion(QuatX, QuatY, QuatZ, QuatW);

        //Apply the transform
        this.transform.Translate(SpeedX * Time.deltaTime, SpeedZ * Time.deltaTime, SpeedY * Time.deltaTime, Space.Self);
        this.transform.rotation = rotate;

        LineIndex++;
        //Repeat the movement
        if (LineIndex >= QuatTestLines.Length)
        {
            LineIndex = 0;
        }

    }

    void QuatTests()
    {

        int index = 0;
        foreach (string line in QuatTestLines)
        {

            string WholeLine = line;

            string[] QuatData = WholeLine.Split(new[] { ',' });

            if (QuatData.Length != 4) {
                Debug.Log("Misforned input on line " + index.ToString());
            }
            else
            {
                Debug.Log(QuatData[0]+ ", " + QuatData[1] + ", " + QuatData[2] + ", " + QuatData[3]);

            }

            index++;

            
            float.TryParse(QuatData[0], out QuatX);
            float.TryParse(QuatData[1], out QuatY);
            float.TryParse(QuatData[2], out QuatZ);
            float.TryParse(QuatData[3], out QuatW);

            Quaternion rotate = new Quaternion(QuatX,QuatY,QuatZ,QuatW);

            this.transform.rotation = rotate;         

        }

    }
    
    /*
    void Update()
    {

        //如果按下W或上方向键
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            //以MoveSpeed的速度向正前方移动
            this.transform.Translate(Vector3.forward * MoveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            this.transform.Translate(Vector3.back * MoveSpeed * Time.deltaTime);
        }

        //如果按下A或左方向键
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            //以RotateSpeed为速度向左旋转
            this.transform.Rotate(Vector3.down * RotateSpeed);
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            this.transform.Rotate(Vector3.up * RotateSpeed);
        }
        if (Input.GetKey(KeyCode.R))
        {
            this.transform.rotation = Quaternion.identity;
        }
        if (Input.GetKey(KeyCode.X))
        {
            Quaternion rotate = Random.rotation;
            this.transform.rotation = this.transform.rotation * rotate;
        }
        if (Input.GetKey(KeyCode.Z))
        {
            Quaternion rotate = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
            this.transform.rotation = rotate;
            //this.transform.rotation = this.transform.rotation * rotate;
        }
    }
    */

}