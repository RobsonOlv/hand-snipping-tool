using UnityEngine;

public class DrawFingerLines : MonoBehaviour
{
    public enum LineDirection { Up, Left, Right}
    private OVRSkeleton ovrSkeleton; // Componente para acessar o rastreamento dos ossos das mãos
    public LineRenderer lineRendererIndicator; // Linha para o indicador
    public LineRenderer lineRendererThumb; // Linha para o polegar
    public enum HandSide { Left, Right };
    public HandSide handSide;
    public Camera mainCamera;
    public enum FingerType { index, thumb };
    public bool indexUp = false; // Variável para verificar se o indicador está apontando para cima
    public bool thumbHorizontal = false; // Variável para verificar se o polegar está apontando na horizontal

    void Start()
    {
        // Obter o componente OVRSkeleton no GameObject onde o script está
        ovrSkeleton = GetComponentInChildren<OVRSkeleton>();

        // Configuração inicial dos LineRenderers
        // SetupLineRenderer(lineRendererIndicator, Color.red);
        // SetupLineRenderer(lineRendererThumb, Color.green);
    }

    void Update()
    {
        if (!ovrSkeleton.IsDataValid || !ovrSkeleton.IsDataHighConfidence)
           return;
        if (ovrSkeleton != null)
        {
            UpdateLineForFinger(FingerType.index, OVRSkeleton.BoneId.XRHand_IndexIntermediate, OVRSkeleton.BoneId.XRHand_IndexTip, lineRendererIndicator, LineDirection.Up);
            UpdateLineForFinger(FingerType.thumb, OVRSkeleton.BoneId.Hand_Thumb1, OVRSkeleton.BoneId.Hand_Thumb3, lineRendererThumb, handSide == HandSide.Left ? LineDirection.Right : LineDirection.Left);
        }
    }

    // Função para atualizar as linhas com base nos ossos do dedo
    void UpdateLineForFinger(FingerType finger, OVRSkeleton.BoneId baseBoneId, OVRSkeleton.BoneId tipBoneId, LineRenderer lineRenderer, LineDirection lineDirection)
    {
        // Obter os transforms dos ossos base e ponta dos dedos
        Transform baseBone = GetBoneTransform(baseBoneId);
        Transform tipBone = GetBoneTransform(tipBoneId);

        if (baseBone != null && tipBone != null)
        {
            // Atualizar a posição do LineRenderer em tempo real para seguir o dedo
            // lineRenderer.SetPosition(0, baseBone.position);
            // lineRenderer.SetPosition(1, tipBone.position);

            AdjustLineWidthBasedOnDirection(finger, baseBone.position, tipBone.position, lineRenderer, lineDirection);
        }
    }

    // Função para pegar o transform do osso com base no ID
    Transform GetBoneTransform(OVRSkeleton.BoneId boneId)
    {
        foreach (var bone in ovrSkeleton.Bones)
        {
            if (bone.Id == boneId)
            {
                return bone.Transform;
            }
        }
        return null;
    }

    // Função para configurar o LineRenderer
    void SetupLineRenderer(LineRenderer lineRenderer, Color lineColor)
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2; // A linha terá dois pontos (base e ponta do dedo)
            lineRenderer.startWidth = 0.01f; // Largura da linha no início
            lineRenderer.endWidth = 0.01f; // Largura da linha no final
            lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // Material básico
            lineRenderer.startColor = lineColor; // Cor inicial
            lineRenderer.endColor = lineColor;   // Cor final
        }
    }

        // Função para ajustar a largura da linha dependendo da direção do dedo
    // Função para ajustar a largura da linha dependendo da direção do dedo
    void AdjustLineWidthBasedOnDirection(FingerType finger, Vector3 basePosition, Vector3 tipPosition, LineRenderer lineRenderer, LineDirection lineDirection)
    {
        // Calcular a direção do dedo
        Vector3 direction = (tipPosition - basePosition).normalized;

        // Obter o vetor "para cima" relativo à câmera (em vez de usar o vetor global)
        Vector3 cameraDirection = lineDirection == LineDirection.Up ? mainCamera.transform.up : mainCamera.transform.right;

        bool validation = false;
        if(lineDirection == LineDirection.Up)
        {
          // Calcular o ângulo entre a direção do dedo e o vetor "para cima" da câmera
          float angle = Vector3.Angle(direction, cameraDirection);
          validation = angle < 45f;
        }
        else if(lineDirection == LineDirection.Right)
        {
            // Verifica se o dedo está apontando para a direita (em relação à câmera)
            validation = Vector3.Dot(direction, cameraDirection) > 0.8f; // Ajuste o valor de 0.5f conforme necessário
        } else if(lineDirection == LineDirection.Left)
        {
            // Verifica se o dedo está apontando para a esquerda (em relação à câmera)
            validation = Vector3.Dot(direction, cameraDirection) < - 0.8f;
        }

        // Se o ângulo for pequeno (indicando que o dedo está apontando para cima em relação à câmera)
        if (validation)
        {
            if(finger == FingerType.index)
            {
                indexUp = true;
            } else if(finger == FingerType.thumb)
            {
                thumbHorizontal = true;
            }
            // Aumenta a largura da linha
            // lineRenderer.startWidth = 0.05f;
            // lineRenderer.endWidth = 0.05f;
        }
        else
        {
            if(finger == FingerType.index)
            {
                indexUp = false;
            } else if(finger == FingerType.thumb)
            {
                thumbHorizontal = false;
            }
            // Mantém a largura normal
            // lineRenderer.startWidth = 0.01f;
            // lineRenderer.endWidth = 0.01f;
        }
    }
}
