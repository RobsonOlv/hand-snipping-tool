using PassthroughCameraSamples;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CameraTest : MonoBehaviour
{
  public WebCamTextureManager webCamTextureManager;
  public RawImage rawImage;

  private IEnumerator Start()
  {
    while (webCamTextureManager.WebCamTexture == null)
    {
      yield return null;
    }
    rawImage.texture = webCamTextureManager.WebCamTexture;
  }
} 