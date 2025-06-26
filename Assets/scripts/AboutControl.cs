using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AboutControl : MonoBehaviour
{
  public MenuOptions menu;
  public Image playing;
  public Image play;
  public TextMeshProUGUI field;

  void Start()
  {
      
  }

  void Update()
  {
    if(menu == null)
    {
      Debug.LogError("MenuOptions is not assigned in AboutControl.");
      return;
    }
    if (menu.audioSource.isPlaying)
    {
      field.gameObject.SetActive(false);
      play.gameObject.SetActive(false);
      playing.gameObject.SetActive(true);
    }
    else if (menu.cachedAudioClip != null)
    {
      field.gameObject.SetActive(true);
      play.gameObject.SetActive(true);
      playing.gameObject.SetActive(false);
    }
    else
    {
      field.gameObject.SetActive(true);
      play.gameObject.SetActive(false);
      playing.gameObject.SetActive(false);
    }
  }
}
