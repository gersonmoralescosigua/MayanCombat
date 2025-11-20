using UnityEngine;
using Firebase.Auth;
using UnityEngine.SceneManagement; 
using System.Collections;
using System;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    // Datos de Jugador
    public string playerEmail;
    public string playerNickname;
    public int currentTeam = -1; // 0: Maya, 1: Español
    public bool isGuest;

    // --- DATOS CLAVE PARA UI Y VIDEO ---
    public string GameOverMessage = "Esperando confirmación..."; 
    
    // --- LA VARIABLE QUE FALTABA ---
    public bool IsFinalMatch = false; 
    
    // -1: Nadie, 0: Maya, 1: Español
    public int FinalWinnerTeam = -1; 

    // Nombre de la escena única de videos
    public string VideoSceneName = "Winners";

    // Internals
    private FirebaseAuth auth;
    private bool listeningAuth = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        TryAttachFirebase();
    }

    void OnDisable()
    {
        DetachFirebase();
    }

    public void SetSession(string email, string nickname, bool guest = false)
    {
        playerEmail = email;
        playerNickname = nickname;
        isGuest = guest;
    }

    public void SetTeam(int team) => currentTeam = team;

    // --- FUNCIÓN DE SALIDA ---
    public void GoToVideoScene()
    {
        StartCoroutine(LoadVideoSceneRoutine());
    }

    IEnumerator LoadVideoSceneRoutine()
    {
        Debug.Log("🎬 SessionManager: Cargando escena de videos...");
        yield return new WaitForSeconds(0.5f); 
        SceneManager.LoadScene(VideoSceneName);
    }

    // ... (Mantén aquí abajo tus métodos de Firebase Auth, SignOut, etc.) ...
    // ... (Copia tus funciones TryAttachFirebase, OnAuthStateChanged, etc.) ...

    // Llamar desde FirebaseInitializer o Start cuando quieras forzar re-check
    public void TryAttachFirebase()
    {
        try
        {
            auth = FirebaseAuth.DefaultInstance;
            if (auth != null && !listeningAuth)
            {
                auth.StateChanged += OnAuthStateChanged;
                listeningAuth = true;
                if (auth.CurrentUser != null)
                    UpdateFromFirebaseUser(auth.CurrentUser);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SessionManager: FirebaseAuth no disponible aún. {ex.Message}");
            auth = null;
            listeningAuth = false;
        }
    }

    void DetachFirebase()
    {
        if (auth != null && listeningAuth)
        {
            auth.StateChanged -= OnAuthStateChanged;
            listeningAuth = false;
        }
    }

    void OnAuthStateChanged(object sender, EventArgs e)
    {
        var a = sender as FirebaseAuth;
        if (a == null) return;

        var user = a.CurrentUser;
        if (user != null)
        {
            UpdateFromFirebaseUser(user);
        }
        else
        {
            ClearSession();
        }
    }

    void UpdateFromFirebaseUser(FirebaseUser user)
    {
        if (user == null) return;
        playerEmail = user.Email ?? user.UserId ?? "unknown";
        playerNickname = string.IsNullOrEmpty(user.DisplayName) ? (user.Email?.Split('@')[0] ?? "Player") : user.DisplayName;
        isGuest = user.IsAnonymous;
        Debug.Log($"[SessionManager] Cargado desde Firebase: {playerNickname} ({playerEmail}) guest={isGuest}");
    }

    public void ClearSession()
    {
        playerEmail = "";
        playerNickname = "";
        isGuest = false;
        currentTeam = -1;
        Debug.Log("[SessionManager] Sesión limpiada.");
    }

    /// <summary>
    /// Cierra sesión tanto local como en Firebase (si está disponible).
    /// </summary>
    public void SignOut()
    {
        try
        {
            if (auth != null)
            {
                auth.SignOut();
                Debug.Log("[SessionManager] SignOut Firebase ejecutado.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SessionManager.SignOut: error signOut Firebase: {ex.Message}");
        }

        ClearSession();
    }

    public bool HasActiveSession() => !string.IsNullOrEmpty(playerNickname);
}