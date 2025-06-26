using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

public class MakeGrabbable : MonoBehaviour
{
    public void SetupGrabbable(GameObject screenshot)
    {
      // var rectTransform = screenshot.GetComponent<RectTransform>();
      
      // 1. Adiciona Rigidbody no objeto principal
      var rb = screenshot.AddComponent<Rigidbody>();
      rb.useGravity = false;
      rb.isKinematic = true;

      // var bc = screenshot.AddComponent<BoxCollider>();
      // bc.size = new Vector3(rectTransform.rect.width, rectTransform.rect.height, 0.01f); 

      // Vector3 scale = screenshot.transform.localScale;
      // bc.size = new Vector3(scale.x, scale.y, 1f);

      // 2. Cria o filho "[BuildingBlock] HandGrab"
      GameObject handGrabGO = new GameObject("[Grabbable] Hand Grab");
      handGrabGO.transform.SetParent(screenshot.transform, false);
      handGrabGO.transform.localPosition = Vector3.zero;


      // 4. Adiciona os scripts NO FILHO
      var grabbable = handGrabGO.AddComponent<Grabbable>();
      grabbable.InjectOptionalTargetTransform(screenshot.transform);
      grabbable.InjectOptionalRigidbody(rb);

      var handGrabInteractable = handGrabGO.AddComponent<HandGrabInteractable>();
      handGrabInteractable.InjectRigidbody(rb);
      handGrabInteractable.InjectOptionalPointableElement(grabbable);

      var grabInteractable = handGrabGO.AddComponent<GrabInteractable>();
      grabInteractable.InjectRigidbody(rb);
    }
}