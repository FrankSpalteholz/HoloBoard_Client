using System.Collections;
using UnityEngine;
using System;



//iPhone 7
//FRONT CAMERA
//Resolution: 7MP
//Sensor: 5,05 x 3,75mm Sony Exmor RS(1.0 µm, 1/?"
//Focal Length: 2.87mm (32mm eff)
//Resolution: 3088x2320 px
//Max Aperture: F/2.2

//iPhone 11 Pro
//FRONT CAMERA
//12MP 23mm-equivalent in landscape
//7MP 30mm-equivalent in portrait
//4032x3024 pixels 

//focalLength: (978.9, 978.9) principalPoint: (718.1, 539.9) resolution: (1440, 1080)

//Sensor: 4mm x 3mm (1.0 µm, 1/3.6"
//Focal Length: 3.622mm (30mm eff)

//Resolution: 1440x1080 px

//Max Aperture:ƒ/2.0-2.2



public class DebugOutputGUI : MonoBehaviour
{

    public String FPVCamTag = "FPVRenderCam";

    GameObject fpvCamObject;

    //public GameObject TrackerManager;

    int CropImagePosXRT;
    int CropImagePosYRT;

    int CropImagePosXLF;
    int CropImagePosYLF;

    float IPD;

    Vector2 arCamFocalLength = new Vector2(978.9f, 978.9f);
    Vector2 arCamPrinciplePoint = new Vector2(718.1f, 539.9f);
    Vector2 arCamResolution = new Vector2(1440, 1080);

    Vector3 HeadPos;
    int HeadScreenPosX;
    int HeadScreenPosY;

    Quaternion HeadRot;

    Vector3 EyeToCamPosition;
    Vector3 EyeToCamOrient;

    DateTime captureBegin;
    DateTime currentTime;
    long elapsedTicks;
    TimeSpan elapsedSpan;
    float fps;

    private String OutputLog = "";

    private void Awake()
    {
        //TrackerManager = GameObject.FindGameObjectWithTag("TrackerManager");
        fpvCamObject = GameObject.FindGameObjectWithTag(FPVCamTag);
    }

    private void Start()
    {
        StartCoroutine(RecalculateFPS());
        Debug.Log("[DEBUG] FPV Cam Specs" + fpvCamObject.transform.position.ToString() + "\n");
    }

    private void Update()
    {
        currentTime = DateTime.Now;
        elapsedTicks = currentTime.Ticks - captureBegin.Ticks;
        elapsedSpan = new TimeSpan(elapsedTicks);

        //Debug.Log("[DEBUG] Cam " + fpvCamObject.transform.position.ToString() +  "\n");

    }

    private IEnumerator RecalculateFPS()
    {

        while (true)
        {
            fps = 1 / Time.deltaTime;
            yield return new WaitForSeconds(1);
        }
    }

    protected void OnGUI()
    {

        OutputLog = "" + "" + "" +
                    String.Format("{0:0}", DateTime.Now.Hour + "::" +
                    String.Format("{0:0}", DateTime.Now.Minute) + "::" +
                    String.Format("{0:00}", DateTime.Now.Second) + "::" +
                    String.Format("{0:0.###}", elapsedSpan.TotalSeconds) + "::" +
                    String.Format("{0:0.###}", elapsedTicks) + "__" +
                    Time.frameCount) + "__" +
                    //String.Format("{0:0.###}", fpvCamObject.transform.position.x) + "::" +
                    //String.Format("{0:0.###}", fpvCamObject.transform.position.y) + "::" +
                    //String.Format("{0:0.###}", fpvCamObject.transform.position.z) +
                    //"__" +
                    String.Format("{0:0.###}", Input.gyro.attitude.x) + "::" +
                    String.Format("{0:0.###}", Input.gyro.attitude.y) + "::" +
                    String.Format("{0:0.###}", Input.gyro.attitude.z) + "::" +
                    String.Format("{0:0.###}", Input.gyro.attitude.w);

        int fontSizeScalar = 80;

        GUI.skin.label.fontSize = Screen.width / fontSizeScalar;
        GUI.skin.label.normal.textColor = Color.white;

        GUILayout.Label("\n\n\n");
        GUILayout.Label("   TimeStamp: [" + String.Format("{0:00}", DateTime.Now.Hour) +
                                    ":" + String.Format("{0:00}", DateTime.Now.Minute) +
                                    ":" + String.Format("{0:00}", DateTime.Now.Second) + "]");
        //":" + String.Format("{0:0.###}", elapsedSpan.TotalSeconds));

        //":" + String.Format("{0:#}", elapsedTicks) + "]");

        GUILayout.Label("   FrameCount: " + Time.frameCount);
        GUILayout.Label("   FrameRate: " + String.Format("{0:0.#}", fps));
        //GUILayout.Label("   Orientation: " + Screen.orientation);
        //GUILayout.Label("   RectPosRT: " + CropImagePosXRT + ":" + CropImagePosYRT);
        //GUILayout.Label("   RectPosLF: " + CropImagePosXLF + ":" + CropImagePosYLF);
        //GUILayout.Label("   IPD: " + String.Format("{0:0.###}", IPD * 100));
       // GUILayout.Label("   //////////////////////////////////////////////////");
        // GUILayout.Label("   Gyro Quaternions: ");
        // GUILayout.Label("   x: " + String.Format("{0:0.###}", Input.gyro.attitude.x));
        // GUILayout.Label("   y: " + String.Format("{0:0.###}", Input.gyro.attitude.y));
        // GUILayout.Label("   z: " + String.Format("{0:0.###}", Input.gyro.attitude.z));
        // GUILayout.Label("   w: " + String.Format("{0:0.###}", Input.gyro.attitude.w));
        // GUILayout.Label("   Gyro Euler: " + Input.gyro.attitude.eulerAngles.ToString());
        // //GUILayout.Label(" Cam Pos: ");
        // //GUILayout.Label(" x: " + String.Format("{0:0.###}", fpvCamObject.transform.position.x) +
        // //                " y: " + String.Format("{0:0.###}", fpvCamObject.transform.position.y) +
        // //                " z: " + String.Format("{0:0.###}", fpvCamObject.transform.position.z));
        // GUILayout.Label("   Cam Rot: ");
        // GUILayout.Label("   x: " + String.Format("{0:0.###}", fpvCamObject.transform.rotation.x) +
        //                 "   y: " + String.Format("{0:0.###}", fpvCamObject.transform.rotation.y) +
        //                 "   z: " + String.Format("{0:0.###}", fpvCamObject.transform.rotation.z) +
        //                 "   w: " + String.Format("{0:0.###}", fpvCamObject.transform.rotation.w));
        // GUILayout.Label("   //////////////////////////////////////////////////");
        // GUILayout.Label("   Head Pos: ");
        // GUILayout.Label("   x: " + String.Format("{0:0.###}", HeadPos.x) +
        //                 "   y: " + String.Format("{0:0.###}", HeadPos.y) +
        //                 "   z: " + String.Format("{0:0.###}", HeadPos.z));
        // GUILayout.Label("   Head ScreenPos: " + HeadScreenPosX + ":" + HeadScreenPosY);

        //GUILayout.Label(" Head Rot: ");
        //GUILayout.Label(" x: " + String.Format("{0:0.###}", HeadRot.x));
        //GUILayout.Label(" y: " + String.Format("{0:0.###}", HeadRot.y));
        //GUILayout.Label(" z: " + String.Format("{0:0.###}", HeadRot.z));
        //GUILayout.Label(" w: " + String.Format("{0:0.###}", HeadRot.w));
        //GUILayout.Label("   //////////////////////////////////////////////////");
        //GUILayout.Label("   EyeTrackR Pos: " + EyeRightTrackPosX + ":" + EyeRightTrackPosY);
        //GUILayout.Label("   EyeTrackL Pos: " + EyeLeftTrackPosX + ":" + EyeLeftTrackPosY);

        //Debug.Log(OutputLog + "\n");

    }


}
