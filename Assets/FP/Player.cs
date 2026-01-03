using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance;
    
    
    public int health = 1;
    
    public Transform GetTransform()
    {
        return transform;
    }

    public void Awake()
    {
        Instance = this;
    }
    
}
