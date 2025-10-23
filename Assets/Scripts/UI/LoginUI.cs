using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase;
using System;

public class LoginUI : MonoBehaviour
{
    [Header("Botones")]
    public Button btnAcceder;
    public Button btnCrearNuevaLeyenda;
    public Button btnInvitado;

    [Header("Campos")]
    public TMP_InputField emailField;
    public TMP_InputField passwordField;

    [Header("UI")]
    public TextMeshProUGUI mensajeError;

    private FirebaseAuth auth;
    private bool firebaseReady => FirebaseInitializer.IsReady;

    void Start()
    {
        // listeners
        if (btnAcceder != null) btnAcceder.onClick.AddListener(OnAccederClicked);
        if (btnCrearNuevaLeyenda != null) btnCrearNuevaLeyenda.onClick.AddListener(OnCrearNuevaLeyendaClicked);
        if (btnInvitado != null) btnInvitado.onClick.AddListener(OnEntrarComoInvitadoClicked);

        // clear
        if (emailField != null) emailField.text = "";
        if (passwordField != null) passwordField.text = "";

        // Si Firebase está listo y auth no inicializado, obtener instancia
        if (firebaseReady)
        {
            try { auth = FirebaseAuth.DefaultInstance; }
            catch (Exception ex) { Debug.LogWarning($"LoginUI: No se pudo obtener auth: {ex}"); }
        }
    }

    async void OnAccederClicked()
    {
        if (emailField == null || passwordField == null) { MostrarError("Error interno: campos no asignados."); return; }

        string email = emailField.text.Trim();
        string pass = passwordField.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass)) { MostrarError("Completa todos los campos."); return; }

        // Si Firebase no está listo, usamos modo local (dummy)
        if (!firebaseReady)
        {
            Debug.Log("LoginUI: Firebase NO listo, entrando en modo local.");
            SessionManager.Instance?.SetSession(email, email.Split('@')[0]);
            SceneManager.LoadScene("Menu");
            return;
        }

        try
        {
            var userCred = await FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(email, pass);
            var user = userCred?.User;
            if (user != null)
            {
                string nickname = string.IsNullOrEmpty(user.DisplayName) ? user.Email.Split('@')[0] : user.DisplayName;
                SessionManager.Instance?.SetSession(user.Email, nickname);
                SceneManager.LoadScene("Menu");
            }
            else MostrarError("Error inesperado en autenticación.");
        }
        catch (FirebaseException fex)
        {
            var code = (AuthError)fex.ErrorCode;
            Debug.LogWarning($"Login failed: {code}");
            HandleAuthError(code);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Login exception: {ex}");
            MostrarError("Error de conexión o credenciales.");
        }
        finally
        {
            //j
        }
    }

    void HandleAuthError(AuthError code)
    {
        switch (code)
        {
            case AuthError.WrongPassword:
            case AuthError.InvalidEmail:
            case AuthError.UserNotFound:
                MostrarError("Correo o contraseña incorrectos.");
                break;
            case AuthError.NetworkRequestFailed:
                MostrarError("Error de red. Reintenta.");
                break;
            default:
                MostrarError("Error de autenticación.");
                break;
        }
    }

    void OnCrearNuevaLeyendaClicked() => SceneManager.LoadScene("Register");

    void OnEntrarComoInvitadoClicked()
    {
        // Sign-in anonymously if firebase ready, otherwise local guest
        if (firebaseReady)
        {
            FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync().ContinueWith(t =>
            {
                if (t.IsCompleted && !t.IsFaulted)
                {
                    var user = t.Result.User;
                    SessionManager.Instance?.SetSession(user.UserId ?? "guest", "Invitado", true);
                    SceneManager.LoadScene("Menu");
                }
                else
                {
                    Debug.LogWarning("LoginUI: Error signInAnonymously");
                    SessionManager.Instance?.SetSession("guest@mayancombat", "Invitado", true);
                    SceneManager.LoadScene("Menu");
                }
            });
        }
        else
        {
            SessionManager.Instance?.SetSession("guest@mayancombat", "Invitado", true);
            SceneManager.LoadScene("Menu");
        }
    }

    void MostrarError(string mensaje)
    {
        if (mensajeError != null) { mensajeError.text = mensaje; mensajeError.gameObject.SetActive(true); }
        else Debug.LogWarning(mensaje);
    }
}