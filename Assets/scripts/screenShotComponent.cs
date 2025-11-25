using System.Collections;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScreenShotComponent
{
  public GameObject interactionContainer;
  public GameObject imageObj;
  private GameObject frontQuad;
  private MakeInteractable interactableMaker;
  public Texture2D currentTexture; // Armazena a textura atual (pode ser original ou segmentada)

  public ScreenShotComponent(Transform cameraCanvas, GameObject parent, GameObject menu, Texture2D texture, TextMeshProUGUI debugText, float worldWidth, float worldHeight)
  {
    try
    {
      // Armazenar a textura inicial
      currentTexture = texture;
      
      Material cubeMaterial = Resources.Load<Material>("Materials/ScreenshotBackgroundMaterial");
      
      if (cubeMaterial == null)
      {
        throw new System.Exception("Material 'ScreenshotBackgroundMaterial' não encontrado em Resources/Materials.");
      }
      
      // Criar instância do material e aplicar textura com escala invertida em Y
      Material cubeMaterialInstance = new Material(cubeMaterial);
      // cubeMaterialInstance.SetTexture("_BaseMap", texture);
      // cubeMaterialInstance.SetTextureScale("_BaseMap", new Vector2(-1, -1)); // Inverte X e Y

      interactionContainer = new GameObject("ScreenshotInteractionContainer");
      interactionContainer.transform.position = cameraCanvas.position;
      interactionContainer.transform.rotation = cameraCanvas.rotation;
      
      // Adicionar este componente ao container para permitir acesso posterior
      var componentHolder = interactionContainer.AddComponent<ScreenShotComponentHolder>();
      componentHolder.screenshotComponent = this;

      // Armazenar as dimensões reais do screenshot
      var dimensions = interactionContainer.AddComponent<ScreenshotDimensions>();
      dimensions.worldWidth = worldWidth;
      dimensions.worldHeight = worldHeight;

      AudioHolder audioHolder = interactionContainer.AddComponent<AudioHolder>();


      imageObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
      imageObj.name = "ScreenshotCube";
      imageObj.transform.SetParent(interactionContainer.transform, false);
      var cubeRenderer = imageObj.GetComponent<Renderer>();
      cubeRenderer.material = cubeMaterialInstance;

      // Usar as dimensões world space do preview (mesmas do RawImage)
      float cubeDepth = 0.005f;

      // Aplicar escala diretamente no cubo usando as dimensões do preview
      imageObj.transform.localScale = new Vector3(worldWidth, worldHeight, cubeDepth);

      GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
      quad.name = "ScreenshotQuad";
      quad.transform.SetParent(imageObj.transform, false);
      quad.transform.localPosition = new Vector3(0, 0, -0.65f); //traz pra frente do cubo
      quad.transform.localRotation = Quaternion.identity;
      quad.transform.localScale = Vector3.one;
      
      frontQuad = quad; // Salvar referência para atualização posterior

      var quadRenderer = quad.GetComponent<Renderer>();
      
      // Usa shader URP Unlit com transparência (configurado programaticamente)
      Shader transparentShader = Shader.Find("Universal Render Pipeline/Lit");
      
      if (transparentShader == null)
      {
        // Fallback para shaders built-in
        transparentShader = Shader.Find("Unlit/Transparent");
      }

      if (transparentShader == null)
      {
        throw new System.Exception("Nenhum shader de transparência foi encontrado. Verifique se o URP está configurado no projeto.");
      }
      
      Material quadMaterial = new Material(transparentShader);
      
      // Configura para modo Transparent (URP)
      if (transparentShader.name.Contains("Universal Render Pipeline"))
      {
        // URP usa _BaseMap ao invés de _MainTex
        quadMaterial.SetTexture("_BaseMap", texture);
        
        // URP: Surface Type = Transparent
        quadMaterial.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
        quadMaterial.SetFloat("_Blend", 0); // 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply
        
        // Configura blend mode para alpha blending
        quadMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        quadMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        quadMaterial.SetInt("_ZWrite", 0);
        
        // Render queue para transparent
        quadMaterial.renderQueue = 3000;
        
        // Habilita keywords corretas
        quadMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        quadMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
      }
      else if (transparentShader.name == "Standard")
      {
        // Built-in Standard shader usa _MainTex
        quadMaterial.mainTexture = texture;
        
        // Built-in Standard shader
        quadMaterial.SetFloat("_Mode", 3); // Transparent mode
        quadMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        quadMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        quadMaterial.SetInt("_ZWrite", 0);
        quadMaterial.DisableKeyword("_ALPHATEST_ON");
        quadMaterial.EnableKeyword("_ALPHABLEND_ON");
        quadMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        quadMaterial.renderQueue = 3000;
      }
      else
      {
        // Fallback genérico
        quadMaterial.mainTexture = texture;
      }

      quadRenderer.material = quadMaterial;
      Debug.Log("Shader usado em runtime: " + quadMaterial.shader.name);

      Material cubeFaceMaterial = Resources.Load<Material>("Materials/CubeFaceMaterial");

      // Criar quads para todas as outras faces do cubo
      // Quad traseiro (Back)
      GameObject backQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
      backQuad.name = "BackQuad";
      backQuad.transform.SetParent(imageObj.transform, false);
      backQuad.transform.localPosition = new Vector3(0, 0, 0.65f);
      backQuad.transform.localRotation = Quaternion.Euler(0, 0, 0);
      backQuad.transform.localScale = Vector3.one;
      backQuad.GetComponent<Renderer>().material = new Material(cubeFaceMaterial);
      
      // Quad direito (Right)
      GameObject rightQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
      rightQuad.name = "RightQuad";
      rightQuad.transform.SetParent(imageObj.transform, false);
      rightQuad.transform.localPosition = new Vector3(0.5f, 0, 0);
      rightQuad.transform.localRotation = Quaternion.Euler(0, 90, 0);
      rightQuad.transform.localScale = new Vector3(1.3f, 1f, 1f); // Ajusta para profundidade do cubo
      rightQuad.GetComponent<Renderer>().material = new Material(cubeFaceMaterial);
      
      // Quad esquerdo (Left)
      GameObject leftQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
      leftQuad.name = "LeftQuad";
      leftQuad.transform.SetParent(imageObj.transform, false);
      leftQuad.transform.localPosition = new Vector3(-0.5f, 0, 0);
      leftQuad.transform.localRotation = Quaternion.Euler(0, -90, 0);
      leftQuad.transform.localScale = new Vector3(1.3f, 1f, 1f); // Ajusta para profundidade do cubo
      leftQuad.GetComponent<Renderer>().material = new Material(cubeFaceMaterial);
      
      // Quad superior (Top)
      GameObject topQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
      topQuad.name = "TopQuad";
      topQuad.transform.SetParent(imageObj.transform, false);
      topQuad.transform.localPosition = new Vector3(0, 0.5f, 0);
      topQuad.transform.localRotation = Quaternion.Euler(-90, 0, 0);
      topQuad.transform.localScale = new Vector3(1f, 1.3f, 1f); // Ajusta para profundidade do cubo
      topQuad.GetComponent<Renderer>().material = new Material(cubeFaceMaterial);
      
      // Quad inferior (Bottom)
      GameObject bottomQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
      bottomQuad.name = "BottomQuad";
      bottomQuad.transform.SetParent(imageObj.transform, false);
      bottomQuad.transform.localPosition = new Vector3(0, -0.5f, 0);
      bottomQuad.transform.localRotation = Quaternion.Euler(90, 0, 0);
      bottomQuad.transform.localScale = new Vector3(1f, 1.3f, 1f); // Ajusta para profundidade do cubo
      bottomQuad.GetComponent<Renderer>().material = new Material(cubeFaceMaterial);


      MakeGrabbable grabbableMaker = new MakeGrabbable();
      grabbableMaker.SetupGrabbable(interactionContainer);
      
      interactableMaker = new MakeInteractable();
      interactableMaker.SetupInteractable(interactionContainer, imageObj, menu, texture, debugText);
    }
    catch (System.Exception e)
    {
      if (debugText != null)
      {
        debugText.text = $"Erro ScreenShotComponent: \n{e.Message}\n{e.StackTrace}";
        debugText.enabled = true;
      }
    }
  }

  public void DestroySelf()
  {
    if (interactionContainer != null)
    {
        Object.Destroy(interactionContainer);
    }
  }

  // Método público para atualizar a textura do quad frontal
  public void UpdateTexture(Texture2D newTexture)
  {
    // Atualizar a textura armazenada
    currentTexture = newTexture;
    
    if (frontQuad != null)
    {
      var renderer = frontQuad.GetComponent<Renderer>();
      if (renderer != null && renderer.material != null)
      {
        renderer.material.SetTexture("_BaseMap", newTexture);
      }
    }
  }
}
