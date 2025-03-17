using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SystemParameters : MonoBehaviour
{

    public int ScreenPixelWidth = 2048;
    public int ScreenPixelHeight = 2732;

    public float ScreenPhysicalWidth = 0.198f;
    public float ScreenPhysicalHeight = 0.262f;



    public Vector3 ARCamPosition;
    public Quaternion ARCamRotation;
    public Vector2 ARCamFocalLength;
    public Vector2 ARCamPrinciplePoint;
    public Vector2 ARCamResolution;

    public int CropRectPosXRT;
    public int CropRectPosYRT;
    public int CropRectWidthRT;
    public int CropRectHeightRT;

    public int CropRectPosXLF;
    public int CropRectPosYLF;
    public int CropRectWidthLF;
    public int CropRectHeightLF;

    public Vector3 HeadPosition;
    public Quaternion HeadRotation;
    int HeadScreenPositionX;
    int HeadScreenPositionY;

    public Vector3 EyeLeftPosition;
    public Vector3 EyeLeftLocalPosition;
    public Vector3 EyeARLeftPosition;
    public Quaternion EyeLeftRotation;
    public Quaternion EyeARLeftRotation;

    public Vector3 EyeLeftCornerPosition;
    public Vector3 EyeLeftMidPosition;

    public int EyeLeftTrackPositionX;
    public int EyeLeftTrackPositionY;

    public int EyeLeftTrackScreenPositionX;
    public int EyeLeftTrackScreenPositionY;

    public float EyeLeftTrackScreenPositionXf;
    public float EyeLeftTrackScreenPositionYf;


    public Vector3 EyeRightPosition;
    public Vector3 EyeRightLocalPosition;
    public Vector3 EyeARRightPosition;
    public Quaternion EyeRightRotation;
    public Quaternion EyeARRightRotation;

    public Vector3 EyeRightCornerPosition;
    public Vector3 EyeRightMidPosition;

    public int EyeRightTrackPositionX;
    public int EyeRightTrackPositionY;

    public int EyeRightTrackScreenPositionX;
    public int EyeRightTrackScreenPositionY;

    public int EyeToScreenProjectionPosX;
    public int EyeToScreenProjectionPosY;


    public Vector3 EyeToCamOrient;
    public Vector3 EyeToCamPosition;
    public float IPD;

    bool isEyeCamCalibrated;
    float eyeCalibrationSum;

    private void Awake()
    {

        ARCamPosition = new Vector3();
        ARCamRotation = new Quaternion();
        ARCamFocalLength = new Vector2();
        ARCamPrinciplePoint = new Vector2();
        ARCamResolution = new Vector2();

        // CropRectPosXRT = 0;
        // CropRectPosYRT = 0;
        // CropRectWidthRT = 0;
        // CropRectHeightRT = 0;

        // CropRectPosXLF = 0;
        // CropRectPosYLF = 0;
        // CropRectWidthLF = 0;
        // CropRectHeightLF = 0;

        HeadPosition = new Vector3();
        HeadRotation = new Quaternion();
        HeadScreenPositionX = 0;
        HeadScreenPositionY = 0;


        EyeLeftPosition = new Vector3();
        EyeLeftLocalPosition = new Vector3();
        EyeARLeftPosition = new Vector3();
        EyeLeftRotation = new Quaternion();

        EyeARLeftRotation = new Quaternion();
        EyeLeftCornerPosition = new Vector3();
        EyeLeftMidPosition = new Vector3();

        EyeLeftTrackPositionX = 0;
        EyeLeftTrackPositionY = 0;

        EyeLeftTrackScreenPositionX = 0;
        EyeLeftTrackScreenPositionY = 0;


        EyeRightPosition = new Vector3();
        EyeRightLocalPosition = new Vector3();
        EyeARRightPosition = new Vector3();
        EyeARRightRotation = new Quaternion();

        EyeRightRotation = new Quaternion();
        EyeRightCornerPosition = new Vector3();
        EyeRightMidPosition = new Vector3();

        EyeRightTrackPositionX = 0;
        EyeRightTrackPositionY = 0;

        EyeRightTrackScreenPositionX = 0;
        EyeRightTrackScreenPositionY = 0;


        IPD = 0.0f;
        isEyeCamCalibrated = false;
        eyeCalibrationSum = 0.0f;

        EyeToCamOrient = new Vector3();
        EyeToCamPosition = new Vector3();

        EyeToScreenProjectionPosX = 0;
        EyeToScreenProjectionPosY = 0;


}

public void SetARCamTransforms(Vector3 camPosition, Quaternion camRotation,
        Vector2 focalLength, Vector2 principlePoint, Vector2 resolution)
    {
        ARCamPosition = camPosition;
        ARCamRotation = camRotation;
        ARCamFocalLength = focalLength;
        ARCamPrinciplePoint = principlePoint;
        ARCamResolution = resolution;
    }


    public void SetHeadTransforms(Vector3 headPosition, Quaternion headRotation)
    {
        HeadRotation = headRotation;
        HeadPosition = headPosition;
    }

    public void SetHeadScreenPosition(int headPosX, int headPosY)
    {
        HeadScreenPositionX = headPosX;
        HeadScreenPositionY = headPosY;
    }

    public void SetEyeTransforms(int eyeIndex, Vector3 eyePosition, Vector3 eyeLocalPosition, Quaternion eyeRotation,
        Quaternion eyeARRotation, Vector3 eyeARPosition, Vector3 eyeCornerPosition, Vector3 eyeMidPosition)
    {
        switch (eyeIndex)
        {
            case 1:
                EyeLeftPosition = eyePosition;
                EyeLeftLocalPosition = eyeLocalPosition;
                EyeLeftRotation = eyeRotation;

                EyeARLeftRotation = eyeARRotation;
                EyeARLeftPosition = eyeARPosition;

                EyeLeftCornerPosition = eyeCornerPosition;
                EyeLeftMidPosition = eyeMidPosition;

                break;

            case 2:
                EyeRightPosition = eyePosition;
                EyeRightLocalPosition = eyeLocalPosition;

                EyeRightRotation = eyeRotation;

                EyeARRightRotation = eyeARRotation;
                EyeARRightPosition = eyeARPosition;

                EyeRightCornerPosition = eyeCornerPosition;
                EyeRightMidPosition = eyeMidPosition;

                break;
        }
    }

    public void SetEyeTrackPosition(int eyeIndex, int eyeTrackPositionX, int eyeTrackPositionY)
    {
        switch (eyeIndex)
        {
            case 1:
                EyeLeftTrackPositionX = eyeTrackPositionX;
                EyeLeftTrackPositionY = eyeTrackPositionY;
                break;

            case 2:
                EyeRightTrackPositionX = eyeTrackPositionX;
                EyeRightTrackPositionY = eyeTrackPositionY;
                break;
        }
    }

    public void SetEyeTrackScreenPosition(int eyeIndex, int eyeTrackScreenPositionX, int eyeTrackScreenPositionY)
    {
        switch (eyeIndex)
        {
            case 1:
                EyeLeftTrackScreenPositionX = eyeTrackScreenPositionX;
                EyeLeftTrackScreenPositionY = eyeTrackScreenPositionY;
                break;

            case 2:
                EyeRightTrackScreenPositionX = eyeTrackScreenPositionX;
                EyeRightTrackScreenPositionY = eyeTrackScreenPositionY;
                break;
        }
    }

    public void SetEyeToCamPosition(Vector3 eyeToCamPositionVec)
    {
        EyeToCamPosition = eyeToCamPositionVec;
    }

    public void SetEyeToCamOrient(Vector3 eyeToCamOrientVec)
    {
        EyeToCamOrient = eyeToCamOrientVec;
    }

    public void SetIPD(float ipd) { IPD = ipd; }

    public void SetIsEyeCamCalibrated(bool calibStatus, float eyeCalibSum)
    {
        isEyeCamCalibrated = calibStatus;
        eyeCalibrationSum = eyeCalibSum;
    }

    public void SetEyeToScreenProjectionPos (int eyeToScreenProjectionPosX, int eyeToScreenProjectionPosY)
    {
        EyeToScreenProjectionPosX = eyeToScreenProjectionPosX;
        EyeToScreenProjectionPosY = eyeToScreenProjectionPosY;
    }

    public Vector3 GetARCamPosition() { return ARCamPosition; }
    public Quaternion GetARCamRotation() { return ARCamRotation; }

    // public int GetCropPosXRT() { return CropRectPosXRT; }
    // public int GetCropPosYRT() { return CropRectPosYRT; }
    // public int GetCropRectWidthRT() { return CropRectWidthRT; }
    // public int GetCropRectHeightRT() { return CropRectHeightRT; }

    // public int GetCropPosXLF() { return CropRectPosXLF; }
    // public int GetCropPosYLF() { return CropRectPosYLF; }
    // public int GetCropRectWidthLF() { return CropRectWidthLF; }
    // public int GetCropRectHeightLF() { return CropRectHeightLF; }

    public Vector3 GetHeadPosition() { return HeadPosition; }
    public Quaternion GetHeadRotation() { return HeadRotation; }
    public int GetHeadPosX() { return HeadScreenPositionX; }
    public int GetHeadPosY() { return HeadScreenPositionY; }


    public Vector3 GetEyeLeftLocalPosition() { return EyeLeftLocalPosition; }
    public Vector3 GetEyeLeftPosition() { return EyeLeftPosition; }
    public Quaternion GetEyeLeftRotation() { return EyeLeftRotation; }
    public Vector3 GetEyeARLeftPosition() { return EyeARLeftPosition; }
    public Quaternion GetEyeARLeftRotation() { return EyeARLeftRotation; }
    public Vector3 GetEyeLeftCornerPosition() { return EyeLeftCornerPosition; }
    public Vector3 GetEyeLeftMidPosition() { return EyeLeftMidPosition; }
    public int GetEyeLeftTrackPositionX() { return EyeLeftTrackPositionX; }
    public int GetEyeLeftTrackPositionY() { return EyeLeftTrackPositionY; }
    public int GetEyeLeftTrackScreenPositionX() { return EyeLeftTrackScreenPositionX; }
    public int GetEyeLeftTrackScreenPositionY() { return EyeLeftTrackScreenPositionY; }

    //public float GetEyeLeftTrackScreenPositionXf() { return EyeLeftTrackScreenPositionXf; }
    //public float GetEyeLeftTrackScreenPositionYf() { return EyeLeftTrackScreenPositionYf; }

    public Vector3 GetEyeRightPosition() { return EyeRightPosition; }
    public Vector3 GetEyeRightLocalPosition() { return EyeRightLocalPosition; }

    public Quaternion GetEyeRightRotation() { return EyeRightRotation; }
    public Vector3 GetEyeARRightPosition() { return EyeARRightPosition; }
    public Quaternion GetEyeARRightRotation() { return EyeARRightRotation; }
    public Vector3 GetEyeRightCornerPosition() { return EyeRightCornerPosition; }
    public Vector3 GetEyeRightMidPosition() { return EyeRightMidPosition; }
    public int GetEyeRightTrackPositionX() { return EyeRightTrackPositionX; }
    public int GetEyeRightTrackPositionY() { return EyeRightTrackPositionY; }
    public int GetEyeRightScreenTrackPositionX() { return EyeRightTrackScreenPositionX; }
    public int GetEyeRightScreenTrackPositionY() { return EyeRightTrackScreenPositionY; }

    //public float GetEyeRightScreenTrackPositionXf() { return EyeRightTrackScreenPositionXf; }
    //public float GetEyeRightScreenTrackPositionYf() { return EyeRightTrackScreenPositionYf; }



    public float GetIPD() { return IPD; }
    public bool GetIsEyeCamCalibrated() { return isEyeCamCalibrated; }
    public float GetEyeCalibSum() { return eyeCalibrationSum; }
    public Vector3 GetEyeToCamPosition() { return EyeToCamPosition; }
    public Vector3 GetEyeToCamOrient() { return EyeToCamOrient; }

    public int GetEyeToScreenProjectionPosX() { return EyeToScreenProjectionPosX; }
    public int GetEyeToScreenProjectionPosY() { return EyeToScreenProjectionPosY; }



}


