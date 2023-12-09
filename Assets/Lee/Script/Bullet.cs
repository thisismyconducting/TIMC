using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Processors;

public class Bullet : MonoBehaviour
{
    public GameObject fireExplosion;
    public GameObject shadowExplosion;

    private float time;

    private int type; // 0: 방해꾼등장, 1: 악단실수, 2: 바이올린2
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private SphereCollider targetCollider;

    private GameObject fireEffect;
    private GameObject shadowEffect;

    public void InitializeBullet(int _type, Vector3 srcPos, Vector3 dstPos, SphereCollider collider)
    {
        type = _type;
        startPosition = srcPos;
        targetPosition = dstPos;
        targetCollider = collider;

        fireEffect = transform.Find("Fire").gameObject;
        shadowEffect = transform.Find("Shadow").gameObject;
        fireEffect.SetActive((type == 1 || type == 2) ? true : false);
        shadowEffect.SetActive((type == 0) ? true : false);

        transform.position = startPosition;
    }

    void Start()
    {
    }

    void Update()
    {
        Move();
    }

    float easeInQuart(float x)
    {
        return x * x * x * x;
    }

    void Move()
    {
        time += Time.deltaTime;

        transform.position = Vector3.Lerp(startPosition, targetPosition, easeInQuart(time));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == targetCollider)
        {
            GameObject gameMode = GameObject.Find("GameMode");
            Score_A scoreA = gameMode.GetComponent<Score_A>();

            GameObject explosionObj;
            if (scoreA != null)
            {
                switch (type)
                {
                    case 0:
                        scoreA.RemoveDisturberComplete();
                        explosionObj = Instantiate(shadowExplosion);
                        break;
                    case 1:
                        scoreA.RecoverPartMistakeComplete();
                        explosionObj = Instantiate(fireExplosion);
                        break;
                    default:
                        explosionObj = Instantiate(fireExplosion);
                        break;
                }
            }
            else
            {
                Score_C scoreC = gameMode.GetComponent<Score_C>();

                switch (type)
                {
                    case 0:
                        scoreC.RemoveDisturberComplete();
                        explosionObj = Instantiate(shadowExplosion);
                        break;
                    case 1:
                        scoreC.RecoverPartMistakeComplete();
                        explosionObj = Instantiate(fireExplosion);
                        break;
                    default:
                        explosionObj = Instantiate(fireExplosion);
                        break;
                }
            }

            explosionObj.transform.position = transform.position;

            Destroy(gameObject);
        }
    }
}
