using UnityEngine;
using System.Collections;

public class HandPalmDirection : MonoBehaviour
{
    public bool handBackDectected = false;
    private OVRSkeleton ovrSkeleton;
    private Transform palmTransform;
    private Camera mainCamera;
    private Renderer handRenderer;
    private Material handMaterialInstance;

    [Header("Configurações")]
    public Color facingCameraColor;
    private Color defaultColor;

    public float maxFacingAngle = 45f;

    void Start()
    {
        mainCamera = Camera.main;

        // Tenta obter o OVRSkeleton do objeto atual ou de seus filhos
        ovrSkeleton = GetComponentInChildren<OVRSkeleton>();
        if (ovrSkeleton == null)
        {
            Debug.LogError("OVRSkeleton não encontrado!");
            return;
        }

        // Inicia a corrotina para aguardar a inicialização dos ossos
        StartCoroutine(InitializeBones());
    }

    private IEnumerator InitializeBones()
    {
        // Aguarda até que a lista de ossos seja populada
        while (ovrSkeleton.Bones.Count == 0)
        {
            yield return null;
        }

        // Procura pelo osso da palma da mão
        foreach (var bone in ovrSkeleton.Bones)
        {
            if (bone.Id == OVRSkeleton.BoneId.XRHand_Palm)
            {
                palmTransform = bone.Transform;
                break;
            }
        }

        if (palmTransform == null)
        {
            Debug.LogError("Transform da palma não encontrado!");
            yield break;
        }

        // Obtém o Renderer da mão
        handRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (handRenderer != null)
        {
            handMaterialInstance = Instantiate(handRenderer.material);
            handRenderer.material = handMaterialInstance;
            defaultColor = handMaterialInstance.GetColor("_ColorTop");
        }
    }

    void Update()
    {
        if (palmTransform == null || mainCamera == null)
            return;

        // Calcula a direção da palma e o vetor para a câmera
        Vector3 handUpDirection = palmTransform.up;
        Vector3 toCamera = (mainCamera.transform.position - palmTransform.position).normalized;

        // Calcula o ângulo entre a direção da palma e a direção para a câmera
        float angle = Vector3.Angle(handUpDirection, toCamera);
        // Altera a cor da mão com base no ângulo
        if (handMaterialInstance != null)
        {
            if (angle < maxFacingAngle)
            {
                handBackDectected = true;
                handMaterialInstance.color = facingCameraColor;
                handMaterialInstance.SetColor("_ColorTop", facingCameraColor);
            }
            else {
              handMaterialInstance.SetColor("_ColorTop", defaultColor);
              handBackDectected = false;
            }
        }
    }
}
