using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class AssignmentUI : MonoBehaviour
{
    public TMP_Text txtAssignment;
    public float delayBeforeStart = 5f;

    void Start()
    {
        // Recupera los datos guardados por el SessionManager o PlayerPrefs
        int team = SessionManager.Instance != null
            ? SessionManager.Instance.currentTeam
            : PlayerPrefs.GetInt("Team", -1);

        int charId = PlayerPrefs.GetInt("AssignedCharacter", -1);

        string teamName = team == 0 ? "Español" : "Maya";
        string charName = charId == 0 ? "Beatriz de la Cueva" : "Ixquic";

        txtAssignment.text =
            $"🏹 Has sido asignado al equipo: <b>{teamName}</b>\n\n" +
            $"Personaje: <b>{charName}</b>";

        StartCoroutine(GoToMapAfterDelay());
    }

    IEnumerator GoToMapAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeStart);
        SceneManager.LoadScene("Map_Tikal_Base");
    }
} 
