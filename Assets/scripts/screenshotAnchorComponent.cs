using System.Collections;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScreenShotAnchorComponent
{
  public GameObject interactionContainer;
  public GameObject imageObj;

  public ScreenShotAnchorComponent(GameObject loadedObj, GameObject menu, Texture2D texture)
  {
    try
    {
      // Texture2D textureCopy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
      // RenderTexture tempRT = RenderTexture.GetTemporary(texture.width, texture.height);
      // Graphics.Blit(texture, tempRT);
      // RenderTexture.active = tempRT;
      // textureCopy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
      // textureCopy.Apply();
      // RenderTexture.active = null;
      // RenderTexture.ReleaseTemporary(tempRT);

      interactionContainer = loadedObj;

      Material cubeMaterial = Resources.Load<Material>("Materials/CubeMaterial");

      if (cubeMaterial == null)
      {
        throw new System.Exception("Material 'CubeMaterial' não encontrado em Resources/Materials.");
      }

      AudioHolder audioHolder = interactionContainer.AddComponent<AudioHolder>();


      imageObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
      imageObj.name = "ScreenshotCube";
      imageObj.transform.SetParent(interactionContainer.transform, false);
      // imageObj.transform.localPosition += new Vector3(0, 0, 1f);
      // imageObj.transform.position = cameraCanvas.position;
      // imageObj.transform.rotation = cameraCanvas.rotation;
      var cubeRenderer = imageObj.GetComponent<Renderer>();
      cubeRenderer.material = cubeMaterial;

      // imageObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
      // imageObj.name = "TemporaryImage";
      // var renderer = imageObj.GetComponent<Renderer>();
      // Shader unlitShader = Shader.Find("Unlit/Texture");
      // if (unlitShader == null)
      // {
      //     throw new System.Exception("Shader 'Unlit/Texture' não foi encontrado.");
      // }
      // Material material = new Material(unlitShader);
      // material.mainTexture = textureCopy;
      // renderer.material = material;

      // imageObj.transform.position = cameraCanvas.position;
      // imageObj.transform.rotation = cameraCanvas.rotation;

      // Ajusta escala proporcional ao tamanho da textura ou do canvas
      float width = texture.width;
      float height = texture.height;
      float aspectRatio = width / height;

      float targetWidth = 0.2f;
      float targetHeight = targetWidth / aspectRatio;
      float cubeDepth = 0.005f;

      interactionContainer.transform.localScale = new Vector3(targetWidth, targetHeight, cubeDepth);
      // imageObj.transform.localScale = new Vector3(targetWidth, targetHeight, cubeDepth);
      imageObj.transform.localScale = new Vector3(1f, 1f, 1f); // Ajusta a posição do cubo

      GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
      quad.name = "ScreenshotQuad";
      quad.transform.SetParent(imageObj.transform, false);
      quad.transform.localPosition = new Vector3(0, 0, -0.65f); //traz pra frente do cubo
      quad.transform.localRotation = Quaternion.identity;
      quad.transform.localScale = Vector3.one;

      var quadRenderer = quad.GetComponent<Renderer>();
      Shader unlitShader = Shader.Find("Unlit/Texture");
      if (unlitShader == null)
      {
        throw new System.Exception("Shader 'Unlit/Texture' não foi encontrado.");
      }

      Material quadMaterial = new Material(unlitShader);
      quadMaterial.mainTexture = texture;
      quadRenderer.material = quadMaterial;


      MakeGrabbable grabbableMaker = new MakeGrabbable();
      grabbableMaker.SetupGrabbable(interactionContainer);

      MakeInteractable interactableMaker = new MakeInteractable();
      interactableMaker.SetupInteractable(interactionContainer, imageObj, menu, texture);

      var anchor = interactionContainer.GetComponent<OVRSpatialAnchor>();
      anchor.enabled = false;
    }
    catch (System.Exception e)
    {
      Debug.LogError($"[ScreenShotAnchorComponent] Error creating screenshot anchor: {e.Message}\n{e.StackTrace}");
      // Optionally, you can rethrow the exception or handle it as needed
      // throw;
    }
  }

  public void DestroySelf()
  {
    if (interactionContainer != null)
    {
        Object.Destroy(interactionContainer);
    }
  }
}
