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
  public AudioClip cachedAudioClip = null;
  public AnchorManager anchorManager;

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

  void Start()
  {
    if (followObject == null)
      followObject = gameObject.GetComponent<FollowObject>();
  }

  public void GenerateGeminiInput()
  {
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
    }

    gameObject.SetActive(false);
  }
  private IEnumerator SendPromptMediaRequestToGemini()
  {
    if (loadingSpinner != null) loadingSpinner.SetActive(true);
    string apiKey = "YOUR_API_KEY_HERE"
    string promptText = "Considerando o objeto principal dessa imagem e sua espécie, tipo ou material, gere um texto curto explicando sobre esse objeto para uma criança usando uma linguagem divertida e que vise o aprendizado. Ignore mãos na imagem. Não cite o prompt na resposta. Responda apenas com o texto, sem formatação ou tags HTML. Não inclua informações sobre a imagem, apenas descreva o objeto principal de forma educativa e divertida.";
    string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={YOUR_API_KEY_HERE}";
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
    
    anchorManager.SaveAnchor(screenshotContainer, screenshotImage);
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
}

[System.Serializable]
public class GeminiAudioResponse
{
    public string audioContent;
}