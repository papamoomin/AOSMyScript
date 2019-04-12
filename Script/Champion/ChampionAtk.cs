using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// 챔피언의 공격을 담당하는 스크립트
/// </summary>
public class ChampionAtk : MonoBehaviour
{
    public AIPath TheAIPath;
    public GameObject myChamp = null;
    public GameObject aStarTargetObj = null;
    public GameObject atkTargetObj = null;
    public GameObject stunParticle = null;
    public bool isTargetting = false;
    public bool isWillAtkAround = false;
    public bool isWarding = false;
    public bool isStun = false;
    public bool isPushing = false;
    public string skillKey = ""; 
    public float atkRange = 3f;
    public int wardAmount = 1;
    public int skillKeyNum = 0;

    private AIDestinationSetter TheAIDest;
    private PlayerMouse ThePlayerMouse;
    private ChampionData myChampionData = null;
    private ChampionBehavior myChampBehav;
    private ChampionAnimation myChampionAnimation;
    private AsheSkill asheSkill = null;
    private SystemMessage sysmsg;
    private MouseFxPooling fxpool;
    private Tweener pushTween = null;
    private Coroutine atkCoroutine;
    private List<GameObject> enemiesList;
    private Vector3 atkTargetPos, myChampPos;
    private const float wardMadeMinTime = 120f;
    private const float wardMadeMaxTime = 240f;
    private const float wardMadeTermTime = 120f;
    private bool isAtkPause = false;
    private bool isAtkDelayTime = false;
    private bool isAshe = false;
    private string champName; 
    private float wardMadeCooldown = 240f;
    private float atkDelayTime = 1f;

    private void Awake()
    {
        if (myChamp == null)
            myChamp = transform.parent.gameObject;

        myChampionData = myChamp.GetComponent<ChampionData>();
        TheAIPath = myChamp.GetComponent<AIPath>();
        TheAIDest = myChamp.GetComponent<AIDestinationSetter>();
        ThePlayerMouse = myChamp.GetComponent<PlayerMouse>();
        aStarTargetObj = ThePlayerMouse.myTarget;
        enemiesList = new List<GameObject>();
        myChampBehav = myChamp.GetComponent<ChampionBehavior>();
        myChampionAnimation = myChamp.GetComponent<ChampionAnimation>();
        champName = PlayerData.Instance.championName;

        if (myChamp.transform.parent.name.Contains("Ashe"))
        {
            asheSkill = myChamp.GetComponent<AsheSkill>();

            if (asheSkill != null)
                isAshe = true;
        }
    }

    void OnLevelWasLoaded(int level)
    {
        if (SceneManager.GetSceneByBuildIndex(level).name.Equals("InGame"))
        {
            sysmsg = GameObject.FindGameObjectWithTag("SystemMsg").GetComponent<SystemMessage>();
            fxpool = GameObject.FindGameObjectWithTag("MouseFxPool").GetComponent<MouseFxPooling>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 공격 반경에 적이 들어온다면 적 리스트에 추가한다. (논 타겟팅 어택 명령을 위한 것)
        if (!other.name.Contains(myChampBehav.team) && other.tag.Equals("Minion"))
            AddEnemiesList(other);
        else if (other.tag.Equals("Tower"))
        {
            TowerBehaviour towerBehav = other.gameObject.GetComponent<TowerBehaviour>();

            if (!myChampBehav.team.Equals(towerBehav.team))
                AddEnemiesList(other);
        }
        else if (other.tag.Equals("Suppressor") || other.tag.Equals("Nexus"))
        {
            SuppressorBehaviour supBehav = other.gameObject.GetComponent<SuppressorBehaviour>();

            if (!myChampBehav.team.Equals(supBehav.team))
                AddEnemiesList(other);
        }
        else if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Monster")))
            AddEnemiesList(other);
        else if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
            AddEnemiesList(other);
    }

    private void OnTriggerExit(Collider other)
    {
        // 적이 공격 반경에서 나간다면 적 리스트에서 제거한다
        if (!other.name.Contains(myChampBehav.team) && other.tag.Equals("Minion"))
            RemoveEnemiesList(other);
        else if (other.tag.Equals("Tower"))
        {
            TowerBehaviour towerBehav = other.gameObject.GetComponent<TowerBehaviour>();

            if (!myChampBehav.team.Equals(towerBehav.team))
                RemoveEnemiesList(other);
        }
        else if (other.tag.Equals("Suppressor") || other.tag.Equals("Nexus"))
        {
            SuppressorBehaviour supBehav = other.gameObject.GetComponent<SuppressorBehaviour>();

            if (!myChampBehav.team.Equals(supBehav.team))
                RemoveEnemiesList(other);
        }
        else if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Monster")))
            RemoveEnemiesList(other);
        else if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
            RemoveEnemiesList(other);
    }

    private void Update()
    {
        //딜레이 타임 확인
        CheckAtkDelayTime();

        //와드 설치 명령이 있다면 와드를 설치한다
        if (isWarding)
            InstallWard();

        //논 타겟팅 어택 명령이 있다면 적 리스트에 있는 이 중 가장 가까운 이를 찾는다.
        if (isWillAtkAround)
        {
            float dist = 1000000, nowDist;
            GameObject tempObj = null;

            for (int i = 0; i < enemiesList.Count; ++i)
            {
                if (enemiesList[i].tag.Equals("Tower"))
                {
                    TowerBehaviour towerBehav = enemiesList[i].GetComponent<TowerBehaviour>();

                    if (!towerBehav.isCanAtkMe)
                        continue;

                    if (towerBehav.team == myChampBehav.team)
                        continue;
                }
                else if (enemiesList[i].tag.Equals("Suppressor") || enemiesList[i].tag.Equals("Nexus"))
                {
                    SuppressorBehaviour supBehav = enemiesList[i].GetComponent<SuppressorBehaviour>();

                    if (!supBehav.isCanAtkMe)
                        continue;

                    if (supBehav.team == myChampBehav.team)
                        continue;
                }
                else if (enemiesList[i].layer.Equals(LayerMask.NameToLayer("Monster")))
                {
                    MonsterBehaviour monBehav = enemiesList[i].GetComponent<MonsterBehaviour>();

                    if (!monBehav.TheFogEntity.isCanTargeting)
                        continue;

                    if (!monBehav.monAtk.isAtking)
                        continue;
                }
                else if (enemiesList[i].layer.Equals(LayerMask.NameToLayer("Champion")))
                {
                    ChampionBehavior champBehav = enemiesList[i].GetComponent<ChampionBehavior>();

                    if (champBehav.team == myChampBehav.team)
                        continue;
                }

                nowDist = (enemiesList[i].transform.position - myChamp.transform.position).sqrMagnitude;

                if (dist > nowDist)
                {
                    dist = nowDist;
                    tempObj = enemiesList[i];
                }

                // 만약 타겟팅할 적을 찾았다면 타겟팅한다
                if (tempObj != null)
                {
                    atkTargetObj = tempObj;
                    isTargetting = true;
                    isWillAtkAround = false;
                    fxpool.GetPool("Force", tempObj.transform.position, tempObj.transform.gameObject);
                }
            }
        }

        // 공격할 적이 있는 경우
        if (atkTargetObj != null)
        {
            if (isTargetting && atkTargetObj.activeInHierarchy)
            {
                if (TheAIDest.target != atkTargetObj.transform)
                    TheAIDest.target = atkTargetObj.transform;

                atkTargetPos = atkTargetObj.transform.position;
                myChampPos = myChamp.transform.position;
                atkTargetPos.y = 0;
                myChampPos.y = 0;
                float atkRevision = 0;

                // 건물의 반지름에 따라 거리 보정을 넣어준다
                if (atkTargetObj.tag.Equals("Tower") && atkRange < 5)
                    atkRevision = 1f;
                else if (atkTargetObj.tag.Equals("Suppressor") && atkRange < 5)
                    atkRevision = 2.5f;
                else if (atkTargetObj.tag.Equals("Nexus") && atkRange < 5)
                    atkRevision = 7f;

                // 공격 반경 밖에 타겟이 있다면 다가간다
                if (Vector3.Distance(atkTargetPos, myChampPos) > atkRange + atkRevision)
                {
                    if (!TheAIPath.canMove)
                    {
                        ToggleMove(true);
                        myChampionAnimation.AttackAnimation(false);
                    }

                    if (atkCoroutine != null)
                    {
                        myChampionAnimation.AttackAnimation(false);
                        StopCoroutine(atkCoroutine);
                        atkCoroutine = null;
                    }
                }
                else
                {// 공격 반경 안에 타겟이 있다면 공격한다
                    if (!isAtkDelayTime)
                    {
                        if (TheAIPath.canMove)
                        {
                            ToggleMove(false);
                            myChampionAnimation.AttackAnimation(true);
                        }

                        if (atkCoroutine == null)
                            atkCoroutine = StartCoroutine(Attack());
                    }
                }
            }
            else
                ResetTarget();
        }
        else
            ResetTarget();

        // 시간이 지난 만큼 와드의 쿨타임을 감소시킨다
        CheckMadeWardCooldown();
    }

    private void LateUpdate()
    {
        // 적 리스트에 있는 적이 사망했다면 리스트에서 제거한다
        for (int i = 0; i < enemiesList.Count;)
        {
            if (!enemiesList[i].activeInHierarchy)
                enemiesList.RemoveAt(i);
            else
                ++i;
        }
    }

    /// <summary>
    /// 공격을 담당하는 코루틴
    /// </summary>
    IEnumerator Attack()
    {
        while (!myChampBehav.isDead)
        {
            if (!isAtkPause)
            {
                bool isCheck = true;

                if (!isTargetting)
                    isCheck = false;
                else if (atkTargetObj == null)
                    isCheck = false;
                else if (atkTargetObj.Equals(aStarTargetObj))
                    isCheck = false;

                //타겟이 있다면 공격
                if (isCheck)
                {
                    myChampBehav.transform.DOLookAt(atkTargetObj.transform.position, 0);

                    //공격하는 대상과 피격당하는 대상을 구분하여 처리
                    if (atkTargetObj.tag.Equals("Minion"))
                    {
                        MakeAsheArrow();

                        MinionBehavior minBehav = atkTargetObj.GetComponent<MinionBehavior>();
                        AudioSource minAudio = minBehav.transform.GetChild(minBehav.transform.childCount - 1).GetComponent<AudioSource>();

                        if (minBehav != null)
                        {
                            int viewID = minBehav.GetComponent<PhotonView>().viewID;
                            myChampBehav.HitRPC(viewID);
                            ChampionSound.instance.IamAttackedSound(minAudio, champName);

                            if (isAshe)
                                asheSkill.QCountUp();

                            minBehav.HitMe(myChampionData.totalStat.AttackDamage, "AD", myChampBehav.gameObject);

                            if (!skillKey.Equals(""))
                                if (--skillKeyNum < 1)
                                    skillKey = "";
                        }
                    }
                    else if (atkTargetObj.layer.Equals(LayerMask.NameToLayer("Champion")))
                    {
                        MakeAsheArrow();

                        ChampionBehavior champBehav = atkTargetObj.GetComponent<ChampionBehavior>();
                        AudioSource champaudio = champBehav.gameObject.GetComponent<AudioSource>();

                        if (champBehav != null)
                        {
                            int viewID = champBehav.GetComponent<PhotonView>().viewID;
                            myChampBehav.HitRPC(viewID);
                            ChampionSound.instance.IamAttackedSound(champaudio, champName);

                            if (isAshe)
                                asheSkill.QCountUp();

                            champBehav.HitMe(myChampionData.totalStat.AttackDamage, "AD", myChampBehav.gameObject, myChampBehav.name);

                            if (!skillKey.Equals(""))
                                if (--skillKeyNum < 1)
                                    skillKey = "";
                        }
                    }
                    else if (atkTargetObj.tag.Equals("Tower"))
                    {
                        MakeAsheArrow();
                        TowerBehaviour towerBehav = atkTargetObj.GetComponent<TowerBehaviour>();
                        AudioSource towerAudio = towerBehav.GetComponent<AudioSource>();

                        if (towerBehav != null)
                        {
                            string key = "";
                            char[] keyChar = towerBehav.gameObject.name.ToCharArray();

                            for (int i = 13; i < 16; ++i)
                                key += keyChar[i];

                            myChampBehav.HitRPC(key);
                            ChampionSound.instance.IamAttackedSound(towerAudio, champName);

                            if (isAshe)
                                asheSkill.QCountUp();

                            // 타워를 파괴 시 팀의 cs, 골드, 경험치를 올린다
                            if (towerBehav.HitMe(myChampionData.totalStat.AttackDamage))
                            {
                                myChampionData.Kill_CS_Gold_Exp(atkTargetObj.name, 2, atkTargetObj.transform.position);
                                ResetTarget();
                            }

                            if (!skillKey.Equals(""))
                                if (--skillKeyNum < 1)
                                    skillKey = "";
                        }
                    }
                    else if (atkTargetObj.tag.Equals("Suppressor"))
                    {
                        MakeAsheArrow();
                        SuppressorBehaviour supBehav = atkTargetObj.GetComponent<SuppressorBehaviour>();

                        if (supBehav != null)
                        {
                            string key = "";
                            char[] keyChar = supBehav.gameObject.name.ToCharArray();

                            for (int i = 11; i < 14; ++i)
                                key += keyChar[i];

                            myChampBehav.HitRPC(key);

                            if (isAshe)
                                asheSkill.QCountUp();

                            supBehav.HitMe(myChampionData.totalStat.AttackDamage);

                            if (!skillKey.Equals(""))
                                if (--skillKeyNum < 1)
                                    skillKey = "";
                        }
                    }
                    else if (atkTargetObj.tag.Equals("Nexus"))
                    {
                        MakeAsheArrow();
                        SuppressorBehaviour supBehav = atkTargetObj.GetComponent<SuppressorBehaviour>();

                        if (supBehav != null)
                        {
                            string key = "";
                            char[] keyChar = supBehav.gameObject.name.ToCharArray();
                            key += keyChar[6];
                            myChampBehav.HitRPC(key);

                            if (isAshe)
                                asheSkill.QCountUp();

                            supBehav.HitMe(myChampionData.totalStat.AttackDamage);

                            if (!skillKey.Equals(""))
                                if (--skillKeyNum < 1)
                                    skillKey = "";
                        }
                    }
                    else if (atkTargetObj.layer.Equals(LayerMask.NameToLayer("Monster")))
                    {
                        MakeAsheArrow();
                        MonsterBehaviour monBehav = atkTargetObj.GetComponent<MonsterBehaviour>();

                        if (monBehav != null)
                        {
                            int viewID = monBehav.GetComponent<PhotonView>().viewID;
                            myChampBehav.HitRPC(viewID);

                            if (isAshe)
                                asheSkill.QCountUp();

                            monBehav.HitMe(myChampionData.totalStat.AttackDamage, "AD", myChamp);

                            if (!skillKey.Equals(""))
                                if (--skillKeyNum < 1)
                                    skillKey = "";
                        }
                    }
                }
            }

            float AtkSpeed = myChampionData.myStat.AttackSpeed * (1 + (myChampionData.totalStat.UP_AttackSpeed * (myChampionData.totalStat.Level - 1) + (myChampionData.totalStat.AttackSpeed - myChampionData.myStat.AttackSpeed)) / 100);
            //어택 딜레이타임을 1초로 설정
            atkDelayTime = 1f / AtkSpeed;

            yield return new WaitForSeconds(atkDelayTime);
        }
    }

    /// <summary>
    /// 와드의 쿨타임을 처리하는 함수
    /// </summary>
    private void CheckMadeWardCooldown()
    {
        // 와드 갯수가 가득차지 않았다면 쿨타임을 깎는다
        if (wardAmount < 2)
        {
            wardMadeCooldown -= Time.deltaTime;

            // 와드의 쿨타임을 다 채웠다면 와드를 하나 추가하고 쿨타임을 초기화한다
            if (wardMadeCooldown <= 0f)
            {
                ++wardAmount;
                wardMadeCooldown = Mathf.Round(wardMadeMaxTime - ((wardMadeTermTime * ((float)(myChampionData.myStat.Level - 1))) / 17f));
            }
        }
    }

    /// <summary>
    /// 애쉬의 기본 공격 시 화살을 생성하는 함수
    /// </summary>
    private void MakeAsheArrow()
    {
        if (isAshe)
        {
            float moveTime = 0.4f;
            myChampBehav.ArrowRPC(atkTargetObj.transform.position, moveTime);
        }
    }

    /// <summary>
    /// 타겟을 초기화시키는 함수
    /// </summary>
    public void ResetTarget()
    {
        if (atkCoroutine != null)
        {
            myChampionAnimation.AttackAnimation(false);
            StopCoroutine(atkCoroutine);
            atkCoroutine = null;
        }

        if (isTargetting)
            isTargetting = false;

        if (atkTargetObj != null)
            atkTargetObj = null;

        if (!isWillAtkAround)
        {
            if (TheAIDest.target != aStarTargetObj.transform)
            {
                TheAIDest.target = aStarTargetObj.transform;
                aStarTargetObj.transform.position = transform.position;
            }
        }
        else
            TheAIDest.target = aStarTargetObj.transform;

        if (!TheAIPath.canMove)
            ToggleMove(true);
    }

    /// <summary>
    /// 리스트에 적을 추가하는 함수
    /// </summary>
    /// <param name="other">적의 콜라이더</param>
    private void AddEnemiesList(Collider other)
    {
        if (!enemiesList.Contains(other.gameObject))
            enemiesList.Add(other.gameObject);
    }

    /// <summary>
    /// 리스트에 있는 적을 제거하는 함수
    /// </summary>
    /// <param name="other">적의 콜라이더</param>
    private void RemoveEnemiesList(Collider other)
    {
        if (enemiesList.Contains(other.gameObject))
        {
            if (other.gameObject.Equals(atkTargetObj))
                atkTargetObj = null;

            enemiesList.Remove(other.gameObject);

            if (enemiesList.Count.Equals(0))
                atkTargetObj = null;
        }
    }

    /// <summary>
    /// 공격 불가를 해제하는 함수
    /// </summary>
    private void OffPauseAtk()
    {
        isAtkPause = false;
        myChampionData.canSkill = true;
    }

    /// <summary>
    /// 이동 가능 유무를 설정하는 함수
    /// </summary>
    /// <param name="isMove">이동 가능한 상태인가</param>
    private void ToggleMove(bool isMove)
    {
        TheAIPath.canMove = isMove;
        TheAIPath.canSearch = isMove;
    }

    /// <summary>
    /// 기절 이펙트를 켜고 끄는 함수
    /// </summary>
    /// <param name="isStun">기절했는가의 유무</param>
    /// <param name="time">몇 초 후에 토글되는가</param>
    public void StunEffectToggle(bool isStun, float time = 0f)
    {
        if (isStun)
            Invoke("_OnStunEffect", time);
        else
            Invoke("_OffStunEffect", time);
    }

    /// <summary>
    /// 기절 이펙트를 켜는 함수
    /// </summary>
    private void _OnStunEffect()
    {
        isStun = true;
        stunParticle.SetActive(true);
    }

    /// <summary>
    /// 기절 이펙트를 끄는 함수
    /// </summary>
    private void _OffStunEffect()
    {
        isStun = false;
        stunParticle.SetActive(false);
    }

    /// <summary>
    /// 공격을 불가능하게 하는 함수
    /// </summary>
    /// <param name="time">정지가 지속될 시간</param>
    /// <param name="isMoveToo">이동도 정지되는가의 여부</param>
    public void PauseAtk(float time, bool isMoveToo = false)
    {
        isAtkPause = true;
        Invoke("OffPauseAtk", time);

        if (isMoveToo)
            PauseMove(time);

        myChampionData.canSkill = false;
        myChampionData.playerSkill.CancelSkill();
    }

    /// <summary>
    /// 이동을 불가능하게 하는 함수
    /// </summary>
    /// <param name="time">정지가 지속될 시간</param>
    private void PauseMove(float f)
    {
        if (TheAIPath == null)
            TheAIPath = myChamp.GetComponent<AIPath>();

        TheAIPath.isStopped = true;
        Invoke("OnMove", f);
    }

    /// <summary>
    /// 이동 불가를 해제하는 함수
    /// </summary>
    private void OnMove()
    {
        if (TheAIPath != null)
            TheAIPath.isStopped = false;
    }

    /// <summary>
    /// 밀침을 당했을 때 처리하는 함수
    /// </summary>
    /// <param name="finishVec">최대로 밀릴 위치</param>
    /// <param name="time">밀쳐지는 동안 공격이 정지될 지속 시간</param>
    public void PushMe(Vector3 finishVec, float time = 0.1f)
    {
        PauseAtk(time, true);
        isPushing = true;
        finishVec.y = 0;

        pushTween = myChamp.transform.DOMove(finishVec, time).OnUpdate(() =>
        {
            if (myChampBehav.isDead)
                if (pushTween != null)
                    pushTween.Kill();
        }).OnKill(() =>
        {
            isPushing = false;
            pushTween = null;
        });
    }

    /// <summary>
    /// 와드 설치 시 쿨타임과 설치 위치를 잡는 함수
    /// </summary>
    /// <param name="pos">와드를 설치할 위치</param>
    public void WantBuildWard(Vector3 pos)
    {
        if (wardAmount > 0)
        {
            wardMadeCooldown = Mathf.Round(wardMadeMaxTime - ((wardMadeTermTime * ((float)(myChampionData.myStat.Level - 1))) / 17f));
            isWarding = true;
            aStarTargetObj.transform.position = pos;
        }
    }

    /// <summary>
    /// 밀쳐진 후 벽에 부딪혔을 때 밀침이 중지되도록 하는 함수
    /// </summary>
    public void PushWall()
    {
        if (pushTween != null)
            pushTween.Kill();
    }

    /// <summary>
    /// 챔피언을 죽였을 때의 처리를 위한 함수
    /// </summary>
    public void IKillChamp()
    {
        if (atkTargetObj != null)
        {
            ChampionBehavior behav = atkTargetObj.GetComponent<ChampionBehavior>();
            ResetTarget();
        }
    }

    /// <summary>
    /// 정지 명령을 내렸을 때의 처리를 위한 함수
    /// </summary>
    public void Stop()
    {
        isWillAtkAround = false;
        isTargetting = false;

        if (atkCoroutine != null)
        {
            myChampionAnimation.AttackAnimation(false);
            StopCoroutine(atkCoroutine);
        }

        atkTargetObj = null;
        isWarding = false;
        myChampionData.playerSkill.TheSplatManager.Cancel();
        myChampionData.playerSkill.CancelSkill();
        StopOnlyMove();
    }

    /// <summary>
    /// 이동만 멈추는 함수
    /// </summary>
    private void StopOnlyMove()
    {
        ToggleMove(false);
        aStarTargetObj.transform.position = myChamp.transform.position;
    }

    /// <summary>
    /// 공격이 연속적으로 들어가지 않도록 딜레이 타임을 계산해주는 함수
    /// </summary>
    private void CheckAtkDelayTime()
    {
        if (atkDelayTime > 0)
        {
            atkDelayTime -= Time.deltaTime;

            if (!isAtkDelayTime)
                isAtkDelayTime = true;
        }
        else if (isAtkDelayTime)
            isAtkDelayTime = false;
    }

    /// <summary>
    /// 와드를 설치하는 함수
    /// </summary>
    private void InstallWard()
    {
        if (isWillAtkAround)
            isWillAtkAround = false;

        if (Vector3.Distance(transform.position, aStarTargetObj.transform.position) < 15f)
        {
            isWarding = false;
            --wardAmount;
            myChampBehav.WardRPC(myChampBehav.team, myChampionData.myStat.Level, aStarTargetObj.transform.position);
            aStarTargetObj.transform.position = transform.position;
            ResetTarget();
        }
    }
}