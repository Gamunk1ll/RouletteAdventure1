using UnityEngine;

public class test : MonoBehaviour
{
    public GameObject objectToSpawn; // Перетащите префаб сюда в инспекторе
    public Vector3 spawnPosition = new Vector3(0, 0, 0); // Позиция спавна

    void Start()
    {
        
        Instantiate(objectToSpawn, spawnPosition, Quaternion.identity);
    }
}
