using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> 와드 내부 스크립트 </summary>
public class Ward : MonoBehaviour
{
    public string team = "";
    
    private SkinnedMeshRenderer myMesh;
    private GameObject particle;
    private bool isWardOn = false;
    private string playerTeam = "";
    private const float minTime = 90f;
    private const float maxTime = 180f;
    private const float termTime = 90f;
    private float fCooldown = 90f;

    //값 초기화
    private void Awake()
    {
        myMesh = GetComponent<SkinnedMeshRenderer>();
        particle = transform.GetChild(0).gameObject;
        playerTeam = PhotonNetwork.player.GetTeam().ToString();

        if (playerTeam.Equals("red"))
            playerTeam = "Red";
        else if (playerTeam.Equals("blue"))
            playerTeam = "Blue";

        particle.SetActive(false);
    }

    private void Update()
    {
        //현재 이 와드가 켜져있다
        if (isWardOn)
        {
            //정해진 수명에서 시간을 뺀다.
            fCooldown -= Time.deltaTime;

            //수명이 다하면 WardTimeOut 함수를 호출한다.
            if (fCooldown < 0)
                WardTimeOut();
        }
    }

    /// <summary> 와드를 생성하는 함수 </summary>
    /// <param name="_team">와드를 생성하는 챔피언의 팀 (Red, Blue)</param>
    /// <param name="champLv">와드를 생성하는 챔피언의 레벨</param>
    public void MakeWard(string _team, int champLv)
    {
        //현재 챔피언의 레벨과 팀에 맞게 내부 설정값을 고친다.
        fCooldown = Mathf.Round(minTime + ((termTime * ((float)(champLv - 1))) / 17f));
        team = _team;
        isWardOn = true;

        //와드가 와드를 설치한 플레이어의 팀 시야에만 보이도록 설정한다.
        if (team.Equals(playerTeam))
        {
            myMesh.enabled = true;
            particle.SetActive(true);
        }
    }

    /// <summary> 와드의 수명(시간)이 다했을 때 호출하는 함수 </summary>
    private void WardTimeOut()
    {
        //와드를 맵 밖으로 이동시키고 Dead 함수를 호출한다.
        transform.position = new Vector3(-50, 0, -50);
        Invoke("Dead", 1f);
    }

    /// <summary> 와드의 활성 상태를 꺼주는 함수 </summary>
    private void Dead()
    {
        team = "";
        isWardOn = false;
        gameObject.SetActive(false);
    }
}