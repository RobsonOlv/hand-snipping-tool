using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Text;
using System.IO;

public class MenuOptions : MonoBehaviour
{
  public GameObject screenshotContainer;
  public Texture2D screenshotImage;
  public TextMeshProUGUI debugText;
  public FollowObject followObject;
  public GameObject loadingSpinner;
  public AudioSource audioSource;
  public AudioClip cachedAudioClip = null; // Cache para áudio gerado por TTS
  public AnchorManager anchorManager;
  public AirSnipSegmentation AirSnipSegmentationInstance;
  public AudioSource CachedRecordingAudioSource => cachedRecordingAudioSource;
  // Cache separado para áudio gravado pelo usuário
  public AudioSource cachedRecordingAudioSource;
  
  // Áudio gravado armazenado localmente no menuOptions
  public AudioClip recordedAudioClip = null;
  
  // Estado da gravação de áudio (público para ser acessível pelo RecordControl)
  public bool isRecording = false;

  public GameObject deleteRecordedAudioButton;
  public ToastController toastController; // Referência direta ao ToastController
  private const int maxRecordingTime = 60; // 1 minuto em segundos
  private string microphoneDevice;

  [System.Serializable]
  private class TTSRequest
  {
    public InputData input;
    public VoiceSelectionParams voice;
    public AudioConfig audioConfig;
  }

  [System.Serializable]
  private class InputData
  {
    public string text;
  }

  [System.Serializable]
  private class VoiceSelectionParams
  {
    public string languageCode;
    public string name;
  }

  [System.Serializable]
  private class AudioConfig
  {
    public string audioEncoding;
  }

  [System.Serializable]
  private class TTSResponse
  {
    public string audioContent;
  }
  private const string AnchorIdsKey = "SavedAnchorIds";
  private static bool logCleared = false;

  void Start()
  {
    if (!logCleared)
    {
        ClearSegmentationLog();
        logCleared = true;
    }

    if (followObject == null)
      followObject = gameObject.GetComponent<FollowObject>();
  }

  private void ClearSegmentationLog()
  {
      #if UNITY_ANDROID && !UNITY_EDITOR
      string logDir = "/sdcard/Documents/AirSnip";
      #else
      string logDir = Path.Combine(Application.persistentDataPath, "Logs");
      #endif

      string filePath = Path.Combine(logDir, "segmentation_times.csv");
      try
      {
          if (File.Exists(filePath))
          {
              File.Delete(filePath);
              Debug.Log("Segmentation log cleared on app start.");
          }
      }
      catch (Exception e)
      {
          Debug.LogError($"Failed to clear segmentation log: {e.Message}");
      }
  }

  public void GenerateGeminiInput()
  {
    try {
      Debug.Log("GenerateGeminiInput called");
      if (cachedAudioClip != null)
      {
        if (audioSource.isPlaying)
          audioSource.Stop();
        else
          audioSource.Play();

        return;
      }

      if (screenshotImage != null)
      {
        StartCoroutine(SendPromptMediaRequestToGemini());
      }
      else
      {
        if (debugText != null)
        {
          debugText.text = "Screenshot image is null.";
          debugText.enabled = true;
        }
        Debug.LogError("Screenshot image is null.");
      }
    } catch
    {
      toastController.ShowToast("Failed to generate AI response.");
    }
  }

  public async void DestroyElement()
  {
    followObject.target = null;
    if (screenshotContainer != null)
    {
      OVRSpatialAnchor anchor = screenshotContainer.GetComponent<OVRSpatialAnchor>();
      if(anchor != null)
      {
        await anchorManager.EraseAnchor(anchor);
      }
      Destroy(screenshotContainer);
      cachedAudioClip = null;
      audioSource.clip = null;
      cachedRecordingAudioSource.clip = null;
      recordedAudioClip = null;
    }

    gameObject.SetActive(false);
  }
  private IEnumerator SendPromptMediaRequestToGemini()
  {
    if (loadingSpinner != null) loadingSpinner.SetActive(true);
    string apiKey = "YOUR_API_KEY_HERE";
    string promptText = "Considerando o objeto principal dessa imagem e sua espécie, tipo ou material, gere um texto curto explicando sobre esse objeto para uma criança usando uma linguagem divertida e que vise o aprendizado. Ignore mãos na imagem. Não cite o prompt na resposta. Responda apenas com o texto, sem formatação ou tags HTML. Não inclua informações sobre a imagem, apenas descreva o objeto principal de forma educativa e divertida.";
    string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";
    string base64Media = ConvertTextureToBase64(screenshotImage);

    string jsonBody = $@"
    {{
    ""contents"": [
        {{
        ""parts"": [
            {{
            ""text"": ""{promptText}""
            }},
            {{
            ""inline_data"": {{
                ""mime_type"": ""image/png"",
                ""data"": ""{base64Media}""
            }}
            }}
        ]
        }}
    ]
    }}";

    Debug.Log("Sending JSON: " + jsonBody);
    byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonBody);

    using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
    {
      www.uploadHandler = new UploadHandlerRaw(jsonToSend);
      www.downloadHandler = new DownloadHandlerBuffer();
      www.SetRequestHeader("Content-Type", "application/json");

      yield return www.SendWebRequest();

      if (www.result != UnityWebRequest.Result.Success)
      {
        Debug.LogError(www.error);
        Debug.LogError("Response: " + www.downloadHandler.text);
      }
      else
      {
        Debug.Log("Request complete!");
        TextResponse response = JsonUtility.FromJson<TextResponse>(www.downloadHandler.text);
        if (response.candidates.Length > 0 && response.candidates[0].content.parts.Length > 0)
        {
          string text = response.candidates[0].content.parts[0].text;
          StartCoroutine(SendTextToSpeechRequest(text));
          Debug.Log(text);
        }
        else
        {
          if (debugText != null)
          {
            debugText.text = "No text found.";
            debugText.enabled = true;
          }
          Debug.Log("No text found.");
        }
      }
    }


    if (loadingSpinner != null) loadingSpinner.SetActive(false);
  }
  private IEnumerator SendTextToSpeechRequest(string text)
  {
    // Endpoint da API Google Cloud Text-to-Speech
    // Adicionamos a chave de API diretamente na URL, o que é comum para esta API
    string url = "https://texttospeech.googleapis.com/v1/text:synthesize?key=AIzaSyC9-ptAqG1zaq9l62TE8EZ1B90SK12jUMw";

    // Montando o corpo da requisição JSON para a API TTS
    TTSRequest requestData = new TTSRequest
    {
      input = new InputData { text = text },
      voice = new VoiceSelectionParams
      {
        languageCode = "pt-BR",
        name = "pt-BR-Standard-C" // Uma voz neural de alta qualidade.
                                  // Veja outras opções: https://cloud.google.com/text-to-speech/docs/voices
      },
      audioConfig = new AudioConfig
      {
        audioEncoding = "MP3" // Formato do áudio de saída
      }
    };

    string jsonBody = JsonUtility.ToJson(requestData);
    Debug.Log("JSON Enviado: " + jsonBody);

    using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
    {
      byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
      www.uploadHandler = new UploadHandlerRaw(bodyRaw);
      www.downloadHandler = new DownloadHandlerBuffer();
      www.SetRequestHeader("Content-Type", "application/json");

      yield return www.SendWebRequest();

      if (www.result == UnityWebRequest.Result.ConnectionError ||
          www.result == UnityWebRequest.Result.ProtocolError ||
          www.result == UnityWebRequest.Result.DataProcessingError)
      {
        Debug.LogError($"Erro na requisição TTS: {www.error}");
        Debug.LogError($"Detalhes do erro: {www.downloadHandler.text}"); // Informação útil do erro do servidor
        debugText.text = "Detalhes do erro: " + www.downloadHandler.text;
        debugText.enabled = true;
      }
      else
      {
        string responseJson = www.downloadHandler.text;
        //Debug.Log("JSON Recebido: " + responseJson);

        TTSResponse ttsResponse = JsonUtility.FromJson<TTSResponse>(responseJson);

        if (ttsResponse != null && !string.IsNullOrEmpty(ttsResponse.audioContent))
        {
          byte[] audioBytes = System.Convert.FromBase64String(ttsResponse.audioContent);

          // Salvar temporariamente e carregar o áudio
          string tempFileName = "tempTTSAudio.mp3";
          string tempPath = Path.Combine(Application.persistentDataPath, tempFileName);
          File.WriteAllBytes(tempPath, audioBytes);

          StartCoroutine(PlayAudioFromFile(tempPath));
        }
        else
        {
          Debug.LogError("Resposta TTS inválida ou sem 'audioContent'. Resposta: " + responseJson);
          debugText.text = "Resposta TTS inválida ou sem 'audioContent'. Resposta: " + responseJson;
          debugText.enabled = true;
        }
      }
    }
  }
  private IEnumerator PlayAudioFromFile(string path)
  {
    using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG))
    {
      yield return audioRequest.SendWebRequest();

      if (audioRequest.result == UnityWebRequest.Result.Success)
      {
        AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
        if (clip != null)
        {
          //Armazenar em cache o clip e também no container do screenshot
          cachedAudioClip = clip;
          AudioHolder audioHolder = screenshotContainer.GetComponent<AudioHolder>();
          if (audioHolder != null)
          {
            audioHolder.audioClip = clip;
          }
          PlayAudioClip(clip);
          Debug.Log("Reproduzindo áudio TTS.");
        }
        else
        {
          Debug.LogError("Falha ao decodificar o AudioClip.");
          debugText.text = "Falha ao decodificar o AudioClip.";
          debugText.enabled = true;
        }
      }
      else
      {
        Debug.LogError("Erro ao carregar áudio do arquivo: " + audioRequest.error);
        debugText.text = "Erro ao carregar áudio do arquivo: " + audioRequest.error;
        debugText.enabled = true;
      }
    }
  }
  private void PlayAudioClip(AudioClip clip)
  {
    AudioSource audioSource = GetComponent<AudioSource>();
    if (audioSource == null)
      audioSource = gameObject.AddComponent<AudioSource>();

    audioSource.clip = clip;
    audioSource.Play();
  }
  public string ConvertTextureToBase64(Texture2D texture)
  {
    // Codifica a textura para PNG
    byte[] imageBytes = texture.EncodeToPNG();

    // Converte os bytes para uma string Base64
    string base64String = Convert.ToBase64String(imageBytes);

    return base64String;
  }
  public void SavePosition()
  {
    // Debug.Log("SavePosition called");
    // var anchor = screenshotContainer.GetComponent<OVRSpatialAnchor>();
    // if (anchor == null)
    // {
    //   anchor = screenshotContainer.AddComponent<OVRSpatialAnchor>();
    // }
    // anchor.enabled = true;
    // OVRSpatialAnchor anchor = screenshotContainer.GetComponent<OVRSpatialAnchor>();
    // var anchor = screenshotContainer.AddComponent<OVRSpatialAnchor>();
    // var renderer = screenshotContainer.GetComponentsInChildren<Renderer>();
    // renderer[0].enabled = false;
    // await anchor.WhenLocalizedAsync();
    try {
      anchorManager.SaveAnchor(screenshotContainer, screenshotImage);
      toastController.ShowToast("Anchor saved successfully.");
    } catch (Exception e) {
      Debug.LogError("Error saving anchor: " + e.Message);
      toastController.ShowToast("Failed to save anchor.");
    }
    
    // if (anchor != null)
    // {
    //   Debug.Log("Anchor saved with UUID: " + anchor.Uuid);
    // }
    // else
    // {
    //   Debug.LogError("OVRSpatialAnchor component not found on screenshotContainer.");
    // }
    // renderer[0].enabled = true;
  }

  public void SegmentImage()
  {
    try
    {
      loadingSpinner.SetActive(true);
      
      // Iniciar medição de tempo
      System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
      stopwatch.Start();

      // Realizar a segmentação da imagem
      Texture2D segmentedTexture = AirSnipSegmentationInstance.GetSegmentationMask(screenshotImage);
      
      stopwatch.Stop();
      long elapsedMs = stopwatch.ElapsedMilliseconds;
      LogSegmentationTime(elapsedMs);

      if (segmentedTexture == null)
      {
        Debug.LogError("Segmentation returned null texture.");
        toastController.ShowToast("Something went wrong during segmentation.");
        return;
      }
      
      // Atualizar a textura no screenshotImage (usada pelo menu)
      screenshotImage = segmentedTexture;
      
      // Encontrar o ScreenShotComponent no container via holder
      ScreenShotComponentHolder holder = screenshotContainer.GetComponent<ScreenShotComponentHolder>();
      
      if (holder != null && holder.screenshotComponent != null)
      {
        // Atualizar a textura do quad frontal
        holder.screenshotComponent.UpdateTexture(segmentedTexture);
        Debug.Log($"Segmented texture applied successfully. Time: {elapsedMs}ms");
        toastController.ShowToast($"Segmentation completed in {elapsedMs}ms.");
      }
      else
      {
        Debug.LogError("ScreenShotComponentHolder or screenshotComponent not found.");
        toastController.ShowToast("ScreenShotComponentHolder or screenshotComponent not found.");
      }
      
    } 
    catch (System.Exception e)
    {
      Debug.LogError("Error during image segmentation: " + e.Message);
      toastController.ShowToast("Error during image segmentation");
      if (debugText != null)
      {
        debugText.text = "Erro na segmentação: " + e.Message;
        debugText.enabled = true;
      }
    }
    finally
    {
      loadingSpinner.SetActive(false);
    }
  }

  private void LogSegmentationTime(long durationMs)
  {
      string logLine = $"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss},{durationMs}\n";
      
      #if UNITY_ANDROID && !UNITY_EDITOR
      string logDir = "/sdcard/Documents/AirSnip";
      #else
      string logDir = Path.Combine(Application.persistentDataPath, "Logs");
      #endif

      try
      {
          if (!Directory.Exists(logDir))
          {
              Directory.CreateDirectory(logDir);
          }

          string filePath = Path.Combine(logDir, "segmentation_times.csv");
          
          // Adicionar cabeçalho se o arquivo não existir
          if (!File.Exists(filePath))
          {
              File.AppendAllText(filePath, "Timestamp,DurationMs\n");
          }

          File.AppendAllText(filePath, logLine);
          Debug.Log($"Segmentation time logged: {durationMs}ms to {filePath}");
      }
      catch (Exception e)
      {
          Debug.LogError($"Failed to log segmentation time: {e.Message}");
      }
  }

  public void DownloadImage()
  {   
    if (screenshotImage == null)
    {
      toastController.ShowToast("Screenshot not found.");
      return;
    }

    try
    {
      // Gerar nome do arquivo com timestamp
      string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
      string fileName = $"AirSnip_{timestamp}.png";
      
      // Codificar a textura para PNG
      byte[] imageBytes = screenshotImage.EncodeToPNG();
      
      Debug.Log($"Image encoded, size: {imageBytes.Length} bytes");
      
      // Salvar usando a API do Android MediaStore
      bool success = SaveImageToGallery(imageBytes, fileName);
      
      if (success)
      {
        Debug.Log($"Image saved successfully: {fileName}");
        toastController.ShowToast("Image saved successfully. Check your gallery.");
      }
      else
      {
        Debug.LogError("Failed to save image to gallery");
        toastController.ShowToast("Failed to save image to gallery");
      }
    }
    catch (System.Exception e)
    {
      Debug.LogError($"Error saving image: {e.Message}\n{e.StackTrace}");
      if (debugText != null)
      {
        debugText.text = $"Erro: {e.Message}";
        debugText.enabled = true;
      }
    }
  }
  
  private bool SaveImageToGallery(byte[] imageBytes, string fileName)
  {
    #if UNITY_ANDROID && !UNITY_EDITOR
    try
    {
      // No Quest 3, o caminho padrão para screenshots é /sdcard/Oculus/Screenshots
      // Mas para garantir compatibilidade e visibilidade na galeria, DCIM ou Pictures são recomendados.
      // Vamos tentar salvar diretamente no diretório público de Pictures primeiro.
      
      string picturesDir = "/sdcard/Pictures/AirSnip";
      if (!Directory.Exists(picturesDir))
      {
          Directory.CreateDirectory(picturesDir);
      }
      
      string filePath = Path.Combine(picturesDir, fileName);
      File.WriteAllBytes(filePath, imageBytes);
      
      // Forçar o MediaScanner a indexar o arquivo para aparecer na galeria
      using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
      using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
      using (AndroidJavaClass mediaScanner = new AndroidJavaClass("android.media.MediaScannerConnection"))
      {
          mediaScanner.CallStatic("scanFile", currentActivity, new string[] { filePath }, new string[] { "image/png" }, null);
      }
      
      Debug.Log($"Image saved to: {filePath}");
      return true;
    }
    catch (System.Exception e)
    {
      Debug.LogError($"Android File Save error: {e.Message}\n{e.StackTrace}");
      return false;
    }
    #else
    // Fallback para editor ou outras plataformas
    string path = Path.Combine(Application.persistentDataPath, fileName);
    File.WriteAllBytes(path, imageBytes);
    Debug.Log($"Image saved to: {path}");
    return true;
    #endif
  }
  
  private IEnumerator HideDebugTextAfterDelay(float delay)
  {
    yield return new WaitForSeconds(delay);
    if (debugText != null)
    {
      debugText.enabled = false;
    }
  }
  
  // Função para gravar áudio (máximo 1 minuto)
  public void RecordAudio()
  {    
    // Se já está gravando, parar e salvar a gravação
    if (isRecording)
    {
      StopRecording();
      deleteRecordedAudioButton.SetActive(true);
      return;
    }
    
    // Se já existe um áudio gravado, reproduzir ou parar
    if (recordedAudioClip != null)
    {
      if (cachedRecordingAudioSource.isPlaying)
      {
        cachedRecordingAudioSource.Stop();
      }
      else
      {
        cachedRecordingAudioSource.loop = false; // Garantir que não esteja em loop
        cachedRecordingAudioSource.clip = recordedAudioClip;
        cachedRecordingAudioSource.Play();
      }
      return;
    }
    
    // Iniciar nova gravação apenas se não tiver áudio gravado
    StartRecording();
  }
  
  private void StartRecording()
  {
    // Obter o dispositivo de microfone padrão
    if (Microphone.devices.Length > 0)
    {
      microphoneDevice = Microphone.devices[0];
      Debug.Log($"Starting recording with microphone: {microphoneDevice}");
      
      // Iniciar gravação em variável local (frequência de 44100 Hz, máximo 60 segundos)
      recordedAudioClip = Microphone.Start(microphoneDevice, false, maxRecordingTime, 44100);
      isRecording = true;
      
      Debug.Log("Recording started...");
      
      // Iniciar corrotina para parar automaticamente após 1 minuto
      StartCoroutine(StopRecordingAfterTime(maxRecordingTime));
    }
    else
    {
      Debug.LogError("No microphone detected!");
      if (debugText != null)
      {
        debugText.text = "Erro: Nenhum microfone detectado.";
        debugText.enabled = true;
      }
    }
  }
  
  private void StopRecording()
  {
    if (!isRecording) return;
    
    // Capturar a posição atual do microfone antes de parar
    int lastPos = Microphone.GetPosition(microphoneDevice);
    bool isMicrophoneRecording = Microphone.IsRecording(microphoneDevice);

    isRecording = false;
    Debug.Log("Stopping recording...");
    
    // Parar o microfone
    Microphone.End(microphoneDevice);

    // Se estava gravando e temos uma posição válida, cortamos o áudio
    // Se o microfone parou sozinho (isMicrophoneRecording == false), significa que atingiu o tempo máximo, então usamos o clip inteiro
    if (isMicrophoneRecording && lastPos > 0)
    {
        float[] samples = new float[lastPos * recordedAudioClip.channels];
        recordedAudioClip.GetData(samples, 0);
        
        AudioClip trimmedClip = AudioClip.Create(recordedAudioClip.name, lastPos, recordedAudioClip.channels, recordedAudioClip.frequency, false);
        trimmedClip.SetData(samples, 0);
        
        recordedAudioClip = trimmedClip;
    }

    AudioHolder audioHolder = screenshotContainer.GetComponent<AudioHolder>();
    
    // Salvar o clip no audioHolder para persistência
    if (audioHolder != null)
    {
      audioHolder.recordedClip = recordedAudioClip;
    }
  
    
    Debug.Log($"Recording stopped. Audio clip duration: {recordedAudioClip.length} seconds");
  }
  
  private IEnumerator StopRecordingAfterTime(float time)
  {
    yield return new WaitForSeconds(time);
    
    if (isRecording)
    {
      StopRecording();
      Debug.Log("Recording stopped automatically after max time.");
    }
  }
  
  // Função para reproduzir o áudio gravado
  public void PlayRecordedAudio()
  {
    if (recordedAudioClip == null)
    {
      Debug.LogError("No audio clip to play.");
      return;
    }

    if(recordedAudioClip != null)
    {
      if (cachedRecordingAudioSource.isPlaying)
      {
        cachedRecordingAudioSource.Stop();
      }
      else
      {
        cachedRecordingAudioSource.loop = false;
        cachedRecordingAudioSource.clip = recordedAudioClip;
        cachedRecordingAudioSource.Play();
      }
    }
  }
  
  // Função para apagar o áudio gravado
  public void DeleteAudio()
  {
    // Parar reprodução se estiver tocando
    if (cachedRecordingAudioSource.isPlaying)
    {
      cachedRecordingAudioSource.Stop();
    }
    
    // Parar gravação se estiver gravando
    if (isRecording)
    {
      Microphone.End(microphoneDevice);
      isRecording = false;
    }
    
    // Limpar o clip local
    recordedAudioClip = null;
    deleteRecordedAudioButton.SetActive(false);

    AudioHolder audioHolder = screenshotContainer.GetComponent<AudioHolder>();
    
    // Limpar também do audioHolder para persistência
    if (audioHolder != null)
    {
      audioHolder.recordedClip = null;
    }
    
    cachedRecordingAudioSource.clip = null;

    toastController.ShowToast("Audio deleted.");
    Debug.Log("Audio deleted.");
  }
}

[System.Serializable]
public class GeminiAudioResponse
{
    public string audioContent;
}