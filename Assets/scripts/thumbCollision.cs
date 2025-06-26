using UnityEngine;
using System.Collections.Generic;

public class ThumbCollision : MonoBehaviour
{
    public enum HandSide { Left, Right }

    [System.Serializable]
    public class BoneTargetPair
    {
        public OVRSkeleton hand;
        public GameObject target;
        public HandSide side;
    }
    public Material sphereMaterial;
    public OVRSkeleton ovrLeftHandSkeleton;
    public OVRSkeleton ovrRightHandSkeleton;
    // private Dictionary<OVRSkeleton.BoneId, GameObject> jointRightHandSpheres = new Dictionary<OVRSkeleton.BoneId, GameObject>();
    private GameObject leftThumbSphere;
    private GameObject rightThumbSphere;

    private bool initialized = false;
    public bool isTriggered = false;

    void Start()
    {}

    void Update()
    {
        if (!ovrLeftHandSkeleton.IsDataValid || !ovrLeftHandSkeleton.IsDataHighConfidence)
            return;

        if (!ovrRightHandSkeleton.IsDataValid || !ovrRightHandSkeleton.IsDataHighConfidence)
            return;

        if (!initialized)
        {
            InitializeSpheres();
            initialized = true;
        }
    }

    void InitializeSpheres()
    {
        // Criando esferas para todos os ossos rastreados
        // var leftThumbTip = ovrLeftHandSkeleton.BoneId.XRHand_ThumbTip;
        // var rightThumbTip = ovrRightHandSkeleton.BoneId.XRHand_ThumbTip;

        BoneTargetPair[] thumbs = new BoneTargetPair[]
        {
            new BoneTargetPair { hand = ovrLeftHandSkeleton, target = leftThumbSphere, side = HandSide.Left },
            new BoneTargetPair { hand = ovrRightHandSkeleton, target = rightThumbSphere, side = HandSide.Right }
        };

        foreach (var thumb in thumbs)
        {
            var hand = thumb.hand;
            var bone = hand.Bones[(int)OVRSkeleton.BoneId.XRHand_ThumbTip]; // Obtém o osso do polegar
            thumb.target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            thumb.target.transform.localScale = Vector3.one * 0.06f; // Tamanho das esferas
            thumb.target.transform.localPosition = Vector3.zero;
            thumb.target.transform.localRotation = Quaternion.identity;

            thumb.target.transform.SetParent(bone.Transform, false); // Ajusta como filho do objeto
            Renderer renderer = thumb.target.GetComponent<Renderer>();
            renderer.material = sphereMaterial;

            thumb.target.SetActive(true); // Ativa a esfera

            SphereCollider sphereCollider = thumb.target.GetComponent<SphereCollider>();
            sphereCollider.isTrigger = true;

            // Adiciona um Rigidbody à esfera
            Rigidbody sphereRigidbody = thumb.target.AddComponent<Rigidbody>();
            sphereRigidbody.isKinematic = true;  // Isso impede que a física afete a esfera, mas permite detecção de colisões

            // Adiciona o script HandCollision à esfera
            HandCollision handCollisionScript = thumb.target.AddComponent<HandCollision>();  // Adicionando o script de colisão
            handCollisionScript.handSide = thumb.side == HandSide.Left ? HandCollision.HandSide.Left : HandCollision.HandSide.Right;
            handCollisionScript.thumbCollisionManager = this; // Passa a referência do ThumbCollision
        }
    }
}
