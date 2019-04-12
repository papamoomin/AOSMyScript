using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 시작 시 몬스터의 영역 오브젝트 생성을 담당하는 스크립트
/// </summary>
public class MonsterManager : MonoBehaviour
{
    public Vector3[] krugBigVec;
    public Vector3[] krugSmallVec;
    public Vector3[] grompVec;
    public Vector3[] rSentinelVec;
    public Vector3[] bSentinelVec;
    public Vector3[] wolf_BigVec;
    public Vector3[] wolf_SmallVec;
    public Vector3[] raptorBigVec;
    public Vector3[] raptorSmallVec;
    public Vector3[] riftHeraldVec;
    public Vector3[] baronVec;
    public Vector3[] dragonVec;

    private GameObject[] krugBig;
    private GameObject[] krugSmall;
    private GameObject[] gromp;
    private GameObject[] rSentinel;
    private GameObject[] bSentinel;
    private GameObject[] wolfBig;
    private GameObject[] wolfSmall;
    private GameObject[] raptorBig;
    private GameObject[] raptorSmall;
    private GameObject riftHerald;
    private GameObject baron;
    private GameObject dragon;

    private void Start()
    {
        MakeMonster();
    }

    /// <summary>
    /// 몬스터의 영역 오브젝트를 만들어주는 함수
    /// 각 영역 오브젝트가 각각이 담당하는 몬스터를 생성한다.
    /// </summary>
    private void MakeMonster()
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        krugBig = new GameObject[2];
        krugSmall = new GameObject[2];
        gromp = new GameObject[2];
        rSentinel = new GameObject[2];
        bSentinel = new GameObject[2];
        wolfBig = new GameObject[2];
        wolfSmall = new GameObject[4];
        raptorBig = new GameObject[2];
        raptorSmall = new GameObject[4];

        //맵에 등장해야하는 개체 수에 따라 분류하여 생성. 포톤으로 생성하기에 모든 클라이언트가 공유한다.
        for (int i = 0; i < 4; ++i)
        {
            //0123 (맵에 4마리가 나오는 경우)
            wolfSmall[i] = PhotonNetwork.Instantiate("Monster/Wolf_Small", wolf_SmallVec[i * 2], Quaternion.identity, 0);
            wolfSmall[i].GetComponent<MonsterRespawn>().respawnRotating = wolf_SmallVec[i * 2 + 1];
            raptorSmall[i] = PhotonNetwork.Instantiate("Monster/Raptor_Small", raptorSmallVec[i * 2], Quaternion.identity, 0);
            raptorSmall[i].GetComponent<MonsterRespawn>().respawnRotating = raptorSmallVec[i * 2 + 1];

            if (i > 1)
                continue;

            //01 (맵에 2마리가 나오는 경우)
            krugBig[i] = PhotonNetwork.Instantiate("Monster/Krug_Big", krugBigVec[i * 2], Quaternion.identity, 0);
            krugBig[i].GetComponent<MonsterRespawn>().respawnRotating = krugBigVec[i * 2 + 1];
            krugSmall[i] = PhotonNetwork.Instantiate("Monster/Krug_Small", krugSmallVec[i * 2], Quaternion.identity, 0);
            krugSmall[i].GetComponent<MonsterRespawn>().respawnRotating = krugSmallVec[i * 2 + 1];
            gromp[i] = PhotonNetwork.Instantiate("Monster/Gromp", grompVec[i * 2], Quaternion.identity, 0);
            gromp[i].GetComponent<MonsterRespawn>().respawnRotating = grompVec[i * 2 + 1];
            rSentinel[i] = PhotonNetwork.Instantiate("Monster/R_Sentinel", rSentinelVec[i * 2], Quaternion.identity, 0);
            rSentinel[i].GetComponent<MonsterRespawn>().respawnRotating = rSentinelVec[i * 2 + 1];
            bSentinel[i] = PhotonNetwork.Instantiate("Monster/B_Sentinel", bSentinelVec[i * 2], Quaternion.identity, 0);
            bSentinel[i].GetComponent<MonsterRespawn>().respawnRotating = bSentinelVec[i * 2 + 1];
            wolfBig[i] = PhotonNetwork.Instantiate("Monster/Wolf_Big", wolf_BigVec[i * 2], Quaternion.identity, 0);
            wolfBig[i].GetComponent<MonsterRespawn>().respawnRotating = wolf_BigVec[i * 2 + 1];
            raptorBig[i] = PhotonNetwork.Instantiate("Monster/Raptor_Big", raptorBigVec[i * 2], Quaternion.identity, 0);
            raptorBig[i].GetComponent<MonsterRespawn>().respawnRotating = raptorBigVec[i * 2 + 1];
            
            if (i > 0)
                continue;

            //0 (맵에 1마리가 나오는 경우)
            baron = PhotonNetwork.Instantiate("Monster/Baron", baronVec[i * 2], Quaternion.identity, 0);
            baron.GetComponent<MonsterRespawn>().respawnRotating = baronVec[i * 2 + 1];
            riftHerald = PhotonNetwork.Instantiate("Monster/Rift_Herald", riftHeraldVec[i * 2], Quaternion.identity, 0);
            riftHerald.GetComponent<MonsterRespawn>().respawnRotating = riftHeraldVec[i * 2 + 1];
            dragon = PhotonNetwork.Instantiate("Monster/Dragon", dragonVec[i * 2], Quaternion.identity, 0);
            dragon.GetComponent<MonsterRespawn>().respawnRotating = dragonVec[i * 2 + 1];
        }
    }
}