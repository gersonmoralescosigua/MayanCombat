using UnityEngine;
using Firebase.Auth;
using System;

/// <summary>
/// SessionManager mejorado: mantiene datos de sesión y sincroniza con FirebaseAuth si está disponible.
/// Mantén este objeto en Splash.unity (DontDestroyOnLoad).
/// </summary>
public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [Header("Datos del jugador actual (en memoria)")]
    public string playerEmail;
    public string playerNickname;
    public bool isGuest;

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
        // Si FirebaseInit hizo su job y FirebaseAuth existe, conéctalo.
        try
        {
            auth = FirebaseAuth.DefaultInstance;
            if (auth != null && !listeningAuth)
            {
                auth.StateChanged += OnAuthStateChanged;
                listeningAuth = true;
                // inicializa con el usuario actual si ya hay uno
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

        // Si usuario cambió
        var user = a.CurrentUser;
        if (user != null)
        {
            UpdateFromFirebaseUser(user);
        }
        else
        {
            // Usuario deslogueado en Firebase -> limpiar sesión local (pero dejamos la opción)
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

    /// <summary>Usado por el flujo local / dummy antes de Firebase</summary>
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