using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecordControl : MonoBehaviour
{
  public MenuOptions menu;
  public AudioSource cachedRecordingAudioSource;
  public AudioClip recordedAudioClip;
  public Image defaultImage;
  public Image recordingImage;
  public Image playingImage;
  public Image playImage;
  public TextMeshProUGUI field;

  void Update()
  {
    if(menu == null)
    {
      Debug.LogError("MenuOptions is not assigned in RecordControl.");
      return;
    }
    
    // Ler estados diretamente do menu
    bool isRecording = menu.isRecording;
    AudioSource cachedAudio = menu.cachedRecordingAudioSource;
    AudioClip recordedAudio = menu.recordedAudioClip;
    
    // Verificar se está tocando (cachedAudio pode ser null se nunca gravou)
    bool isPlayingRecorded = cachedAudio != null && cachedAudio.isPlaying;
    // Debug.Log($"[RecordControl] isRecording: {isRecording}, isPlayingRecorded: {isPlayingRecorded}, recordedAudio: {(recordedAudio != null ? "exists" : "null")}");
    
    if (isRecording)
    {
      defaultImage.gameObject.SetActive(false);
      playImage.gameObject.SetActive(false);
      recordingImage.gameObject.SetActive(true);
      playingImage.gameObject.SetActive(false);
      field.text = "Recording...";
    }
    else if (isPlayingRecorded)
    {
      defaultImage.gameObject.SetActive(false);
      playImage.gameObject.SetActive(false);
      recordingImage.gameObject.SetActive(false);
      playingImage.gameObject.SetActive(true);
      field.text = "Listening...";
    }
    else if (recordedAudio != null)
    {
      defaultImage.gameObject.SetActive(false);
      playImage.gameObject.SetActive(true);
      recordingImage.gameObject.SetActive(false);
      playingImage.gameObject.SetActive(false);
      field.text = "Listen";
    }
    else
    {
      defaultImage.gameObject.SetActive(true);
      playImage.gameObject.SetActive(false);
      recordingImage.gameObject.SetActive(false);
      playingImage.gameObject.SetActive(false);
      field.text = "Start Recording";
    }
  }
}
