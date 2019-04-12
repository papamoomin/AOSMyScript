using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 미니언의 공격을 담당하는 스크립트
/// </summary>
public class MinionAtk : MonoBehaviour
{
    /* 타겟팅 우선순위
    * 1. 아챔 때린 적챔
    * 2. 아챔 때린 적미니언
    * 3. 가까운 적미니언
    * 4. 가까운 적포탑
    * 5. 가까운 적챔피언
    */
    public AIPath TheAIPath;
    public AIDestinationSetter TheAIDest;
    public List<GameObject> enemiesList = new List<GameObject>();
    public GameObject moveTarget = null;
    public GameObject nowTarget = null;
    public GameObject myMinion = null;
    public float atkRange = 10; // Melee 2.5 / Archer 10 / Siege 10
    public bool isPushing = false;

    private InGameManager TheInGameManager;
    private MinionBehavior myBehav;
    private SystemMessage sysmsg;
    private Tweener pushTween = null;
    private Animator anim;
    private Coroutine atkCoroutine;
    private Vector3 nowTargetPos, myMinionPos;
    private float atkDelayTime = 1f;
    private int targetPriority = 6; // default = 6
    private bool isAtkDelayTime = false;
    private bool isAtkPause = false;
    private string enemyColor;

    //값 초기화
    private void Awake()
    {
        TheAIPath = myMinion.GetComponent<AIPath>();
        TheAIDest = myMinion.GetComponent<AIDestinationSetter>();
        anim = myMinion.GetComponent<Animator>();
        sysmsg = GameObject.FindGameObjectWithTag("SystemMsg").GetComponent<SystemMessage>();
    }

    //걷는 애니메이션 활성화
    private void OnEnable()
    {
        anim.SetBool("walking", true);
    }

    //값 초기화
    private void Start()
    {
        if (myMinion.name.Contains("Blue"))
            enemyColor = "Red";
        else
            enemyColor = "Blue";

        myBehav = myMinion.GetComponent<MinionBehavior>();
        TheInGameManager = GameObject.FindGameObjectWithTag("InGameManager").GetComponent<InGameManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        //TriggerEnter한 콜라이더의 공격 우선순위 계산
        int tempPriority = SetPriority(other);

        //TriggerEnter한 콜라이더의 공격 우선순위가 높은 경우 타겟 재설정
        if (tempPriority < targetPriority)
            TargetSearch();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        //TriggerExit한 콜라이더가 리스트에 있는지 확인
        bool isInTheList = CheckEnemyInTheList(other);

        //TriggerExit한 콜라이더가 리스트에 있다면 리스트에서 삭제
        if (isInTheList)
            RemoveEnemiesList(other);
    }

    private void Update()
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        //딜레이 타임 확인
        CheckAtkDelayTime();

        //적이 리스트에 있는지 확인
        if (enemiesList.Count > 0)
        {
            //적이 있다면 공격
            CheckHasTarget();
            WillAtk();
        }
        else if (nowTarget != null)
        {//주변에 적이 없는데 내가 타겟을 가지고 있음
            //거기다 타겟이 웨이포인트가 아님
            if (nowTarget.tag != "WayPoint")
            {
                //값을 초기화한 후
                StopAttackAndInitState();

                //타겟 재설정
                nowTarget = moveTarget;
                TheAIDest.target = nowTarget.transform;
            }
            else //타겟이 웨이포인트인 경우 값을 설정함
                StopAttackAndInitState();

        }
        else
        {//그 외의 경우에도 값 초기화
            if (moveTarget != null)
            {
                nowTarget = moveTarget;

                if (TheAIDest == null)
                    TheAIDest = myMinion.GetComponent<AIDestinationSetter>();

                TheAIDest.target = nowTarget.transform;

                if (TheAIPath == null)
                    TheAIPath = myMinion.GetComponent<AIPath>();

                if (!TheAIPath.canMove)
                {
                    TheAIPath.canMove = true;
                    TheAIPath.canSearch = true;
                }
            }
        }
    }

    /// <summary>
    /// 공격을 멈추고 이동을 켜줌.
    /// </summary>
    private void StopAttackAndInitState()
    {
        if (TheAIPath == null)
            TheAIPath = myMinion.GetComponent<AIPath>();

        //이동을 켜줌
        if (!TheAIPath.canMove)
        {
            TheAIPath.canMove = true;
            TheAIPath.canSearch = true;
            anim.SetBool("walking", true);

            //공격을 멈춤
            if (atkCoroutine != null)
            {
                StopCoroutine(atkCoroutine);
                atkCoroutine = null;
            }
        }
    }

    /// <summary>
    /// TriggerExit한 콜라이더가 적 개체인지를 판단하는 함수.
    /// </summary>
    /// <param name="trigCol">TriggerExit한 콜라이더</param>
    /// <returns>적인지 여부를 리턴</returns>
    private bool CheckEnemyInTheList(Collider trigCol)
    {
        bool isInTheList = false;

        //적이라면 리스트에 있음을 표시
        if (trigCol.name.Contains(enemyColor) && trigCol.tag.Equals("Minion"))
            isInTheList = true;
        else if (trigCol.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
        {
            if (trigCol.GetComponent<ChampionBehavior>().team.Equals(enemyColor))
                isInTheList = true;
        }
        else if (trigCol.tag.Equals("Tower"))
        {
            if (trigCol.GetComponent<TowerBehaviour>().team.Equals(enemyColor))
                isInTheList = true;
        }
        else if (trigCol.tag.Equals("Suppressor") || trigCol.tag.Equals("Nexus"))
        {
            if (trigCol.GetComponent<SuppressorBehaviour>().team.Equals(enemyColor))
                isInTheList = true;
        }

        return isInTheList;
    }

    /// <summary>
    /// TriggerEnter한 콜라이더가 있을 때 공격 우선순위를 계산하는 함수
    /// </summary>
    /// <param name="trigCol">TriggerEnter한 콜라이더</param>
    /// <returns>계산한 우선순위 값을 리턴</returns>
    private int SetPriority(Collider trigCol)
    {
        int tempPriority = 6;

        if (trigCol.name.Contains(enemyColor) && trigCol.tag.Equals("Minion"))
        {
            AddEnemiesList(trigCol, 3);
            tempPriority = 3;
        }
        else if (trigCol.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
        {
            if (trigCol.GetComponent<ChampionBehavior>().team.Equals(enemyColor))
            {
                AddEnemiesList(trigCol, 5);
                tempPriority = 5;
            }
        }
        else if (trigCol.tag.Equals("Tower"))
        {
            TowerBehaviour TowerBehav = trigCol.GetComponent<TowerBehaviour>();

            if (TowerBehav.isCanAtkMe)
            {
                if (TowerBehav.team.Equals(enemyColor))
                {
                    AddEnemiesList(trigCol, 4);
                    tempPriority = 4;
                }
            }
        }
        else if (trigCol.tag.Equals("Suppressor") || trigCol.tag.Equals("Nexus"))
        {
            SuppressorBehaviour SupBehav = trigCol.GetComponent<SuppressorBehaviour>();

            if (SupBehav.isCanAtkMe)
            {
                if (SupBehav.team.Equals(enemyColor))
                {
                    AddEnemiesList(trigCol, 4);
                    tempPriority = 4;
                }
            }
        }

        return tempPriority;
    }

    /// <summary>
    /// TriggerEnter한 콜라이더가 적인 경우 적 리스트에 추가하는 함수
    /// </summary>
    /// <param name="trigCol">TriggerEnter한 콜라이더</param>
    /// <param name="tempPriority">TrigCol의 공격 우선순위</param>
    private void AddEnemiesList(Collider trigCol, int tempPriority = 6)
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        //적이 리스트에 없다면 추가
        if (!enemiesList.Contains(trigCol.gameObject))
            enemiesList.Add(trigCol.gameObject);

        //방금 추가한 적의 공격 우선순위가 현재 가진 우선순위보다 높다면 타겟으로 설정
        if (tempPriority < targetPriority)
        {
            nowTarget = trigCol.gameObject;
            TheAIDest.target = nowTarget.transform;
        }
    }

    /// <summary>
    /// 리스트에 있는 적이 TriggerExit했을 때 리스트에서 제거
    /// </summary>
    /// <param name="trigCol">TriggerExit한 콜라이더</param>
    private void RemoveEnemiesList(Collider trigCol)
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        //적이 리스트에 있는지 확인
        if (enemiesList.Contains(trigCol.gameObject))
        {
            //리스트에서 제거될 적이 현재의 타겟이라면 타겟을 null로 바꿈
            if (trigCol.gameObject.Equals(nowTarget))
                nowTarget = null;

            //리스트에서 적 제거
            enemiesList.Remove(trigCol.gameObject);

            //리스트에 적이 없다면 공격이 아닌 이동으로 전환
            if (enemiesList.Count.Equals(0))
            {
                nowTarget = moveTarget;
                TheAIDest.target = moveTarget.transform;
            }
        }
    }

    /// <summary>
    /// 타겟이 정글로 들어간 경우 타겟을 초기화해주는 함수
    /// </summary>
    public void RemoveNowTarget()
    {//쫓던 애가 정글 등 쫓으면 안되는 영역으로 들어감
        if (!PhotonNetwork.isMasterClient)
            return;

        //타겟을 리스트에서 제거한다.
        if (nowTarget != null)
            if (enemiesList.Contains(nowTarget))
                enemiesList.Remove(nowTarget);
    }

    /// <summary>
    /// 리스트에 들어온 개체들의 공격 우선순위를 계산해 타겟을 설정하는 함수
    /// </summary>
    private void TargetSearch()
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        nowTarget = moveTarget;
        targetPriority = 6;
        float dist = 1000000, nowD;
        bool isLockOn = false;

        for (int i = 0, tp = 6; i < enemiesList.Count; ++i)
        {
            //우선순위를 계산한다
            if (enemiesList[i].layer.Equals(LayerMask.NameToLayer("Champion")))
            {
                FogOfWarEntity fogEntity = enemiesList[i].GetComponent<FogOfWarEntity>();

                if (!fogEntity.isInTheBush)
                    tp = 5;
                else if (fogEntity.isInTheBushMyEnemyToo)
                    tp = 5;
                else
                    continue;
            }
            else if (enemiesList[i].tag.Equals("Minion"))
                tp = 3;
            else if (enemiesList[i].tag.Equals("Tower"))
            {
                if (enemiesList[i].GetComponent<TowerBehaviour>().isCanAtkMe)
                    tp = 4;
            }
            else if (enemiesList[i].tag.Equals("Suppressor") || enemiesList[i].tag.Equals("Nexus"))
                if (enemiesList[i].GetComponent<SuppressorBehaviour>().isCanAtkMe)
                    tp = 4;

            //현재의 우선순위보다 더 높은 우선순위를 가졌다면 타겟 갱신
            if (targetPriority >= tp)
            {
                isLockOn = true;
                targetPriority = tp;
                nowD = (enemiesList[i].transform.position - myMinion.transform.position).sqrMagnitude;

                if (dist > nowD)
                {
                    dist = nowD;
                    nowTarget = enemiesList[i];
                }

                TheAIDest.target = nowTarget.transform;
            }
        }

        // 타겟이 없는 경우 공격에서 이동으로 전환한다.
        if (!isLockOn)
        {
            TheAIDest.target = nowTarget.transform;

            if (atkCoroutine != null)
            {
                StopCoroutine(atkCoroutine);
                atkCoroutine = null;
            }

            anim.SetBool("walking", true);

            if (TheAIPath != null)
            {
                TheAIPath.canMove = true;
                TheAIPath.canSearch = true;
            }
        }
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
        else
        {
            if (isAtkDelayTime)
                isAtkDelayTime = false;
        }
    }

    /// <summary>
    /// 현재 타겟을 가지고 있는지 확인 후 타겟이 없으면 타겟팅을 하는 함수
    /// </summary>
    private void CheckHasTarget()
    {
        bool isCheck = false;

        //주변에 적이 있는데 타겟이 제대로 잡히지 않았다면 타겟팅
        if (nowTarget == null)
            isCheck = true;
        else if (nowTarget.tag.Equals("WayPoint"))
            isCheck = true;
        else if (!nowTarget.activeInHierarchy)
        {
            isCheck = true;
            enemiesList.Remove(nowTarget);
        }
        else if (nowTarget.layer.Equals(LayerMask.NameToLayer("Champion")))
        {
            FogOfWarEntity fogEntity = nowTarget.GetComponent<FogOfWarEntity>();
            if (fogEntity.isInTheBush)
                if (!fogEntity.isInTheBushMyEnemyToo)
                    isCheck = true;
        }

        //주변에 적 있는데 타겟팅 안한거 확인했으니 TargetSearch 함수 호출
        if (isCheck)
            TargetSearch();
    }

    /// <summary>
    /// 타겟에 따라 거리값을 계산, 보정하고 어택 코루틴을 호출하는 함수
    /// </summary>
    private void WillAtk()
    {
        //현재 타겟이 웨이포인트가 아닌 공격할 대상인 경우
        if (nowTarget.tag != "WayPoint")
        {
            if (TheAIPath == null)
                TheAIPath = myMinion.GetComponent<AIPath>();

            //타겟팅 대상마다 반지름이 다르므로 거리 보정값을 넣어줌
            float atkRevision = 0;

            if (nowTarget.tag.Equals("Tower"))
                atkRevision = 3f;
            else if (nowTarget.tag.Equals("Suppressor"))
                atkRevision = 2.5f;
            else if (nowTarget.tag.Equals("Nexus"))
                atkRevision = 8.5f;

            nowTargetPos = nowTarget.transform.position;
            myMinionPos = myMinion.transform.position;
            nowTargetPos.y = 0;
            myMinionPos.y = 0;
            float distance = Vector3.Distance(nowTargetPos, myMinionPos);

            //공격 범위 밖에 적이 있는 경우 이동
            if (distance > atkRange + atkRevision)
            {
                if (!TheAIPath.canMove)
                {
                    if (atkCoroutine != null)
                    {
                        StopCoroutine(atkCoroutine);
                        atkCoroutine = null;
                    }
                    anim.SetBool("walking", true);
                    TheAIPath.canMove = true;
                    TheAIPath.canSearch = true;
                }
            }
            else
            {//공격 범위 안에 적이 있는 경우 공격
                if (!isAtkDelayTime)
                {
                    if (TheAIPath.canMove)
                    {
                        anim.SetBool("walking", false);
                        TheAIPath.canMove = false;
                        TheAIPath.canSearch = false;
                        atkCoroutine = StartCoroutine(Attack());

                        Vector3 v = nowTarget.transform.position;
                        v.y = 0;
                        myMinion.transform.DOLookAt(v, 1);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 공격을 담당하는 코루틴
    /// </summary>
    IEnumerator Attack()
    {
        while (true)
        {
            if (!isAtkPause)
            {
                bool isCheck = true;

                if (nowTarget == null)
                    isCheck = false;
                else if (!nowTarget.activeInHierarchy)
                    isCheck = false;

                //타겟이 있다면 공격
                if (isCheck)
                {
                    if (anim.GetBool("walking"))
                        anim.SetBool("walking", false);

                    anim.SetTrigger("attack");
                    Vector3 v = nowTarget.transform.position;
                    v.y = 0;
                    myMinion.transform.DOLookAt(v, 1);

                    //공격하는 대상과 피격당하는 대상을 구분하여 처리
                    if (nowTarget.tag.Equals("Minion"))
                    {
                        if (myMinion.name.Contains("Melee"))
                        {
                            MinionBehavior minBehav = nowTarget.GetComponent<MinionBehavior>();

                            if (minBehav != null)
                            {
                                int viewID = minBehav.GetComponent<PhotonView>().viewID;
                                myBehav.HitRPC(viewID);
                                minBehav.HitMe(myBehav.stat.AttackDamage);
                            }
                        }
                        else if (myMinion.name.Contains("Magician"))
                        {
                            float moveTime = 0.4f;
                            myBehav.ArrowRPC(nowTarget.transform.position, moveTime);
                            Invoke("ProjectileAtk", moveTime);
                        }
                        else if (myMinion.name.Contains("Siege"))
                        {
                            float moveTime = 0.4f;
                            myBehav.CannonballRPC(nowTarget.transform.position, moveTime);
                            Invoke("ProjectileAtk", moveTime);
                        }
                    }
                    else if (nowTarget.layer.Equals(LayerMask.NameToLayer("Champion")))
                    {
                        if (myMinion.name.Contains("Melee"))
                        {
                            ChampionBehavior champBehav = nowTarget.GetComponent<ChampionBehavior>();

                            if (champBehav != null)
                            {
                                int viewID = champBehav.GetComponent<PhotonView>().viewID;
                                myBehav.HitRPC(viewID);
                                champBehav.HitMe(myBehav.stat.AttackDamage, "AD", myMinion, myMinion.name);
                            }
                        }
                        else if (myMinion.name.Contains("Magician"))
                        {
                            float moveTime = 0.4f;
                            myBehav.ArrowRPC(nowTarget.transform.position, moveTime);
                            Invoke("ProjectileAtk", moveTime);
                        }
                        else if (myMinion.name.Contains("Siege"))
                        {
                            float moveTime = 0.4f;
                            myBehav.CannonballRPC(nowTarget.transform.position, moveTime);
                            Invoke("ProjectileAtk", moveTime);
                        }
                    }
                    else if (nowTarget.tag.Equals("Tower"))
                    {
                        if (myMinion.name.Contains("Melee"))
                        {
                            TowerBehaviour towerBehav = nowTarget.GetComponent<TowerBehaviour>();

                            if (towerBehav != null)
                            {
                                string key = "";
                                char[] keyChar = towerBehav.gameObject.name.ToCharArray();

                                for (int i = 13; i < 16; ++i)
                                    key += keyChar[i];

                                myBehav.HitRPC(key);

                                if (towerBehav.HitMe(myBehav.stat.AttackDamage))
                                {
                                    if (enemyColor.Equals("Red"))
                                        TheInGameManager.blueTeamPlayer[0].GetComponent<PhotonView>().RPC("GlobalGold", PhotonTargets.All, "blue", 100);
                                    else
                                        TheInGameManager.redTeamPlayer[0].GetComponent<PhotonView>().RPC("GlobalGold", PhotonTargets.All, "red", 100);

                                    enemiesList.Remove(nowTarget);
                                }
                            }
                        }
                        else if (myMinion.name.Contains("Magician"))
                        {
                            float moveTime = 0.4f;
                            myBehav.ArrowRPC(nowTarget.transform.position, moveTime);
                            Invoke("ProjectileAtk", moveTime);
                        }
                        else if (myMinion.name.Contains("Siege"))
                        {
                            float moveTime = 0.4f;
                            myBehav.CannonballRPC(nowTarget.transform.position, moveTime);
                            Invoke("ProjectileAtk", moveTime);
                        }
                    }
                    else if (nowTarget.tag.Equals("Suppressor") || nowTarget.tag.Equals("Nexus"))
                    {
                        if (myMinion.name.Contains("Melee"))
                        {
                            SuppressorBehaviour supBehav = nowTarget.GetComponent<SuppressorBehaviour>();

                            if (supBehav != null)
                            {
                                string key = "";
                                char[] keyChar = supBehav.gameObject.name.ToCharArray();

                                if (nowTarget.tag.Equals("Nexus"))
                                    key += keyChar[6];
                                else
                                    for (int i = 11; i < 14; ++i)
                                        key += keyChar[i];

                                myBehav.HitRPC(key);

                                if (supBehav.HitMe(myBehav.stat.AttackDamage))
                                    enemiesList.Remove(nowTarget);
                            }
                        }
                        else if (myMinion.name.Contains("Magician"))
                        {
                            float moveTime = 0.4f;
                            myBehav.ArrowRPC(nowTarget.transform.position, moveTime);
                            Invoke("ProjectileAtk", moveTime);
                        }
                        else if (myMinion.name.Contains("Siege"))
                        {
                            float moveTime = 0.4f;
                            myBehav.CannonballRPC(nowTarget.transform.position, moveTime);
                            Invoke("ProjectileAtk", moveTime);
                        }
                    }
                }
            }

            //어택 딜레이타임을 1초로 설정
            atkDelayTime = 1f;

            yield return new WaitForSeconds(1);
        }
    }

    /// <summary>
    /// 투사체 공격을 처리하는 함수
    /// </summary>
    private void ProjectileAtk()
    {
        //피격당하는 대상을 구분하여 처리
        if (nowTarget != null)
        {
            if (nowTarget.tag.Equals("Minion"))
            {
                MinionBehavior minBehav;
                minBehav = nowTarget.GetComponent<MinionBehavior>();

                if (minBehav != null)
                {
                    int viewID = minBehav.GetComponent<PhotonView>().viewID;
                    myBehav.HitRPC(viewID);

                    if (minBehav.HitMe(myBehav.stat.AttackDamage))
                        enemiesList.Remove(nowTarget);
                }
            }
            else if (nowTarget.layer.Equals(LayerMask.NameToLayer("Champion")))
            {
                ChampionBehavior champBehav;
                champBehav = nowTarget.GetComponent<ChampionBehavior>();

                if (champBehav != null)
                {
                    int viewID = champBehav.GetComponent<PhotonView>().viewID;
                    myBehav.HitRPC(viewID);

                    if (champBehav.HitMe(myBehav.stat.AttackDamage, "AD", myMinion, myMinion.name))
                    {
                        enemiesList.Remove(nowTarget);
                        sysmsg.sendKillmsg("minion", champBehav.GetComponent<ChampionData>().championName, myBehav.team.ToString());
                    }
                }
            }
            else if (nowTarget.tag.Equals("Tower"))
            {
                TowerBehaviour towerBehav;
                towerBehav = nowTarget.GetComponent<TowerBehaviour>();

                if (towerBehav != null)
                {
                    string key = "";
                    char[] keyChar = towerBehav.gameObject.name.ToCharArray();

                    for (int i = 13; i < 16; ++i)
                        key += keyChar[i];

                    myBehav.HitRPC(key);
                    if (towerBehav.HitMe(myBehav.stat.AttackDamage))
                    {
                        if (enemyColor.Equals("Red"))
                            TheInGameManager.blueTeamPlayer[0].GetComponent<ChampionData>().photonView.RPC("GlobalGold", PhotonTargets.All, "blue", 100);
                        else
                            TheInGameManager.redTeamPlayer[0].GetComponent<ChampionData>().photonView.RPC("GlobalGold", PhotonTargets.All, "red", 100);

                        enemiesList.Remove(nowTarget);
                    }
                }
            }
            else if (nowTarget.tag.Equals("Suppressor") || nowTarget.tag.Equals("Nexus"))
            {
                SuppressorBehaviour supBehav;
                supBehav = nowTarget.GetComponent<SuppressorBehaviour>();

                if (supBehav != null)
                {
                    string key = "";
                    char[] keyChar = supBehav.gameObject.name.ToCharArray();

                    if (nowTarget.tag.Equals("Nexus"))
                        key += keyChar[6];
                    else
                        for (int i = 11; i < 14; ++i)
                            key += keyChar[i];

                    myBehav.HitRPC(key);

                    if (supBehav.HitMe(myBehav.stat.AttackDamage))
                        enemiesList.Remove(nowTarget);
                }
            }
        }
    }

    /// <summary>
    /// 공격 불가를 해제하는 함수
    /// </summary>
    private void OffPauseAtk()
    {
        isAtkPause = false;
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
    }

    /// <summary>
    /// 이동을 불가능하게 하는 함수
    /// </summary>
    /// <param name="time">정지가 지속될 시간</param>
    private void PauseMove(float time)
    {
        if (TheAIPath == null)
            TheAIPath = myMinion.GetComponent<AIPath>();

        TheAIPath.isStopped = true;
        Invoke("OnMove", time);
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
        pushTween = myMinion.transform.DOMove(finishVec, time).OnUpdate(() =>
        {
            if (myBehav.isDead)
                if (pushTween != null)
                    pushTween.Kill();
        }).OnKill(() =>
        {
            isPushing = false;
            pushTween = null;
        });
    }

    /// <summary>
    /// 미니언을 초기화하는 함수
    /// </summary>
    public void InitMinionStatus()
    {
        if (TheAIPath == null)
            TheAIPath = myMinion.GetComponent<AIPath>();

        TheAIPath.canMove = true;
        TheAIPath.canSearch = true;
        isAtkPause = false;
        targetPriority = 6;
    }

    /// <summary>
    /// 밀쳐진 후 벽에 부딪혔을 때 밀침이 중지되도록 하는 함수
    /// </summary>
    public void PushWall()
    {
        if (pushTween != null)
            pushTween.Kill();
    }
}