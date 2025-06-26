using UnityEngine;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using TMPro;

public class MakeInteractable : MonoBehaviour
{
  public void SetupInteractable(GameObject parent, GameObject screenshot, GameObject menu, Texture2D texture, TextMeshProUGUI debugText = null)
  {
    // Garantir que tenha um collider
    // var collider = screenshot.GetComponent<BoxCollider>();
    // if (collider == null)
    // {
    //   collider = screenshot.AddComponent<BoxCollider>();
    //   collider.size = new Vector3(0.1f, 0.1f, 0.001f);
    // }

    // Criar Surface
    GameObject surfaceObj = new GameObject("Surface");
    surfaceObj.transform.SetParent(parent.transform, false);
    surfaceObj.transform.localPosition = new Vector3(0, 0, 5f);
    surfaceObj.transform.localRotation = Quaternion.identity;
    surfaceObj.transform.localScale = Vector3.one;

    var planeSurface = surfaceObj.AddComponent<PlaneSurface>();
    var clippedSurface = surfaceObj.AddComponent<ClippedPlaneSurface>();
    clippedSurface.InjectPlaneSurface(planeSurface);
    var boundsSurface = surfaceObj.AddComponent<BoundsClipper>();
    boundsSurface.Size = new Vector3(0.8f, 0.8f, 1f);
    // boundsSurface.Position = Vector3.zero;
    clippedSurface.InjectClippers(new List<IBoundsClipper> { boundsSurface });

    // Criar e configurar PokeInteractable
    var pokeInteractable = parent.AddComponent<PokeInteractable>();
    pokeInteractable.InjectSurfacePatch(clippedSurface);

    var visualInteraction = screenshot.AddComponent<PokeInteractableVisual>();
    visualInteraction.InjectPokeInteractable(pokeInteractable);
    visualInteraction.InjectButtonBaseTransform(surfaceObj.transform);

    pokeInteractable.WhenStateChanged += (args) => UpdateState(args, parent, menu, texture, debugText);
  }

  private void UpdateState(InteractableStateChangeArgs args, GameObject parent, GameObject menu, Texture2D texture, TextMeshProUGUI debugText)
  {
    if (args.NewState == InteractableState.Select)
    {
      // if (!menu.activeSelf)
      //   menu.SetActive(true);
      
      // var menuOptions = menu.GetComponent<MenuOptions>();

      // menuOptions.HandleMenuStateChange(parent, texture, debugText);
      try
      {
        var menuOptions = menu.GetComponent<MenuOptions>();
        menuOptions.screenshotContainer = parent;
        menuOptions.screenshotImage = texture;
        menuOptions.debugText = debugText;

        AudioHolder audioHolder = parent.GetComponent<AudioHolder>();
        if (audioHolder != null)
        {
          Debug.Log("[UPDATESTATE] AudioHolder found");
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
