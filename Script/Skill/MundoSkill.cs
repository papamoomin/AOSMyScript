using Pathfinding;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 문도의 스킬을 담당하는 스크립트
/// </summary>
public class MundoSkill : Skills
{
    public GameObject qSkillObj = null;
    public GameObject wSkillObj = null;
    public GameObject[] eSkillObj = null;
    public GameObject rSkillObj = null;
    public GameObject[] rSkillOnlyFirstEffect = null;
    public GameObject mundoQEffect;
    public FogOfWarEntity myFogEntity;
    
    private GameObject mundoQEffectParticle;
    private Vector3 arguVec = Vector3.zero;
    private bool isW = false;
    private bool isE = false;
    private bool isR = false;
    private float wTime = 1f;
    private float eTime = 5f;
    private float rTime = 1f;
    private float rHealValue = 0;
    private int rCount = 12;
    
    private void Awake()
    {
        InitInstance();
    }

    private void Update()
    {
        //우클릭하거나 esc키를 누르면 스킬 선택이 해제된다.
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            CancelSkill();

        // Q 스킬을 선택한 후 마우스 왼쪽 버튼을 클릭
        if (skillSelect.Equals(SkillSelect.Q))
        {
            if (Input.GetMouseButtonDown(0))
            {
                skillSelect = SkillSelect.none;
                Vector3 mousePos = Input.mousePosition;
                Ray r = Camera.main.ScreenPointToRay(mousePos);
                RaycastHit[] hits = Physics.RaycastAll(r, 50f);
                arguVec = Vector3.zero;

                foreach (RaycastHit hit in hits)
                {
                    //클릭된 지점을 향해 스킬 발동
                    if (hit.collider.tag.Equals("Terrain"))
                    {
                        isSkilling = true;
                        TheSplatManager.Cancel();
                        CallChampDataUsedSkill("Q");
                        arguVec = hit.point;
                        arguVec.y = 0.5f;
                        championAnimation.AnimationApply("Q", true);
                        championAnimation.AnimationApply("Q", false, 0.5f);
                        Invoke("Q", 0.2f);
                        break;
                    }
                }
            }
        }

        // W 스킬을 사용중이라면 시간을 계산
        if (isW)
        {
            wTime -= Time.deltaTime;

            if (wTime < 0)
            {
                wTime += 1;

                // 체력이 소진되었다면 W 스킬을 해제한다
                if (TheChampionData.totalStat.Hp - 2 < TheChampionData.mana_W)
                {
                    HitEffectRPC("Mundo", "W");
                    wSkillObj.SetActive(false);
                    isW = false;
                    wTime = 1;
                    EndSkill(0f);
                }
                else // 체력이 남아있다면 체력을 깎는다
                    TheChampionData.totalStat.Hp -= TheChampionData.mana_W;
            }
        }

        // E 스킬을 사용중이라면 시간을 계산
        if (isE)
        {
            if (photonView.isMine)
            {
                //일정 시간동안 공격력이 증가한다
                eTime -= Time.deltaTime;
                float losedHpPercent = ((TheChampionData.totalStat.MaxHp - TheChampionData.totalStat.Hp) / TheChampionData.totalStat.MaxHp);
                float minimalSkillAD = skillData.eDamage[TheChampionData.skill_E - 1] / 2f;
                TheChampionData.skillPlusAtkDam = minimalSkillAD + (minimalSkillAD * losedHpPercent);

                if (TheChampionAtk.skillKey.Equals("MundoE") && TheChampionAtk.skillKeyNum > 0)
                {
                    int eLv = TheChampionData.skill_E - 1;
                    TheChampionData.skillPlusAtkDam += TheChampionData.totalStat.MaxHp * (0.03f + (float)eLv * 0.005f);
                }

                TheChampionData.skillPlusAtkDam = Mathf.Round(TheChampionData.skillPlusAtkDam);

                // 지속 시간이 끝나면 E 스킬을 해제
                if (eTime <= 0)
                {
                    HitEffectRPC("Mundo", "E");
                    E();
                }

                TheChampionData.TotalStatDamDefUpdate();
                TheChampionData.UIStat.Refresh();
            }
        }

        // R 스킬을 사용중이라면 시간을 계산
        if (isR)
        {
            if (photonView.isMine)
            {
                // 만약 스킬이 지속되는 도중 죽는다면 스킬 관련 값을 초기화한다
                if (TheChampionBehaviour.isDead)
                {
                    rSkillObj.SetActive(false);
                    TheChampionData.totalStat.Hp = 0;
                    ChampionSound.instance.TempAudio.Stop();
                    ChampionSound.instance.TempAudio.loop = false;
                    ChampionSound.instance.TempAudio.clip = null;
                    TheChampionData.skillPlusSpeed = 0;
                    TheChampionData.TotalStatSpeedUpdate();
                    TheChampionData.UIStat.Refresh();
                    return;
                }

                rTime -= Time.deltaTime;

                // 1초마다 체력을 회복한다
                if (rTime <= 0)
                {
                    rTime = 1f;
                    TheChampionData.totalStat.Hp += rHealValue;

                    // 지속시간이 끝나면 스킬을 해제한다
                    if (--rCount == 0)
                    {
                        HitEffectRPC("Mundo", "R");
                        R();
                    }
                }
            }
        }
    }

    /// <summary>
    /// 최초 설정 값을 잡는 함수
    /// </summary>
    public override void InitInstance()
    {
        base.InitInstance();
        TheChampionData.playerSkill = this;
        playerAStarTarget = GetComponent<PlayerMouse>().myTarget;
        TheAIPath = GetComponent<AIPath>();
        skillData = TheSkillClass.skillData["Mundo"];
        string team = GetComponent<PhotonView>().owner.GetTeam().ToString();

        if (team.Equals("red"))
        {
            qSkillObj.GetComponent<FogOfWarEntity>().faction = FogOfWar.Players.Player00;
            wSkillObj.GetComponent<FogOfWarEntity>().faction = FogOfWar.Players.Player00;
            rSkillObj.GetComponent<FogOfWarEntity>().faction = FogOfWar.Players.Player00;
        }
        else if (team.Equals("blue"))
        {
            qSkillObj.GetComponent<FogOfWarEntity>().faction = FogOfWar.Players.Player01;
            wSkillObj.GetComponent<FogOfWarEntity>().faction = FogOfWar.Players.Player01;
            rSkillObj.GetComponent<FogOfWarEntity>().faction = FogOfWar.Players.Player01;
        }
    }

    /// <summary>
    /// Q 스킬을 호출하는 함수
    /// </summary>
    public override void QCasting()
    {
        isSkilling = true;
        skillSelect = SkillSelect.Q;
        TheSplatManager.Direction.Select();
        TheSplatManager.Direction.Scale = 25f;
    }

    /// <summary>
    /// W 스킬을 호출하는 함수
    /// </summary>
    public override void WCasting()
    {
        NotUseSplatSkillCasting("W");
    }

    /// <summary>
    /// E 스킬을 호출하는 함수
    /// </summary>
    public override void ECasting()
    {
        NotUseSplatSkillCasting("E");
    }

    /// <summary>
    /// R 스킬을 호출하는 함수
    /// </summary>
    public override void RCasting()
    {
        NotUseSplatSkillCasting("R");
    }

    /// <summary>
    /// 반경이 표시되지 않는 스킬의 공통된 계산 처리를 담당하는 함수
    /// </summary>
    /// <param name="skillKey"></param>
    private void NotUseSplatSkillCasting(string skillKey)
    {
        isSkilling = true;
        skillSelect = SkillSelect.none;
        TheSplatManager.Cancel();
        HitEffectRPC("Mundo", skillKey);
        CallChampDataUsedSkill(skillKey);
        CallSkill(skillKey);
        EndSkill(0f);
    }

    /// <summary>
    /// ChampionData에 스킬을 사용했음을 알리는 함수
    /// </summary>
    /// <param name="skillKey">사용한 스킬 단축키("Q", "W", "E", "R")</param>
    private void CallChampDataUsedSkill(string skillKey)
    {
        switch (skillKey)
        {
            case "Q": TheChampionData.UsedQ(); break;
            case "W": TheChampionData.UsedW(); break;
            case "E": TheChampionData.UsedE(); break;
            case "R": TheChampionData.UsedR(); break;
        }
    }

    /// <summary>
    /// 해당 스킬 함수를 호출하는 함수
    /// </summary>
    /// <param name="skillKey">사용한 스킬 단축키("W", "E", "R")</param>
    private void CallSkill(string skillKey)
    {
        switch (skillKey)
        {
            case "W": W(); break;
            case "E": E(); break;
            case "R": R(); break;
        }
    }

    /// <summary>
    /// Q 스킬 함수
    /// </summary>
    public override void Q()
    {
        Vector3 dest = arguVec;
        arguVec = Vector3.zero;
        qSkillObj.SetActive(true);
        qSkillObj.transform.position = transform.position;
        transform.DOLookAt(dest, 0);
        qSkillObj.transform.DOLookAt(dest, 0);
        HitEffectVectorRPC("Mundo", "Q", dest);
        qSkillObj.GetComponent<MundoQ>().SkillOn(dest);
        PauseMove(0.5f);
        EndSkill(0.5f);
    }

    /// <summary>
    /// W 스킬 함수
    /// </summary>
    public override void W()
    {
        // 스킬을 켜고 끄는 것을 W 함수 하나가 담당하게 하기 위해 bool 값을 토글시킴
        isW = !isW;

        if (isW)
        {
            wSkillObj.SetActive(true);
            TheChampionData.current_Cooldown_W = -1f;
        }
        else
        {
            wSkillObj.SetActive(false);
            wTime = 1;
        }
    }

    /// <summary>
    /// E 스킬 함수
    /// </summary>
    public override void E()
    {
        // 스킬을 켜고 끄는 것을 E 함수 하나가 담당하게 하기 위해 bool 값을 토글시킴
        isE = !isE;

        if (isE)
        {
            TheChampionAtk.skillKey = "MundoE";
            TheChampionAtk.skillKeyNum = 1;
            eTime = 5f;

            for (int i = 0; i < 2; ++i)
                eSkillObj[i++].SetActive(true);
        }
        else
        {
            TheChampionAtk.skillKey = "";
            TheChampionAtk.skillKeyNum = 0;

            for (int i = 0; i < 2; ++i)
                eSkillObj[i].SetActive(false);

            TheChampionData.skillPlusAtkDam = 0;
        }
    }

    /// <summary>
    /// R 스킬 함수
    /// </summary>
    public override void R()
    {
        // 스킬을 켜고 끄는 것을 R 함수 하나가 담당하게 하기 위해 bool 값을 토글시킴
        isR = !isR;

        if (isR)
        {
            rTime = 1f;
            rCount = 12;
            rSkillObj.SetActive(true);
            ChampionSound.instance.TempAudio.loop = true;
            ChampionSound.instance.TempAudio.clip = ChampionSound.instance.Mundo_RActive;
            ChampionSound.instance.TempAudio.Play();
            float hpPercent = TheChampionData.totalStat.MaxHp;

            // 스킬 레벨에 따라 회복량을 설정
            if (TheChampionData.skill_R.Equals(2))
                hpPercent *= 0.75f;
            else if (TheChampionData.skill_R.Equals(1))
                hpPercent *= 0.5f;

            // 12회에 걸쳐 회복시킴
            rHealValue = hpPercent / 12f;

            if (photonView.isMine)
            {
                float percent = 0.15f + ((float)(TheChampionData.skill_R - 1) * 0.1f);
                TheChampionData.skillPlusSpeed = (TheChampionData.myStat.MoveSpeed + TheChampionData.itemStat.movementSpeed) * percent;
                TheChampionData.TotalStatSpeedUpdate();
                TheChampionData.UIStat.Refresh();

                for (int i = 0; i < rSkillOnlyFirstEffect.Length; ++i)
                    rSkillOnlyFirstEffect[i].SetActive(true);
            }
            else
            {// 현재 플레이어의 위치에 따라 스킬 이펙트가 누구에게 보이는지 계산
                // 적이 시야 범위에 있음
                if (myFogEntity.isInTheSightRange)
                {
                    // 시야 범위 내의 부쉬 안에 있음
                    if (myFogEntity.isInTheBush)
                    {
                        // 그 부쉬에 우리 팀도 있으면 보임
                        if (myFogEntity.isInTheBushMyEnemyToo)
                            for (int i = 0; i < rSkillOnlyFirstEffect.Length; ++i)
                                rSkillOnlyFirstEffect[i].SetActive(true);
                        else// 그 부쉬에 우리 팀이 없으면 안보임
                            for (int i = 0; i < rSkillOnlyFirstEffect.Length; ++i)
                                rSkillOnlyFirstEffect[i].SetActive(false);
                    }
                    else// 시야 범위 내인데 부쉬에도 없으면 보임
                        for (int i = 0; i < rSkillOnlyFirstEffect.Length; ++i)
                            rSkillOnlyFirstEffect[i].SetActive(true);
                }
                else// 적이 시야 범위에 없으면 안보임
                    for (int i = 0; i < rSkillOnlyFirstEffect.Length; ++i)
                        rSkillOnlyFirstEffect[i].SetActive(false);
            }
        }
        else
        {// 스킬을 해제함
            rSkillObj.SetActive(false);
            ChampionSound.instance.TempAudio.Stop();
            ChampionSound.instance.TempAudio.loop = false;
            ChampionSound.instance.TempAudio.clip = null;

            if (photonView.isMine)
            {
                TheChampionData.skillPlusSpeed = 0;
                TheChampionData.TotalStatSpeedUpdate();
                TheChampionData.UIStat.Refresh();
            }
        }
    }

    /// <summary>
    /// Q 스킬을 동기화한 함수
    /// </summary>
    public override void QVecEffect()
    {
        Vector3 dest = invokeVec;
        invokeVec = Vector3.zero;
        qSkillObj.SetActive(true);
        qSkillObj.transform.position = transform.position;
        qSkillObj.transform.DOLookAt(dest, 0);
        transform.DOLookAt(dest, 0);
        qSkillObj.GetComponent<MundoQ>().SkillOn(dest);
        EndSkill(0);
    }

    /// <summary>
    /// W 스킬을 동기화한 함수
    /// </summary>
    public override void WEffect()
    {
        W();
    }

    /// <summary>
    /// E 스킬을 동기화한 함수
    /// </summary>
    public override void EEffect()
    {
        E();
    }

    /// <summary>
    /// R 스킬을 동기화한 함수
    /// </summary>
    public override void REffect()
    {
        R();
    }

    /// <summary>
    /// 체력을 회복시키는 함수
    /// </summary>
    /// <param name="value">회복될 hp 값</param>
    public void Heal(float value)
    {
        TheChampionData.totalStat.Hp += value;

        if (TheChampionData.totalStat.Hp > TheChampionData.totalStat.MaxHp)
            TheChampionData.totalStat.Hp = TheChampionData.totalStat.MaxHp;
    }

    /// <summary>
    /// Q 스킬 시전시 이펙트를 켜는 함수
    /// </summary>
    /// <param name="pos">이펙트가 켜질 위치</param>
    public void activeMundoQEffect(Vector3 pos)
    {
        if (mundoQEffect)
        {
            if (!mundoQEffectParticle)
                mundoQEffectParticle = mundoQEffect.transform.GetChild(0).gameObject;

            mundoQEffect.transform.position = pos;
            mundoQEffectParticle.SetActive(true);
        }
    }
}