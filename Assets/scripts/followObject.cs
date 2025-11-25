using UnityEngine;

public class FollowObject : MonoBehaviour
{
  public Transform target;
  public Vector3 offsetLocalPosition;
  public Vector3 offsetLocalRotation = Vector3.zero;

  void LateUpdate()
  {
      if (target == null) return;
      
      // Calcular offset X baseado na largura real do target
      Vector3 adjustedOffset = offsetLocalPosition;
      
      // Tentar obter as dimensões reais do screenshot
      ScreenshotDimensions dimensions = target.GetComponent<ScreenshotDimensions>();
      if (dimensions != null)
      {
          // Para alinhar pela borda direita:
          // 1. Mover metade da largura do target para a direita (borda direita do quadro)
          // 2. Mover metade da largura do menu para a direita (para alinhar borda esquerda do menu com borda direita do quadro)
          // 3. Adicionar o offset extra
          float menuWidth = transform.localScale.x;
          adjustedOffset.x = (dimensions.worldWidth / 2f) + (menuWidth / 2f) + Mathf.Abs(offsetLocalPosition.x);
      }
      
      transform.position = target.position + target.rotation * adjustedOffset;
      transform.rotation = target.rotation * Quaternion.Euler(offsetLocalRotation);
  }
}