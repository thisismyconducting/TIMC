using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class CondObjMan : MonoBehaviour
{

    #region Singleton
    public static CondObjMan Instance;
    public List<GameObject> Figures;
    static List<GameObject> CondObjList = new List<GameObject>();
    public static string SuccessCond;
    public static float time = 0;
    public static float cycle = 2.0f;
    public static bool isConducting = false;
    // public static Vector3 thisposition;
    // Start is called before the first frame update

    void Awake()
    {
        if (Instance != null)
        {
            return;
        }
        Instance = this;
    }
    // Update is called once per frame
    void Update(){
        if(!isConducting){
            if (Input.GetButtonDown("XRI_Right_PrimaryButton"))//오른쪽 X = A버튼
            {
                MakeConductingShape();
            }
            if (Input.GetButtonDown("XRI_Right_SecondaryButton"))//오른쪽 Y = B버튼
            {
                MakeConductingShape();
            }
            if (Input.GetKeyDown(KeyCode.A))//Y = B버튼
            {
                MakeConductingShape();
            }
        }
    }
    void FixedUpdate()
    {
        if (time >= cycle && isConducting)//지휘를 했고 한 지휘 사이클이 지난다면
        {
            time = 0;
            isConducting = false;
            if (CondObjList.Count > 0)
            {
                SuccessCond = CondObjList[0].name; //이 리스트의 0번을 받으면 지휘를 한 모양을 이름으로 받음.
                CondObjList.Clear();
                Debug.Log("Success!");
                Debug.Log(SuccessCond);
                interaction();
            }
            else{
                
                Debug.Log("Fail...");
            }
        }
        else
        {
            
        }
        time += 0.02f;
    }
    public void MakeConductingShape(){
        isConducting = true;
        time = 0;
        foreach (GameObject figure in Figures)
        {
            GameObject temp = Instantiate(figure, figure.gameObject.transform.position + this.gameObject.transform.position, figure.gameObject.transform.rotation);
            CondObjList.Add(temp);
        }
    }
    public static void addobjList(GameObject obj)
    {
        CondObjList.Add(obj);
    }
    public static void delobjList(GameObject obj)
    {
        CondObjList.Remove(obj);
    }

    void interaction()
    {
        GameObject gameMode = GameObject.Find("GameMode");
        Score_A score = gameMode.GetComponent<Score_A>();

        string str = SuccessCond;
        if (str.Equals("Circle(Clone)"))
        {
            score.IncreaseTempo();
        }
        if (str.Equals("square(Clone)"))
        {
            score.DecreaseTempo();
        }
        if (str.Equals("Triangle1(Clone)"))
        {
            score.IncreaseDynamic();
        }
        if (str.Equals("Triangle2(Clone)"))
        {
            score.DecreaseDynamic();
        }
        if (str.Equals("rhombus(Clone)"))
        {
            score.RemoveDisturber();
        }
        if (str.Equals("UpDown(Clone)"))
        {
            score.RecoverPartMistake();
        }
    }

    #endregion
}