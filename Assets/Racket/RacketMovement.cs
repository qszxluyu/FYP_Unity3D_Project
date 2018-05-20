using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class RacketMovement : MonoBehaviour {

    //Use for original code
    //定义移动的速度
    //public float MoveSpeed = 5f;
    //定义旋转的速度
    //public float RotateSpeed = 5f;

    public float QuatX, QuatY, QuatZ, QuatW;

    public float AccelX, AccelY, AccelZ;

    public float SpeedX, SpeedY, SpeedZ;

    private string[] QuatTestLines;

    private int LineIndex = 0;

    private Vector3 OriginalPosition;

    //Parameters used for Average
    public bool UseAvgAccel = false;

    public int BufferSize = 8;

    public float[] BufferAccelX, BufferAccelY, BufferAccelZ;

    private int BufferPointer;

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

        //RealTimeSerial();

        RealTimeSerialWithAvg();

    }

    private void FixedUpdate()
    {

        //WithoutAccel();

        //WithAccel();

        //RealTimeSerial();

        // run factory self test and calibration routine
        if (Input.GetKey(KeyCode.T))
        {

            GameObject.Find("RacketPviot").GetComponent<SerialController>().SendSerialMessage("t");

            ResetParameters();

        }

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

        float.TryParse(QAData[0], out QuatX);
        float.TryParse(QAData[1], out QuatY);
        float.TryParse(QAData[2], out QuatZ);
        float.TryParse(QAData[3], out QuatW);

        float.TryParse(QAData[4], out AccelX);
        float.TryParse(QAData[5], out AccelY);
        float.TryParse(QAData[6], out AccelZ);

        BufferAccelX[BufferPointer] = AccelX;
        BufferAccelY[BufferPointer] = AccelY;
        BufferAccelZ[BufferPointer] = AccelZ;

        //Update the average
        SumAccelX = AvgAccelX * (BufferSize - 1) + AccelX;
        AvgAccelX = SumAccelX / BufferSize;
        SumAccelY = AvgAccelY * (BufferSize - 1) + AccelY;
        AvgAccelY = SumAccelY / BufferSize;
        SumAccelZ = AvgAccelZ * (BufferSize - 1) + AccelZ;
        AvgAccelZ = SumAccelZ / BufferSize;

        BufferPointer++;

        if (BufferPointer>=BufferSize)
        {
            BufferPointer -= BufferSize;
        }

        /*
        //Use the zero mean version of the accelation
        SpeedX += (AccelX - AvgAccelX) * Time.deltaTime;
        SpeedY += (AccelY - AvgAccelY) * Time.deltaTime;
        SpeedZ += (AccelZ - AvgAccelZ) * Time.deltaTime;
        */

        
        //Original accelation
        SpeedX += AvgAccelX * Time.deltaTime;
        SpeedY += AvgAccelY * Time.deltaTime;
        SpeedZ += AvgAccelZ * Time.deltaTime;
        

        //Quaternion rotate = new Quaternion(QuatX, QuatY, QuatZ, QuatW);
        Quaternion rotate = new Quaternion(QuatY, QuatX, QuatZ, QuatW);

        //Apply the transform
        //this.transform.Translate(SpeedX * Time.deltaTime, SpeedY* Time.deltaTime, SpeedZ * Time.deltaTime, Space.Self);
        this.transform.Translate(SpeedY * Time.deltaTime, SpeedX * Time.deltaTime, SpeedZ * Time.deltaTime, Space.Self);
        //this.transform.Translate(SpeedX, SpeedY, SpeedZ, Space.Self);
        this.transform.rotation = rotate;

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

        float.TryParse(QAData[0], out QuatX);
        float.TryParse(QAData[1], out QuatY);
        float.TryParse(QAData[2], out QuatZ);
        float.TryParse(QAData[3], out QuatW);

        float.TryParse(QAData[4], out AccelX);
        float.TryParse(QAData[5], out AccelY);
        float.TryParse(QAData[6], out AccelZ);

        SpeedX += AccelX * Time.deltaTime;
        SpeedY += AccelY * Time.deltaTime;
        SpeedZ += AccelZ * Time.deltaTime;

        Quaternion rotate = new Quaternion(QuatX, QuatY, QuatZ, QuatW);

        //Apply the transform
        this.transform.Translate(SpeedX * Time.deltaTime, SpeedZ * Time.deltaTime, SpeedY * Time.deltaTime, Space.Self);
        this.transform.rotation = rotate;

    }

    void ResetParameters()
    {

        this.transform.position = OriginalPosition;

        SpeedX = 0;
        SpeedY = 0;
        SpeedZ = 0;

        if (UseAvgAccel) {

            SumAccelX = 0;
            SumAccelY = 0;
            SumAccelZ = 0;
            BufferAccelX = new float[BufferSize];
            BufferAccelY = new float[BufferSize];
            BufferAccelZ = new float[BufferSize];
            BufferPointer = 0;


            //Initiate the Accel Buffers   
            int IniIndex = 0;

            while (IniIndex < BufferSize)
            {

                string WholeLine = GameObject.Find("RacketPviot").GetComponent<SerialController>().ReadSerialMessage();

                string[] QAData = WholeLine.Split(new[] { ',' });

                if (QAData.Length == 7)
                {
                    IniIndex++;

                    float.TryParse(QAData[4], out BufferAccelX[IniIndex]);
                    float.TryParse(QAData[5], out BufferAccelY[IniIndex]);
                    float.TryParse(QAData[6], out BufferAccelZ[IniIndex]);

                    SumAccelX += BufferAccelX[IniIndex];
                    SumAccelY += BufferAccelY[IniIndex];
                    SumAccelZ += BufferAccelZ[IniIndex];

                }

                AvgAccelX = SumAccelX / BufferSize;
                AvgAccelY = SumAccelY / BufferSize;
                AvgAccelZ = SumAccelZ / BufferSize;

            }

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