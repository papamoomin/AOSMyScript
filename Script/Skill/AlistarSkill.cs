using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using DG.Tweening;

/// <summary>
/// 알리스타의 스킬을 담당하는 스크립트
/// </summary>
public class AlistarSkill : Skills
{
    public GameObject qSkillprefab = null;
    public GameObject wSkillprefab = null;
    public GameObject eSkillprefab = null;
    public GameObject rSkillprefab = null;
    
    private SystemMessage sysmsg;
    private GameObject mySkills;
    private Vector3 adjustVec = new Vector3(0, 1f, 0f);
    private Vector3 wArguVec;
    private GameObject wArguObj;
    private string team = "";
    private float rSkillTempVal = 0;

    private void OnLevelWasLoaded(int level)
    {
        if (UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(level).name.Contains("InGame"))
        {
            if (!sysmsg)
                sysmsg = GameObject.FindGameObjectWithTag("SystemMsg").GetComponent<SystemMessage>();

            if (!TheUIStat)
                FindUICanvas();
        }
    }

    private void Awake()
    {
        InitInstance();
        AllPooling();
    }

    private void Update()
    {
        //우클릭하거나 esc키를 누르면 스킬 선택이 해제된다.
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelSkill();
        }

        //돌진 스킬을 선택
        if (skillSelect.Equals(SkillSelect.W))
        {
            //돌진 스킬을 선택한 상황에서 마우스 왼쪽 버튼 클릭
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                int layerMask = LayerMask.NameToLayer("GroundLayer");
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 500, 1 << layerMask))
                {
                    //클릭된 지점의 땅을 검사할 것이다.
                    if (hit.transform.tag.Equals("Terrain"))
                    {
                        bool isEnemyClick = false;
                        float distance = 1000000;
                        Collider enemy = null;
                        Collider[] cols = Physics.OverlapSphere(hit.point, 5f);

                        //오버랩스피어를 이용해 클릭한 지점 주변에 공격할 대상이 있는지 검사한다.
                        foreach (Collider col in cols)
                        {
                            if (col.transform.tag.Equals("Minion"))
                            {
                                if (!col.transform.name.Contains(TheChampionBehaviour.team))
                                    if (col.transform.GetComponent<FogOfWarEntity>().isCanTargeting)
                                        isEnemyClick = true;
                            }
                            else if (col.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
                            {
                                if (!col.transform.GetComponent<ChampionBehavior>().team.Equals(TheChampionBehaviour.team))
                                    if (col.transform.GetComponent<FogOfWarEntity>().isCanTargeting)
                                        isEnemyClick = true;
                            }
                            else if (col.gameObject.layer.Equals(LayerMask.NameToLayer("Monster")))
                                if (col.transform.GetComponent<FogOfWarEntity>().isCanTargeting)
                                    isEnemyClick = true;

                            //적을 설정한다.
                            if (isEnemyClick)
                            {
                                float tempDist = Vector3.SqrMagnitude(hit.point - col.transform.position);

                                if (tempDist < distance)
                                    enemy = col;

                                isEnemyClick = false;
                            }
                        }

                        // 타겟팅한 적을 대상으로 W 스킬을 사용한다.
                        if (enemy != null)
                        {
                            if (Vector3.Distance(transform.position, enemy.transform.position) <= TheSplatManager.Point.Range)
                            {
                                isSkilling = true;
                                TheSplatManager.Cancel();
                                TheAIPath.isStopped = true;
                                wArguObj = enemy.transform.gameObject; 
                                wArguVec = transform.position;
                                CallChampDataUsedSkill("W");
                                transform.DOLookAt(enemy.transform.position, 0.1f);
                                transform.DOMove(enemy.transform.position, 0.1f);
                                Invoke("W", 0.1f);
                                EndSkill(0.1f);
                            }
                        }
                        else
                        {
                            //제대로 적이 선택되지 않은 경우 w 스킬 선택을 해제한다.
                            TheSplatManager.Cancel();
                            skillSelect = SkillSelect.none;
                            isSkilling = false;
                        }
                    }
                }
            }
        }
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
    /// 최초 설정 값을 잡는 함수
    /// </summary>
    public override void InitInstance()
    {
        team = GetComponent<PhotonView>().owner.GetTeam().ToString();
        base.InitInstance();
        TheChampionData.playerSkill = this;
        mySkills = new GameObject("AlistarSkills");
        mySkills.transform.SetParent(skillParticleManager.transform);
        playerAStarTarget = GetComponent<PlayerMouse>().myTarget;
        TheAIPath = GetComponent<AIPath>();
        skillData = TheSkillClass.skillData["Alistar"];
    }

    /// <summary>
    /// 모든 스킬의 프리팹을 풀링해두는 함수
    /// </summary>
    private void AllPooling()
    {
        Pooling(qSkillprefab, "Q");
        Pooling(wSkillprefab, "W");
        Pooling(eSkillprefab, "E");
        Pooling(rSkillprefab, "R");
    }

    /// <summary>
    /// 각 스킬 프리팹을 풀링하는 함수
    /// </summary>
    /// <param name="prefab">풀링할 프리팹</param>
    /// <param name="type">스킬의 단축키("Q", "W", "E", "R")</param>
    /// <param name="amount">풀링할 갯수</param>
    private void Pooling(GameObject prefab, string type, int amount = 10)
    {
        if (!skillObj.ContainsKey(type))
        {
            List<GameObject> list = new List<GameObject>();
            skillObj.Add(type, list);
        }

        List<GameObject> tempList = new List<GameObject>();

        // 프리팹을 생성
        for (int i = 0; i < amount; ++i)
        {
            GameObject obj = Instantiate(prefab, mySkills.transform);
            obj.GetComponent<SkillFactioner>().skillChampFogEntity = TheChampionBehaviour.GetComponent<FogOfWarEntity>();

            if (team == "")
                team = GetComponent<PhotonView>().owner.GetTeam().ToString();

            if (team.Equals("red"))
                obj.GetComponent<FogOfWarEntity>().faction = FogOfWar.Players.Player00;
            else if (team.Equals("blue"))
                obj.GetComponent<FogOfWarEntity>().faction = FogOfWar.Players.Player01;

            //임시 리스트에 프리팹들을 담는다
            obj.SetActive(false);
            tempList.Add(obj);

            switch (type)
            {
                case "Q":
                    obj.GetComponent<AlistarQ>().mySkill = this;
                    break;
                case "E":
                    obj.GetComponent<AlistarE>().mySkill = this;
                    break;
            }
        }

        //기존의 스킬 프리팹 리스트에 임시 리스트의 항목을 합쳐 넣는다.
        skillObj[type].InsertRange(0, tempList);
    }

    /// <summary>
    /// Q 스킬을 호출하는 함수
    /// </summary>
    public override void QCasting()
    {
        isSkilling = true;
        skillSelect = SkillSelect.none;
        TheSplatManager.Cancel();
        HitEffectRPC("Alistar", "Q");
        Q();
        CallChampDataUsedSkill("Q");
        EndSkill(1f);
        championAnimation.AnimationApply("Q", true);
        championAnimation.AnimationApply("Q", false, 0.7f);
    }

    /// <summary>
    /// W 스킬을 호출하는 함수
    /// </summary>
    public override void WCasting()
    {
        if (skillSelect != SkillSelect.W)
        {
            skillSelect = SkillSelect.W;
            TheSplatManager.Point.Select();
            TheSplatManager.Point.Scale = 14f;
            TheSplatManager.Point.Range = 14f;
        }
    }

    /// <summary>
    /// E 스킬을 호출하는 함수
    /// </summary>
    public override void ECasting()
    {
        isSkilling = true;
        skillSelect = SkillSelect.E;
        HitEffectRPC("Alistar", "E", 10, 0.5f);

        // E 스킬의 경우 프리팹이 시간 텀을 두고 여러 차례 반복되어 나타나므로 Invoke에 시간 텀을 두어 돌린다
        for (int i = 0; i < 10; ++i)
            Invoke("E", 0.5f * i);

        EndSkill(5);
        CallChampDataUsedSkill("E");
    }

    /// <summary>
    /// R 스킬을 호출하는 함수
    /// </summary>
    public override void RCasting()
    {
        HitEffectRPC("Alistar", "R", 1, 0.5f);
        Invoke("R", 0.5f);
        CallChampDataUsedSkill("R");

        // 스킬 레벨에 따라 값을 설정한다
        switch (TheChampionData.skill_R)
        {
            case 1: rSkillTempVal = 122; break; //55% -> 45 = 10000/(100+x) -> x = 1100/9 = 122.2222222222
            case 2: rSkillTempVal = 186; break; //65% -> 35 = 10000/(100+x) -> x = 1300/7 = 185.7142857142
            case 3: rSkillTempVal = 300; break; //75% -> 25 = 10000/(100+x) -> x = 300
        }

        TheChampionData.totalStat.AbilityDef += rSkillTempVal;
        TheChampionData.totalStat.AttackDef += rSkillTempVal;

        if (!TheUIStat)
            FindUICanvas();

        TheUIStat.Refresh();
        Invoke("DebuffDefence", 7f);
    }

    /// <summary>
    /// 스킬 프리팹을 담은 풀에서 프리팹을 꺼내온다.
    /// </summary>
    /// <param name="skillKey">꺼내올 스킬의 단축키</param>
    private GameObject GetSkillInThePool(string skillKey)
    {
        GameObject obj = skillObj[skillKey][0];

        if (obj.activeInHierarchy)
        {
            switch (skillKey)
            {
                case "Q": Pooling(qSkillprefab, skillKey, 10); break;
                case "W": Pooling(wSkillprefab, skillKey, 10); break;
                case "E": Pooling(eSkillprefab, skillKey, 10); break;
                case "R": Pooling(rSkillprefab, skillKey, 10); break;
            }

            obj = skillObj[skillKey][0];
        }

        skillObj[skillKey].RemoveAt(0);
        skillObj[skillKey].Add(obj);
        return obj;
    }

    /// <summary>
    /// Q 스킬 함수
    /// </summary>
    public override void Q()
    {
        GameObject obj = GetSkillInThePool("Q");
        PauseMove(0.7f);
        obj.transform.position = transform.position;
        obj.SetActive(true);
    }

    /// <summary>
    /// W 스킬 함수
    /// </summary>
    public override void W()
    {
        GameObject enemyObj = wArguObj;
        Vector3 enemyVector = wArguVec;        
        OnMove();
        HitEffectRPC("Alistar", "W");
        GameObject obj = GetSkillInThePool("W");
        obj.transform.position = transform.position;
        obj.SetActive(true);

        // 공격하는 대상에 따라 공격 처리를 한다.
        if (enemyObj.tag.Equals("Minion"))
        {
            MinionBehavior minBehav = enemyObj.GetComponent<MinionBehavior>();

            // 적인지 확인
            if (!enemyObj.gameObject.name.Contains(TheChampionBehaviour.team))
            {
                MinionAtk minAtk = minBehav.minAtk;
                Vector3 directionVec = (enemyObj.transform.position - enemyVector).normalized;
                Vector3 maxVec = enemyObj.transform.position + (directionVec * 10); ;
                RaycastHit hit;

                if (Physics.Raycast(minAtk.transform.position, directionVec, out hit, 12, 1 << LayerMask.NameToLayer("WallCollider")))
                {
                    float dist = Vector3.Distance(hit.point, enemyObj.transform.position);
                    maxVec = enemyObj.transform.position + (directionVec * (dist - 1f));
                }

                //밀친 후 공격당한 대상의 HitMe 함수를 호출
                maxVec.y = 0;
                minAtk.PushMe(maxVec, 0.5f);
                minAtk.PauseAtk(1f, true);
                float damage = skillData.wDamage[TheChampionData.skill_W - 1] + Acalculate(skillData.wAstat, skillData.wAvalue);

                if (minBehav != null)
                {
                    int viewID = minBehav.GetComponent<PhotonView>().viewID;
                    HitRPC(viewID, damage, "AP", "Push");
                    minBehav.HitMe(damage, "AP", gameObject);
                }
            }
        }
        else if (enemyObj.layer.Equals(LayerMask.NameToLayer("Champion")))
        {
            ChampionBehavior champBehav = enemyObj.GetComponent<ChampionBehavior>();

            // 적인지 확인
            if (champBehav.team != TheChampionBehaviour.team)
            {
                ChampionAtk champAtk = champBehav.myChampAtk;
                Vector3 directionVec = (enemyObj.transform.position - enemyVector).normalized;
                Vector3 maxVec = enemyObj.transform.position + (directionVec * 5); ;
                RaycastHit hit;

                if (Physics.Raycast(champAtk.transform.position, directionVec, out hit, 6, 1 << LayerMask.NameToLayer("WallCollider")))
                {
                    float dis = Vector3.Distance(hit.point, enemyObj.transform.position);
                    maxVec = enemyObj.transform.position + (directionVec * (dis - 1f));
                }

                //밀친 후 공격당한 대상의 HitMe 함수를 호출
                maxVec.y = 0.5f;
                champAtk.PushMe(maxVec, 0.5f);
                champAtk.PauseAtk(1f, true);
                float damage = skillData.wDamage[TheChampionData.skill_W - 1] + Acalculate(skillData.wAstat, skillData.wAvalue);

                if (champBehav != null)
                {
                    int viewID = champBehav.GetComponent<PhotonView>().viewID;

                    HitRPC(viewID, damage, "AP", "Push");
                    champBehav.HitMe(damage, "AP", gameObject, gameObject.name);
                }
            }
        }
        else if (enemyObj.layer.Equals(LayerMask.NameToLayer("Monster")))
        {
            MonsterBehaviour monBehav = enemyObj.GetComponent<MonsterBehaviour>();
            MonsterAtk monAtk = monBehav.monAtk;
            Vector3 directionVec = (enemyObj.transform.position - enemyVector).normalized;
            Vector3 maxVec = enemyObj.transform.position + (directionVec * 5); ;
            RaycastHit hit;

            if (Physics.Raycast(monAtk.transform.position, directionVec, out hit, 6, 1 << LayerMask.NameToLayer("WallCollider")))
            {
                float dist = Vector3.Distance(hit.point, enemyObj.transform.position);
                maxVec = enemyObj.transform.position + (directionVec * (dist - 1f));
            }

            //밀친 후 공격당한 대상의 HitMe 함수를 호출
            maxVec.y = 0;
            monAtk.PushMe(maxVec, 0.5f);
            monAtk.PauseAtk(1f, true);
            float damage = skillData.wDamage[TheChampionData.skill_W - 1] + Acalculate(skillData.wAstat, skillData.wAvalue);

            if (monBehav != null)
            {
                int viewID = monBehav.GetComponent<PhotonView>().viewID;
                HitRPC(viewID, damage, "AP", "Push");
                monBehav.HitMe(damage, "AP", gameObject);
            }
        }

        // 스킬 선택을 해제함
        skillSelect = SkillSelect.none;
    }

    /// <summary>
    /// E 스킬 함수
    /// </summary>
    public override void E()
    {
        GameObject obj = GetSkillInThePool("E");
        obj.transform.position = transform.position;
        obj.SetActive(true);
    }

    /// <summary>
    /// R 스킬 함수
    /// </summary>
    public override void R() 
    {
        GameObject obj = GetSkillInThePool("R");
        obj.transform.position = transform.position;
        obj.transform.SetParent(transform);
        obj.SetActive(true);
    }

    /// <summary>
    /// Q 스킬을 동기화한 함수
    /// </summary>
    public override void QEffect()
    {
        GameObject obj = GetSkillInThePool("Q");
        obj.transform.position = transform.position + adjustVec;
        obj.SetActive(true);
    }

    /// <summary>
    /// W 스킬을 동기화한 함수
    /// </summary>
    public override void WEffect()
    {
        GameObject obj = GetSkillInThePool("W");
        obj.transform.position = transform.position + adjustVec;
        obj.SetActive(true);
    }

    /// <summary>
    /// E 스킬을 동기화한 함수
    /// </summary>
    public override void EEffect()
    {
        GameObject obj = GetSkillInThePool("E");
        obj.transform.position = transform.position + adjustVec;
        obj.SetActive(true);
    }

    /// <summary>
    /// R 스킬을 동기화한 함수
    /// </summary>
    public override void REffect() 
    {
        GameObject obj = GetSkillInThePool("R");
        obj.transform.position = transform.position + adjustVec;
        obj.transform.SetParent(transform);
        obj.SetActive(true);
    }

    /// <summary>
    /// r 스킬 사용이 끝난 후 버프된 방어력을 원래대로 돌려주는 함수
    /// </summary>
    private void DebuffDefence()
    {
        TheChampionData.totalStat.AbilityDef -= rSkillTempVal;
        TheChampionData.totalStat.AttackDef -= rSkillTempVal;
        rSkillTempVal = 0;

        if (!TheUIStat)
            FindUICanvas();

        TheUIStat.Refresh();
    }
}