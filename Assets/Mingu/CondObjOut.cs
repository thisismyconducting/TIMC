using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CondObjOut : MonoBehaviour
{
    private Renderer _renderer;
    // Start is called before the first frame update
    void Start()
    {
        _renderer = GetComponent<Renderer>();
        Destroy(this.gameObject, CondObjMan.cycle+1);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void ChangeColor(int temp)
    {
        Color randomColor = new Color(1, 0, temp);
        _renderer.material.color = randomColor;
    }
    
    private void OnTriggerStay(Collider other) {
        // Debug.Log(this.gameObject.name);
        if (other.gameObject.name == "Baton"){
            ChangeColor(0);
        }
    }
    private void OnTriggerExit(Collider other) {
        if (other.gameObject.name == "Baton"){
            ChangeColor(1);
            CondObjMan.delobjList(this.gameObject);
            Debug.Log(this.gameObject.name);
            Destroy(this.gameObject, 0.1f);
        }
    }
}
