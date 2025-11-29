using UnityEngine;
using TMPro;
using System.Collections;

public class ToastController : MonoBehaviour
{
    [Tooltip("Reference to the TextMeshPro component that will display the message.")]
    public TextMeshProUGUI messageText;

    [Tooltip("Time in seconds before the toast disappears.")]
    public float duration = 5f;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        // Ensure the toast is hidden at start
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Displays the toast with the specified message for a set duration.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public void ShowToast(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
        else
        {
            Debug.LogWarning("ToastController: Message Text component is not assigned.");
        }

        gameObject.SetActive(true);

        // If a hide coroutine is already running, stop it so we reset the timer
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        hideCoroutine = StartCoroutine(HideToastAfterDelay());
    }

    private IEnumerator HideToastAfterDelay()
    {
        yield return new WaitForSeconds(duration);
        gameObject.SetActive(false);
        hideCoroutine = null;
    }
}
