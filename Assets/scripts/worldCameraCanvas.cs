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
  public AirSnipSegmentation AirSnipSegmentationInstance;
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
      Texture2D sourceTexture;
      
      var webCamTexture = webCamTextureManager.WebCamTexture;
      if (webCamTexture == null || !webCamTexture.isPlaying)
      {
        // Modo dev/play: carrega imagem de teste
        Texture2D dogImage = Resources.Load<Texture2D>("Images/dog");
        
        if (dogImage == null)
        {
          m_debugText.text = "Imagem 'dog.jpeg' não encontrada em Assets/Resources/Images/";
          m_debugText.enabled = true;
          return;
        }

        sourceTexture = dogImage;
      }
      else
      {
        // Captura da textura da câmera
        sourceTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
        m_pixelsBuffer ??= new Color32[webCamTexture.width * webCamTexture.height];
        _ = webCamTexture.GetPixels32(m_pixelsBuffer);
        sourceTexture.SetPixels32(m_pixelsBuffer);
        sourceTexture.Apply();
      }

      // Processa a imagem com AirSnipSegmentation (aplica máscara no canal alpha)
      Texture2D segmentedTexture = AirSnipSegmentationInstance.GetSegmentationMask(sourceTexture);

      // Debug: Verifica se a segmentação foi aplicada
      // Debug.Log($"Textura original: {sourceTexture.width}x{sourceTexture.height}");
      // Debug.Log($"Textura segmentada: {segmentedTexture.width}x{segmentedTexture.height}");
      
      // // Verifica alguns pixels do canal alpha
      // int totalPixels = segmentedTexture.width * segmentedTexture.height;
      // int transparentCount = 0;
      // int opaqueCount = 0;
      
      // Color[] pixels = segmentedTexture.GetPixels();
      // for (int i = 0; i < pixels.Length; i++)
      // {
      //   if (pixels[i].a < 0.1f)
      //     transparentCount++;
      //   else if (pixels[i].a > 0.9f)
      //     opaqueCount++;
      // }
      
      // Debug.Log($"Pixels transparentes (alpha < 0.1): {transparentCount}/{totalPixels} ({(transparentCount * 100f / totalPixels):F1}%)");
      // Debug.Log($"Pixels opacos (alpha > 0.9): {opaqueCount}/{totalPixels} ({(opaqueCount * 100f / totalPixels):F1}%)");
      
      // // Pixel do centro
      // Color centerPixel = segmentedTexture.GetPixel(segmentedTexture.width / 2, segmentedTexture.height / 2);
      // Debug.Log($"Pixel central - R:{centerPixel.r:F2}, G:{centerPixel.g:F2}, B:{centerPixel.b:F2}, A:{centerPixel.a:F2}");

      // var newScreenshot = new ScreenShotComponent(transform, ScreenshotContainer, MenuList, sourceTexture, m_debugText);
      var newScreenshot = new ScreenShotComponent(transform, ScreenshotContainer, MenuList, segmentedTexture, m_debugText);
      
      // Libera a textura temporária se não for da câmera
      // if (webCamTexture == null || !webCamTexture.isPlaying)
      // {
      //   Destroy(sourceTexture);
      // }
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
