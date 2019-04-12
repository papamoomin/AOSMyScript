using DG.Tweening;
using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 애쉬의 스킬을 담당하는 스크립트
/// </summary>
public class AsheSkill : Skills
{
    public GameObject qSkillObj = null;
    public GameObject wSkillprefab = null;
    public GameObject[] eSkillObj = null;
    public GameObject rSkillObj = null;
    
    private new AudioSource audio;
    private StackImage TheStackImage = null;
    private GameObject mySkills;
    private Vector3 arguVec = Vector3.zero;
    private int asheHawkCount = 1;
    private int qStackCount = 0;
    private int beforeELv = 0;
    private float asheHawkChargeTime = 90f;
    private float keepQStackTime = 4f;
    private float reduceQStackTime = 0.75f;
    private float qTIme = 4f;
    private bool isQ = false;
    private bool? isIAmAshe = null;
    private string team = "";

    private void Awake()
    {
        InitInstance();
        AllPooling();
        audio = GetComponent<AudioSource>();
    }

    private void Update()
    {
        // 현재 플레이어의 챔피언이 애쉬인지 확인
        if (isIAmAshe == null)
        {
            if (gameObject.tag.Equals("Player"))
                isIAmAshe = true;
            else
                isIAmAshe = false;
        }

        // 애쉬인 경우
        if (isIAmAshe == true)
        {
            // E 스킬을 새로 찍었는지 검사
            if (beforeELv.Equals(0))
            {
                // E 스킬을 찍었다면 E 스킬에 관한 이미지와 텍스트를 불러온다.
                if (TheChampionData.skill_E > 0)
                {
                    if (TheStackImage == null)
                        TheStackImage = GameObject.FindGameObjectWithTag("StackImage").GetComponent<StackImage>();

                    beforeELv = 1;

                    if (asheHawkCount > 0)
                    {
                        TheStackImage.ImageDic["AsheE"].gameObject.SetActive(true);
                        TheStackImage.TextDic["AsheE"].text = asheHawkCount.ToString();
                    }
                }
            }
        }

        // Q 스킬의 스택 수와 딜레이 타임 확인
        CheckQStackAndTime();

        // 매 날리기 스킬을 찍었는데 매의 수가 2개 미만인 경우
        if (TheChampionData.skill_E > 0 && asheHawkCount < 2)
        {
            //시간을 계산해 일정 시간이 되면 매의 수를 하나 늘린다.
            asheHawkChargeTime -= Time.deltaTime;

            if (asheHawkChargeTime < 0)
            {
                asheHawkChargeTime = 100 - (TheChampionData.skill_E * 10);
                ++asheHawkCount;

                if (isIAmAshe == true)
                {
                    if (asheHawkCount.Equals(1))
                        TheStackImage.ImageDic["AsheE"].gameObject.SetActive(true);

                    TheStackImage.TextDic["AsheE"].text = asheHawkCount.ToString();
                }
            }
        }

        //우클릭하거나 esc키를 누르면 스킬 선택이 해제된다.
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            CancelSkill();

        // E 스킬을 선택한 후 마우스 왼쪽 버튼을 클릭
        if (skillSelect.Equals(SkillSelect.E))
            if (Input.GetMouseButtonDown(0))
                SkillingClick("E");

        // R 스킬을 선택한 후 마우스 왼쪽 버튼을 클릭
        if (skillSelect.Equals(SkillSelect.R))
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (audio != null)
                    ChampionSound.instance.Skill(PlayerData.Instance.championName, 3, audio);

                SkillingClick("R");
            }
        }

        // W 스킬을 선택한 후 마우스 왼쪽 버튼을 클릭
        if (skillSelect.Equals(SkillSelect.W))
            if (Input.GetMouseButtonDown(0))
                SkillingClick("W");
    }

    /// <summary>
    /// 애쉬가 일반 공격을 할 때, Q 스킬의 스택을 쌓아주는 함수
    /// </summary>
    public void QCountUp()
    {
        //Q 스킬을 찍어두었는지 검사
        if (TheChampionData.skill_Q > 0)
        {
            if (TheStackImage == null)
                TheStackImage = GameObject.FindGameObjectWithTag("StackImage").GetComponent<StackImage>();

            //Q 스택이 4개 미만인 경우
            if (qStackCount < 4)
            {
                //Q 스택을 하나 올린다.
                ++qStackCount;

                if (isIAmAshe == true)
                {
                    if (qStackCount.Equals(1))
                        TheStackImage.ImageDic["AsheQ"].gameObject.SetActive(true);

                    TheStackImage.TextDic["AsheQ"].text = qStackCount.ToString();
                }
            }
            else if (qStackCount.Equals(4)) //Q 스택이 4개면 4로 유지한다.
                qStackCount = 4;

            // Q 스택이 앞으로 유지될 시간과 감소될 시간을 초기화한다.
            keepQStackTime = 4f;
            reduceQStackTime = 0.75f;
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
        mySkills = new GameObject("AsheSkills");
        mySkills.transform.SetParent(skillParticleManager.transform);
        playerAStarTarget = GetComponent<PlayerMouse>().myTarget;
        TheAIPath = GetComponent<AIPath>();
        skillData = TheSkillClass.skillData["Ashe"];
    }

    /// <summary>
    /// 스킬의 프리팹을 풀링해두는 함수
    /// </summary>
    private void AllPooling()
    {
        Pooling(wSkillprefab, "W", 20);
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
                case "W":
                    obj.GetComponent<AsheW>().mySkill = this;
                    break;
            }
        }

        //기존의 스킬 프리팹 리스트에 임시 리스트의 항목을 합쳐 넣는다.
        skillObj[type].InsertRange(0, tempList);
    }

    /// <summary>
    /// 현재 Q의 스택을 확인하고 시간이 지나면 감소시키는 함수
    /// </summary>
    private void CheckQStackAndTime()
    {
        // Q 스킬을 찍어두었는지 검사
        if (TheChampionData.skill_Q > 0)
        {
            if (TheStackImage == null)
                TheStackImage = GameObject.FindGameObjectWithTag("StackImage").GetComponent<StackImage>();

            // Q 스킬을 사용중이라면 시간을 계산
            if (isQ)
            {
                qTIme -= Time.deltaTime;

                // Q 스킬 지속시간이 끝나면 스킬을 해제한다.
                if (qTIme <= 0)
                {
                    isQ = false;
                    qTIme = 4f;

                    if (photonView.isMine)
                    {
                        TheChampionData.skillPlusAtkDam = 0;
                        TheChampionData.TotalStatDamDefUpdate();
                        TheChampionData.UIStat.Refresh();
                    }

                    qSkillObj.SetActive(false);
                }
            }
            else if (qStackCount > 0)
            {//Q 스택이 1개 이상일 때
                keepQStackTime -= Time.deltaTime;

                //시간을 계산하여 Q 스택을 감소시킨다.
                if (keepQStackTime < 0)
                {
                    reduceQStackTime -= Time.deltaTime;

                    if (reduceQStackTime < 0)
                    {
                        --qStackCount;

                        if (isIAmAshe == true)
                        {
                            if (qStackCount.Equals(0))
                            {
                                TheStackImage.TextDic["AsheQ"].text = "";
                                TheStackImage.ImageDic["AsheQ"].gameObject.SetActive(false);
                            }
                            else
                                TheStackImage.TextDic["AsheQ"].text = qStackCount.ToString();
                        }

                        reduceQStackTime = 0.75f;
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
    /// 선택 후 클릭 시 발동되는 스킬의 공통된 계산 처리를 담당하는 함수
    /// </summary>
    /// <param name="skillKey"></param>
    private void SkillingClick(string skillKey)
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
                CallChampDataUsedSkill(skillKey);
                arguVec = hit.point;
                arguVec.y = 0.5f;
                Invoke(skillKey, 0.1f);
                float animStopTime = (skillKey == "E") ? 0.7f : 0.8f;
                championAnimation.AnimationApply(skillKey, true);
                championAnimation.AnimationApply(skillKey, false, animStopTime);
                break;
            }
        }
    }

    /// <summary>
    /// Q 스킬을 호출하는 함수
    /// </summary>
    public override void QCasting()
    {
        // Q 스택이 가득 찼는지 확인
        if (qStackCount > 3)
        {
            isSkilling = true;
            qStackCount = 0;

            if (isIAmAshe == true)
            {
                TheStackImage.TextDic["AsheQ"].text = "";
                TheStackImage.ImageDic["AsheQ"].gameObject.SetActive(false);
            }

            TheSplatManager.Cancel();
            CallChampDataUsedSkill("Q");
            HitEffectRPC("Ashe", "Q");
            Q();
            EndSkill(0f);
        }
    }

    /// <summary>
    /// W 스킬을 호출하는 함수
    /// </summary>
    public override void WCasting()
    {
        isSkilling = true;
        skillSelect = SkillSelect.W;
        TheSplatManager.Cone.Select();
    }

    /// <summary>
    /// E 스킬을 호출하는 함수
    /// </summary>
    public override void ECasting()
    {
        // 매가 남아있는지 확인
        if (asheHawkCount > 0)
        {
            isSkilling = true;
            skillSelect = SkillSelect.E;
            TheSplatManager.Cancel();
        }
    }

    /// <summary>
    /// R 스킬을 호출하는 함수
    /// </summary>
    public override void RCasting()
    {
        isSkilling = true;
        skillSelect = SkillSelect.R;
        TheSplatManager.Direction.Select();
        TheSplatManager.Direction.Scale = 25f;
    }

    /// <summary>
    /// Q 스킬 함수
    /// </summary>
    public override void Q()
    {
        isQ = true;
        qSkillObj.SetActive(true);
        qTIme = 4f;

        if (photonView.isMine)
        {
            float dam = 1;

            // 현재 공격력의 몇 배가 더해질 것인가를 정한다
            switch (TheChampionData.skill_Q)
            {
                case 1: dam = 0.31f; break;
                case 2: dam = 0.46f; break;
                case 3: dam = 0.64f; break;
                case 4: dam = 0.84f; break;
                case 5: dam = 1.08f; break;
            }

            // 공격력을 더한다
            TheChampionData.skillPlusAtkDam = Mathf.Round(dam * (TheChampionData.myStat.AttackDamage + TheChampionData.itemStat.attackDamage));
            TheChampionData.TotalStatDamDefUpdate();
            TheChampionData.UIStat.Refresh();
        }
    }

    /// <summary>
    /// W 스킬 함수
    /// </summary>
    public override void W()
    {
        Vector3 dest = arguVec;
        arguVec = Vector3.zero;
        float length = 25f;
        Vector3 champPos = transform.position;
        Vector3 directionVec = (dest - champPos).normalized * length;
        float[] degrees = new float[9];
        float degree = Mathf.Atan2(directionVec.x, directionVec.z) * Mathf.Rad2Deg;
        Vector3[] dests = new Vector3[9];

        // W 스킬은 부채꼴 모양으로 화살이 퍼져야하므로 각 화살마다 도달할 지점을 계산 후 발사
        for (int i = 0; i < 9; ++i)
        {
            degrees[i] = (degree + (7f * (float)(i - 4))) * Mathf.Deg2Rad;
            dests[i] = new Vector3(length * Mathf.Sin(degrees[i]), 0.5f, length * Mathf.Cos(degrees[i]));
            dests[i] += champPos;
            GameObject obj = skillObj["W"][0];
            skillObj["W"].RemoveAt(0);
            skillObj["W"].Add(obj);
            obj.SetActive(true);
            obj.transform.position = champPos;
            obj.transform.DOLookAt(dests[i], 0);
            obj.GetComponent<AsheW>().SkillOn(dests[i]);
        }

        transform.DOLookAt(dests[4], 0);
        HitEffectVectorRPC("Ashe", "W", dests[4]);
        PauseMove(0.8f);
        EndSkill(0.8f);
    }

    /// <summary>
    /// E 스킬 함수
    /// </summary>
    public override void E()
    {
        Vector3 dest = arguVec;
        arguVec = Vector3.zero;
        int eSkillObjNum = 0;

        // 연속으로 스킬을 사용할 경우를 대비해 매 두 마리를 두고 날린다
        if (!eSkillObj[0].activeInHierarchy)
        {
            if (!eSkillObj[0].GetComponent<AsheE>().hawkWard.activeInHierarchy)
                eSkillObjNum = 0;
        }
        else if (!eSkillObj[1].activeInHierarchy)
            if (!eSkillObj[1].GetComponent<AsheE>().hawkWard.activeInHierarchy)
                eSkillObjNum = 1;

        eSkillObj[eSkillObjNum].SetActive(true);
        eSkillObj[eSkillObjNum].transform.position = transform.position;
        eSkillObj[eSkillObjNum].transform.DOLookAt(dest, 0);
        HitEffectVectorRPC("Ashe", "E", dest);
        eSkillObj[eSkillObjNum].GetComponent<AsheE>().SkillOn(dest);
        transform.DOLookAt(dest, 0);
        PauseMove(0.7f);
        EndSkill(0.7f);
        --asheHawkCount;

        // 매가 한 마리 남은 경우
        if (asheHawkCount.Equals(1))
        {
            asheHawkChargeTime = 100 - (TheChampionData.skill_E * 10);

            if (isIAmAshe == true)
                TheStackImage.TextDic["AsheE"].text = asheHawkCount.ToString();
        }
        else if (isIAmAshe == true)
        {// 모든 매를 사용한 경우
            TheStackImage.TextDic["AsheE"].text = asheHawkCount.ToString("");
            TheStackImage.ImageDic["AsheE"].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// R 스킬 함수
    /// </summary>
    public override void R()
    {
        Vector3 dest = arguVec;
        arguVec = Vector3.zero;
        rSkillObj.SetActive(true);
        rSkillObj.transform.position = transform.position;
        transform.DOLookAt(dest, 0);
        rSkillObj.transform.DOLookAt(dest, 0);
        HitEffectVectorRPC("Ashe", "R", dest);
        rSkillObj.GetComponent<AsheR>().SkillOn(dest);
        PauseMove(0.8f);
        EndSkill(0.8f);
    }

    /// <summary>
    /// Q 스킬을 동기화한 함수
    /// </summary>
    public override void QEffect()
    {
        Q();
    }

    /// <summary>
    /// W 스킬을 동기화한 함수
    /// </summary>
    public override void WVecEffect()
    {
        Vector3 dest = invokeVec;
        invokeVec = Vector3.zero;
        float length = 25f;
        Vector3 champPos = transform.position;
        Vector3 directionVec = (dest - champPos).normalized * length;
        float[] degrees = new float[9];
        float degree = Mathf.Atan2(directionVec.x, directionVec.z) * Mathf.Rad2Deg;
        Vector3[] dests = new Vector3[9];

        // W 스킬은 부채꼴 모양으로 화살이 퍼져야하므로 각 화살마다 도달할 지점을 계산 후 발사
        for (int i = 0; i < 9; ++i)
        {
            degrees[i] = (degree + (7f * (float)(i - 4))) * Mathf.Deg2Rad;
            dests[i] = new Vector3(length * Mathf.Sin(degrees[i]), 0.5f, length * Mathf.Cos(degrees[i]));
            dests[i] += champPos;
            GameObject obj = skillObj["W"][0];

            if (obj.activeInHierarchy)
            {
                Pooling(wSkillprefab, "W", 20);

                while (skillObj["W"][0].activeInHierarchy)
                {
                    GameObject temp = skillObj["W"][0];

                    skillObj["W"].RemoveAt(0);
                    skillObj["W"].Add(obj);
                }

                obj = skillObj["W"][0];
            }

            skillObj["W"].RemoveAt(0);
            skillObj["W"].Add(obj);
            obj.SetActive(true);
            obj.transform.position = champPos;
            obj.transform.DOLookAt(dests[i], 0);
            obj.GetComponent<AsheW>().SkillOn(dests[i]);
        }

        transform.DOLookAt(dests[4], 0);
        EndSkill(0);
    }

    /// <summary>
    /// E 스킬을 동기화한 함수
    /// </summary>
    public override void EVecEffect()
    {
        Vector3 dest = invokeVec;
        invokeVec = Vector3.zero;
        int eSkillObjNum = 0;

        // 연속으로 스킬을 사용할 경우를 대비해 매 두 마리를 두고 날린다
        if (!eSkillObj[0].activeInHierarchy)
        {
            if (!eSkillObj[0].GetComponent<AsheE>().hawkWard.activeInHierarchy)
                eSkillObjNum = 0;
        }
        else if (!eSkillObj[1].activeInHierarchy)
            if (!eSkillObj[1].GetComponent<AsheE>().hawkWard.activeInHierarchy)
                eSkillObjNum = 1;

        eSkillObj[eSkillObjNum].SetActive(true);
        eSkillObj[eSkillObjNum].transform.position = transform.position;
        eSkillObj[eSkillObjNum].transform.DOLookAt(dest, 0);
        eSkillObj[eSkillObjNum].GetComponent<AsheE>().SkillOn(dest);
        transform.DOLookAt(dest, 0);
        EndSkill(0);
        --asheHawkCount;
    }

    /// <summary>
    /// R 스킬을 동기화한 함수
    /// </summary>
    public override void RVecEffect()
    {
        Vector3 dest = invokeVec;
        invokeVec = Vector3.zero;
        rSkillObj.SetActive(true);
        rSkillObj.transform.position = transform.position;
        transform.DOLookAt(dest, 0);
        rSkillObj.transform.DOLookAt(dest, 0);
        rSkillObj.GetComponent<AsheR>().SkillOn(dest);
        EndSkill(0);
    }
}