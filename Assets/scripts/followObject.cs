using UnityEngine;

public class FollowObject : MonoBehaviour
{
  public Transform target;
  public Vector3 offsetLocalPosition;
  public Vector3 offsetLocalRotation = Vector3.zero;

  void LateUpdate()
  {
      if (target == null) return;
      Vector3 scaledOffset = Vector3.Scale(offsetLocalPosition, target.lossyScale);
      transform.position = target.position + target.rotation * scaledOffset;
      transform.rotation = target.rotation * Quaternion.Euler(offsetLocalRotation);
  }
}