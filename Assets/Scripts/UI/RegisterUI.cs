using UnityEngine;
using UnityEngine.UI;
using Firebase;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;

public class RegisterUI : MonoBehaviour
{
    [Header("Inputs (TMP)")]
    public TMP_InputField inputEmail;
    public TMP_InputField inputNickname;
    public TMP_InputField inputPassword;
    public TMP_InputField inputConfirmPassword;

    [Header("Botones y feedback")]
    public Button btnRegister;
    public Button btnCancel;
    public TMP_Text feedbackText; // opcional

    void Start()
    {
        if (btnRegister != null) btnRegister.onClick.AddListener(OnRegisterClicked);
        if (btnCancel != null) btnCancel.onClick.AddListener(OnCancelClicked);
        if (inputEmail != null) inputEmail.onValueChanged.AddListener(delegate { EvaluateForm(); });
        if (inputNickname != null) inputNickname.onValueChanged.AddListener(delegate { EvaluateForm(); });
        if (inputPassword != null) inputPassword.onValueChanged.AddListener(delegate { EvaluateForm(); });
        if (inputConfirmPassword != null) inputConfirmPassword.onValueChanged.AddListener(delegate { EvaluateForm(); });

        EvaluateForm();
    }

    void EvaluateForm()
    {
        bool emailOk = inputEmail != null && !string.IsNullOrWhiteSpace(inputEmail.text) && inputEmail.text.Contains("@");
        bool nickOk = inputNickname != null && !string.IsNullOrWhiteSpace(inputNickname.text) && inputNickname.text.Length >= 3;
        bool passOk = inputPassword != null && !string.IsNullOrEmpty(inputPassword.text) && inputPassword.text.Length >= 6;
        bool passMatch = inputConfirmPassword != null && inputConfirmPassword.text == inputPassword.text;

        bool formOk = emailOk && nickOk && passOk && passMatch;

        if (btnRegister != null) btnRegister.interactable = formOk;

        if (feedbackText != null)
        {
            if (!emailOk) feedbackText.text = "Ingrese un correo válido.";
            else if (!nickOk) feedbackText.text = "Nickname mínimo 3 caracteres.";
            else if (!passOk) feedbackText.text = "La contraseña debe tener al menos 6 caracteres.";
            else if (!passMatch) feedbackText.text = "Las contraseñas no coinciden.";
            else feedbackText.text = "";
        }
    }

    async void OnRegisterClicked()
    {
        string email = inputEmail.text.Trim();
        string pass = inputPassword.text;
        string nickname = inputNickname.text.Trim();

        if (!FirebaseInitializer.IsReady)
        {
            Debug.LogWarning("Firebase no está listo, modo local.");
            SessionManager.Instance?.SetSession(email, nickname);
            SceneManager.LoadScene("Menu");
            return;
        }

        try
        {
            var userCred = await FirebaseAuth.DefaultInstance.CreateUserWithEmailAndPasswordAsync(email, pass);
            var user = userCred.User;
            await user.UpdateUserProfileAsync(new UserProfile { DisplayName = nickname });
            SessionManager.Instance?.SetSession(email, nickname);
            Debug.Log($"✅ Usuario creado: {email}");
            SceneManager.LoadScene("Menu");
        }
        catch (FirebaseException fex)
        {
            Debug.LogError($"❌ Error al crear usuario: {fex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error inesperado: {ex.Message}");
        }
    }

    void OnCancelClicked()
    {
        SceneManager.LoadScene("Login");
    }
}