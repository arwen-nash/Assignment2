using UnityEditor;
using UnityEngine;

public static class ConvertToSpawnPoints
{
    [MenuItem("Tools/Convert Selected Prefab Instances To Spawn Points")]
    static void Convert()
    {
        var holder = GameObject.Find("SpawnPoints");
        if (holder == null)
        {
            holder = new GameObject("SpawnPoints");
            Undo.RegisterCreatedObjectUndo(holder, "Create SpawnPoints");
        }

        int converted = 0;
        foreach (var go in Selection.gameObjects)
        {
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (prefab == null)
            {
                Debug.LogWarning($"'{go.name}' is not a prefab instance, so it was skipped. Drag it into your Prefabs folder first.");
                continue;
            }

            var marker = new GameObject(go.name + " Spawn");
            Undo.RegisterCreatedObjectUndo(marker, "Create spawn point");
            marker.transform.SetParent(holder.transform, false);
            marker.transform.SetPositionAndRotation(go.transform.position, go.transform.rotation);
            marker.transform.localScale = go.transform.lossyScale;

            var sp = marker.AddComponent<SpawnPoint>();
            sp.prefab = prefab;

            Undo.DestroyObjectImmediate(go);
            converted++;
        }

        Debug.Log($"Converted {converted} objects to spawn points.");
    }
}
