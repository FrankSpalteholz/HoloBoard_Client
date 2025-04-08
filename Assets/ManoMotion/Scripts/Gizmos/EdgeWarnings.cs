using UnityEngine;

namespace ManoMotion.Gizmos
{
    public class EdgeWarnings : MonoBehaviour
    {
        [SerializeField] GameObject top, bottom, left, right;

        private void LateUpdate()
        {
            bool topTriggered = false;
            bool bottomTriggered = false;
            bool leftTriggered = false;
            bool rightTriggered = false;

            HandInfo[] handInfos = ManoMotionManager.Instance.HandInfos;
            for (int i = 0; i < handInfos.Length; i++)
            {
                topTriggered |= handInfos[i].trackingInfo.skeleton.confidence == 1 && handInfos[i].warning == Warning.WARNING_APPROACHING_UPPER_EDGE;
                bottomTriggered |= handInfos[i].trackingInfo.skeleton.confidence == 1 && handInfos[i].warning == Warning.WARNING_APPROACHING_LOWER_EDGE;
                leftTriggered |= handInfos[i].trackingInfo.skeleton.confidence == 1 && handInfos[i].warning == Warning.WARNING_APPROACHING_LEFT_EDGE;
                rightTriggered |= handInfos[i].trackingInfo.skeleton.confidence == 1 && handInfos[i].warning == Warning.WARNING_APPROACHING_RIGHT_EDGE;
            }

            SetWarning(top, topTriggered);
            SetWarning(bottom, bottomTriggered);
            SetWarning(left, leftTriggered);
            SetWarning(right, rightTriggered);
        }

        private void SetWarning(GameObject warning, bool warningTriggered)
        {
            warning.SetActive(warningTriggered);
        }
    }
}