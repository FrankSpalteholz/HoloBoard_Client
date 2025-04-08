using ManoMotion;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HandPlacementZone : MonoBehaviour
{
    [SerializeField] Image borderImage;
    [SerializeField] RectTransform left, right;
    [SerializeField] float insideTimeToFinish = 3f;
    [SerializeField] Color insideColor, outsideColor;
    [SerializeField] Image progressImage;
    [SerializeField] UnityEvent OnStart, OnFinish;

    RectTransform rect;
    float timeInside = 0;

    private void Awake()
    {
        rect = (RectTransform)transform;
    }

    private void Start()
    {
        OnStart?.Invoke();
    }

    void Update()
    {
        bool inside = (RectContains(rect, left) && ManoMotionManager.Instance.TryGetHandInfo(LeftOrRightHand.LEFT_HAND, out _)) ||
                      (RectContains(rect, right) && ManoMotionManager.Instance.TryGetHandInfo(LeftOrRightHand.RIGHT_HAND, out _));
        float sign = inside ? 1f : -1f;
        timeInside = Mathf.Clamp(timeInside + Time.deltaTime * sign, 0, insideTimeToFinish);
        borderImage.color = inside ? insideColor : outsideColor;
        progressImage.fillAmount = timeInside / insideTimeToFinish;

        if (timeInside >= insideTimeToFinish)
        {
            OnFinish?.Invoke();
        }
    }

    bool RectContains(RectTransform rect, RectTransform other)
    {
        Rect a = GetRect(rect);
        Rect b = GetRect(other);
        return b.xMin > a.xMin && b.xMax < a.xMax &&
               b.yMin > a.yMin && b.yMax < a.yMax;
    }
    Rect GetRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        Vector2 min = corners[0];
        Vector2 max = corners[2];
        Vector2 size = max - min;
        return new Rect(min, size);
    }
}
