using UnityEngine;

public class HandGestureDetection : MonoBehaviour
{
    public ThumbCollision thumbCollision;
    public GameObject handTrackingLeft;
    public GameObject handTrackingRight;
    private DrawFingerLines linesLeft;
    private HandPalmDirection palmLeft;
    private DrawFingerLines linesRight;
    private HandPalmDirection palmRight;

    public bool isGestureDetected = false;

    void Start()
    {
        if(handTrackingLeft != null)
        {
            linesLeft = handTrackingLeft.GetComponent<DrawFingerLines>();
            palmLeft = handTrackingLeft.GetComponent<HandPalmDirection>();
        }

        if(handTrackingRight != null)
        {
            linesRight = handTrackingRight.GetComponent<DrawFingerLines>();
            palmRight = handTrackingRight.GetComponent<HandPalmDirection>();
        }
    }

    void Update()
    {
        if (linesLeft == null || palmLeft == null || linesRight == null || palmRight == null)
            return;
        bool validationLeft = linesLeft.indexUp && linesLeft.thumbHorizontal && palmLeft.handBackDectected;
        bool validationRight = linesRight.indexUp && linesRight.thumbHorizontal && palmRight.handBackDectected;
        bool validationThumb = thumbCollision.isTriggered;

        isGestureDetected = validationLeft && validationRight && validationThumb;
    }

}
