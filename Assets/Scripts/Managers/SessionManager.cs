using UnityEngine;
using Firebase.Auth;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    // Datos de Jugador
    public string playerEmail;
    public string playerNickname;
    public int currentTeam = -1; 
    public bool isGuest;

    // Datos de Flujo
    public string GameOverMessage = ""; 
    public bool IsFinalMatch = false;
    public string VideoSceneToLoad = "";
    public int RoundIndex = 0;
    public string WinnerName = "";

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

    public void SetSession(string email, string nickname, bool guest = false)
    {
        playerEmail = email;
        playerNickname = nickname;
        isGuest = guest;
        Debug.Log($"[SessionManager] Sesión (manual) iniciada: {nickname} ({(guest ? "Invitado" : email)})");
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

    public void SetTeam(int team)
    {
        currentTeam = team;
        Debug.Log($"[SessionManager] Team asignado: {(team == 0 ? "Maya" : "Español")}");
    }

    // --- FUNCIÓN SEGURA PARA CARGAR VIDEO ---
    public void LoadFinalVideoScene(string sceneName)
    {
        VideoSceneToLoad = sceneName;
        StartCoroutine(DisconnectAndLoad());
    }

    IEnumerator DisconnectAndLoad()
    {
        Debug.Log($"🎬 SessionManager: Preparando transición a {VideoSceneToLoad}...");
        
        yield return new WaitForSeconds(1.0f);

        if (!string.IsNullOrEmpty(VideoSceneToLoad))
        {
            SceneManager.LoadScene(VideoSceneToLoad);
        }
        else
        {
            Debug.LogError("❌ No hay escena de video asignada. Volviendo al Menu.");
            SceneManager.LoadScene("Menu");
        }
    }
}