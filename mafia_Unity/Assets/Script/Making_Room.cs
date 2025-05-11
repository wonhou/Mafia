using Unity.VisualScripting;
using UnityEngine;

public class Making_Room : MonoBehaviour
{

    public GameObject obj;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        setActive(false);
    }

    public void setActive(bool active)
    {
        obj.gameObject.SetActive(active);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
