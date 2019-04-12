using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 오브젝트간의 공격, 사망을 동기화하는 매니저 스크립트
/// </summary>
public class KillManager : Photon.PunBehaviour
{
    public static List<MinionBehavior> photonMinionList = new List<MinionBehavior>();
    public static Dictionary<int, MonsterBehaviour> photonMonsterDic = new Dictionary<int, MonsterBehaviour>();
    public Dictionary<int, ChampionData> photonChampDic = new Dictionary<int, ChampionData>();
    public Dictionary<int, ChampionBehavior> photonChampBehavDic = new Dictionary<int, ChampionBehavior>();
    public static KillManager instance
    {
        get
        {
            if (_instance == null)
                _instance = (KillManager)FindObjectOfType(typeof(KillManager));

            return _instance;
        }
    }

    private SystemMessage sysMsg;
    private static KillManager _instance = null;

    void Awake()
    {
        sysMsg = GameObject.FindGameObjectWithTag("SystemMsg").GetComponent<SystemMessage>();
    }

    private void OnDestroy()
    {
        photonMinionList.Clear();
        photonChampDic.Clear();
        photonMonsterDic.Clear();
        photonChampBehavDic.Clear();
        _instance = null;
    }

    /// <summary>
    /// 몬스터의 체력을 동기화시키는 함수
    /// </summary>
    /// <param name="monViewID">체력이 변경된 몬스터의 viewID</param>
    /// <param name="hp">변경된 체력 값</param>
    [PunRPC]
    public void ChangeMonsterHP(int monViewID, float hp)
    {
        if (this != null)
        {
            if (!photonMonsterDic.ContainsKey(monViewID))
            {
                MonsterBehaviour monBehav = PhotonView.Find(monViewID).GetComponent<MonsterBehaviour>();

                if (monBehav != null)
                    photonMonsterDic.Add(monViewID, monBehav);
            }

            if (photonMonsterDic[monViewID] != null)
                if (photonMonsterDic[monViewID].gameObject.activeInHierarchy)
                    photonMonsterDic[monViewID].stat.Hp = hp;
        }
    }

    /// <summary>
    /// 다른 클라이언트에게 몬스터의 체력을 동기화시키기 위해 RPC를 돌리는 함수
    /// </summary>
    /// <param name="monViewID">체력이 변경된 몬스터의 viewID</param>
    /// <param name="hp">변경된 체력 값</param>
    public void ChangeMonsterHPRPC(int monViewID, float hp)
    {
        photonView.RPC("ChangeMonsterHP", PhotonTargets.Others, monViewID, hp);
    }

    /// <summary>
    /// 미니언의 체력을 동기화시키는 함수
    /// </summary>
    /// <param name="minKey">체력이 변경된 미니언의 키 값</param>
    /// <param name="hp">변경된 체력 값</param>
    [PunRPC]
    public void ChangeMinionHP(int minKey, float hp)
    {
        if (this != null)
            if (photonMinionList.Count > minKey)
                if (photonMinionList[minKey] != null)
                    if (photonMinionList[minKey].gameObject.activeInHierarchy)
                        photonMinionList[minKey].stat.Hp = hp;
    }

    /// <summary>
    /// 다른 클라이언트에게 미니언의 체력을 동기화시키기 위해 RPC를 돌리는 함수
    /// </summary>
    /// <param name="minKey">체력이 변경된 미니언의 키 값</param>
    /// <param name="hp">변경된 체력 값</param>
    public void ChangeMinionHPRPC(int minKey, float hp)
    {
        photonView.RPC("ChangeMinionHP", PhotonTargets.Others, minKey, hp);
    }

    /// <summary>
    /// 미니언의 사망을 동기화시키는 함수
    /// </summary>
    /// <param name="minKey">체력이 변경된 미니언의 키 값</param>
    /// <param name="champViewID">미니언을 죽인 적이 챔피언인 경우의 viewID</param>
    /// <param name="isChamp">챔피언이 미니언을 죽인 것인가의 유무</param>
    [PunRPC]
    public void SomebodyKillMinion(int minKey, int champViewID, bool isChamp)
    {
        if (this != null)
        {
            if (isChamp)
            {
                if (!photonChampDic.ContainsKey(champViewID))
                {
                    ChampionData ChampData = PhotonView.Find(champViewID).GetComponent<ChampionData>();

                    if (ChampData != null)
                        photonChampDic.Add(champViewID, ChampData);
                }

                if (photonChampDic[champViewID].GetComponent<PhotonView>().owner.Equals(PhotonNetwork.player))
                    photonChampDic[champViewID].Kill_CS_Gold_Exp(photonMinionList[minKey].name, 1, photonMinionList[minKey].transform.position);
            }

            photonMinionList[minKey].CallDead(0.2f);
        }
    }

    /// <summary>
    /// 다른 클라이언트에게 미니언의 사망을 동기화시키기 위해 RPC를 돌리는 함수
    /// </summary>
    /// <param name="minKey">체력이 변경된 미니언의 키 값</param>
    /// <param name="champViewID">미니언을 죽인 적이 챔피언인 경우의 viewID</param>
    /// <param name="isChamp">챔피언이 미니언을 죽인 것인가의 유무</param>
    public void SomebodyKillMinionRPC(int minKey, int champViewID, bool isChamp)
    {
        this.photonView.RPC("SomebodyKillMinion", PhotonTargets.AllViaServer, minKey, champViewID, isChamp);
    }

    /// <summary>
    /// 몬스터의 사망을 동기화시키는 함수
    /// </summary>
    /// <param name="monViewID">사망한 몬스터의 viewID</param>
    /// <param name="champViewID">몬스터를 죽인 챔피언의 viewID</param>
    /// <param name="isChamp">챔피언이 죽였는가의 유무</param>
    /// <param name="isDragon">죽은 몬스터가 드래곤인가의 유무</param>
    /// <param name="team">몬스터를 죽인 챔피언이 속한 팀</param>
    [PunRPC]
    public void SomebodyKillMonster(int monViewID, int champViewID, bool isChamp, bool isDragon, string team = "")
    {
        if (this != null)
        {
            if (!photonMonsterDic.ContainsKey(monViewID))
            {
                MonsterBehaviour monBehav = PhotonView.Find(monViewID).GetComponent<MonsterBehaviour>();

                if (monBehav != null)
                    photonMonsterDic.Add(monViewID, monBehav);
            }

            if (isChamp)
            {
                if (!photonChampDic.ContainsKey(champViewID))
                {
                    ChampionData ChampData = PhotonView.Find(champViewID).GetComponent<ChampionData>();

                    if (ChampData != null)
                        photonChampDic.Add(champViewID, ChampData);
                }

                if (photonChampDic[champViewID].GetComponent<PhotonView>().owner.Equals(PhotonNetwork.player))
                    photonChampDic[champViewID].Kill_CS_Gold_Exp(photonMonsterDic[monViewID].name, 3, photonMonsterDic[monViewID].transform.position);
            }

            photonMonsterDic[monViewID].CallDead(0.05f, isDragon, team);
        }
    }

    /// <summary>
    /// 다른 클라이언트에게 몬스터의 사망을 동기화시키기 위해 RPC를 돌리는 함수
    /// </summary>
    /// <param name="monViewID">사망한 몬스터의 viewID</param>
    /// <param name="champViewID">몬스터를 죽인 챔피언의 viewID</param>
    /// <param name="isChamp">챔피언이 죽였는가의 유무</param>
    /// <param name="isDragon">죽은 몬스터가 드래곤인가의 유무</param>
    /// <param name="team">몬스터를 죽인 챔피언이 속한 팀</param>
    public void SomebodyKillMonsterRPC(int monViewID, int champViewID, bool isChamp, bool isDragon, string team = "")
    {
        photonView.RPC("SomebodyKillMonster", PhotonTargets.AllViaServer, monViewID, champViewID, isChamp, isDragon, team);
    }

    /// <summary>
    /// 챔피언의 사망을 동기화시키는 함수
    /// </summary>
    /// <param name="dieViewID">사망한 챔피언의 viewID</param>
    /// <param name="atkViewID">챔피언을 죽인 오브젝트의 viewID</param>
    /// <param name="isAtkerJobChamp">죽인 이가 챔피언인가의 유무</param>
    /// <param name="killerName">죽인 이의 이름</param>
    [PunRPC]
    public void SomebodyKillChampion(int dieViewID, int atkViewID, bool isAtkerJobChamp, string killerName)
    {
        ChampionBehavior dieChampBehav;
        ChampionData atkChampData;

        if (!photonChampBehavDic.ContainsKey(dieViewID))
        {
            dieChampBehav = PhotonView.Find(dieViewID).GetComponent<ChampionBehavior>();

            if (dieChampBehav != null)
                photonChampBehavDic.Add(dieViewID, dieChampBehav);
        }

        if (isAtkerJobChamp)
        {
            if (!photonChampDic.ContainsKey(atkViewID))
            {
                atkChampData = PhotonView.Find(atkViewID).GetComponent<ChampionData>();

                if (atkChampData != null)
                    photonChampDic.Add(atkViewID, atkChampData);
            }

            if (photonChampDic[atkViewID].GetComponent<PhotonView>().owner.Equals(PhotonNetwork.player))
            {
                if (atkViewID == dieViewID) 
                    return;

                photonChampDic[atkViewID].GetComponent<ChampionBehavior>().myChampAtk.IKillChamp();
                photonChampDic[atkViewID].Kill_CS_Gold_Exp(photonChampBehavDic[dieViewID].name, 0, photonChampBehavDic[dieViewID].transform.position);
            }
        }

        //시스템 메세지 출력
        if (killerName.Contains("tower") || killerName.Contains("Tower"))
            sysMsg.sendKillmsg("tower", photonChampBehavDic[dieViewID].name.ToString(), "ex");
        else if (killerName.Contains("Minion") || killerName.Contains("minion"))
        {
            if (photonChampBehavDic[dieViewID].team.ToLower().Equals("red"))
                sysMsg.sendKillmsg("minion", photonChampBehavDic[dieViewID].name.ToString(), "blue");
            else if (photonChampBehavDic[dieViewID].team.ToLower().Equals("blue"))
                sysMsg.sendKillmsg("minion", photonChampBehavDic[dieViewID].name.ToString(), "red");
        }
        else if (killerName.Contains("Obj") || killerName.Contains("obj"))
            sysMsg.sendKillmsg("monster", photonChampBehavDic[dieViewID].name.ToString(), "ex");
        else
        {// 죽인 이가 챔피언인 경우
            if (photonChampBehavDic[dieViewID].team.ToLower().Equals("red"))
                sysMsg.sendKillmsg(killerName, photonChampBehavDic[dieViewID].name.ToString(), "blue");
            else if (photonChampBehavDic[dieViewID].team.ToLower().Equals("blue"))
                sysMsg.sendKillmsg(killerName, photonChampBehavDic[dieViewID].name.ToString(), "red");
        }

        photonChampBehavDic[dieViewID].CallDead(0.2f, atkViewID, isAtkerJobChamp);
    }

    /// <summary>
    /// 다른 클라이언트에게 몬스터의 사망을 동기화시키기 위해 RPC를 돌리는 함수
    /// </summary>
    /// <param name="dieViewID">사망한 챔피언의 viewID</param>
    /// <param name="atkViewID">챔피언을 죽인 오브젝트의 viewID</param>
    /// <param name="isAtkerJobChamp">죽인 이가 챔피언인가의 유무</param>
    /// <param name="killerName">죽인 이의 이름</param>
    public void SomebodyKillChampionRPC(int dieViewID, int atkViewID, bool atkIsChamp, string killerName)
    {
        photonView.RPC("SomebodyKillChampion", PhotonTargets.AllViaServer, dieViewID, atkViewID, atkIsChamp, killerName);
    }
}