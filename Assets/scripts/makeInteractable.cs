using UnityEngine;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using TMPro;

public class MakeInteractable : MonoBehaviour
{
  public void SetupInteractable(GameObject parent, GameObject screenshot, GameObject menu, Texture2D texture, TextMeshProUGUI debugText = null)
  {
    // Criar Surface
    GameObject surfaceObj = new GameObject("Surface");
    surfaceObj.transform.SetParent(parent.transform, false);
    surfaceObj.transform.localPosition = new Vector3(0, 0, 0.02f); // Ajustado: 2cm em world units
    surfaceObj.transform.localRotation = Quaternion.identity;
    surfaceObj.transform.localScale = Vector3.one;

    var planeSurface = surfaceObj.AddComponent<PlaneSurface>();
    var clippedSurface = surfaceObj.AddComponent<ClippedPlaneSurface>();
    clippedSurface.InjectPlaneSurface(planeSurface);
    var boundsSurface = surfaceObj.AddComponent<BoundsClipper>();
    // Usar o tamanho do parent (screenshot) em local space - já que é filho do parent
    Vector3 parentScale = screenshot.transform.localScale;
    // boundsSurface size com 75% da largura e da altura do parent
    boundsSurface.Size = new Vector3(parentScale.x * 0.75f, parentScale.y * 0.75f, 0.01f); // Plano com espessura mínima
    clippedSurface.InjectClippers(new List<IBoundsClipper> { boundsSurface });

    // Criar e configurar PokeInteractable
    var pokeInteractable = parent.AddComponent<PokeInteractable>();
    pokeInteractable.InjectSurfacePatch(clippedSurface);

    var visualInteraction = screenshot.AddComponent<PokeInteractableVisual>();
    visualInteraction.InjectPokeInteractable(pokeInteractable);
    visualInteraction.InjectButtonBaseTransform(surfaceObj.transform);

    // Não passa a textura diretamente - busca do holder quando clicar
    pokeInteractable.WhenStateChanged += (args) => UpdateState(args, parent, menu, debugText);
  }

  private void UpdateState(InteractableStateChangeArgs args, GameObject parent, GameObject menu, TextMeshProUGUI debugText)
  {
    if (args.NewState == InteractableState.Select)
    {
      try
      {
        // Buscar a textura atual do ScreenShotComponentHolder
        ScreenShotComponentHolder holder = parent.GetComponent<ScreenShotComponentHolder>();
        Texture2D currentTexture = null;
        
        if (holder != null && holder.screenshotComponent != null)
        {
          currentTexture = holder.screenshotComponent.currentTexture;
        }
        
        if (currentTexture == null)
        {
          Debug.LogError("Current texture not found in ScreenShotComponentHolder.");
          return;
        }
        
        var menuOptions = menu.GetComponent<MenuOptions>();
        menuOptions.screenshotContainer = parent;
        menuOptions.screenshotImage = currentTexture; // Usa a textura atual (original ou segmentada)
        menuOptions.debugText = debugText;

        AudioHolder audioHolder = parent.GetComponent<AudioHolder>();
        if (audioHolder != null)
        {
          Debug.Log("[UPDATESTATE] AudioHolder found");
          
          // Carregar áudio TTS do audioHolder
          if (audioHolder.audioClip == null)
          {
            Debug.Log("[UPDATESTATE] has'nt audioClip");
            menuOptions.cachedAudioClip = null;
            menuOptions.audioSource.clip = null;
          }
          else
          {
            Debug.Log("[UPDATESTATE] has audioClip");
            menuOptions.cachedAudioClip = audioHolder.audioClip;
            menuOptions.audioSource.clip = audioHolder.audioClip;
          }
          
          // Carregar áudio gravado do audioHolder (se existir)
          if (audioHolder.recordedClip != null)
          {
            Debug.Log("[UPDATESTATE] has recordedClip");
            menuOptions.cachedRecordingAudioSource.clip = audioHolder.recordedClip;
            menuOptions.recordedAudioClip = audioHolder.recordedClip;
          }
          else
          {
            Debug.Log("[UPDATESTATE] has'nt recordedClip");
            menuOptions.cachedRecordingAudioSource.clip = null;
            menuOptions.recordedAudioClip = null;
          }
        }
        else
        {
          if(debugText != null)
          {
            debugText.text = "[UpdateState] audioHolder not found";
            debugText.enabled = true;
          }
        }

        var follow = menu.GetComponent<FollowObject>();
        if (follow == null)
        {
          follow = menu.AddComponent<FollowObject>();
        }
        follow.target = parent.transform;
        menu.SetActive(true);
      }
      catch (System.Exception e)
      {
        Debug.LogError("Error in UpdateState: " + e.Message);
        if (debugText != null)
        {
          debugText.text = "Erro ao atualizar estado: " + e.Message;
          debugText.enabled = true;
        }
      }
    }
  }
}
