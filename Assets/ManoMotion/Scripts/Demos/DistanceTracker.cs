using ManoMotion;
using ManoMotion.Demos;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DistanceTracker : MonoBehaviour
{
    [SerializeField] Grabber[] grabbers;
    [SerializeField] float castRadius = 0.03f;
    [SerializeField] float targetRadius = 0.1f;
    [SerializeField] Transform target;

    [Space, Header("UI")]
    [SerializeField] TMP_Text distanceText;
    [SerializeField] Slider distanceSlider;
    [SerializeField] float sliderWidth = 160;
    [SerializeField] RectTransform minImage, maxImage;
    [SerializeField] UnityEvent OnStart, OnFinish;

    Transform originalParent;
    float min, max;

    private void OnEnable()
    {
        Grabbable.OnReleased += OnReleased;
    }

    private void OnDisable()
    {
        Grabbable.OnReleased -= OnReleased;
    }

    void OnReleased(Grabbable _)
    {
        target.SetParent(originalParent);
        OnFinish?.Invoke();
    }

    private void Start()
    {
        OnStart?.Invoke();

        originalParent = target.parent;
        target.SetParent(Camera.main.transform);

        float padding = castRadius + targetRadius / 2f;
        min = 0.5f - padding;
        max = 0.5f + padding;
        minImage.anchoredPosition = new Vector2(sliderWidth * min, 0);
        maxImage.anchoredPosition = new Vector2(sliderWidth * max, 0);
    }

    void Update()
    {
        HandInfo[] handInfos = ManoMotionManager.Instance.HandInfos;
        distanceSlider.value = 0;

        for (int i = 0; i < handInfos.Length; i++)
        {
            if (handInfos[i].trackingInfo.skeleton.confidence == 1)
            {
                SetSliderValue(i);
                SetText(i);
                break;
            }
        }
    }

    Vector3 GetHandPosition(int index)
    {
        ManoClass gesture = ManoMotionManager.Instance.HandInfos[index].gestureInfo.manoClass;
        return gesture switch
        {
            ManoClass.GRAB_GESTURE => grabbers[index].GrabPosition,
            ManoClass.PINCH_GESTURE => grabbers[index].PinchPosition,
            _ => grabbers[index].GrabPosition
        };
    }

    private void SetSliderValue(int i)
    {
        Vector3 position = GetHandPosition(i);
        float distance = Vector3.Distance(position, Camera.main.transform.position);
        float targetDistance = Vector3.Distance(target.position, Camera.main.transform.position);
        float sliderValue = Mathf.LerpUnclamped(0, targetDistance * 2, distance);
        distanceSlider.value = sliderValue;
    }

    private void SetText(int i)
    {
        ManoClass gesture = ManoMotionManager.Instance.HandInfos[i].gestureInfo.manoClass;
        if (IsInRange())
        {
            distanceText.text = gesture switch
            {
                ManoClass.GRAB_GESTURE => "Grab now!",
                ManoClass.PINCH_GESTURE => "Pinch now!",
                _ => "Grab now!"
            };
        }
        else
        {
            distanceText.text = gesture switch
            {
                ManoClass.GRAB_GESTURE => "Grab range",
                ManoClass.PINCH_GESTURE => "Pinch range",
                _ => "Grab range"
            };
        }
    }

    bool IsInRange()
    {
        return distanceSlider.value > min && distanceSlider.value < max;
    }
}