using UnityEngine;
using Fusion;
using System.Collections;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour
{
    public Button btnSelectCharacter;
    public int characterId = 0;

    private bool selected = false;

    void Start()
    {
        btnSelectCharacter.onClick.AddListener(OnSelectClicked);
        StartCoroutine(AutoSelectAfterDelay());
    }

    void OnSelectClicked()
    {
        if (selected) return;
        selected = true;

        var mc = FindObjectOfType<MatchController>();
        if (mc == null)
        {
            Debug.LogError("❌ No se encontró MatchController en escena.");
            return;
        }

        mc.RPC_PlayerSelected(characterId);
        Debug.Log($"[Client] Enviado characterId {characterId} al host.");
    }

    IEnumerator AutoSelectAfterDelay()
    {
        yield return new WaitForSeconds(10f);
        if (!selected)
        {
            Debug.Log("⌛ No se seleccionó personaje, asignando automático.");
            OnSelectClicked();
        }
    }
}
