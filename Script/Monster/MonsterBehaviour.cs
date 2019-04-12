using Pathfinding;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 개체를 제어하는 스크립트
/// </summary>
public class MonsterBehaviour : Photon.MonoBehaviour
{
    public FogOfWarEntity TheFogEntity;
    public MonsterAtk monAtk;
    public StatClass.Stat stat;
    public MonsterRespawn myCenter;
    public SkinnedMeshRenderer meshRenderForHit;
    public Material hitMat;
    public Material originMat;
    public List<GameObject> friendsList = new List<GameObject>();
    public string monsterJsonName;
    public bool isDead = false;

    private AIDestinationSetter TheAIDest;
    private AIPath TheAIPath;
    private BigJungleHP bigJungleHP;
    private SmallJungleHP smallJungleHP;
    private AOSMouseCursor cursor;
    private bool isFirstload = false;
    private bool isMouseChanged = false;

    private void Awake()
    {
        bigJungleHP = transform.GetComponent<BigJungleHP>();
        smallJungleHP = transform.GetComponent<SmallJungleHP>();
    }

    private void OnEnable()
    {
        isDead = false;

        // 처음 생성된 경우 사이즈에 맞는 HP 바를 붙여준다.
        if (isFirstload)
        {
            if (bigJungleHP != null)
                bigJungleHP.BasicSetting();

            if (smallJungleHP != null)
                smallJungleHP.BasicSetting();

            isDead = true;
        }

        if (!cursor)
            cursor = GameObject.FindGameObjectWithTag("MouseCursor").GetComponent<AOSMouseCursor>();
    }

    private void Start()
    {
        isFirstload = true;
    }

    private void OnMouseOver()
    {
        cursor.SetCursor(2, Vector2.zero);
        isMouseChanged = true;
    }

    private void OnMouseExit()
    {
        if (isMouseChanged)
        {
            cursor.SetCursor(cursor.PreCursor, Vector2.zero);
            isMouseChanged = false;
        }
    }

    /// <summary>
    /// 몬스터 사망 시 값을 초기화해주는 함수
    /// </summary>
    private void InitJungleStatus()
    {
        TheFogEntity.ClearEnteties();
        TheAIPath.isStopped = false;
        stat.Hp = stat.MaxHp;

        if (bigJungleHP != null)
            bigJungleHP.InitProgressBar();

        if (smallJungleHP != null)
            smallJungleHP.InitProgressBar();
    }

    /// <summary>
    /// 몬스터가 리스폰될 시 값을 초기화해주는 함수
    /// </summary>
    public void InitValue()
    {
        monAtk.InitValue();
    }

    /// <summary>
    /// 뒤늦게 할당해야하는 초기화 값들을 설정해주는 함수
    /// </summary>
    public void LateInit()
    {
        TheAIDest = GetComponent<AIDestinationSetter>();
        TheAIPath = GetComponent<AIPath>();
        monAtk = GetComponentInChildren<MonsterAtk>();
        TheFogEntity = GetComponent<FogOfWarEntity>();
        monAtk.LateInit();
    }

    /// <summary>
    /// 몬스터의 기본 스텟값을 설정하는 함수
    /// </summary>
    /// <param name="powerUp">한 번 죽은 후 강해지는 몬스터들을 위한 변수. 처음 설정할 때는 0, 죽고 부활한 경우는 1</param>
    public void SetStat(int powerUp)
    {
        //드래곤이나 골렘의 경우 죽어서 부활해도 더 강해지지 않으므로 0으로 고정
        if (monsterJsonName.Contains("Dragon") || monsterJsonName.Contains("Golem"))
            powerUp = 0;

        switch (powerUp)
        {
            case 0:
                stat = StatClass.Instance.characterData[monsterJsonName].ClassCopy();
                break;
            case 1:
                stat = StatClass.Instance.characterData[monsterJsonName + "2"].ClassCopy();
                break;
        }
    }

    /// <summary>
    /// 사망 시 Dead 함수를 호출해주는 함수
    /// </summary>
    /// <param name="time">몇 초 뒤에 사망하는가</param>
    private void IamDead(float time = 0)
    {
        myCenter.SetPosition();
        Invoke("Dead", time);
    }

    /// <summary>
    /// 사망처리를 하는 함수
    /// </summary>
    private void Dead()
    {
        InitJungleStatus();

        if (monAtk == null)
            monAtk = transform.GetComponentInChildren<MonsterAtk>();

        if (TheAIDest == null)
            TheAIDest = gameObject.GetComponent<AIDestinationSetter>();

        if (TheAIPath == null)
            TheAIPath = gameObject.GetComponent<AIPath>();

        monAtk.TheAIPath = null;
        monAtk.nowTarget = null;
        monAtk.StopAllCoroutines();
        TheAIPath.canMove = true;
        TheAIPath.canSearch = true;
        gameObject.GetComponent<AIDestinationSetter>().target = null;
        myCenter.StartCoroutine("Respawn");
        TheAIDest.target = myCenter.transform;
        monAtk.nowTarget = null;
        gameObject.SetActive(false);

        // 죽을때 마우스바뀐상태면 원래대로
        if (isMouseChanged)
            cursor.SetCursor(cursor.PreCursor, Vector2.zero);
    }

    /// <summary>
    /// 피격을 당했을 때의 처리를 담당하는 함수
    /// </summary>
    /// <param name="damage">공격자의 공격력</param>
    /// <param name="atkType">공격의 타입</param>
    /// <param name="atker">공격한 챔피언</param>
    public bool HitMe(float damage = 0, string atkType = "AD", GameObject atker = null) // AD, AP, FD(고정 데미지 = Fixed damage)
    {
        if (!PhotonNetwork.isMasterClient)
            return false;

        bool isDead = false;

        //데미지 계산
        if (atkType.Equals("AD") || atkType.Equals("ad"))
            damage = (damage * 100f) / (100f + stat.AttackDef);
        else if (atkType.Equals("AP") || atkType.Equals("ap"))
            damage = (damage * 100f) / (100f + stat.AbilityDef);

        //이미 다른 개체에 피격당해 체력이 0 이하라면 더 맞아봐야 소용없으니 리턴
        if (stat.Hp < 1)
            return false;

        //몬스터의 상태는 마스터에서만 판단하고 처리한다.
        if (PhotonNetwork.isMasterClient)
        {
            if (!monAtk.isReturn)
            {
                if (!monAtk.isAtking)
                {
                    monAtk.isAtking = true;
                    monAtk.nowTarget = atker;
                    TheAIDest.target = atker.transform;

                    for (int i = 0; i < friendsList.Count; ++i)
                        if (friendsList[i] != null)
                            if (friendsList[i].activeInHierarchy)
                                friendsList[i].GetComponent<MonsterBehaviour>().monAtk.isAtking = true;
                }
            }
        }

        //체력을 감소시킴
        stat.Hp -= damage;

        //사망한 경우
        if (stat.Hp < 1)
        {
            isDead = true;
            bool isDragon = false;
            string team = "";

            //드래곤이 죽은 경우의 처리
            if (gameObject.name.Contains("Dragon"))
            {
                isDragon = true;

                if (atker.GetComponent<PhotonView>().owner.GetTeam().Equals("red"))
                {
                    GameObject.FindGameObjectWithTag("InGameManager").GetComponent<InGameManager>().blueTeamDragonKill++;
                    team = "blue";
                }
                else
                {
                    GameObject.FindGameObjectWithTag("InGameManager").GetComponent<InGameManager>().redTeamDragonKill++;
                    team = "red";
                }
            }

            bool isChamp = true;
            int id;

            if (atker == null)
                isChamp = false;
            else if (!atker.layer.Equals(LayerMask.NameToLayer("Champion")))
                isChamp = false;

            if (isChamp)
                id = atker.GetPhotonView().viewID;
            else
                id = -1;

            //킬매니저에게 자신이 죽었음을 알림
            KillManager.instance.SomebodyKillMonsterRPC(this.photonView.viewID, id, isChamp, isDragon, team);
        }
        else
        {//사망하지 않은 경우
            if (meshRenderForHit.materials.Length > 1 && meshRenderForHit)
            {
                meshRenderForHit.material = hitMat;
                StartCoroutine(ResetMaterial(0.05f));
            }

            //킬매니저에게 자신이 맞았음을 알림
            KillManager.instance.ChangeMonsterHPRPC(this.photonView.viewID, stat.Hp);
        }

        return isDead;
    }

    /// <summary>
    /// 몬스터가 공격을 당하여 빛나는 마테리얼로 교체된 경우, 본래대로 돌려주는 함수
    /// </summary>
    /// <param name="Time">본래의 마테리얼로 몇 초 뒤에 돌아가는가</param>
    System.Collections.IEnumerator ResetMaterial(float Time)
    {
        yield return new WaitForSeconds(Time);

        meshRenderForHit.material = originMat;
        meshRenderForHit.materials[0] = originMat;
    }

    /// <summary>
    /// 사망 판정 시 사망 함수를 불러주고 기타 처리를 해주는 함수
    /// </summary>
    /// <param name="time">사망이 지연되는 시간</param>
    /// <param name="isDragon">자신이 드래곤인지에 대한 여부</param>
    /// <param name="team">자신을 죽인 팀의 종류 (red, blue)</param>
    public void CallDead(float time, bool isDragon, string team)
    {
        isDead = true;
        stat.Hp = 0;
        IamDead(time); //사망 함수를 호출

        if (monAtk == null)
            monAtk = transform.GetComponentInChildren<MonsterAtk>();

        monAtk.enemiesList.Clear();

        if (PhotonNetwork.isMasterClient)
            return;

        //드래곤인 경우의 처리
        if (isDragon)
        {
            if (team.Equals("red"))
                GameObject.FindGameObjectWithTag("InGameManager").GetComponent<InGameManager>().blueTeamDragonKill++;
            else
                GameObject.FindGameObjectWithTag("InGameManager").GetComponent<InGameManager>().redTeamDragonKill++;
        }
    }

    /// <summary>
    /// RPC로 피격을 동기화시키는 함수
    /// </summary>
    /// <param name="viewID">피격당한 오브젝트의 viewID</param>
    [PunRPC]
    public void HitSync(int viewID)
    {
        GameObject obj = PhotonView.Find(viewID).gameObject;

        //피격당한 대상의 HitMe 함수를 호출함.
        if (obj != null)
        {
            if (obj.layer.Equals(LayerMask.NameToLayer("Champion")))
            {
                ChampionBehavior champBehav = obj.GetComponent<ChampionBehavior>();

                if (champBehav != null)
                    champBehav.HitMe(stat.AttackDamage, "AD", gameObject, gameObject.name);
            }
        }
    }

    /// <summary>
    /// 다른 클라이언트에게 피격을 동기화시키기 위해 RPC를 돌리는 함수
    /// </summary>
    /// <param name="viewID">피격당한 오브젝트의 viewID</param>
    public void HitRPC(int viewID)
    {
        if (this.photonView == null)
            return;

        this.photonView.RPC("HitSync", PhotonTargets.Others, viewID);
    }

    /// <summary>
    /// RPC로 현재 리턴하는지의 여부를 동기화시키는 함수
    /// </summary>
    /// <param name="_return">마스터 클라이언트에서의 몬스터 리턴 값</param>
    [PunRPC]
    public void ReturnOtherClientsSync(bool _return)
    {
        monAtk.isReturn = _return;
    }

    /// <summary>
    /// 다른 클라이언트에게 리턴값을 동기화시키기 위해 RPC를 돌리는 함수
    /// </summary>
    /// <param name="_return">마스터 클라이언트에서의 몬스터 리턴 값</param>
    public void ReturnOtherClients(bool _return)
    {
        this.photonView.RPC("ReturnOtherClientsSync", PhotonTargets.Others, _return);
    }
}