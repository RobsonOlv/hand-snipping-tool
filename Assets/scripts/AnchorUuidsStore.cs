using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.Samples;
using UnityEngine;
public static class AnchorUuidStore
{
    private static string FilePath => Path.Combine(Application.persistentDataPath, "screenshot_anchors.json");

    public static void Save(ScreenshotAnchorData data)
    {
        var list = LoadAll();
        list.RemoveAll(d => d.uuid == data.uuid);
        list.Add(data);
        string json = JsonUtility.ToJson(new Wrapper { anchors = list });
        File.WriteAllText(FilePath, json);
    }

    public static void Remove(Guid uuid)
    {
        var list = LoadAll();
        
        // Encontrar itens para remover e deletar arquivos associados
        var itemsToRemove = list.Where(d => d.uuid == uuid.ToString()).ToList();
        foreach (var item in itemsToRemove)
        {
            if (!string.IsNullOrEmpty(item.texturePath) && File.Exists(item.texturePath))
            {
                try 
                {
                    File.Delete(item.texturePath);
                    Debug.Log($"[AnchorUuidStore] Deleted texture file: {item.texturePath}");
                }
                catch (Exception e) { Debug.LogError($"[AnchorUuidStore] Failed to delete texture: {e.Message}"); }
            }

            if (!string.IsNullOrEmpty(item.audioPath) && File.Exists(item.audioPath))
            {
                try
                {
                    File.Delete(item.audioPath);
                    Debug.Log($"[AnchorUuidStore] Deleted audio file: {item.audioPath}");
                }
                catch (Exception e) { Debug.LogError($"[AnchorUuidStore] Failed to delete audio: {e.Message}"); }
            }
        }

        int removedCount = list.RemoveAll(d => d.uuid == uuid.ToString());
        
        if (removedCount > 0)
        {
            string json = JsonUtility.ToJson(new Wrapper { anchors = list });
            File.WriteAllText(FilePath, json);
            Debug.Log($"[AnchorUuidStore] Removed anchor with UUID: {uuid}");
        }
        else
        {
            Debug.LogWarning($"[AnchorUuidStore] UUID not found: {uuid}");
        }
    }

    public static List<ScreenshotAnchorData> LoadAll()
    {
        if (!File.Exists(FilePath)) return new List<ScreenshotAnchorData>();
        string json = File.ReadAllText(FilePath);
        return JsonUtility.FromJson<Wrapper>(json).anchors;
    }

    public static void EraseAll()
    {
        // Deletar todos os arquivos associados antes de apagar o JSON
        var list = LoadAll();
        foreach (var item in list)
        {
            if (!string.IsNullOrEmpty(item.texturePath) && File.Exists(item.texturePath))
            {
                try { File.Delete(item.texturePath); } catch { }
            }
            if (!string.IsNullOrEmpty(item.audioPath) && File.Exists(item.audioPath))
            {
                try { File.Delete(item.audioPath); } catch { }
            }
        }

        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
            Debug.Log("[AnchorUuidStore] All anchors erased and file deleted.");
        }
        else
        {
            Debug.LogWarning("[AnchorUuidStore] No anchor file found to erase.");
        }
    }


    [System.Serializable]
    private class Wrapper
    {
        public List<ScreenshotAnchorData> anchors;
    }
}
