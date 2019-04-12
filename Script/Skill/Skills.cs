using System.Collections.Generic;
using Pathfinding;
using UnityEngine;
using UnityEngine.SceneManagement;
using Werewolf.SpellIndicators;

/// <summary>
/// 모든 챔피언의 스킬의 부모 클래스이자, 공통된 부분을 묶어둔 스크립트
/// </summary>
public class Skills : Photon.PunBehaviour
{
    public ChampionAtk TheChampionAtk
    {
        get
        {
            return TheChampionBehaviour.myChampAtk;
        }
    }
    public ChampionData TheChampionData;
    public ChampionBehavior TheChampionBehaviour;
    public SplatManager TheSplatManager;
    public UIStat TheUIStat = null;
    public SkillClass.Skill skillData = null;
    public Dictionary<string, List<GameObject>> skillObj;
    public enum SkillSelect { none, Q, W, E, R };
    public SkillSelect skillSelect = SkillSelect.none;
    public bool isSkilling = false;

    protected AIPath TheAIPath = null;
    protected GameObject playerAStarTarget = null;
    protected Vector3 invokeVec = Vector3.zero;
    protected SkillClass TheSkillClass;
    protected ChampionAnimation championAnimation;
    protected GameObject skillParticleManager = null;

    private void OnLevelWasLoaded(int level)
    {
        if (SceneManager.GetSceneByBuildIndex(level).name.Equals("InGame"))
            Invoke("FindUICanvas", 3f);
    }

    /// <summary>
    /// 선택된 스킬을 해제하는 함수
    /// </summary>
    public void CancelSkill()
    {
        TheSplatManager.Cancel();
        skillSelect = SkillSelect.none;
        isSkilling = false;
    }

    /// <summary>
    /// 최초 설정 값을 잡는 함수
    /// </summary>
    public virtual void InitInstance()
    {
        skillObj = new Dictionary<string, List<GameObject>>();
        TheSkillClass = SkillClass.Instance;
        skillParticleManager = new GameObject("SkillParticleManager");
        skillParticleManager.transform.parent = this.transform.parent;
        TheChampionData = GetComponent<ChampionData>();
        TheSplatManager = GetComponentInChildren<SplatManager>();
        TheChampionBehaviour = GetComponent<ChampionBehavior>();
        championAnimation = GetComponent<ChampionAnimation>();
    }

    /// <summary>
    /// UI를 연결하는 함수
    /// </summary>
    public void FindUICanvas()
    {
        TheUIStat = GameObject.FindGameObjectWithTag("UICanvas").GetComponent<UICanvas>().stat.GetComponent<UIStat>();
    }

    public virtual void QCasting() { } // QCasting - Q - UsedQ(ChampionData) 순으로 불림
    public virtual void WCasting() { }
    public virtual void ECasting() { }
    public virtual void RCasting() { }

    public virtual void Q() { }
    public virtual void W() { }
    public virtual void E() { }
    public virtual void R() { }

    public virtual void QEffect() { } // 이펙트 동기화 함수 (벡터 필요 x일때)
    public virtual void WEffect() { }
    public virtual void EEffect() { }
    public virtual void REffect() { }

    public virtual void QVecEffect() { } // 이펙트 동기화 함수 (벡터가 들어갈 때)
    public virtual void WVecEffect() { }
    public virtual void EVecEffect() { }
    public virtual void RVecEffect() { }

    /// <summary>
    /// 챔피언의 스텟을 리턴하는 함수
    /// </summary>
    /// <param name="Astat">리턴을 원하는 스텟 종류</param>
    /// <param name="Avalue">곱해지는 가중치 값</param>
    /// <param name="isNotBuffDam">스킬로 버프된 값을 제외할 것인지 여부</param>
    /// <returns>챔피언의 스텟 값</returns>
    public int Acalculate(string Astat, float Avalue, bool isNotBuffDam = false)
    {
        float result = 0;

        switch (Astat)
        {
            case "AD":
                result = TheChampionData.totalStat.AttackDamage;
                if (isNotBuffDam)
                    result -= TheChampionData.skillPlusAtkDam;
                break;

            case "AP":
                result = TheChampionData.totalStat.AbilityPower;
                if (isNotBuffDam)
                    result -= TheChampionData.skillPlusAtkDam;
                break;

            case "DEF":
                result = TheChampionData.totalStat.AttackDef;
                break;

            case "MDEF":
                result = TheChampionData.totalStat.AbilityDef;
                break;

            case "maxHP":
                result = TheChampionData.totalStat.MaxHp;
                break;

            case "maxMP":
                result = TheChampionData.totalStat.MaxMp;
                break;

            case "minusHP":
                result = TheChampionData.totalStat.MaxHp - TheChampionData.totalStat.Hp;
                break;

            case "Critical":
                result = TheChampionData.totalStat.CriticalPercentage;
                break;

            default:
                break;
        }

        result *= Avalue;

        return Mathf.RoundToInt(result);
    }

    /// <summary>
    /// ~VecEffect 함수를 실행시키기 위한 Invoke 함수
    /// </summary>
    /// <param name="methodName">실행할 함수 명</param>
    /// <param name="number">이펙트가 재생되는 횟수</param>
    /// <param name="term">몇 초 뒤에 발동하는가</param>
    /// <param name="vec">스킬이 발동되는 목표 지점</param>
    public void InvokeVecEffect(string methodName, int number, float term, Vector3 vec)
    {
        invokeVec = vec;

        for (int i = 0; i < number; ++i)
            Invoke(methodName, term * i);
    }

    /// <summary>
    /// ~Effect 함수를 실행시키기 위한 Invoke 함수
    /// </summary>
    /// <param name="methodName">실행할 함수 명</param>
    /// <param name="number">이펙트가 재생되는 횟수</param>
    /// <param name="term">몇 초 뒤에 발동하는가</param>
    public void InvokeEffect(string methodName, int number, float term)
    {
        for (int i = 0; i < number; ++i)
            Invoke(methodName, term * i);
    }

    /// <summary>
    /// 다른 클라이언트에게 피격을 동기화시키기 위해 RPC를 돌리는 함수
    /// </summary>
    /// <param name="name">스킬을 사용한 챔피언의 이름</param>
    /// <param name="key">사용한 스킬 단축키</param>
    /// <param name="vec">스킬이 발동되는 목표 지점</param>
    /// <param name="number">이펙트가 몇 회 재생되어야 하는가</param>
    /// <param name="term">몇 초 뒤에 발동하는가</param>
    public void HitEffectVectorRPC(string name, string key, Vector3 vec, int number = 1, float term = 0)
    {
        int myViewID = GetComponent<PhotonView>().viewID;
        this.photonView.RPC("HitSyncEffectVector", PhotonTargets.Others, myViewID, name, key, vec, number, term);
    }

    /// <summary>
    /// 다른 클라이언트에게 피격을 동기화시키기 위해 RPC를 돌리는 함수
    /// </summary>
    /// <param name="name">스킬을 사용한 챔피언의 이름</param>
    /// <param name="key">사용한 스킬 단축키</param>
    /// <param name="number">이펙트가 몇 회 재생되어야 하는가</param>
    /// <param name="term">몇 초 뒤에 발동하는가</param>
    public void HitEffectRPC(string name, string key, int number = 1, float term = 0)
    {
        int myViewID = GetComponent<PhotonView>().viewID;
        this.photonView.RPC("HitSyncEffect", PhotonTargets.Others, myViewID, name, key, number, term);
    }

    /// <summary>
    /// 다른 클라이언트에게 피격을 동기화시키기 위해 RPC를 돌리는 함수
    /// </summary>
    /// <param name="viewID">피격당한 오브젝트의 viewID</param>
    /// <param name="damage">데미지</param>
    /// <param name="atkType">어택 타입</param>
    /// <param name="cc">상태 이상의 종류</param>
    public void HitRPC(int viewID, float damage, string atktype, string cc = null)
    {
        int myViewID = GetComponent<PhotonView>().viewID;
        this.photonView.RPC("HitSyncCCSkill", PhotonTargets.Others, viewID, damage, atktype, cc, myViewID);
    }

    /// <summary>
    /// 스킬을 종료하는 함수
    /// </summary>
    protected void OffSkillIng()
    {
        skillSelect = SkillSelect.none;
        isSkilling = false;
    }

    /// <summary>
    /// 스킬 종료를 요청하는 함수
    /// </summary>
    /// <param name="time">몇 초 뒤에 종료될 것인가</param>
    /// <param name="next">다음에 할 행동이 담긴 함수명</param>
    /// <param name="nextTime">현재 시점에서 몇 초 뒤에 다음 행동이 실행될 것인가</param>
    protected void EndSkill(float time, string next = "", float nextTime = 0)
    {
        Invoke("OffSkillIng", time);

        if (next != "")
            Invoke(next, time + nextTime);
    }

    /// <summary>
    /// 이동 불가를 해제하는 함수
    /// </summary>
    protected void OnMove()
    {
        Vector3 champPos = transform.position;
        champPos.y = 1;
        playerAStarTarget.transform.position = champPos;
        TheAIPath.isStopped = false;
    }

    /// <summary>
    /// 이동을 불가능하게 하는 함수
    /// </summary>
    /// <param name="time">정지가 지속될 시간</param>
    protected void PauseMove(float time)
    {
        TheAIPath.isStopped = true;
        Invoke("OnMove", time);
    }
}