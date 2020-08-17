using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OriginController : MonoBehaviour
{
    int threshold = 5000;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 pos = transform.position;
        if(pos.magnitude > threshold)
        {
            foreach(GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                g.transform.position -= pos;
            }
        }
    }
}
