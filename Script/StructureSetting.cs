using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> 네비게이션 메쉬를 만드는데에 방해되는 지상의 오브젝트들을, 메쉬가 생성된 후에 활성화해주는 스크립트. </summary>
public class StructureSetting : MonoBehaviour
{
    public static StructureSetting instance
    {
        get
        {
            if (_instance == null)
                _instance = (StructureSetting)FindObjectOfType(typeof(StructureSetting));
            return _instance;
        }
    }

    public GameObject cattail; //부쉬
    public GameObject tower; //타워
    public GameObject player; //챔피언
    public GameObject outlineTrees; //외곽의 나무들

    private static StructureSetting _instance;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform.GetChild(0).gameObject;

        //Start 함수가 불리기 전에 네비게이션 메쉬 생성이 끝난다. 그러므로 ActiveTrue 함수를 호출.
        if (!cattail.activeInHierarchy)
            ActiveTrue();
    }

    /// <summary> 네비게이션 메쉬 생성을 위해 꺼두었던 오브젝트들을 활성화시키는 함수 </summary>
    public void ActiveTrue()
    {
        cattail.SetActive(true);
        tower.SetActive(true);
        player.SetActive(true);
        outlineTrees.SetActive(true);
    }
}