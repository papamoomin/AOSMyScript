using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타워의 공격을 담당하는 스크립트
/// </summary>
public class TowerAtk : Photon.PunBehaviour
{
    public Transform shootPointTransform;
    public GameObject nowTarget = null;
    public GameObject crystal;
    public GameObject projectileObj;
    public GameObject magicMissilePoolObj;
    public GameObject myTower = null;
    public GameObject towerProjectileHitPrefab;
    public string enemyColor = "Blue";
    public float fireInterval = 1.5f;

    private Coroutine AtkCoroutine = null;
    private TowerBehaviour myTowerBehav = null;
    private SystemMessage sysMsg;
    private Transform targetTransform;
    private Transform CurTargetForSound = null;
    private Queue<GameObject> projectilePool;
    private List<GameObject> enemiesList;
    private GameObject towerProjectileHitObj;
    private Vector3 targetDir;
    private bool isOnce = false;
    private bool isAfterDelaying = false;
    private float rotationDegreePerSecond = 4;
    private float currentRotation = 0;

    private void Awake()
    {
        enemiesList = new List<GameObject>();
        projectilePool = new Queue<GameObject>();
        shootPointTransform = transform.GetChild(0).transform;
        PoolingProjectile();
        myTowerBehav = myTower.GetComponent<TowerBehaviour>();
        sysMsg = GameObject.FindGameObjectWithTag("SystemMsg").GetComponent<SystemMessage>();
        towerProjectileHitObj = Instantiate(towerProjectileHitPrefab, transform.position, Quaternion.identity, transform);
        towerProjectileHitObj.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 자신의 영역에 적이 들어오면 타겟 리스트에 넣어둔다.
        if (other.name.Contains(enemyColor) && other.tag.Equals("Minion"))
            AddEnemiesList(other);
        else if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
        {
            if (other.GetComponent<ChampionBehavior>().team.Equals(enemyColor))
                AddEnemiesList(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 자신의 영역에서 적이 나가면 타겟 리스트에서 제거한다.
        if (other.name.Contains(enemyColor) && other.tag.Equals("Minion"))
            RemoveEnemiesList(other);
        else if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
        {
            if (other.GetComponent<ChampionBehavior>().team.Equals(enemyColor))
                RemoveEnemiesList(other);
        }
    }

    private void Update()
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        //타겟이 없는데 적 리스트에 적이 있다면 타겟을 설정한다
        if (nowTarget == null)
        {
            if (enemiesList.Count > 0)
                SetAtkPriority();
        }
        else if (!nowTarget.activeInHierarchy)
        {//타겟이 죽었다면 적 리스트에서 타겟을 제거한다
            enemiesList.Remove(nowTarget);
            nowTarget = null;
            targetTransform = null;
            targetDir = Vector3.forward;
        }

        //타겟이 있다면 거리와 부쉬에 숨었는지 유무를 판단한다
        if (nowTarget != null)
        {
            targetTransform = nowTarget.transform;
            targetDir = Vector3.Normalize(targetTransform.transform.position - myTower.transform.position);
            targetDir.y = 0;

            if (nowTarget.layer.Equals(LayerMask.NameToLayer("Champion")))
            {
                if (CurTargetForSound != targetTransform)
                    WarningSound();

                FogOfWarEntity fogEntity = nowTarget.GetComponent<FogOfWarEntity>();

                if (fogEntity.isInTheBush)
                    if (!fogEntity.isInTheBushMyEnemyToo)
                    {
                        nowTarget = null;
                        targetTransform = null;
                        targetDir = Vector3.forward;
                    }
            }
        }

        currentRotation = Mathf.MoveTowardsAngle(currentRotation, Mathf.Atan2(targetDir.x, targetDir.z) / Mathf.PI * 180, rotationDegreePerSecond);
        transform.localRotation = Quaternion.AngleAxis(currentRotation, Vector3.forward);

        //타겟이 있다면 공격하고, 없다면 공격을 해제한다
        if (nowTarget == null)
        {
            if (AtkCoroutine != null)
            {
                StopCoroutine(AtkCoroutine);
                AtkCoroutine = null;
            }
        }
        else if (AtkCoroutine == null)
            AtkCoroutine = StartCoroutine(Attack());
    }

    /// <summary>
    /// 공격 우선순위를 계산, 설정하는 함수
    /// </summary>
    private void SetAtkPriority()
    {
        int priority = -1, nowPriority = -1; // 0 = champ, 1 = magician, 2 = melee, 3 = siege or super
        float dist = 1000000, nowDist;
        GameObject tempObj = null;
        Stack<int> removeNumStack = new Stack<int>();

        //적 리스트에서 제거해야 할 대상을 분류한다
        for (int i = 0; i < enemiesList.Count; ++i)
        {
            bool isPush = false;

            if (enemiesList[i] == null)
                isPush = true;
            else if (!enemiesList[i].activeInHierarchy)
                isPush = true;
            else if (enemiesList[i].transform.position.y < -50f)
                isPush = true;

            if (isPush)
                removeNumStack.Push(i);
        }

        //위에서 분류한 대상을 적 리스트에서 제거한다
        for (; removeNumStack.Count > 0; enemiesList.RemoveAt(removeNumStack.Pop())) ;

        for (int i = 0; i < enemiesList.Count; ++i)
        {
            if (enemiesList[i].tag.Equals("Minion"))
            {
                if (enemiesList[i].name.Contains("Siege") || enemiesList[i].name.Contains("Super"))
                    nowPriority = 3;
                else if (enemiesList[i].name.Contains("Melee"))
                    nowPriority = 2;
                else if (enemiesList[i].name.Contains("Magician"))
                    nowPriority = 1;
            }
            else if (enemiesList[i].layer.Equals(LayerMask.NameToLayer("Champion")))
            {
                FogOfWarEntity fogEntity = enemiesList[i].GetComponent<FogOfWarEntity>();

                if (!fogEntity.isInTheBush)
                    nowPriority = 0;
                else if (fogEntity.isInTheBushMyEnemyToo)
                    nowPriority = 0;
                else
                    nowPriority = -1;
            }
            else
                nowPriority = -1;

            if (nowPriority >= priority && nowPriority > -1)
            {
                priority = nowPriority;
                nowDist = (enemiesList[i].transform.position - myTower.transform.position).sqrMagnitude;

                if (dist > nowDist)
                {
                    dist = nowDist;
                    tempObj = enemiesList[i];
                }
            }
        }

        if (tempObj != null)
            nowTarget = tempObj;
    }

    /// <summary>
    /// 공격을 담당하는 코루틴
    /// </summary>
    IEnumerator Attack()
    {
        while (true)
        {
            if (!isAfterDelaying)
            {
                float moveTime = 0.5f;
                ProjectileRPC(targetTransform.GetComponent<PhotonView>().viewID, moveTime);
                isAfterDelaying = true;
                Invoke("AfterDelayFinish", fireInterval);
                StartCoroutine(ProjectileAtk(moveTime, nowTarget));

                yield return new WaitForSeconds(fireInterval);
            }
            else
                yield return null;
        }
    }

    /// <summary>
    /// 투사체 공격을 처리하는 함수
    /// </summary>
    IEnumerator ProjectileAtk(float moveTime, GameObject myTarget)
    {
        yield return new WaitForSeconds(moveTime);

        //피격당하는 대상을 구분하여 처리
        if (myTarget != null)
        {
            if (myTarget.tag.Equals("Minion"))
            {
                MinionBehavior minBehav;
                minBehav = myTarget.GetComponent<MinionBehavior>();

                if (minBehav != null)
                {
                    int viewID = minBehav.GetComponent<PhotonView>().viewID;
                    HitRPC(viewID, myTowerBehav.attackDamage);
                    SoundManager.Instance.PlaySound(SoundManager.Instance.Tower_Attacked);

                    if (minBehav.HitMe(myTowerBehav.attackDamage))
                    {
                        myTowerBehav.towerAudio.PlayOneShot(SoundManager.Instance.Tower_Attacked);
                        enemiesList.Remove(nowTarget);
                        nowTarget = null;
                    }
                }
            }
            else if (myTarget.layer.Equals(LayerMask.NameToLayer("Champion")))
            {
                ChampionBehavior champBehav;
                champBehav = myTarget.GetComponent<ChampionBehavior>();

                if (champBehav != null)
                {
                    ChampionSound.instance.PlayPlayerFx(SoundManager.Instance.Tower_Attacked);
                    int viewID = champBehav.GetComponent<PhotonView>().viewID;
                    HitRPC(viewID, myTowerBehav.attackDamage);

                    if (champBehav.HitMe(myTowerBehav.attackDamage, "AD", myTower, myTower.name))
                    {
                        enemiesList.Remove(nowTarget);
                        nowTarget = null;

                        //시스템메세지 출력
                        sysMsg.sendKillmsg("tower", champBehav.GetComponent<ChampionData>().championName, "ex");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 투사체를 풀링하는 함수
    /// </summary>
    /// <param name="amount">풀링할 갯수</param>
    private void PoolingProjectile(int amount = 10)
    {
        for (int i = 0; i < amount; ++i)
        {
            GameObject obj = Instantiate(projectileObj, shootPointTransform.position, Quaternion.identity, magicMissilePoolObj.transform);
            obj.SetActive(false);
            projectilePool.Enqueue(obj);
        }
    }

    /// <summary>
    /// TriggerEnter한 콜라이더를 적 리스트에 추가하는 함수
    /// </summary>
    /// <param name="trigCol">TriggerEnter한 콜라이더</param>
    public void AddEnemiesList(Collider trigCol)
    {
        if (!enemiesList.Contains(trigCol.gameObject))
            enemiesList.Add(trigCol.gameObject);
    }

    /// <summary>
    /// 리스트에 있는 적이 TriggerExit했을 때 리스트에서 제거
    /// </summary>
    /// <param name="trigCol">TriggerExit한 콜라이더</param>
    private void RemoveEnemiesList(Collider trigCol)
    {
        if (enemiesList.Contains(trigCol.gameObject))
        {
            if (trigCol.gameObject.Equals(nowTarget))
                nowTarget = null;

            enemiesList.Remove(trigCol.gameObject);

            if (enemiesList.Count.Equals(0))
                nowTarget = null;
        }
    }

    /// <summary>
    /// 풀링된 투사체를 꺼내는 함수
    /// </summary>
    /// <param name="targetViewID">타겟의 viewID</param>
    /// <param name="moveTime">이동에 걸리는 시간</param>
    [PunRPC]
    public void ProjectileCreate(int targetViewID, float moveTime)
    {
        if (this != null)
        {
            GameObject obj = projectilePool.Dequeue();
            projectilePool.Enqueue(obj);
            obj.SetActive(true);
            obj.GetComponent<TowerProjectile>().target = PhotonView.Find(targetViewID).transform;
            obj.GetComponent<TowerProjectile>().ActiveFalse(moveTime);
            Vector3 targetPos = targetTransform.position;
            StartCoroutine(EnableHitEffect(targetPos, moveTime));
            myTowerBehav.towerAudio.PlayOneShot(SoundManager.Instance.Tower_Attack);
            Invoke("TowerAttackedSound", moveTime);
        }
    }

    /// <summary>
    /// 타겟이 피격 되었을 때 이펙트를 켜주는 코루틴
    /// </summary>
    /// <param name="pos">피격 위치</param>
    /// <param name="time">이펙트가 나올 때 까지 걸리는 시간</param>
    /// <returns></returns>
    IEnumerator EnableHitEffect(Vector3 pos, float time)
    {
        yield return new WaitForSeconds(time);

        Vector3 tempVec = pos;
        pos.y = 0.5f;
        towerProjectileHitObj.transform.position = tempVec;
        towerProjectileHitObj.SetActive(true);

    }

    /// <summary>
    /// 타워 공격시 음향을 출력하는 함수
    /// </summary>
    private void TowerAttackedSound()
    {
        myTowerBehav.towerAudio.PlayOneShot(SoundManager.Instance.Tower_Attacked);
    }

    /// <summary>
    /// 다른 클라이언트에도 투사체를 생성하기 위해 RPC를 돌리는 함수
    /// </summary>
    /// <param name="targetViewID">타겟의 viewID</param>
    /// <param name="moveTime">이동에 걸리는 시간</param>
    public void ProjectileRPC(int targetViewID, float moveTime)
    {
        ProjectileCreate(targetViewID, moveTime);
        this.photonView.RPC("ProjectileCreate", PhotonTargets.Others, targetViewID, moveTime);
    }

    /// <summary>
    /// RPC로 피격을 동기화시키는 함수
    /// </summary>
    /// <param name="viewID">피격당한 오브젝트의 viewID</param>
    /// <param name="attackDamage">공격자의 데미지</param>
    [PunRPC]
    public void HitSync(int viewID, float attackDamage)
    {
        GameObject HitObj = PhotonView.Find(viewID).gameObject;
        if (HitObj != null)
        {
            if (HitObj.tag.Equals("Minion"))
                HitObj.GetComponent<MinionBehavior>().HitMe(attackDamage);
            else if (HitObj.layer.Equals(LayerMask.NameToLayer("Champion")))
                HitObj.GetComponent<ChampionBehavior>().HitMe(attackDamage, "AD", myTower, myTower.name);
        }
    }

    /// <summary>
    /// 다른 클라이언트에게 피격을 동기화시키기 위해 RPC를 돌리는 함수
    /// </summary>
    /// <param name="viewID">피격당한 오브젝트의 viewID</param>
    /// <param name="attackDamage">공격자의 데미지</param>
    public void HitRPC(int viewID, float attackDamage)
    {
        this.photonView.RPC("HitSync", PhotonTargets.Others, viewID, attackDamage);
    }

    /// <summary>
    /// 공격이 연속적으로 들어가지 않도록 둔 딜레이가 끝났을 때 풀어주는 함수
    /// </summary>
    public void AfterDelayFinish()
    {
        isAfterDelaying = false;
    }

    /// <summary>
    /// 경고음을 내보내는 함수
    /// </summary>
    private void WarningSound()
    {
        CurTargetForSound = targetTransform;

        if (!isOnce)
        {
            isOnce = true;
            CurTargetForSound.gameObject.GetComponent<AudioSource>();
            ChampionSound.instance.PlayOtherFx(CurTargetForSound.gameObject.GetComponent<AudioSource>(), SoundManager.Instance.Tower_Warnig);
            Invoke("Reset", 1.5f);
        }
    }

    /// <summary>
    /// 경고음 재생이 연속적으로 계속되지 않도록 건 시간 제한이 끝났을 때 호출되는 함수
    /// </summary>
    private void Reset()
    {
        isOnce = false;
    }
}