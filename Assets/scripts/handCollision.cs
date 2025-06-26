using UnityEngine;

public class HandCollision : MonoBehaviour
{
    // Variável para armazenar a cor original
    private Color originalColor;
    public enum HandSide { Left, Right }
    public HandSide handSide;
    [HideInInspector] // Variável para armazenar o estado do trigger (visível no Inspector para debug)
    public bool isTriggerActivated = false;  // Visível no Inspector para debug
    public ThumbCollision thumbCollisionManager;

    void Start()
    {
        // Guarda a cor original da esfera
        originalColor = GetComponent<Renderer>().material.color;
    }

    // Método que é chamado quando outra esfera entra em colisão (trigger)
    void OnTriggerEnter(Collider other)
    {
        HandCollision otherHandCollision = other.GetComponent<HandCollision>();
        isTriggerActivated = true;
        // Verifica se a colisão é com uma esfera da outra mão
        if (otherHandCollision != null && otherHandCollision.handSide != handSide)
        {
            // Muda a cor da esfera para vermelho (ou outra cor)
            // GetComponent<Renderer>().material.color = Color.red;

            // Muda a cor da outra esfera também (colidiu com ela)
            // other.GetComponent<Renderer>().material.color = Color.red;

            if (thumbCollisionManager != null)
                thumbCollisionManager.isTriggered = true;
        }
    }

    // Método que é chamado quando a colisão sai (opcional)
    void OnTriggerExit(Collider other)
    {
        HandCollision otherHandCollision = other.GetComponent<HandCollision>();
        isTriggerActivated = false;
        if (otherHandCollision != null && otherHandCollision.handSide != handSide)
        {
            // Restaura a cor original quando a colisão sair
            GetComponent<Renderer>().material.color = originalColor;
            other.GetComponent<Renderer>().material.color = originalColor;
            if (thumbCollisionManager != null)
                thumbCollisionManager.isTriggered = false;
        }
    }
}