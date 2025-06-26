using UnityEngine;
using System.Collections.Generic;

public class leftHandBones : MonoBehaviour
{
    private OVRHand ovrLeftHand;
    private OVRSkeleton ovrLeftHandSkeleton;
    private Dictionary<OVRSkeleton.BoneId, GameObject> jointLeftHandSpheres = new Dictionary<OVRSkeleton.BoneId, GameObject>();
    public Material sphereMaterial;

    private bool initialized = false;

    void Start()
    {
        ovrLeftHand = GetComponentInChildren<OVRHand>();
        ovrLeftHandSkeleton = GetComponentInChildren<OVRSkeleton>();
    }

    void Update()
    {
        if (ovrLeftHand == null || ovrLeftHandSkeleton == null)
            return;

        if (!ovrLeftHand.IsTracked || !ovrLeftHandSkeleton.IsDataValid || !ovrLeftHandSkeleton.IsDataHighConfidence)
            return;

        if (!initialized)
        {
            InitializeSpheres();
            initialized = true;
        }

        // Atualizando as posições e rotações de cada esfera
        foreach (var bone in ovrLeftHandSkeleton.Bones)
        {
            if (jointLeftHandSpheres.TryGetValue(bone.Id, out var sphere))
            {
                // sphere.SetActive(true);
                sphere.transform.position = bone.Transform.position;
                // sphere.transform.rotation = bone.Transform.rotation;
            }
        }
    }

    void InitializeSpheres()
    {
        // Criando esferas para todos os ossos rastreados
        foreach (var bone in ovrLeftHandSkeleton.Bones)
        {
            if (bone.Id == OVRSkeleton.BoneId.Invalid) continue;

            // Cria uma esfera para o osso
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = Vector3.one * 0.02f; // Tamanho das esferas
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localRotation = Quaternion.identity;

            Renderer renderer = sphere.GetComponent<Renderer>();
            if (sphereMaterial != null)
            {
                renderer.material = sphereMaterial; // Aplica o material se fornecido
            }
            else
            {
                renderer.material.color = Color.green; // Cor padrão se nenhum material for fornecido
            }
            sphere.transform.SetParent(bone.Transform); // Ajusta como filho do objeto
            sphere.SetActive(true); // Ativa a esfera

            SphereCollider sphereCollider = sphere.GetComponent<SphereCollider>();
            sphereCollider.isTrigger = true;

            // Adiciona um Rigidbody à esfera
            Rigidbody sphereRigidbody = sphere.AddComponent<Rigidbody>();
            sphereRigidbody.isKinematic = true;  // Isso impede que a física afete a esfera, mas permite detecção de colisões


            // Adiciona o script HandCollision à esfera
            HandCollision handCollisionScript = sphere.AddComponent<HandCollision>();  // Adicionando o script de colisão
            handCollisionScript.handSide = HandCollision.HandSide.Left; // Define o lado da mão como esquerda

            // Armazena a esfera associada ao osso
            jointLeftHandSpheres.Add(bone.Id, sphere);
        }

        Debug.Log("[HAND DEBUG] Esferas criadas para " + jointLeftHandSpheres.Count + " ossos!");
    }
}
