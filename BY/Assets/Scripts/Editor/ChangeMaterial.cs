using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChangeMaterial 
{
    static Material toMaterial;
    static string matName = "Sprite-Lit-Default";
    static string targetName = "SpriteDiffuse";
    [MenuItem("Tools/Replace Material In Prefabs")]
    static void ReplaceMaterialInPrefabs()
    {
        // ===== 설정 영역 =====
        string targetFolderPath = "Assets/AddressableAssets/Prefabs"; // 프리팹 폴더
        SetMaterial();

        // =====================

        if (toMaterial == null)
        {
            Debug.LogError("Material 경로가 잘못되었습니다.");
            return;
        }

        string[] prefabGuids = AssetDatabase.FindAssets(
            "t:Prefab",
            new[] { targetFolderPath }
        );

        int modifiedCount = 0;

        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            bool changed = false;
            if (IsUIObject(prefabRoot))
                changed |= ChangeUI(prefabRoot, prefabRoot.name);
            else
                changed |= ChangeObject(prefabRoot, prefabRoot.name);


            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                modifiedCount++;
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Material 교체 완료: {modifiedCount}개 프리팹 수정됨");
    }

    [MenuItem("Tools/Replace Material In Scenes")]
    static void ReplaceMaterialInScenes()
    {
        string sceneFolder = "Assets/Scenes";
        SetMaterial();

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { sceneFolder });

        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            bool changed = false;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (IsUIObject(root))
                    changed |= ChangeUI(root, scene.name);
                else
                    changed |= ChangeObject(root, scene.name);

            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            EditorSceneManager.CloseScene(scene, true);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("씬 Material 교체 완료");
    }

    static void SetMaterial()
    {
        toMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            $"Assets/AddressableAssets/{matName}.mat");
    }

    static bool ChangeObject(GameObject go, string root)
    {
        bool changed = false;
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials;
            bool rendererChanged = false;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null)
                    continue;
                Debug.Log($"[{root}] {go.name} materials name : {materials[i].name}");
                if (materials[i].name == targetName)
                {
                    materials[i] = toMaterial;
                    rendererChanged = true;
                }
            }

            if (rendererChanged)
            {
                renderer.sharedMaterials = materials;
                changed = true;
            }
        }

        return changed;
    }

    static bool ChangeUI(GameObject go, string root)
    {
        bool changed = false;
        var graphics = go.GetComponentsInChildren<Graphic>(true);
        foreach (var graphic in graphics)
        {
            bool rendererChanged = false;

            if (graphic.material == null)
                continue;
            Debug.Log($"[{root}] {go.name} materials name : {graphic.material.name}");
            if (graphic.material.name == targetName)
            {
                graphic.material = toMaterial;
                rendererChanged = true;
            }

            if (rendererChanged)
            {
                changed = true;
            }
        }

        return changed;
    }

    static bool IsUIObject(GameObject go)
    {
        if (go.GetComponent<RectTransform>() == null)
            return false;

        return true;
    }
}
