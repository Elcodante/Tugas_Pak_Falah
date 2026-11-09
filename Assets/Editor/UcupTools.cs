using UnityEngine;
using UnityEditor;

public static class UcupTools
{
    [MenuItem("Tools/Ucup/1. Spawn Test GameObject di Scene")]
    public static void SpawnGameObjectInScene()
    {
        GameObject go = new GameObject("Test_GameObject_Ucup");
        go.transform.position = Vector3.zero;
        Selection.activeGameObject = go;
        Undo.RegisterCreatedObjectUndo(go, "Create Test_GameObject_Ucup");
        Debug.Log("<color=green>[UcupTools]</color> Berhasil menambahkan 'Test_GameObject_Ucup' ke Hierarchy Scene!");
    }

    [MenuItem("Tools/Ucup/2. Buat Prefab di folder Ucup_Project")]
    public static void CreatePrefabInFolder()
    {
        string folderPath = "Assets/Ucup_Project";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Ucup_Project");
        }

        GameObject tempGO = new GameObject("Test_GameObject_Ucup");
        string localPath = folderPath + "/Test_GameObject_Ucup.prefab";
        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

        PrefabUtility.SaveAsPrefabAssetAndConnect(tempGO, localPath, InteractionMode.UserAction);
        Object.DestroyImmediate(tempGO);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>[UcupTools]</color> Berhasil membuat prefab GameObject di {localPath}");
    }
}

