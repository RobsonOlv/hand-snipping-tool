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
      // Texture2D segmentedTexture = AirSnipSegmentationInstance.GetSegmentationMask(sourceTexture);

      var newScreenshot = new ScreenShotComponent(transform, ScreenshotContainer, MenuList, sourceTexture, m_debugText);
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
