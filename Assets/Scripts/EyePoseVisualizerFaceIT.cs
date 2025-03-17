using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System;
using UnityEngine.UI;
#if UNITY_IOS && !UNITY_EDITOR
using UnityEngine.XR.ARKit;
#endif



[RequireComponent(typeof(ARFace))]
public class EyePoseVisualizer : MonoBehaviour
{

    [SerializeField]
    GameObject m_AxesEyesPrefab;

    [SerializeField]
    GameObject m_CubeEyesPrefab;

    [SerializeField]
    GameObject m_CubeEyeCornersPrefab;

    [SerializeField]
    GameObject m_AxesHeadPrefab;

    [SerializeField]
    GameObject m_EyeRectPrefabRT;

    [SerializeField]
    GameObject m_EyeRectPrefabLF;

    [SerializeField]
    GameObject m_EyeDummyGeoPrefab;

    [SerializeField]
    GameObject m_EyeTrackPrefab;

    [SerializeField]
    GameObject m_EyeBallHitPrefab;

    [SerializeField]
    GameObject m_EyeToScreenProjPrefab;

    public bool showFaceMesh;
    public bool showAxes;
    public float axesScale;

    public int calibrationTime;

    public GameObject EyeTrackPrefab
    {
        get => m_EyeTrackPrefab;
        set => m_EyeTrackPrefab = value;
    }

    public GameObject AxesPrefab
    {
        get => m_AxesHeadPrefab;
        set => m_AxesHeadPrefab = value;
    }

    public GameObject CubePrefab
    {
        get => m_CubeEyesPrefab;
        set => m_CubeEyesPrefab = value;
    }

    public GameObject EyePrefab
    {
        get => m_AxesEyesPrefab;
        set => m_AxesEyesPrefab = value;
    }

    public GameObject EyeCornerPrefab
    {
        get => m_CubeEyeCornersPrefab;
        set => m_CubeEyeCornersPrefab = value;
    }

    public GameObject EyeRectPrefabRT
    {
        get => m_EyeRectPrefabRT;
        set => m_EyeRectPrefabRT = value;
    }

    public GameObject EyeRectPrefabLF
    {
        get => m_EyeRectPrefabLF;
        set => m_EyeRectPrefabLF = value;
    }

    public GameObject EyeDummyGeoPrefab
    {
        get => m_EyeDummyGeoPrefab;
        set => m_EyeDummyGeoPrefab = value;
    }

    public GameObject EyeBallHitGeoPrefab
    {
        get => m_EyeBallHitPrefab;
        set => m_EyeBallHitPrefab = value;
    }

    public GameObject EyeToScreenrefab
    {
        get => m_EyeToScreenProjPrefab;
        set => m_EyeToScreenProjPrefab = value;
    }

    int m_HeadScreenPosX;
    int m_HeadScreenPosY;

    const int m_EyeCornerVertRT = 1193;
    const int m_EyeCornerVertLF = 1168;

    Quaternion m_HeadRotation;

    Vector3 m_EyeWorldPosVecLF;
    Vector3 m_EyeLocalPosVecLF;
    Vector3 m_EyeCornerVtxPosLF;
    Vector3 m_EyeToCornerVecLF;
    Vector3 m_EyeDotPosVecLF;
    int m_EyeTrackScreenPosXLF;
    int m_EyeTrackScreenPosYLF;

    Vector3 m_EyeWorldPosVecRT;
    Vector3 m_EyeLocalPosVecRT;
    Vector3 m_EyeCornerVtxPosRT;
    Vector3 m_EyeToCornerVecRT;
    Vector3 m_EyeDotPosVecRT;
    int m_EyeTrackScreenPosXRT;
    int m_EyeTrackScreenPosYRT;

    Vector3 m_EyeToCamOrientVecRT = new Vector3();
    Vector3 m_EyeToCamParallelVecRT = new Vector3();

    GameObject m_HeadAxesGObj;

    GameObject m_EyeGObjLF;
    GameObject m_EyeGObjRT;

    GameObject m_EyeCornerGObjLF;
    GameObject m_EyeCornerGObjRT;

    GameObject m_EyeDotGObjLF;
    GameObject m_EyeDotGObjRT;
    GameObject m_EyeDotMidGObjRT;

    GameObject m_EyeRectGObjLF;
    GameObject m_EyeRectGObjRT;

    GameObject m_EyeDummyGeoGObjRT;
    GameObject m_EyeDummyGeoGObjLF;

    GameObject m_EyeTrackGeoGObjRT;
    GameObject m_EyeTrackGeoGObjLF;

    GameObject m_EyeBallHitGObjRT;
    GameObject m_EyeBallHitGObjLF;

    GameObject m_EyeToScreenProjGObjRT;
    GameObject m_EyeToScreenProjGObjLF;

    GameObject m_EyeToScreenProjGObjMid;

    GameObject m_FPVCam;
    GameObject m_ProjOn3DScreenCubeRT;
    GameObject m_ProjOn3DScreenCubeLF;

    //Button m_ToggleEyeSettingsButton;
    bool m_IsEyeTransformLock = true;

    Quaternion m_EyeBallRotRT;
    Quaternion m_EyeBallRotLF;

    int m_ScreenPixelWidth;
    int m_ScreenPixelHeight;

    float m_ScreenMetricWidth;
    float m_ScreenMetricHeight;

    Vector2[] m_ScreenProjLFVecs = {new Vector2(0,0),
                                    new Vector2(0,0),
                                    new Vector2(0,0),
                                    new Vector2(0,0),
                                    new Vector2(0,0),
                                    new Vector2(0,0),
                                    new Vector2(0,0),
                                    new Vector2(0,0),
                                    new Vector2(0,0),
                                    new Vector2(0,0)};

    Vector2[] m_ScreenProjRTVecs = {new Vector2(0,0),
                                    new Vector2(0,0),
                                    new Vector2(0,0),
                                    new Vector2(0,0),
                                    new Vector2(0,0),
                                    new Vector2(0,0),
                                    new Vector2(0,0),
                                    new Vector2(0,0),
                                    new Vector2(0,0),
                                    new Vector2(0,0) };

    int m_ScreenProjVecCount = 5;

    public int m_CropSizeWidth = 300;
    public int m_CropSizeHeight = 300;

    public int m_TrackRectSize = 50;

    Vector2Int m_CropImgPosRT = new Vector2Int();
    Vector2Int m_CropImgPosLF = new Vector2Int();
    
    ARFace m_ARFace;
    Canvas m_Canvas;
    Camera m_MainCamera;

    GameObject m_EyeRectVisToggle;
    GameObject m_ExponentialsToggle;
    GameObject m_EyeDotRayToggle;
    GameObject m_EyeDotRayGO;

    private GameObject m_TrackerManager;

    void Awake()
    {
        
        m_ARFace = GetComponent<ARFace>();
        m_Canvas = FindObjectOfType<Canvas>();
        m_MainCamera = Camera.main;
        m_TrackerManager = GameObject.FindGameObjectWithTag("TrackerManager");

        m_ScreenPixelWidth = m_TrackerManager.GetComponent<SystemParameters>().ScreenPixelWidth;
        m_ScreenPixelHeight = m_TrackerManager.GetComponent<SystemParameters>().ScreenPixelHeight;

        m_ScreenMetricWidth = m_TrackerManager.GetComponent<SystemParameters>().ScreenPhysicalWidth;
        m_ScreenMetricHeight = m_TrackerManager.GetComponent<SystemParameters>().ScreenPhysicalHeight;

        m_FPVCam = GameObject.FindGameObjectWithTag("CamFPV");

        m_ProjOn3DScreenCubeRT = GameObject.FindGameObjectWithTag("CubeRT");
        m_ProjOn3DScreenCubeLF = GameObject.FindGameObjectWithTag("CubeLF");

        m_EyeRectVisToggle = GameObject.Find("TrackRectsVisToggle");
        m_ExponentialsToggle = GameObject.Find("ExponentialsToggle");

        m_EyeDotRayToggle = GameObject.Find("EyeRayVisToggle");
       

        

        //m_ToggleEyeSettingsButton = GameObject.Find("LockEyeSettingsButton").GetComponent<Button>();
        //m_ToggleEyeSettingsButton.onClick.AddListener(ToggleLockEyeTransforms);
    }

    void CreateEyeGameObjectsIfNecessary()
    {
        if (m_ARFace.transform != null && m_HeadAxesGObj == null)
            m_HeadAxesGObj = Instantiate(m_AxesHeadPrefab, m_ARFace.transform);

        if (m_ARFace.leftEye != null && m_EyeGObjLF == null)
            m_EyeGObjLF = Instantiate(m_AxesEyesPrefab);

        if (m_ARFace.rightEye != null && m_EyeGObjRT == null)
            m_EyeGObjRT = Instantiate(m_AxesEyesPrefab);


        if (m_ARFace.transform != null && m_EyeDummyGeoGObjRT == null)
            m_EyeDummyGeoGObjRT = Instantiate(m_EyeDummyGeoPrefab);

        if (m_ARFace.transform != null && m_EyeDummyGeoGObjLF == null)
            m_EyeDummyGeoGObjLF = Instantiate(m_EyeDummyGeoPrefab);


        if (m_ARFace.leftEye != null && m_EyeCornerGObjLF == null)
            m_EyeCornerGObjLF = Instantiate(m_CubeEyeCornersPrefab, m_EyeGObjLF.transform);

        if (m_ARFace.rightEye != null && m_EyeCornerGObjRT == null)
            m_EyeCornerGObjRT = Instantiate(m_CubeEyeCornersPrefab, m_EyeGObjRT.transform);



        if (m_ARFace.leftEye != null && m_EyeDotGObjLF == null)
            m_EyeDotGObjLF = Instantiate(m_CubeEyeCornersPrefab, m_EyeGObjLF.transform);

        if (m_ARFace.rightEye != null && m_EyeDotGObjRT == null)
            m_EyeDotGObjRT = Instantiate(m_CubeEyeCornersPrefab, m_EyeGObjRT.transform);

        if (m_ARFace.rightEye != null && m_EyeDotMidGObjRT == null)
            m_EyeDotMidGObjRT = Instantiate(m_CubeEyeCornersPrefab);



        if (m_ARFace.transform != null && m_EyeRectGObjRT == null)
            m_EyeRectGObjRT = Instantiate(m_EyeRectPrefabRT, m_Canvas.transform);

        if (m_ARFace.transform != null && m_EyeRectGObjLF == null)
            m_EyeRectGObjLF = Instantiate(m_EyeRectPrefabRT, m_Canvas.transform);



        if (m_ARFace.transform != null && m_EyeTrackGeoGObjLF == null)
            m_EyeTrackGeoGObjLF = Instantiate(m_EyeTrackPrefab, m_Canvas.transform);

        if (m_ARFace.transform != null && m_EyeTrackGeoGObjRT == null)
            m_EyeTrackGeoGObjRT = Instantiate(m_EyeTrackPrefab, m_Canvas.transform);

        if (m_ARFace.transform != null && m_EyeBallHitGObjRT == null)
            m_EyeBallHitGObjRT = Instantiate(m_EyeBallHitPrefab, m_Canvas.transform);

        if (m_ARFace.transform != null && m_EyeBallHitGObjLF == null)
            m_EyeBallHitGObjLF = Instantiate(m_EyeBallHitPrefab, m_Canvas.transform);

        if (m_ARFace.transform != null && m_EyeToScreenProjGObjRT == null)
            m_EyeToScreenProjGObjRT = Instantiate(m_EyeToScreenProjPrefab, m_Canvas.transform);

        if (m_ARFace.transform != null && m_EyeToScreenProjGObjLF == null)
            m_EyeToScreenProjGObjLF = Instantiate(m_EyeToScreenProjPrefab, m_Canvas.transform);

        if (m_ARFace.transform != null && m_EyeToScreenProjGObjMid == null)
            m_EyeToScreenProjGObjMid = Instantiate(m_EyeToScreenProjPrefab, m_Canvas.transform);

        


    }

    void SetVisible(bool visible)
    {

        if (m_HeadAxesGObj != null)
            m_HeadAxesGObj.SetActive(visible);

        if (m_EyeGObjLF != null)
            m_EyeGObjLF.SetActive(visible);

        if (m_EyeGObjRT != null)
            m_EyeGObjRT.SetActive(visible);

        if (m_EyeCornerGObjLF != null)
            m_EyeCornerGObjLF.SetActive(visible);

        if (m_EyeCornerGObjRT != null)
            m_EyeCornerGObjRT.SetActive(visible);

        if (m_EyeDotGObjLF != null)
            m_EyeDotGObjLF.SetActive(visible);

        if (m_EyeDotGObjRT != null)
            m_EyeDotGObjRT.SetActive(visible);

        if (m_EyeDotMidGObjRT != null)
            m_EyeDotMidGObjRT.SetActive(visible);


        if (m_EyeRectGObjRT != null)
            m_EyeRectGObjRT.SetActive(visible);

        if (m_EyeRectGObjLF != null)
            m_EyeRectGObjLF.SetActive(visible);

        if (m_EyeTrackGeoGObjLF != null)
            m_EyeTrackGeoGObjLF.SetActive(visible);

        if (m_EyeTrackGeoGObjRT != null)
            m_EyeTrackGeoGObjRT.SetActive(visible);


        if (m_EyeBallHitGObjLF != null)
            m_EyeBallHitGObjLF.SetActive(visible);

        if (m_EyeBallHitGObjRT != null)
            m_EyeBallHitGObjRT.SetActive(visible);


        if (m_EyeToScreenProjGObjRT != null)
            m_EyeToScreenProjGObjRT.SetActive(visible);

        if (m_EyeToScreenProjGObjLF != null)
            m_EyeToScreenProjGObjLF.SetActive(visible);

        if (m_EyeToScreenProjGObjMid != null)
            m_EyeToScreenProjGObjMid.SetActive(visible);

    }

    void OnEnable()
    {
        // var faceManager = FindObjectOfType<ARFaceManager>();
        // if (faceManager != null && faceManager.subsystem != null && faceManager.subsystem.SubsystemDescriptor.supportsEyeTracking)
        // {
        //     //m_FaceSubsystem = (XRFaceSubsystem)faceManager.subsystem;
        //     SetVisible((m_ARFace.trackingState == TrackingState.Tracking) && (ARSession.state > ARSessionState.Ready));
        //     m_ARFace.updated += OnUpdated;
        // }
        // else
        // {
        //     enabled = false;
        // }
        
    }

    void OnDisable()
    {
        m_ARFace.updated -= OnUpdated;
        SetVisible(false);
    }

    void OnUpdated(ARFaceUpdatedEventArgs eventArgs)
    {
        CreateEyeGameObjectsIfNecessary();      
        SetVisible((m_ARFace.trackingState == TrackingState.Tracking) &&
            (ARSession.state > ARSessionState.Ready));
    }

    private void Update()
    {
        Vector2 globalOffsetTranslate = new Vector2();
        Vector2 localOffsetTranslateRT = new Vector2();
        Vector2 localOffsetTranslateLF = new Vector2();

        globalOffsetTranslate.x = GameObject.Find("EyeOffsetXSlider").GetComponent<Slider>().value;
        globalOffsetTranslate.y = GameObject.Find("EyeOffsetYSlider").GetComponent<Slider>().value;

        localOffsetTranslateRT.x = GameObject.Find("EyeOffsetXSliderRT").GetComponent<Slider>().value;
        localOffsetTranslateRT.y = GameObject.Find("EyeOffsetYSliderRT").GetComponent<Slider>().value;

        localOffsetTranslateLF.x = GameObject.Find("EyeOffsetXSliderLF").GetComponent<Slider>().value;
        localOffsetTranslateLF.y = GameObject.Find("EyeOffsetYSliderLF").GetComponent<Slider>().value;


        CalcEyeSpaceModel(globalOffsetTranslate, localOffsetTranslateRT, localOffsetTranslateLF);

        CalcEyeHitTransforms();

        SetEyeGObjTransforms();

        SetCropRectTransform();

        SetTransformsOnSystemParameters();
    }

    private void CalcEyeSpaceModel(Vector2 globalOffset, Vector2 localOffsetRT, Vector2 localOffsetLF)
    {

        //Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(new Vector3(xRotation, yRotation, zRotation)), Vector3.one);
        //You can easily rotate a direction Vector by calling

        //matrix.MultiplyPoint(directionVector);
        //m_HeadRotation = m_ARFace.transform.rotation;

        m_EyeLocalPosVecLF = new Vector3(   m_ARFace.leftEye.localPosition.x + globalOffset.x + localOffsetLF.x,
                                            m_ARFace.leftEye.localPosition.y - globalOffset.y - localOffsetLF.y,
                                            m_ARFace.leftEye.localPosition.z);

        m_EyeLocalPosVecRT = new Vector3(   m_ARFace.rightEye.localPosition.x - globalOffset.x + localOffsetRT.x,
                                            m_ARFace.rightEye.localPosition.y - globalOffset.y - localOffsetRT.y,
                                            m_ARFace.rightEye.localPosition.z);


        m_EyeWorldPosVecLF = new Vector3(   m_ARFace.leftEye.position.x + globalOffset.x + localOffsetLF.x,
                                            m_ARFace.leftEye.position.y - globalOffset.y - localOffsetLF.y,
                                            m_ARFace.leftEye.position.z);

        m_EyeWorldPosVecRT = new Vector3(   m_ARFace.rightEye.position.x - globalOffset.x + localOffsetRT.x,
                                            m_ARFace.rightEye.position.y - globalOffset.y - localOffsetRT.y,
                                            m_ARFace.rightEye.position.z);


        m_EyeCornerVtxPosRT = transform.TransformPoint(m_ARFace.vertices[m_EyeCornerVertRT]);
        m_EyeCornerVtxPosLF = transform.TransformPoint(m_ARFace.vertices[m_EyeCornerVertLF]); 

        m_EyeToCornerVecLF = m_EyeCornerVtxPosLF - m_EyeWorldPosVecLF;
        m_EyeToCornerVecRT = m_EyeCornerVtxPosRT - m_EyeWorldPosVecRT;


        m_EyeToCornerVecRT.y = 0.0f;
        m_EyeToCornerVecLF.y = 0.0f;

        m_EyeDotPosVecRT = m_HeadRotation * new Vector3(0.0f, 0.0f, -m_EyeToCornerVecRT.z) + m_EyeWorldPosVecRT;
        m_EyeDotPosVecLF = m_HeadRotation * new Vector3(0.0f, 0.0f, -m_EyeToCornerVecLF.z) + m_EyeWorldPosVecLF;

    }

    private void SetCropRectTransform()
    {

        m_EyeRectGObjRT.GetComponent<Image>().enabled =
        GameObject.Find("Rect Vis Toggle").GetComponent<Toggle>().isOn;
        m_EyeRectGObjLF.GetComponent<Image>().enabled =
        GameObject.Find("Rect Vis Toggle").GetComponent<Toggle>().isOn;

        var fixationInViewSpaceRT = m_MainCamera.WorldToScreenPoint(m_EyeDotPosVecRT);
        var fixationInViewSpaceLF = m_MainCamera.WorldToScreenPoint(m_EyeDotPosVecLF);

        m_CropImgPosRT.x = Convert.ToInt32(fixationInViewSpaceRT.x - (m_CropSizeWidth / 2));
        m_CropImgPosRT.y = Convert.ToInt32(fixationInViewSpaceRT.y + (m_CropSizeHeight / 2));

        m_CropImgPosLF.x = Convert.ToInt32(fixationInViewSpaceLF.x - (m_CropSizeWidth / 2));
        m_CropImgPosLF.y = Convert.ToInt32(fixationInViewSpaceLF.y + (m_CropSizeHeight / 2));


        m_EyeRectGObjRT.GetComponent<RectTransform>().anchoredPosition3D =
        new Vector3(m_CropImgPosRT.x, m_CropImgPosRT.y, 0.01f);

        m_EyeRectGObjLF.GetComponent<RectTransform>().anchoredPosition3D =
        new Vector3(m_CropImgPosLF.x, m_CropImgPosLF.y, 0.01f);


        RectTransform rectTransformRT = m_EyeRectGObjRT.GetComponent<RectTransform>();
        RectTransform rectTransformLF = m_EyeRectGObjLF.GetComponent<RectTransform>();

        rectTransformRT.sizeDelta = new Vector2(m_CropSizeWidth, m_CropSizeHeight);
        rectTransformLF.sizeDelta = new Vector2(m_CropSizeWidth, m_CropSizeHeight);

        m_EyeTrackScreenPosXRT = m_TrackerManager.GetComponent<SystemParameters>().GetEyeRightTrackPositionX();
        m_EyeTrackScreenPosYRT = m_TrackerManager.GetComponent<SystemParameters>().GetEyeRightTrackPositionY();

        m_EyeTrackScreenPosXLF = m_TrackerManager.GetComponent<SystemParameters>().GetEyeLeftTrackPositionX();
        m_EyeTrackScreenPosYLF = m_TrackerManager.GetComponent<SystemParameters>().GetEyeLeftTrackPositionY();


        m_EyeTrackScreenPosXRT = Convert.ToInt32(fixationInViewSpaceRT.x + (m_EyeTrackScreenPosXRT - 150));
        m_EyeTrackScreenPosYRT = Convert.ToInt32(fixationInViewSpaceRT.y - (m_EyeTrackScreenPosYRT - 150));

        m_EyeTrackScreenPosXLF = Convert.ToInt32(fixationInViewSpaceLF.x + (m_EyeTrackScreenPosXLF - 150));
        m_EyeTrackScreenPosYLF = Convert.ToInt32(fixationInViewSpaceLF.y - (m_EyeTrackScreenPosYLF - 150));


        m_EyeTrackGeoGObjRT.GetComponent<RectTransform>().anchoredPosition3D =
        new Vector3(m_EyeTrackScreenPosXRT, m_EyeTrackScreenPosYRT, 0.0f);

        m_EyeTrackGeoGObjLF.GetComponent<RectTransform>().anchoredPosition3D =
        new Vector3(m_EyeTrackScreenPosXLF, m_EyeTrackScreenPosYLF, 0.0f);



    }

    private void CalcEyeHitTransforms()
    {

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        bool isEyeTrack = GameObject.Find("EyeTrackToggle").GetComponent<Toggle>().isOn;


        float tauXRot = GameObject.Find("EyeExpTauXRotSlider").GetComponent<Slider>().value;
        float kappaXRot = GameObject.Find("EyeExpKappaXRotSlider").GetComponent<Slider>().value;

        float tauYRot = GameObject.Find("EyeExpTauYRotSlider").GetComponent<Slider>().value;
        float kappaYRot = GameObject.Find("EyeExpKappaYRotSlider").GetComponent<Slider>().value;

        //Vector3 eyeMidPosVec = (m_EyeDotGObjLF.transform.position - m_EyeDotGObjRT.transform.position) / 2 + m_EyeDotGObjRT.transform.position;
        //m_EyeDotMidGObjRT.transform.position = eyeMidPosVec;


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //RIGHT

        Vector3 screenToEyeHitRT = GetRayCastHitPointFromCam(new Vector3(m_EyeTrackScreenPosXRT,
                                                                            m_EyeTrackScreenPosYRT, 0.0f));
        Vector3 eyeBallToHitVecRT = m_EyeWorldPosVecRT - screenToEyeHitRT;


        if (m_ExponentialsToggle.GetComponent<Toggle>().isOn)
        {
            eyeBallToHitVecRT = CalcExpRotOffset('Y', eyeBallToHitVecRT, m_EyeWorldPosVecRT, tauXRot, tauYRot, kappaXRot, kappaYRot);
            eyeBallToHitVecRT = CalcExpRotOffset('X', eyeBallToHitVecRT, m_EyeWorldPosVecRT, tauXRot, tauYRot, kappaXRot, kappaYRot);
        }

        m_EyeBallRotRT = Quaternion.LookRotation(eyeBallToHitVecRT.normalized, Vector3.up);

        Vector3 eyeToScreenHitRT = new Vector3();
        if (isEyeTrack)
            eyeToScreenHitRT = GetRayCastHitPoint(screenToEyeHitRT, -eyeBallToHitVecRT);
        else
            eyeToScreenHitRT = GetRayCastHitPoint(m_EyeWorldPosVecRT, new Vector3(0, 0, -1));

        Vector2Int screenProjMultRT = GetEyeToScreenProjVec(1, eyeToScreenHitRT, 0.005f);

        Vector2 avrgLinPosRT = CalcAvrgPosEyeToScreenRect(m_ScreenProjRTVecs, screenProjMultRT);

        SetEyeToScreenRectPos(m_EyeToScreenProjGObjRT, avrgLinPosRT,
            new Vector2(80, 80), new Color32(255, 120, 55, 255));

        m_EyeToScreenProjGObjRT.SetActive(m_EyeRectVisToggle.GetComponent<Toggle>().isOn);

        //m_ProjOn3DScreenCubeRT.transform.position = new Vector3(-eyeToScreenHitRT.x,
        //    eyeToScreenHitRT.y, eyeToScreenHitRT.z);


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // LEFT

        Vector3 screenToEyeHitLF = GetRayCastHitPointFromCam(new Vector3(m_EyeTrackScreenPosXLF,
                                                                            m_EyeTrackScreenPosYLF, 0.0f));
        Vector3 eyeBallToHitVecLF = m_EyeWorldPosVecLF - screenToEyeHitLF;

        if (m_ExponentialsToggle.GetComponent<Toggle>().isOn)
        {
            eyeBallToHitVecLF = CalcExpRotOffset('Y', eyeBallToHitVecLF, m_EyeWorldPosVecLF, tauXRot, tauYRot, kappaXRot, kappaYRot);
            eyeBallToHitVecLF = CalcExpRotOffset('X', eyeBallToHitVecLF, m_EyeWorldPosVecLF, tauXRot, tauYRot, kappaXRot, kappaYRot);
        }

        m_EyeBallRotLF = Quaternion.LookRotation(eyeBallToHitVecLF.normalized, Vector3.up);



        Vector3 eyeToScreenHitLF = new Vector3();
        if (isEyeTrack)
            eyeToScreenHitLF = GetRayCastHitPoint(screenToEyeHitLF, -eyeBallToHitVecLF);
        else
            eyeToScreenHitLF = GetRayCastHitPoint(m_EyeWorldPosVecRT, new Vector3(0, 0, -1));

        m_ProjOn3DScreenCubeLF.transform.position = new Vector3(-eyeToScreenHitLF.x,
            eyeToScreenHitLF.y, eyeToScreenHitLF.z);


        Vector2Int screenProjMultLF = GetEyeToScreenProjVec(0, eyeToScreenHitLF, 0.005f);

        Vector2 avrgLinPosLF = CalcAvrgPosEyeToScreenRect(m_ScreenProjLFVecs, screenProjMultLF);

        SetEyeToScreenRectPos(m_EyeToScreenProjGObjLF, avrgLinPosLF,
            new Vector2(80, 80), new Color32(50, 185, 255, 255));


        m_EyeToScreenProjGObjLF.SetActive(m_EyeRectVisToggle.GetComponent<Toggle>().isOn);


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // MID

        Vector2 avrgLinMidPos = CalcAvrgPosEyeToScreenRectMid(screenProjMultRT, screenProjMultLF);

        SetEyeToScreenRectPos(m_EyeToScreenProjGObjMid, avrgLinMidPos,
            new Vector2(m_CropSizeWidth / (float)2.5, m_CropSizeHeight / (float)2.5), new Color(255, 255, 255));

        m_EyeToScreenProjGObjMid.SetActive(m_EyeRectVisToggle.GetComponent<Toggle>().isOn);



    }

    private Vector3 CalcExpRotOffset(char dirAngle, Vector3 eyeBallToHitVec, Vector3 eyePos, float tauXRot, float tauYRot, float kappaXRot, float kappaYRot)
    {
        Quaternion eyeBallRot = Quaternion.LookRotation(eyeBallToHitVec.normalized, Vector3.up);

        Vector3 newHitVec = new Vector3();


        if (dirAngle == 'X')
        {

            ///////////////////////////////////////////////////////////////////////////////////
            /// EXPONENTIALS X ROT

            float angle = Vector3.Angle(new Vector3(0, eyeBallToHitVec.y, -eyeBallToHitVec.z),
                                            new Vector3(0, 0, -eyePos.z));

            float eyeDistNormalizeUP = eyePos.y;
            float eyeDistNormalizeBOT = -m_ScreenMetricHeight - eyePos.y;

            double eyeAngleNormalizeUP = Mathf.Rad2Deg * Mathf.Atan2(eyeDistNormalizeUP, eyePos.z);
            double eyeAngleNormalizeBOT = Mathf.Rad2Deg * Mathf.Atan2(eyeDistNormalizeBOT, eyePos.z);

            double eyeAngleNormalize = Mathf.Abs(angle / (Mathf.Abs( ((float)eyeAngleNormalizeUP) + Mathf.Abs((float)eyeAngleNormalizeBOT) )));

            double omega = Math.Exp(tauXRot * eyeAngleNormalize - kappaXRot) + 1;

            if (eyeBallRot.eulerAngles.x > 270)
            {
                float angleExp_x = angle * (float)omega;
                newHitVec = Quaternion.Euler(-angleExp_x + angle, 0, 0) * eyeBallToHitVec;

            }
            if (eyeBallRot.eulerAngles.x < 90)
            {
                float angleExp_x = angle * (float)omega;
                newHitVec = Quaternion.Euler(angleExp_x - angle, 0, 0) * eyeBallToHitVec;
            }

        }

        if (dirAngle == 'Y')
        {

            ///////////////////////////////////////////////////////////////////////////////////
            /// EXPONENTIALS Y ROT

            float angle = Vector3.Angle(new Vector3(eyeBallToHitVec.x, 0, -eyeBallToHitVec.z),
                                new Vector3(0, 0, -eyePos.z));

            float eyeDistNormalizeRT = (m_ScreenMetricWidth / 2) - Mathf.Abs(eyePos.x);
            float eyeDistNormalizeLF = m_ScreenMetricWidth - ((m_ScreenMetricWidth / 2) - Mathf.Abs(eyePos.x));

            double eyeAngleNormalizeLF = Mathf.Rad2Deg * Mathf.Atan2(eyeDistNormalizeLF, eyePos.z);
            double eyeAngleNormalizeRT = Mathf.Rad2Deg * Mathf.Atan2(eyeDistNormalizeRT, eyePos.z);

            double eyeAngleNormalize = Mathf.Abs(angle / (Mathf.Abs(((float)eyeAngleNormalizeLF) + Mathf.Abs((float)eyeAngleNormalizeRT))));

            double omega = Math.Exp(tauYRot * eyeAngleNormalize - kappaYRot) + 1;

            if (eyeBallRot.eulerAngles.y > 270)
            {
                float angleExp_y = angle * (float)omega;
                newHitVec = Quaternion.Euler(0, -angleExp_y + angle, 0) * eyeBallToHitVec;

            }
            if (eyeBallRot.eulerAngles.y < 90)
            {
                float angleExp_y = angle * (float)omega;
                newHitVec = Quaternion.Euler(0, (angleExp_y - angle), 0) * eyeBallToHitVec;
            }

        } 


        return newHitVec;

    }

    private void ToggleLockEyeTransforms()
    {
        if (!m_IsEyeTransformLock)
            m_IsEyeTransformLock = true;
        else
            m_IsEyeTransformLock = false;
    }

    private Vector2 CalcAvrgPosEyeToScreenRectMid(Vector2Int rectScreenPosRT, Vector2Int rectScreenPosLF)
    {
        Vector2 avrgVec = new Vector2();

        for (int i = 0; i < m_ScreenProjVecCount - 1; i++)
        {
            m_ScreenProjLFVecs[i] = m_ScreenProjLFVecs[i + 1];
            m_ScreenProjRTVecs[i] = m_ScreenProjRTVecs[i + 1];

        }

        m_ScreenProjLFVecs[m_ScreenProjVecCount - 1] = rectScreenPosLF;
        m_ScreenProjRTVecs[m_ScreenProjVecCount - 1] = rectScreenPosRT; 

        for (int i = 0; i < m_ScreenProjVecCount; i++)
        {
            avrgVec.x += m_ScreenProjLFVecs[i].x + m_ScreenProjRTVecs[i].x;
            avrgVec.y += m_ScreenProjLFVecs[i].y + m_ScreenProjRTVecs[i].y;
        }

        avrgVec.x /= m_ScreenProjVecCount * 2;
        avrgVec.y /= m_ScreenProjVecCount * 2;


        return avrgVec;
    }

    private Vector2 CalcAvrgPosEyeToScreenRect(Vector2[] screenProjVec, Vector2Int rectScreenPos)
    {
        Vector2 avrgVec = new Vector2();

        for (int i = 0; i < m_ScreenProjVecCount - 1; i++)
            screenProjVec[i] = screenProjVec[i + 1];

        screenProjVec[m_ScreenProjVecCount - 1] = rectScreenPos;

        for (int i = 0; i < m_ScreenProjVecCount; i++)
        {
            avrgVec.x += screenProjVec[i].x;
            avrgVec.y += screenProjVec[i].y;
        }

        avrgVec.x /= m_ScreenProjVecCount;
        avrgVec.y /= m_ScreenProjVecCount;


        return avrgVec;
    }

    private void SetEyeToScreenRectPos(GameObject rectGameObj, Vector2 screenPos, Vector2 rectSize, Color color)
    {

        rectGameObj.GetComponent<RectTransform>().anchoredPosition3D =
            new Vector3(screenPos.x, screenPos.y, 0.0f);

        RectTransform rectTransformLF = rectGameObj.GetComponent<RectTransform>();

        rectTransformLF.sizeDelta = new Vector2(rectSize.x, rectSize.y);

        rectGameObj.GetComponent<Image>().color = color;


    }

    private void SetEyeGObjTransforms()
    {

        var headInViewSpace = m_MainCamera.WorldToScreenPoint(m_ARFace.transform.position);


        m_HeadScreenPosX = Convert.ToInt32(headInViewSpace.x);
        m_HeadScreenPosY = Convert.ToInt32(headInViewSpace.y);



        m_EyeGObjRT.transform.rotation = m_ARFace.transform.rotation;
        m_EyeGObjRT.transform.position = m_EyeWorldPosVecRT;

        m_EyeCornerGObjRT.transform.localPosition = m_EyeToCornerVecRT;

        m_EyeCornerGObjRT.transform.rotation = m_ARFace.transform.rotation;

        m_EyeCornerGObjRT.transform.localScale = new Vector3(0,0,0);


        m_EyeDotGObjRT.transform.localPosition = new Vector3(0.0f,
            m_EyeToCornerVecRT.y,
            m_EyeToCornerVecRT.z);

        m_EyeDotGObjRT.transform.rotation = m_ARFace.transform.rotation;

        m_EyeDotGObjRT.transform.localScale = new Vector3(0,0,0);


        m_EyeBallHitGObjRT.transform.position = m_EyeWorldPosVecRT;

        m_EyeDummyGeoGObjRT.transform.position = m_EyeGObjRT.transform.position;
        m_EyeDummyGeoGObjRT.transform.localRotation = m_EyeBallRotRT;


        m_EyeGObjLF.transform.position = m_EyeWorldPosVecLF;
        m_EyeGObjLF.transform.rotation = m_ARFace.transform.rotation;




        m_EyeCornerGObjLF.transform.localPosition = m_EyeToCornerVecLF;

        m_EyeCornerGObjLF.transform.rotation = m_ARFace.transform.rotation;

        m_EyeCornerGObjLF.transform.localScale = new Vector3(0,0,0);


        m_EyeDotGObjLF.transform.localPosition = new Vector3(0.0f,
            m_EyeToCornerVecLF.y,
            m_EyeToCornerVecLF.z);

        m_EyeDotGObjLF.transform.rotation = m_ARFace.transform.rotation;

        m_EyeDotGObjLF.transform.localScale = new Vector3(0, 0, 0);





        m_EyeDummyGeoGObjLF.transform.position = m_EyeGObjLF.transform.position;

        m_EyeBallHitGObjLF.transform.position = m_EyeWorldPosVecLF;

        m_EyeDummyGeoGObjLF.transform.localRotation = m_EyeBallRotLF;


        m_EyeDummyGeoGObjLF.transform.GetChild(1).GetComponent<MeshRenderer>().enabled = m_EyeDotRayToggle.GetComponent<Toggle>().isOn;
        m_EyeDummyGeoGObjRT.transform.GetChild(1).GetComponent<MeshRenderer>().enabled = m_EyeDotRayToggle.GetComponent<Toggle>().isOn;

       

    }

    private Vector2Int GetEyeToScreenProjVec(int side, Vector3 hitVec, float offsetY)
    {
        Vector2Int screenVec = new Vector2Int();
        
        double pixToMetricWidth = m_ScreenPixelWidth / m_ScreenMetricWidth;

        double pixToMetricHeight = m_ScreenPixelHeight / m_ScreenMetricHeight;

        // TODO considering cam offset!
        screenVec.x = Convert.ToInt32(pixToMetricWidth * (m_ScreenMetricWidth / 2 + hitVec.x));
        screenVec.y = Convert.ToInt32(m_ScreenPixelHeight - (pixToMetricHeight * Math.Abs(hitVec.y + offsetY)));

        //Debug.Log("[DEBUG screenVec:" + screenVec.x + ":" + screenVec.y + "\n");

        if (screenVec.x > m_ScreenPixelWidth)
            screenVec.x = Convert.ToInt32(m_ScreenPixelWidth);
        if (screenVec.x < 0)
            screenVec.x = 0;


        if (screenVec.y > m_ScreenPixelHeight)
            screenVec.y = Convert.ToInt32(m_ScreenPixelHeight);
        if (screenVec.y < 0)
            screenVec.y = 0;

        return screenVec;

    }

    private Vector3 GetRayCastHitPoint(Vector3 rayOrigin, Vector3 rayDirection)
    {

        RaycastHit hit;
        Ray rayCast = new Ray(rayOrigin, rayDirection);


        if (Physics.Raycast(rayCast, out hit))
            return hit.point;
        else
            return new Vector3(0, 0, 0);
    }

    private Vector3 GetRayCastHitPointFromCam(Vector3 rayDirection)
    {

        RaycastHit hit;

        Ray rayCast = m_MainCamera.ScreenPointToRay(new Vector3(rayDirection.x, rayDirection.y, 0.0f));

        if (Physics.Raycast(rayCast, out hit))
            return hit.point;
        else
            return new Vector3(0, 0, 0);
    }

    void SetTransformsOnSystemParameters()
    {
        //m_TrackerManager.GetComponent<SystemParameters>().SetCropRectValuesRT(m_CropImgPosRT.x, m_CropImgPosRT.y, m_CropSizeWidth, m_CropSizeHeight);

        //m_TrackerManager.GetComponent<SystemParameters>().SetCropRectValuesLF(m_CropImgPosLF.x, m_CropImgPosLF.y, m_CropSizeWidth, m_CropSizeHeight);

        m_TrackerManager.GetComponent<SystemParameters>().SetHeadTransforms(m_ARFace.transform.position, m_ARFace.transform.rotation);

        m_TrackerManager.GetComponent<SystemParameters>().SetHeadScreenPosition(m_HeadScreenPosX, m_HeadScreenPosY);

        m_TrackerManager.GetComponent<SystemParameters>().SetIPD((m_ARFace.leftEye.transform.position -
                                                            m_ARFace.rightEye.transform.position).magnitude);

        m_TrackerManager.GetComponent<SystemParameters>().SetEyeTransforms(1,
                                                                        m_EyeWorldPosVecLF,
                                                                        m_EyeLocalPosVecLF,
                                                                        m_EyeBallRotLF,
                                                                        m_ARFace.leftEye.rotation,
                                                                        m_ARFace.leftEye.position,
                                                                        m_EyeCornerGObjLF.transform.position,
                                                                        m_EyeDotGObjLF.transform.position);

        m_TrackerManager.GetComponent<SystemParameters>().SetEyeTransforms(2,
                                                                        m_EyeWorldPosVecRT,
                                                                        m_EyeLocalPosVecRT,
                                                                        m_EyeBallRotRT,
                                                                        m_ARFace.rightEye.rotation,
                                                                        m_ARFace.rightEye.position,
                                                                        m_EyeCornerGObjRT.transform.position,
                                                                        m_EyeDotGObjRT.transform.position);


        m_TrackerManager.GetComponent<SystemParameters>().SetEyeTrackScreenPosition(1, m_EyeTrackScreenPosXLF, m_EyeTrackScreenPosYLF);

        m_TrackerManager.GetComponent<SystemParameters>().SetEyeTrackScreenPosition(2, m_EyeTrackScreenPosXRT, m_EyeTrackScreenPosYRT);

        //m_TrackerManager.GetComponent<SystemParameters>().SetIsEyeCamCalibrated(isEyeCalibrated, dotEyeCamSum);

        m_TrackerManager.GetComponent<SystemParameters>().SetEyeToCamPosition(m_EyeToCamParallelVecRT);
        m_TrackerManager.GetComponent<SystemParameters>().SetEyeToCamOrient(m_EyeToCamOrientVecRT);

    }


}



//private Vector3 CalcExpRotOffset(char dirAngle, Vector3 eyeBallToHitVec, Vector3 eyePos, float tauXRot, float tauYRot, float kappaXRot, float kappaYRot)
//{
//    Quaternion eyeBallRot = Quaternion.LookRotation(eyeBallToHitVec.normalized, Vector3.up);

//    Vector3 newHitVec = new Vector3();


//    if (dirAngle == 'X')
//    {

//        ///////////////////////////////////////////////////////////////////////////////////
//        /// EXPONENTIALS X ROT


//        float angle = Vector3.Angle(new Vector3(0, eyeBallToHitVec.y, -eyeBallToHitVec.z),
//                                        new Vector3(0, 0, -eyePos.z));

//        //Debug.Log("[DEBUG MULT ///////////////////////////////////////////////////////");
//        //Debug.Log("[DEBUG MULT ///////////////////////////////////////////////////////");
//        //Debug.Log("[DEBUG MULT BEFORE HIT VEC] " + eyeBallToHitVec * 100);
//        //Debug.Log("[DEBUG MULT BEFORE EYE POS VEC] " + eyePos * 100);

//        //Debug.Log("[DEBUG MULT ///////////////////////////////////////////////////////");
//        //Debug.Log("[DEBUG MULT BEFORE ANGLE ROT X] " + eyeBallRot.eulerAngles.x);
//        //Debug.Log("[DEBUG MULT BEFORE QUAT ROT X] " + eyeBallRot.x + ":" + eyeBallRot.y + ":" + eyeBallRot.z + ":" + eyeBallRot.w);
//        //Debug.Log("[DEBUG MULT BEFORE ANGLE X] " + angle);

//        //Debug.Log("[DEBUG MULT ///////////////////////////////////////////////////////");

//        float eyeDistNormalizeUP = eyePos.y;
//        float eyeDistNormalizeBOT = -m_ScreenMetricHeight - eyePos.y;

//        //Debug.Log("[DEBUG MULT DIST UP] " + eyeDistNormalizeUP);
//        //Debug.Log("[DEBUG MULT DIST BOT] " + eyeDistNormalizeBOT);

//        //Debug.Log("[DEBUG MULT ///////////////////////////////////////////////////////");

//        double eyeAngleNormalizeUP = Mathf.Rad2Deg * Mathf.Atan2(eyeDistNormalizeUP, eyePos.z);
//        double eyeAngleNormalizeBOT = Mathf.Rad2Deg * Mathf.Atan2(eyeDistNormalizeBOT, eyePos.z);

//        //Debug.Log("[DEBUG MULT NORM UP X] " + (float)eyeAngleNormalizeUP);
//        //Debug.Log("[DEBUG MULT NORM BOT X] " + (float)eyeAngleNormalizeBOT);

//        //Debug.Log("[DEBUG MULT ///////////////////////////////////////////////////////");

//        double eyeAngleNormalize = Mathf.Abs(angle / (Mathf.Abs(((float)eyeAngleNormalizeUP) + Mathf.Abs((float)eyeAngleNormalizeBOT))));

//        //Debug.Log("[DEBUG MULT ANGLE NORM UP X] " + (float)eyeAngleNormalizeUP);
//        //Debug.Log("[DEBUG MULT ANGLE NORM BOT X] " +(float)eyeAngleNormalizeBOT);
//        //Debug.Log("[DEBUG MULT ANGLE NORM SUM X] " +(float)eyeAngleNormalize);

//        //Debug.Log("[DEBUG MULT ///////////////////////////////////////////////////////");

//        double omega = Math.Exp(tauXRot * eyeAngleNormalize - kappaXRot) + 1;

//        //Debug.Log("[DEBUG MULT OMEGA] " + omega);

//        if (eyeBallRot.eulerAngles.x > 270)
//        {
//            float angleExp_x = angle * (float)omega;
//            //   Debug.Log("[DEBUG MULT ANGLE*OMEGA X] " + angleExp_x);

//            newHitVec = Quaternion.Euler(-angleExp_x + angle, 0, 0) * eyeBallToHitVec;
//            //    Debug.Log("[DEBUG MULT AFTER QUAT ROT X] " + Quaternion.Euler(-angleExp_x, 0, 0).x + ":" + Quaternion.Euler(-angleExp_x, 0, 0).y + ":" + Quaternion.Euler(-angleExp_x, 0, 0).z + ":" + Quaternion.Euler(-angleExp_x, 0, 0).w);
//            //    Debug.Log("[DEBUG MULT NEW ANGLE X] " + (360 - angleExp_x));

//        }
//        if (eyeBallRot.eulerAngles.x < 90)
//        {
//            float angleExp_x = angle * (float)omega;

//            //    Debug.Log("[DEBUG MULT ANGLE*OMEGA X] " + angleExp_x);

//            newHitVec = Quaternion.Euler(angleExp_x - angle, 0, 0) * eyeBallToHitVec;
//            //    Debug.Log("[DEBUG MULT AFTER QUAT ROT X] " + Quaternion.Euler(angleExp_x, 0, 0).x + ":" + Quaternion.Euler(angleExp_x, 0, 0).y + ":" + Quaternion.Euler(angleExp_x, 0, 0).z + ":" + Quaternion.Euler(angleExp_x, 0, 0).w);
//            //   Debug.Log("[DEBUG MULT NEW ANGLE X] " + (angleExp_x - angle));
//        }

//    }

//    if (dirAngle == 'Y')
//    {

//        ///////////////////////////////////////////////////////////////////////////////////
//        /// EXPONENTIALS Y ROT

//        float angle = Vector3.Angle(new Vector3(eyeBallToHitVec.x, 0, -eyeBallToHitVec.z),
//                            new Vector3(0, 0, -eyePos.z));

//        //Debug.Log("[DEBUG MULT ///////////////////////////////////////////////////////");
//        //Debug.Log("[DEBUG MULT ///////////////////////////////////////////////////////");
//        //Debug.Log("[DEBUG MULT BEFORE HIT VEC] " + eyeBallToHitVec * 100);
//        //Debug.Log("[DEBUG MULT BEFORE EYE POS VEC] " + eyePos * 100);

//        //Debug.Log("[DEBUG MULT ///////////////////////////////////////////////////////");
//        //Debug.Log("[DEBUG MULT BEFORE ANGLE ROT Y] " + eyeBallRot.eulerAngles.y);
//        //Debug.Log("[DEBUG MULT BEFORE QUAT ROT Y] " + eyeBallRot.x + ":" + eyeBallRot.y + ":" + eyeBallRot.z + ":" + eyeBallRot.w);
//        //Debug.Log("[DEBUG MULT BEFORE ANGLE Y] " + angle);

//        //Debug.Log("[DEBUG MULT ///////////////////////////////////////////////////////");

//        float eyeDistNormalizeRT = (m_ScreenMetricWidth / 2) - Mathf.Abs(eyePos.x);
//        float eyeDistNormalizeLF = m_ScreenMetricWidth - ((m_ScreenMetricWidth / 2) - Mathf.Abs(eyePos.x));

//        //Debug.Log("[DEBUG MULT DIST LF] " + eyeDistNormalizeLF);
//        //Debug.Log("[DEBUG MULT DIST RT] " + eyeDistNormalizeRT);

//        //Debug.Log("[DEBUG MULT ///////////////////////////////////////////////////////");

//        double eyeAngleNormalizeLF = Mathf.Rad2Deg * Mathf.Atan2(eyeDistNormalizeLF, eyePos.z);
//        double eyeAngleNormalizeRT = Mathf.Rad2Deg * Mathf.Atan2(eyeDistNormalizeRT, eyePos.z);

//        //Debug.Log("[DEBUG MULT NORM LF Y] " + Mathf.Abs((float)eyeAngleNormalizeLF));
//        //Debug.Log("[DEBUG MULT NORM RT Y] " + Mathf.Abs((float)eyeAngleNormalizeRT));

//        //Debug.Log("[DEBUG MULT ///////////////////////////////////////////////////////");

//        double eyeAngleNormalize = Mathf.Abs(angle / (Mathf.Abs(((float)eyeAngleNormalizeLF) + Mathf.Abs((float)eyeAngleNormalizeRT))));

//        //Debug.Log("[DEBUG MULT ANGLE NORM LF Y] " + Mathf.Abs((float)eyeAngleNormalizeLF));
//        //Debug.Log("[DEBUG MULT ANGLE NORM RT Y] " + Mathf.Abs((float)eyeAngleNormalizeRT));
//        //Debug.Log("[DEBUG MULT ANGLE NORM SUM Y] " + (float)eyeAngleNormalize);


//        //Debug.Log("[DEBUG MULT ///////////////////////////////////////////////////////");

//        double omega = Math.Exp(tauYRot * eyeAngleNormalize - kappaYRot) + 1;

//        // Debug.Log("[DEBUG MULT OMEGA] " + omega);

//        if (eyeBallRot.eulerAngles.y > 270)
//        {
//            float angleExp_y = angle * (float)omega;
//            //   Debug.Log("[DEBUG MULT ANGLE*OMEGA X] " + angleExp_y);
//            newHitVec = Quaternion.Euler(0, -angleExp_y + angle, 0) * eyeBallToHitVec;
//            //   Debug.Log("[DEBUG MULT AFTER QUAT ROT Y] " + Quaternion.Euler(0, -angleExp_y, 0).x + ":" + Quaternion.Euler(0, -angleExp_y, 0).y + ":" + Quaternion.Euler(0, -angleExp_y, 0).z + ":" + Quaternion.Euler(0, -angleExp_y, 0).w);
//            //    Debug.Log("[DEBUG MULT NEW ANGLE Y] " + (360 - angleExp_y));

//        }
//        if (eyeBallRot.eulerAngles.y < 90)
//        {
//            float angleExp_y = angle * (float)omega;
//            //  Debug.Log("[DEBUG MULT ANGLE*OMEGA X] " + angleExp_y);
//            newHitVec = Quaternion.Euler(0, (angleExp_y - angle), 0) * eyeBallToHitVec;
//            //   Debug.Log("[DEBUG MULT AFTER QUAT ROT Y] " + Quaternion.Euler(0, angleExp_y, 0).x + ":" + Quaternion.Euler(0, angleExp_y, 0).y + ":" + Quaternion.Euler(0, angleExp_y, 0).z + ":" + Quaternion.Euler(0, angleExp_y, 0).w);
//            //   Debug.Log("[DEBUG MULT NEW ANGLE Y] " + (angleExp_y - angle));
//        }

//    }


//    return newHitVec;

//}

//////FLIP
////[DEBUG MULT ///////////////////////////////////////////////////////
////[DEBUG MULT ///////////////////////////////////////////////////////
////[DEBUG MULT BEFORE HIT VEC] (0.0, 0.1, 1.4)
////[DEBUG MULT BEFORE EYE POS VEC](-4.1, -1.5, 29.7)
////[DEBUG MULT ///////////////////////////////////////////////////////
////[DEBUG MULT BEFORE ANGLE ROT X] 354.2755
////[DEBUG MULT BEFORE QUAT ROT X] - 0.04992931:0.0156553:0.0007827309:0.9986297
////[DEBUG MULT BEFORE ANGLE X] 5.727381
////[DEBUG MULT ///////////////////////////////////////////////////////
////[DEBUG MULT DIST UP] - 0.01464637
////[DEBUG MULT DIST BOT] - 0.2473536
////[DEBUG MULT ///////////////////////////////////////////////////////
////[DEBUG MULT NORM UP X] - 2.826158
////[DEBUG MULT NORM BOT X] - 39.81828
////[DEBUG MULT ///////////////////////////////////////////////////////
////[DEBUG MULT ANGLE NORM UP X] - 2.826158
////[DEBUG MULT ANGLE NORM BOT X] - 39.81828
////[DEBUG MULT ANGLE NORM SUM X] 0.1548271
////[DEBUG MULT ///////////////////////////////////////////////////////
////[DEBUG MULT OMEGA] 1.06785746656605
////[DEBUG MULT ANGLE * OMEGA X] 6.116027
////[DEBUG MULT AFTER QUAT ROT X] 0.05334707:0:0:0.998576
////[DEBUG MULT NEW ANGLE X] 0.3886456


////[DEBUG MULT ///////////////////////////////////////////////////////
////[DEBUG MULT ///////////////////////////////////////////////////////
////[DEBUG MULT BEFORE HIT VEC](-0.2, 0.1, 1.4)
////[DEBUG MULT BEFORE EYE POS VEC](-4.3, -2.4, 27.7)
////[DEBUG MULT ///////////////////////////////////////////////////////
////[DEBUG MULT BEFORE ANGLE ROT X] 355.3406
////[DEBUG MULT BEFORE QUAT ROT X] - 0.04056782:-0.06333612:-0.002576716:0.9971641
////[DEBUG MULT BEFORE ANGLE X] 4.697028
////[DEBUG MULT DIST UP] - 0.02358702
////[DEBUG MULT DIST BOT] - 0.238413
////[DEBUG MULT ///////////////////////////////////////////////////////
////[DEBUG MULT NORM UP X] - 4.863109
////[DEBUG MULT NORM BOT X] - 40.69516
////[DEBUG MULT ///////////////////////////////////////////////////////
////[DEBUG MULT ANGLE NORM UP X] - 4.863109
////[DEBUG MULT ANGLE NORM BOT X] - 40.69516
////[DEBUG MULT ANGLE NORM SUM X] 0.1310845
////[DEBUG MULT ///////////////////////////////////////////////////////
////[DEBUG MULT OMEGA] 1.06471055798422
////[DEBUG MULT ANGLE * OMEGA X] 5.000976
////[DEBUG MULT AFTER QUAT ROT X] - 0.0436279:0:0:0.9990479
////[DEBUG MULT NEW ANGLE X] 354.999