using UnityEngine;
public class NetworkManager : MonoBehaviour {
    void Awake(){ DontDestroyOnLoad(gameObject); }
}