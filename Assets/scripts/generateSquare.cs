using PassthroughCameraSamples;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using System.IO;


public class GenerateSquare : MonoBehaviour
{
    public HandGestureDetection handGestureDetection;
    public OVRSkeleton leftHandSkeleton;
    public OVRSkeleton rightHandSkeleton;
    public float offsetXDistance = 0.02f;
    public float offsetYDistance = 0.04f;

    private Transform leftProx, leftTip, rightProx, rightTip;
    private bool bonesCached = false;

    private float holdTimer = 0f;
    public float holdDuration = 3f;
    private bool screenshotTaken = false;
    private float screenshotWidth; // Default width
    private float screenshotHeight; // Default height
    private Vector3 screenshotCenter; // Center of the screenshot area

    [Header("Canvas")]
    public WorldCameraCanvas cameraCanvas;
    private RectTransform canvasRectTransform;

    void Start()
    {
        if(cameraCanvas != null)
        {
            canvasRectTransform = cameraCanvas.GetComponent<RectTransform>();
        }
    }

    void Update()
    {
        
        var isGestureDetected = handGestureDetection.isGestureDetected;

        if (!isGestureDetected)
        {
            if (!screenshotTaken && cameraCanvas.isActive) cameraCanvas.DisableView();
            holdTimer = 0f;
            return;
        }
        else if (!cameraCanvas.isActive)
        {
            cameraCanvas.EnableView();
        }

        if (screenshotTaken)
        {
            // cameraCanvas.ResumeStreamingFromCamera(); 
            screenshotTaken = false;
        }

        holdTimer += Time.deltaTime;

        if (holdTimer >= holdDuration)
        {
            Debug.Log("[Screenshot] Capturando screenshot...");
            cameraCanvas.MakeCameraSnapshot(screenshotCenter, screenshotWidth, screenshotHeight);
            screenshotTaken = true;
            holdTimer = 0f;
            return;
        }

        if (!AreSkeletonsReady()) return;

        if (!bonesCached)
        {
            leftProx  = GetBone(leftHandSkeleton, OVRSkeleton.BoneId.XRHand_IndexProximal);
            leftTip   = GetBone(leftHandSkeleton, OVRSkeleton.BoneId.XRHand_IndexTip);
            rightProx = GetBone(rightHandSkeleton, OVRSkeleton.BoneId.XRHand_IndexProximal);
            rightTip  = GetBone(rightHandSkeleton, OVRSkeleton.BoneId.XRHand_IndexTip);

            if (leftProx && leftTip && rightProx && rightTip)
                bonesCached = true;
            else
                return;
        }

        Vector3 leftDir  = (leftTip.position - leftProx.position).normalized;
        Vector3 rightDir = (rightTip.position - rightProx.position).normalized;

        float rightOffsetX = offsetXDistance;
        float leftOffsetX = -offsetXDistance;
        float offsetY = -offsetYDistance;

        Vector3 lp = leftProx.position;// + leftDir * offsetY + new Vector3(rightOffsetX, 0, 0);
        Vector3 lt = leftTip.position;// + new Vector3(rightOffsetX, 0, 0);
        Vector3 rp = rightProx.position;// + rightDir * offsetY + new Vector3(leftOffsetX, 0, 0);
        Vector3 rt = rightTip.position;// + new Vector3(leftOffsetX, 0, 0);
        
        UpdateRawImageSizeAndCorners(lp, lt, rt, rp);
    }

    void UpdateRawImageSizeAndCorners(Vector3 lp, Vector3 lt, Vector3 rt, Vector3 rp)
    {

        if (cameraCanvas == null) return;

        // Cálculo do centro entre os 4 pontos
        Vector3 center = (lp + lt + rp + rt) / 4f;
        screenshotCenter = center;

        // Vectors para largura e altura
        Vector3 leftCenter = (lp + lt) * 0.5f;
        Vector3 rightCenter = (rp + rt) * 0.5f;
        Vector3 right = (rightCenter - leftCenter).normalized;

        Vector3 upLeft = (lt - lp).normalized;
        Vector3 upRight = (rt - rp).normalized;
        Vector3 up = ((upLeft + upRight) * 0.5f).normalized;

        Vector3 forward = Vector3.Cross(right, up).normalized;

        float width = ((Vector3.Distance(lp, rp) + Vector3.Distance(lt, rt)) / 2f) - 0.03f;
        float height = ((Vector3.Distance(lp, lt) + Vector3.Distance(rp, rt)) / 2f) + 0.05f;

        screenshotWidth = width;
        screenshotHeight = height;

        canvasRectTransform.position = center;
        canvasRectTransform.rotation = Quaternion.LookRotation(forward, up);
        canvasRectTransform.sizeDelta = new Vector2(width, height);
        
        // Atualizar o preview do recorte da câmera baseado nas posições das mãos
        cameraCanvas.UpdatePreviewRect(center, width, height);
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !bonesCached) return;

        Vector3 center = (leftProx.position + leftTip.position + rightProx.position + rightTip.position) / 4f;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(center, 0.005f);
    }

    Transform GetBone(OVRSkeleton skel, OVRSkeleton.BoneId id)
    {
        foreach (var bone in skel.Bones)
            if (bone.Id == id)
                return bone.Transform;
        return null;
    }

    bool AreSkeletonsReady()
    {
        return leftHandSkeleton != null && rightHandSkeleton != null &&
               leftHandSkeleton.IsDataValid && rightHandSkeleton.IsDataValid &&
               leftHandSkeleton.IsDataHighConfidence && rightHandSkeleton.IsDataHighConfidence;
    }
}
