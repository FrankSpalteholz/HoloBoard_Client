using UnityEngine;

namespace ManoMotion.Instructions.New
{
    /// <summary>
    /// Updates the instruction panels position depending on the device orientation.
    /// </summary>
    public class InstructionPanel : MonoBehaviour
    {
        [SerializeField] Canvas canvas;
        [SerializeField] float portraitHeightPercentage, landscapeHeightPercentage;

        private void Update()
        {
            switch (ManoUtils.Instance.Orientation)
            {
                case SupportedOrientation.UNKNOWN:
                case SupportedOrientation.FACE_UP:
                case SupportedOrientation.FACE_DOWN:
                case SupportedOrientation.PORTRAIT:
                case SupportedOrientation.PORTRAIT_UPSIDE_DOWN:
                case SupportedOrientation.PORTRAIT_FRONT_FACING:
                case SupportedOrientation.PORTRAIT_UPSIDE_DOWN_FRONT_FACING:
                    SetYPosition(portraitHeightPercentage);
                    break;
                case SupportedOrientation.LANDSCAPE_LEFT:
                case SupportedOrientation.LANDSCAPE_RIGHT:
                case SupportedOrientation.LANDSCAPE_LEFT_FRONT_FACING:
                case SupportedOrientation.LANDSCAPE_RIGHT_FRONT_FACING:
                    SetYPosition(landscapeHeightPercentage);
                    break;
            }
        }

        private void SetYPosition(float percentage)
        {
            Vector3 position = transform.position;
            position.y = canvas.pixelRect.size.y * percentage;
            transform.position = position;
        }
    }
}