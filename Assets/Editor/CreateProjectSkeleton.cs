// Assets/Editor/CreateProjectSkeleton.cs
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public class CreateProjectSkeleton : EditorWindow
{
    [MenuItem("MayanCombat/Create Project Skeleton")]
    public static void CreateSkeleton()
    {
        // Root paths
        string[] folders = new string[] {
            "Assets/Art/2D/Characters",
            "Assets/Art/2D/Environments",
            "Assets/Art/2D/UI",
            "Assets/Spritesheets",
            "Assets/Audio/Music",
            "Assets/Audio/SFX",
            "Assets/Scenes/Bootstrap",
            "Assets/Scenes/UI",
            "Assets/Scenes/Maps/Tikal",
            "Assets/Scenes/Maps/Atitlan",
            "Assets/Scenes/Maps/Volcan",
            "Assets/Prefabs/UI",
            "Assets/Prefabs/Characters",
            "Assets/Prefabs/Pickups",
            "Assets/Scripts/Managers",
            "Assets/Scripts/UI",
            "Assets/Scripts/Gameplay",
            "Assets/Scripts/Networking",
            "Assets/Scripts/Data",
            "Assets/Resources",
            "Assets/Editor",
            "Assets/Tests"
        };

        foreach (var f in folders)
        {
            if (!AssetDatabase.IsValidFolder(f))
            {
                string parent = Path.GetDirectoryName(f).Replace("\\","/");
                string newFolderName = Path.GetFileName(f);
                if (AssetDatabase.IsValidFolder(parent))
                    AssetDatabase.CreateFolder(parent, newFolderName);
                else
                    Directory.CreateDirectory(f); // fallback
            }
        }

        AssetDatabase.Refresh();

        // Create skeleton scenes
        CreateScene("Assets/Scenes/UI/Splash.unity");
        CreateScene("Assets/Scenes/UI/Menu.unity");
        CreateScene("Assets/Scenes/UI/Login.unity");
        CreateScene("Assets/Scenes/UI/Register.unity");
        CreateScene("Assets/Scenes/UI/Matchmaking.unity");
        CreateScene("Assets/Scenes/UI/CharacterSelect.unity");
        CreateScene("Assets/Scenes/UI/MatchResults.unity");
        CreateScene("Assets/Scenes/Bootstrap/Bootstrap.unity");
        CreateScene("Assets/Scenes/Maps/Tikal/Map_Tikal_Base.unity");
        CreateScene("Assets/Scenes/Maps/Atitlan/Map_Atitlan_Base.unity");
        CreateScene("Assets/Scenes/Maps/Volcan/Map_Volcan_Base.unity");

        // Create script stubs
        CreateScriptStub("Assets/Scripts/Managers/GameManager.cs", @"using UnityEngine;
public class GameManager : MonoBehaviour {
    void Awake(){ DontDestroyOnLoad(gameObject); }
}");
        CreateScriptStub("Assets/Scripts/Managers/NetworkManager.cs", @"using UnityEngine;
public class NetworkManager : MonoBehaviour {
    void Awake(){ DontDestroyOnLoad(gameObject); }
}");
        CreateScriptStub("Assets/Scripts/UI/UIManager.cs", @"using UnityEngine;
public class UIManager : MonoBehaviour {
    public void Show(string panelName) { /* implement */ }
}");

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("MayanCombat", "Project skeleton created. Check Assets/Scenes and Assets/Scripts.", "OK");
    }

    static void CreateScene(string path)
    {
        if (!File.Exists(path))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("SceneRoot");
            EditorSceneManager.SaveScene(scene, path);
        }
    }

    static void CreateScriptStub(string path, string content)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, content);
        }
    }
}