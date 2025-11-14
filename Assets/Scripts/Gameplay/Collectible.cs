using UnityEngine;

public class Collectible : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verifica si el que tocó tiene la etiqueta "Player"
        if (other.CompareTag("Player"))
        {
            // Desaparece el objeto
            gameObject.SetActive(false);

            // (Opcional) Si quieres destruirlo completamente:
            // Destroy(gameObject);
        }
    }
}
