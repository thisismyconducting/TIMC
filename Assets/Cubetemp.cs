using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cubetemp : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnTriggerStay(Collider other) {
        
    
        Debug.Log("어라");
    }
    void OnCollisionStay(Collision other) { 
    
        Debug.Log("어라");
    }
}