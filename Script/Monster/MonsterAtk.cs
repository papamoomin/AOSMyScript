using DG.Tweening;
using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터의 공격을 담당하는 스크립트
/// </summary>
public class MonsterAtk : MonoBehaviour
{
    public AIPath TheAIPath;
    public List<GameObject> enemiesList = new List<GameObject>();
    public GameObject nowTarget = null;
    public float atkRange = 5;
    public float atkTriggerRange = 15;
    public bool isAtking = false;
    public bool isReturn = false;
    public bool isPushing = false;

    private Animator anim;
    private AIDestinationSetter TheAIDest;
    private MonsterBehaviour myBehav;
    private SystemMessage sysMsg;
    private Tweener pushTween = null;
    private Coroutine atkCoroutine = null;
    private GameObject myMonster = null;
    private GameObject centerTarget = null;
    private float atkDelayTime = 1f;
    private bool isPauseAtk = false;
    private bool isAtkDelayTime = false;

    private void Awake()
    {
        sysMsg = GameObject.FindGameObjectWithTag("SystemMsg").GetComponent<SystemMessage>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 자신의 영역에 챔피언이 들어오면 타겟 리스트에 넣어둔다.
        if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
            if (!enemiesList.Contains(other.gameObject))
                enemiesList.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        // 자신의 영역에서 챔피언이 나가면 타겟 리스트에서 제거한다.
        if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
        {
            if (enemiesList.Contains(other.gameObject))
            {
                // 만약 현재 자신의 공격 목표였을 경우 타겟을 초기화한다.
                if (other.gameObject.Equals(nowTarget))
                    nowTarget = null;

                enemiesList.Remove(other.gameObject);

                // 자신 주변에 타겟이 없다면 기존 위치로 돌아간다.
                if (enemiesList.Count < 1)
                    isReturn = true;
            }
        }
    }

    private void Update()
    {
        // 돌아가는 중에는 체력을 회복한다.
        if (isReturn)
            ReturnHealing();

        if (!PhotonNetwork.isMasterClient)
            return;

        // 딜레이 타임 확인
        CheckAtkDelayTime();

        // 현재 공격 상태일 때의 처리
        if (isAtking)
        {
            bool check = false;

            //주변에 적이 없으면 타겟을 제거하고 공격을 끈다.
            if (enemiesList.Count < 1)
            {
                isAtking = false;
                nowTarget = null;
            }
            else if (nowTarget == null)
                check = true;

            if (!isReturn)
            {
                //공격 상태인데 타겟이 없다면 타겟을 다시 찾는다.
                if (check)
                {
                    float dist = 1000000, nowD;

                    for (int i = 0; i < enemiesList.Count; ++i)
                    {
                        nowD = (enemiesList[i].transform.position - myMonster.transform.position).sqrMagnitude;

                        if (dist > nowD)
                        {
                            dist = nowD;
                            nowTarget = enemiesList[i];
                        }

                        TheAIDest.target = nowTarget.transform;
                    }

                    if (atkCoroutine != null)
                        StopCoroutine(atkCoroutine);

                    atkCoroutine = null;
                    anim.SetBool("attack", false);
                }

                //그래도 타겟이 없다면 공격 상황이 아닌 셈이니 공격을 종료한다.
                if (nowTarget == null)
                {
                    if (atkCoroutine != null)
                    {
                        StopCoroutine(atkCoroutine);
                        atkCoroutine = null;
                        anim.SetBool("attack", false);
                    }

                    if (enemiesList.Count < 1)
                    {
                        nowTarget = null;
                        TheAIDest.target = centerTarget.transform;
                        return;
                    }
                }

                float distance = Vector3.Distance(nowTarget.transform.position, myMonster.transform.position);

                //공격범위 밖에 적이 있으면 이동을 켜준다.
                if (distance > atkRange)
                {
                    OnAndOffMove(true);
                    anim.SetBool("walking", true);

                    if (atkCoroutine != null)
                    {
                        StopCoroutine(atkCoroutine);
                        atkCoroutine = null;
                        anim.SetBool("attack", false);
                    }
                }
                else
                {//공격범위 안에 적이 있다면 공격한다.
                    if (!isAtkDelayTime)
                    {
                        OnAndOffMove(false);
                        anim.SetBool("walking", false);

                        if (atkCoroutine == null)
                            atkCoroutine = StartCoroutine("Attack");
                    }
                }
            }
            else// 공격이 꺼짐. 제자리가 아니라면 제자리로 돌려보내야 함.
                Return();
        }
        else if (isReturn)
            Return();

        // 제자리로 돌아가면 기본 상태로 돌려놓는다.
        if (isReturn == false && isAtking == false)
        {
            anim.SetBool("walking", false);
            anim.SetBool("attack", false);
        }
    }

    /// <summary>
    /// 리턴시 체력을 채워주는 함수
    /// </summary>
    private void ReturnHealing()
    {
        if (myBehav.stat.Hp < myBehav.stat.MaxHp)
            myBehav.stat.Hp += 250 * Time.deltaTime;

        if (myBehav.stat.Hp > myBehav.stat.MaxHp)
            myBehav.stat.Hp = myBehav.stat.MaxHp;
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
    /// 몬스터가 리스폰할 때 기본 값을 설정해주는 함수
    /// </summary>
    public void InitValue()
    {
        if (TheAIPath == null)
            TheAIPath = myMonster.GetComponent<AIPath>();

        isReturn = false;
        isAtking = false;
        isPauseAtk = false;
        TheAIDest.target = centerTarget.transform;
        atkDelayTime = 1;
        isAtkDelayTime = false;
        enemiesList.Clear();
    }

    /// <summary>
    /// 이동을 켜고 끄는 함수
    /// </summary>
    /// <param name="isMove">이동이 켜져야하면 true, 아니면 false</param>
    private void OnAndOffMove(bool isMove)
    {
        if (TheAIPath.canMove == !isMove)
        {
            TheAIPath.canMove = isMove;
            TheAIPath.canSearch = isMove;
        }
    }

    /// <summary>
    /// 뒤늦게 할당해야하는 초기화 값들을 설정해주는 함수
    /// </summary>
    public void LateInit()
    {
        myMonster = transform.parent.gameObject;
        centerTarget = myMonster.transform.parent.gameObject;
        TheAIPath = myMonster.GetComponent<AIPath>();
        TheAIDest = myMonster.GetComponent<AIDestinationSetter>();
        TheAIDest.target = centerTarget.transform;
        anim = myMonster.GetComponent<Animator>();
        anim.SetBool("walking", false);
        anim.SetBool("attack", false);
        myBehav = myMonster.GetComponent<MonsterBehaviour>();
    }

    /// <summary>
    /// 리턴 중인 경우의 처리를 해주는 함수
    /// </summary>
    private void Return()
    {
        // 이동을 켠다.
        OnAndOffMove(true);

        // 리턴해야하는데 타겟을 가지고 있는 경우 초기화
        if (nowTarget != null)
            nowTarget = null;

        TheAIDest.target = centerTarget.transform;

        // 공격 명령이 남아있는 경우 제거
        if (atkCoroutine != null)
        {
            StopCoroutine(atkCoroutine);
            atkCoroutine = null;
        }

        anim.SetBool("attack", false);

        //체력을 회복시킴
        if (myBehav.stat.Hp < myBehav.stat.MaxHp)
            myBehav.stat.Hp += 250 * Time.deltaTime;

        if (myBehav.stat.Hp > myBehav.stat.MaxHp)
            myBehav.stat.Hp = myBehav.stat.MaxHp;

        //체력이 가득차고 제자리로 돌아갔다면 공격과 리턴을 꺼줌.
        if (myBehav.stat.Hp == myBehav.stat.MaxHp)
        {
            if (Vector3.Distance(myMonster.transform.position, centerTarget.transform.position) < 0.5f)
            {
                myBehav.myCenter.SetPosition();
                isAtking = false;
                isReturn = false;
                myBehav.ReturnOtherClients(isReturn);
            }
        }
    }

    /// <summary>
    /// 리턴을 시작하는 함수
    /// </summary>
    public void StartReturn()
    {
        isReturn = true;

        // 공격 애니메이션을 끄고 걷기로 돌린다
        anim.SetBool("walking", true);
        anim.SetBool("attack", false);

        // 다른 클라이언트에도 리턴을 하라고 알린다
        if (myBehav != null)
        {
            if (myBehav.enabled)
                myBehav.ReturnOtherClients(isReturn);
        }

        // 이동이 가능하게 한다.
        if (TheAIPath != null)
            if (TheAIPath.enabled)
                OnAndOffMove(true);
    }

    /// <summary>
    /// 공격을 담당하는 코루틴
    /// </summary>
    IEnumerator Attack()
    {
        while (true)
        {
            if (!isPauseAtk)
            {
                bool isCheck = true;

                if (nowTarget == null)
                    isCheck = false;
                else if (!nowTarget.activeInHierarchy)
                    isCheck = false;

                //타겟이 있다면 공격
                if (isCheck)
                {
                    anim.SetBool("walking", false);
                    anim.SetBool("attack", true);
                    myMonster.transform.DOLookAt(nowTarget.transform.position, 1);

                    //몬스터는 챔피언 외에 상대할 적이 없으므로 챔피언만 처리
                    if (nowTarget.layer.Equals(LayerMask.NameToLayer("Champion")))
                    {
                        ChampionBehavior champBehav = nowTarget.GetComponent<ChampionBehavior>();

                        if (champBehav != null)
                        {
                            int viewID = champBehav.GetComponent<PhotonView>().viewID;
                            myBehav.HitRPC(viewID);
                            champBehav.HitMe(myBehav.stat.AttackDamage, "AD", myMonster, myMonster.name);
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
    /// 공격 불가를 해제하는 함수
    /// </summary>
    private void OffPauseAtk()
    {
        isPauseAtk = false;
    }

    /// <summary>
    /// 공격을 불가능하게 하는 함수
    /// </summary>
    /// <param name="time">정지가 지속될 시간</param>
    /// <param name="isMoveToo">이동도 정지되는가의 여부</param>
    public void PauseAtk(float time, bool isMoveToo = false)
    {
        isPauseAtk = true;
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
            TheAIPath = myMonster.GetComponent<AIPath>();

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
        pushTween = myMonster.transform.DOMove(finishVec, time).OnUpdate(() =>
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
    /// 밀쳐진 후 벽에 부딪혔을 때 밀침이 중지되도록 하는 함수
    /// </summary>
    public void PushWall()
    {
        if (pushTween != null)
            pushTween.Kill();
    }
}