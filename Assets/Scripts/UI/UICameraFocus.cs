using UnityEngine;

[ExecuteAlways]
public class UICameraFocus : MonoBehaviour
{
    public Canvas targetCanvas;
    public float distance = 10f;

    void LateUpdate()
    {
        if (targetCanvas == null) return;

        // Centra la cámara en el canvas y mira hacia él
        Vector3 targetPos = targetCanvas.transform.position;
        transform.position = new Vector3(targetPos.x, targetPos.y, -distance);
        transform.LookAt(targetPos);
    }
}
