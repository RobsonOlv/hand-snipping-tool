using System;
using PassthroughCameraSamples;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class WorldCameraCanvas : MonoBehaviour
{
  public Camera passthroughCamera;
  public Transform centerEyeAnchor;
  public UnityAndGeminiV3 geminiConnect;
  public WebCamTextureManager webCamTextureManager;
  public GameObject ScreenshotContainer;
  public GameObject MenuList;
  public bool isActive = false;
  [SerializeField] private RawImage m_image;
  // private List<ScreenShotComponent> screenshots = new List<ScreenShotComponent>();
  private Texture2D m_cameraSnapshot;
  private Color32[] m_pixelsBuffer;
  public TextMeshProUGUI m_debugText;
  public AnchorManager anchorManager;
  public float canvaSizeHorizontal = 0.15f;
  public float canvaSizeVertical = 0.25f;
  private List<OVRSpatialAnchor.UnboundAnchor> _unboundAnchors = new();
  private const string AnchorIdsKey = "SavedAnchorIds";

  public void DisableView()
  {
    m_image.enabled = false;
    isActive = false;
  }

  public void EnableView()
  {
    m_image.enabled = true;
    isActive = true;
  }

  // public void MakeCameraSnapshot(Vector3 snapshotCenter, float screenshotWidth)
  // {
  //   try
  //   {
  //     var webCamTexture = webCamTextureManager.WebCamTexture;
  //     if (webCamTexture == null || !webCamTexture.isPlaying)
  //     {
  //       Debug.Log("WebCamTexture is null or not playing.");
  //       Debug.Log("Dev playing");

  //       Texture2D defaultTexture = new Texture2D(256, 256);
  //       Color[] colorOptions = new Color[]
  //       {
  //           Color.red,
  //           Color.green,
  //           Color.blue,
  //           Color.yellow,
  //           Color.magenta,
  //           Color.cyan
  //       };
  //       Color fillColor = colorOptions[UnityEngine.Random.Range(0, colorOptions.Length)];
  //       Color[] fillPixels = new Color[256 * 256];

  //       for (int i = 0; i < fillPixels.Length; i++)
  //         fillPixels[i] = fillColor;
  //       defaultTexture.SetPixels(fillPixels);
  //       defaultTexture.Apply();

  //       var newDevScreenshot = new ScreenShotComponent(transform, ScreenshotContainer, MenuList, defaultTexture, m_debugText);
  //       // screenshots.Add(newDevScreenshot);
  //       // OVRSpatialAnchor devAnchor = newDevScreenshot.interactionContainer.GetComponent<OVRSpatialAnchor>();
  //       // anchorManager.SaveAnchor(devAnchor);

  //       return;
  //     }

  //     // if (m_cameraSnapshot == null)
  //     // {
  //     //   Debug.Log("m_cameraSnapshot is null, creating a new Texture2D.");
  //     //   m_cameraSnapshot = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
  //     // }

  //     // m_pixelsBuffer ??= new Color32[webCamTexture.width * webCamTexture.height];
  //     // _ = webCamTextureManager.WebCamTexture.GetPixels32(m_pixelsBuffer);
  //     // m_cameraSnapshot.SetPixels32(m_pixelsBuffer);
  //     // m_cameraSnapshot.Apply();

  //     Texture2D snapshotClone = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
  //     m_pixelsBuffer ??= new Color32[webCamTexture.width * webCamTexture.height];
  //     _ = webCamTextureManager.WebCamTexture.GetPixels32(m_pixelsBuffer);
  //     snapshotClone.SetPixels32(m_pixelsBuffer);
  //     snapshotClone.Apply();

  //     // Passo 1: Converter a posição do mundo para ponto na tela
  //     // Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldCenter);

  //     // // Passo 2: Inverter eixo Y para coincidir com textura
  //     // float screenY = Screen.height - screenPoint.y;
  //     // Vector2 screenPos = new Vector2(screenPoint.x, screenY);

  //     // // Passo 3: Calcular fator de escala entre a tela e a textura da câmera
  //     // float widthRatio = (float)webCamTexture.width / Screen.width;
  //     // float heightRatio = (float)webCamTexture.height / Screen.height;

  //     // // Passo 4: Converter screenPos para coordenadas da textura
  //     // int centerX = (int)(screenPos.x * widthRatio);
  //     // int centerY = (int)(screenPos.y * heightRatio);

  //     // // Passo 5: Definir o tamanho do retângulo de captura em pixels
  //     // int pixelWidth = (int)(worldWidth * webCamTexture.width);
  //     // int pixelHeight = (int)(worldHeight * webCamTexture.height);
  //     // // int pixelWidth = (int)(worldWidth * webCamTexture.width);
  //     // // int pixelHeight = (int)(worldHeight * webCamTexture.height);

  //     // // Garantir que está dentro dos limites
  //     // int startX = Mathf.Clamp(centerX - pixelWidth / 2, 0, webCamTexture.width - pixelWidth);
  //     // int startY = Mathf.Clamp(centerY - pixelHeight / 2, 0, webCamTexture.height - pixelHeight);

  //     // // Passo 6: Recortar os pixels e criar nova textura
  //     // Texture2D croppedTexture = new Texture2D(pixelWidth, pixelHeight, TextureFormat.RGBA32, false);
  //     // Color[] croppedPixels = webCamTexture.GetPixels(startX, startY, pixelWidth, pixelHeight);
  //     // croppedTexture.SetPixels(croppedPixels);
  //     // croppedTexture.Apply();

  //     var newScreenshot = new ScreenShotComponent(transform, ScreenshotContainer, MenuList, snapshotClone, m_debugText);
  //   }
  //   catch (System.Exception e)
  //   {
  //     m_debugText.text = $"Erro WorldCameraCanvas: \n{e.Message}\n{e.StackTrace}";
  //     m_debugText.enabled = true;
  //   }

  // }

  public void MakeCameraSnapshot(Vector3 snapshotCenter2, float screenshotWidth)
  {
    try
    {
      var webCamTexture = webCamTextureManager.WebCamTexture;
      if (webCamTexture == null || !webCamTexture.isPlaying)
      {
        Texture2D defaultTexture = new Texture2D(256, 256);
        Color[] colorOptions = new Color[] {
          Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan
        };
        Color fillColor = colorOptions[UnityEngine.Random.Range(0, colorOptions.Length)];
        Color[] fillPixels = new Color[256 * 256];
        for (int i = 0; i < fillPixels.Length; i++) fillPixels[i] = fillColor;
        defaultTexture.SetPixels(fillPixels);
        defaultTexture.Apply();

        var newDevScreenshot = new ScreenShotComponent(transform, ScreenshotContainer, MenuList, defaultTexture, m_debugText);
        return;
      }

      // Captura da textura da câmera
      Texture2D fullTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
      m_pixelsBuffer ??= new Color32[webCamTexture.width * webCamTexture.height];
      _ = webCamTexture.GetPixels32(m_pixelsBuffer);
      fullTexture.SetPixels32(m_pixelsBuffer);
      fullTexture.Apply();

      // Converter ponto do mundo para ponto da tela
      // Vector3 screenPoint = passthroughCamera.WorldToScreenPoint(snapshotCenter);

      // if (screenPoint.z < 0)
      // {
      //     m_debugText.text = "SnapshotCenter está atrás da câmera.";
      //     return;
      // }

      Vector3 snapshotCenter = transform.position;

      // Converte o ponto do mundo para viewport
      Vector3 viewportPoint = passthroughCamera.WorldToViewportPoint(snapshotCenter);

      // Se estiver atrás da câmera, aborta
      if (viewportPoint.z < 0)
      {
        m_debugText.text = "Ponto está atrás da câmera";
        m_debugText.enabled = true;
        return;
      }

      // Converte para coordenadas de textura
      int centerX = Mathf.RoundToInt(viewportPoint.x * webCamTexture.width);
      int centerY = Mathf.RoundToInt((1f - viewportPoint.y) * webCamTexture.height); // inverte eixo Y

      // Calcula tamanho do frustum no ponto do quadro
      float distance = viewportPoint.z;
      // float frustumHeight = 2.0f * distance * Mathf.Tan(passthroughCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
      // float frustumWidth = frustumHeight * passthroughCamera.aspect;
      float frustumHeight = 2.0f * distance * Mathf.Tan(passthroughCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
      float frustumWidth = frustumHeight * ((float)webCamTexture.width / webCamTexture.height);

      // Proporção real: world space para texture pixels
      float pixelPerMeter = webCamTexture.width / frustumWidth;
      float pixelPerMeterX = webCamTexture.width / frustumWidth;
      float pixelPerMeterY = webCamTexture.height / frustumHeight;

      int pixelSize = Mathf.RoundToInt(screenshotWidth * pixelPerMeter) * 3;
      int pixelWidth = pixelSize;
      int pixelHeight = pixelSize;
      // int pixelWidth = Mathf.RoundToInt(screenshotWidth * pixelPerMeterX) * 4;
      // int pixelHeight = Mathf.RoundToInt(screenshotWidth * pixelPerMeterY) * 4;

      int startX = Mathf.Clamp(centerX - pixelWidth / 2, 0, webCamTexture.width - pixelWidth);
      int startY = Mathf.Clamp(centerY - pixelHeight / 2, 0, webCamTexture.height - pixelHeight);

      // Cria a textura recortada
      Texture2D cropped = new Texture2D(pixelWidth, pixelHeight, TextureFormat.RGBA32, false);
      Color[] pixels = webCamTexture.GetPixels(startX, startY, pixelWidth, pixelHeight);
      cropped.SetPixels(pixels);
      cropped.Apply();

      var newScreenshot = new ScreenShotComponent(transform, ScreenshotContainer, MenuList, cropped, m_debugText);
    }
    catch (System.Exception e)
    {
      m_debugText.text = $"Erro WorldCameraCanvas:\n{e.Message}\n{e.StackTrace}";
      m_debugText.enabled = true;
    }
  }
  public void ResumeStreamingFromCamera()
  {
    m_image.texture = webCamTextureManager.WebCamTexture;
    m_image.uvRect = new Rect(canvaSizeHorizontal, canvaSizeHorizontal, canvaSizeVertical, canvaSizeVertical);
  }

  private IEnumerator Start()
  {
    while (webCamTextureManager.WebCamTexture == null)
    {
      yield return null;
    }
    m_image.enabled = false;
    m_debugText.text = "";
    m_debugText.enabled = false;
    ResumeStreamingFromCamera();
  }
}
