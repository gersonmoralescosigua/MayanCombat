using UnityEngine;
using Firebase;
using Firebase.Auth;
using System.Threading.Tasks;

public class FirebaseInitializer : MonoBehaviour
{
    public static bool IsReady { get; private set; } = false;
    public static FirebaseApp App { get; private set; }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        InitializeFirebase();
    }

    async void InitializeFirebase()
    {
        Debug.Log("FirebaseInitializer: comprobando dependencias...");
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            App = FirebaseApp.DefaultInstance;
            IsReady = true;
            Debug.Log("✅ Firebase listo.");
        }
        else
        {
            Debug.LogError($"❌ Firebase no está listo: {dependencyStatus}");
            IsReady = false;
        }
    }
}