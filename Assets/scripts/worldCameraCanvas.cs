using System;
using PassthroughCameraSamples;
using System.Collections;
using System.Collections.Generic;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class WorldCameraCanvas : MonoBehaviour
{
  public PassthroughCameraAccess m_cameraAccess; // Referência ao PassthroughCameraAccess (olho esquerdo)
  private Vector3 lastWorldCenter; // Último centro calculado para usar na captura
  public GameObject ScreenshotContainer;
  public GameObject MenuList;
  public bool isActive = false;
  [SerializeField] private RawImage m_image;
  private Texture2D m_cameraSnapshot;
  private Color32[] m_pixelsBuffer;
  public TextMeshProUGUI m_debugText;
  public AnchorManager anchorManager;
  public float canvaSizeHorizontal = 0.15f;
  public float canvaSizeVertical = 0.25f;
  public AirSnipSegmentation AirSnipSegmentationInstance;
  private List<OVRSpatialAnchor.UnboundAnchor> _unboundAnchors = new();
  private const string AnchorIdsKey = "SavedAnchorIds";
  
  // Preview do recorte
  private Rect currentPreviewRect = new Rect(0.25f, 0.25f, 0.5f, 0.5f);

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

  public void MakeCameraSnapshot(Vector3 snapshotCenter2, float screenshotWidth, float screenshotHeight)
  {
    try
    {
      Texture2D sourceTexture;
      
      // var webCamTexture = webCamTextureManager.WebCamTexture;
      if (m_cameraAccess == null || !m_cameraAccess.IsPlaying)
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
        var passthroughTexture = m_cameraAccess.GetTexture();
        Debug.Log($"[WorldCameraCanvas] Capturando textura da câmera: {passthroughTexture.width}x{passthroughTexture.height}");
        var size = m_cameraAccess.CurrentResolution;
        sourceTexture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);

        var pixels = m_cameraAccess.GetColors();

        sourceTexture.LoadRawTextureData(pixels);
        sourceTexture.Apply();
      }

      // Marcar o centro calculado na imagem para debug
      // MarkCenterInTexture(sourceTexture, lastWorldCenter);
      
      // Aplicar o recorte baseado no currentPreviewRect
      Texture2D croppedTexture = CropTexture(sourceTexture, currentPreviewRect);

      var screenshotParams = new ScreenShotCreationParams
      {
        CameraCanvas = transform,
        Menu = MenuList,
        Texture = croppedTexture,
        DebugText = m_debugText,
        WorldWidth = screenshotWidth,
        WorldHeight = screenshotHeight
      };

      var newScreenshot = new ScreenShotComponent(screenshotParams);
    }
    catch (System.Exception e)
    {
      m_debugText.text = $"Erro WorldCameraCanvas:\n{e.Message}\n{e.StackTrace}";
      m_debugText.enabled = true;
    }
  }
  public void ResumeStreamingFromCamera()
  {
    m_image.texture = m_cameraAccess.GetTexture();
    m_image.uvRect = currentPreviewRect;
  }
  
  // Atualiza o preview do recorte baseado na posição das mãos
  public void UpdatePreviewRect(Vector3 worldCenter, float worldWidth, float worldHeight)
  {
    if (m_cameraAccess == null || !m_cameraAccess.IsPlaying)
    {
      return;
    }
    
    // Salvar o centro para usar na captura
    lastWorldCenter = worldCenter;
    
    // Obter a pose da câmera
    Pose cameraPose = m_cameraAccess.GetCameraPose();
    
    // Calcular os cantos do retângulo em world space
    Vector3 cameraRight = cameraPose.rotation * Vector3.right;
    Vector3 cameraUp = cameraPose.rotation * Vector3.up;
    
    Vector3 topLeft = worldCenter - cameraRight * (worldWidth / 2f) + cameraUp * (worldHeight / 2f);
    Vector3 bottomRight = worldCenter + cameraRight * (worldWidth / 2f) - cameraUp * (worldHeight / 2f);
    
    // Converter cantos para viewport space usando a API do PassthroughCameraAccess
    Vector2 viewportTopLeft = m_cameraAccess.WorldToViewportPoint(topLeft);
    Vector2 viewportBottomRight = m_cameraAccess.WorldToViewportPoint(bottomRight);
    
    // Calcular largura e altura em viewport space
    float viewportWidth = Mathf.Abs(viewportBottomRight.x - viewportTopLeft.x);
    float viewportHeight = Mathf.Abs(viewportTopLeft.y - viewportBottomRight.y);
    
    // Calcular posição x, y do canto inferior esquerdo (uvRect usa bottom-left origin)
    float uvX = viewportTopLeft.x;
    float uvY = viewportBottomRight.y;
    
    // Clampar valores entre 0 e 1
    uvX = Mathf.Clamp01(uvX);
    uvY = Mathf.Clamp01(uvY);
    viewportWidth = Mathf.Clamp(viewportWidth, 0f, 1f - uvX);
    viewportHeight = Mathf.Clamp(viewportHeight, 0f, 1f - uvY);
    
    // Criar o Rect UV
    currentPreviewRect = new Rect(uvX, uvY, viewportWidth, viewportHeight);
    
    // Aplicar ao RawImage
    if (m_image != null && m_image.enabled)
    {
      m_image.uvRect = currentPreviewRect;
    }
  }

  // Marca o centro calculado na textura com uma cruz vermelha para debug
  private void MarkCenterInTexture(Texture2D texture, Vector3 worldCenter)
  {
    if (texture == null || m_cameraAccess == null || !m_cameraAccess.IsPlaying) return;
    
    // Converter worldCenter para viewport space usando a API
    Vector2 viewportPoint = m_cameraAccess.WorldToViewportPoint(worldCenter);
    
    // Converter viewport para pixel coordinates
    int pixelX = Mathf.RoundToInt(viewportPoint.x * texture.width);
    int pixelY = Mathf.RoundToInt(viewportPoint.y * texture.height);
    
    // Desenhar uma cruz vermelha (10x10 pixels)
    int crossSize = 10;
    Color markColor = Color.red;
    
    for (int i = -crossSize; i <= crossSize; i++)
    {
      // Linha horizontal
      int x = pixelX + i;
      if (x >= 0 && x < texture.width && pixelY >= 0 && pixelY < texture.height)
      {
        texture.SetPixel(x, pixelY, markColor);
      }
      
      // Linha vertical
      int y = pixelY + i;
      if (pixelX >= 0 && pixelX < texture.width && y >= 0 && y < texture.height)
      {
        texture.SetPixel(pixelX, y, markColor);
      }
    }
    
    texture.Apply();
    Debug.Log($"Centro marcado em pixel ({pixelX}, {pixelY}) - viewport ({viewportPoint.x:F2}, {viewportPoint.y:F2})");
  }
  
  // Recorta a textura baseado no Rect UV (viewport coordinates)
  private Texture2D CropTexture(Texture2D sourceTexture, Rect uvRect)
  {
    // Converter UV coordinates (0-1) para pixel coordinates
    int startX = Mathf.RoundToInt(uvRect.x * sourceTexture.width);
    int startY = Mathf.RoundToInt(uvRect.y * sourceTexture.height);
    int width = Mathf.RoundToInt(uvRect.width * sourceTexture.width);
    int height = Mathf.RoundToInt(uvRect.height * sourceTexture.height);
    
    // Clampar valores para garantir que estão dentro dos limites da textura
    startX = Mathf.Clamp(startX, 0, sourceTexture.width - 1);
    startY = Mathf.Clamp(startY, 0, sourceTexture.height - 1);
    width = Mathf.Clamp(width, 1, sourceTexture.width - startX);
    height = Mathf.Clamp(height, 1, sourceTexture.height - startY);
    
    Debug.Log($"Recortando textura: origem=({startX}, {startY}), tamanho=({width}x{height}) de {sourceTexture.width}x{sourceTexture.height}");
    
    // Criar nova textura com o tamanho recortado
    Texture2D croppedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
    
    // Copiar os pixels da região de interesse
    Color[] pixels = sourceTexture.GetPixels(startX, startY, width, height);
    croppedTexture.SetPixels(pixels);
    croppedTexture.Apply();
    
    return croppedTexture;
  }
  
  private IEnumerator Start()
  {
    while (!m_cameraAccess.IsPlaying)
    {
      yield return null;
    }
    m_image.enabled = false;
    m_debugText.text = "";
    m_debugText.enabled = false;
    ResumeStreamingFromCamera();
  }
}
