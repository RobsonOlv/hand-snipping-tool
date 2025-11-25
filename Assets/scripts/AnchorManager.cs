using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AnchorManager : MonoBehaviour
{
    public GameObject MenuList;
    private List<OVRSpatialAnchor.UnboundAnchor> _unboundAnchors = new();
    private const string AnchorIdsKey = "SavedAnchorIds";

    private void Start()
    {
        // AnchorUuidStore.EraseAll();
        LoadAnchorsByUuid();
    }

    public async void SaveAnchor(GameObject screenshotContainer, Texture2D texture = null)
    {
        var existingAnchor = screenshotContainer.GetComponent<OVRSpatialAnchor>();
        if (existingAnchor != null)
        {
            Debug.Log($"[AnchorManager] Erasing {existingAnchor.Uuid}.");
            await EraseAnchor(existingAnchor);
        }

        var anchor = screenshotContainer.AddComponent<OVRSpatialAnchor>();
        await anchor.WhenLocalizedAsync();

        Debug.Log($"[AnchorManager] anchor after Erase {anchor.Uuid}...");
        await UniTask.WaitUntil(() => anchor.Uuid != Guid.Empty);
        var result = await anchor.SaveAnchorAsync();
        Debug.Log($"[AnchorManager] Saving anchor with {anchor.Uuid}...");
        if (result.Success)
        {
            Debug.LogWarning($"[AnchorManager] Anchor with {anchor.Uuid} saved");
            if (texture != null)
            {
                string filename = $"screenshot_{anchor.Uuid}.png";
                string fullPath = Path.Combine(Application.persistentDataPath, filename);
                File.WriteAllBytes(fullPath, texture.EncodeToPNG());

                var data = new ScreenshotAnchorData
                {
                    uuid = anchor.Uuid.ToString(),
                    texturePath = fullPath,
                    textureHeight = texture.height,
                    textureWidth = texture.width,
                    localScale = anchor.transform.localScale
                };

                AnchorUuidStore.Save(data);
            }
        }
        else
        {
            Debug.LogWarning($"[AnchorManager] Anchor with {anchor.Uuid} failed to saved");
        }
        anchor.enabled = false;
    }

    public async UniTask EraseAnchor(OVRSpatialAnchor anchor)
    {
        var uuid = anchor.Uuid;
        var result = await anchor.EraseAnchorAsync();
        if (result.Success)
        {
            Debug.LogWarning($"[AnchorManager] Erased anchor data {uuid}");
            AnchorUuidStore.Remove(uuid);
            Destroy(anchor);
            await UniTask.NextFrame();
        }
        else
        {
            Debug.LogError($"[AnchorManager] Failed to erased anchor data {uuid}");
        }
    }

    async void LoadAnchorsByUuid()
    {
        var allData = AnchorUuidStore.LoadAll(); // Carrega todos os ScreenshotAnchorData salvos
        var uuids = allData.Select(d => Guid.Parse(d.uuid)).ToList();
        Debug.Log($"[AnchorManager] Loading anchors with uuids: {string.Join(", ", uuids)}");

        _unboundAnchors.Clear();

        // Step 1: Load
        var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(uuids, _unboundAnchors);

        if (result.Success)
        {
            Debug.LogWarning($"[AnchorManager] Anchors loaded successfully.");

            // Note result.Value is the same as _unboundAnchors
            foreach (var unboundAnchor in result.Value)
            {
                var data = allData.FirstOrDefault(d => d.uuid == unboundAnchor.Uuid.ToString());
                if (data == null)
                {
                    Debug.LogWarning($"[AnchorManager] No data found for anchor {unboundAnchor.Uuid}. Skipping localization.");
                    continue;
                }
                // Step 2: Localize
                bool success = await unboundAnchor.LocalizeAsync();
                if (success)
                {
                    var interactionContainer = new GameObject($"Anchor_{unboundAnchor.Uuid}");
                    var spatialAnchor = interactionContainer.AddComponent<OVRSpatialAnchor>();
                    unboundAnchor.BindTo(spatialAnchor);

                    await spatialAnchor.WhenLocalizedAsync();

                    interactionContainer.transform.localScale = data.localScale;

                    if (!File.Exists(data.texturePath))
                    {
                        Debug.LogWarning($"[AnchorManager] Texture not found: {data.texturePath}");
                        return;
                    }

                    byte[] textureBytes = File.ReadAllBytes(data.texturePath);
                    Texture2D texture = new Texture2D(data.textureWidth, data.textureHeight, TextureFormat.RGBA32, false);
                    texture.LoadImage(textureBytes);

                    var screenshotParams = new ScreenShotCreationParams
                    {
                        AnchorObject = interactionContainer,
                        Menu = MenuList,
                        Texture = texture
                    };

                    new ScreenShotComponent(screenshotParams);
                }
                else
                {
                    Debug.LogError($"[AnchorManager] Localization failed for anchor {unboundAnchor.Uuid}");
                }
            }
        }
        else
        {
            Debug.LogError($"[AnchorManager] Load failed with error {result.Status}.");
        }
    }
}
