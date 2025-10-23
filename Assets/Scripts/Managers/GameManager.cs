using UnityEngine;
public class GameManager : MonoBehaviour {
    void Awake(){ DontDestroyOnLoad(gameObject); }
}