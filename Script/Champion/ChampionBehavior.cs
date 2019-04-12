using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Pathfinding;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

/// <summary>
/// 챔피언 개체를 제어하는 스크립트
/// </summary>
public class ChampionBehavior : Photon.PunBehaviour
{
    public ChampionData myChampionData = null;
    public ChampionAtk myChampAtk = null;
    public SkinnedMeshRenderer mesh;
    public GameObject hitEffect;
    public GameObject arrowPrefab = null;
    public bool isDead = false;
    public string team = "Red";

    private AOSMouseCursor cursor;
    private ChampionHP champHP;
    private Text uiReviveTime;
    private DeadEffect deadEffect;
    private ChampionAnimation myChampionAnimation;
    private FogOfWarEntity fog;
    private PhotonView myPhotonView;
    private new AudioSource audio;
    private List<GameObject> arrow = new List<GameObject>();
    private List<assistData> assistCheckList = new List<assistData>();
    private GameObject icon;
    private bool isMouseChanged = false;
    private bool isHpinit = false;
    private float reviveTime = 10.0f;

    /// <summary>
    /// 어시스트 계산을 용이하게 하기 위한 클래스
    /// </summary>
    private class assistData
    {
        public int viewID = 0;
        public float LastDamagedTime = 0;
    }

    private void OnEnable()
    {
        audio = GetComponent<AudioSource>();
        fog = GetComponent<FogOfWarEntity>();
        mesh = GetComponent<SkinnedMeshRenderer>();
        myChampionData = GetComponent<ChampionData>();
        myChampionAnimation = GetComponent<ChampionAnimation>();
        champHP = transform.GetComponent<ChampionHP>();
        icon = transform.parent.GetComponentInChildren<ChampionIcon>().gameObject;
        myPhotonView = GetComponent<PhotonView>();

        if (myPhotonView.owner.GetTeam().ToString().Equals("blue"))
            team = "Blue";

        if (photonView.owner.Equals(PhotonNetwork.player) && SceneManager.GetActiveScene().name.Equals("Selection"))
            ChampionSound.instance.SelectionVoice(PlayerData.Instance.championName);

        if (!myPhotonView.isMine)
        {
            audio.volume = 0.5f;
            audio.loop = false;
            audio.spatialBlend = 1f;
            audio.rolloffMode = AudioRolloffMode.Linear;
            audio.maxDistance = 20f;
        }
    }

    private void Update()
    {
        if (GetComponent<PhotonView>().owner.Equals(PhotonNetwork.player))
        {
            // 플레이어가 사망한 경우
            if (isDead)
            {
                if (uiReviveTime == null)
                    uiReviveTime = myChampionData.UIIcon.reviveTimeText;

                if (deadEffect == null)
                    deadEffect = Camera.main.GetComponentInChildren<DeadEffect>();

                // 부활 시간을 처리한다
                reviveTime -= Time.deltaTime;
                uiReviveTime.text = Mathf.FloorToInt(reviveTime).ToString();

                if (Camera.main.GetComponent<RTS_Cam.RTS_Camera>().targetFollow != null)
                    Camera.main.GetComponent<RTS_Cam.RTS_Camera>().ResetTarget();

                // 부활까지 기다리는 시간이 끝났다
                if (reviveTime < 0)
                {
                    reviveTime = 10.0f;

                    // UIText를 변경한다
                    uiReviveTime.text = "";
                    PlayerData.Instance.isDead = false;

                    // 모든 클라이언트에서 동일하게 부활하도록 처리
                    SyncRevive();

                    // 카메라를 캐릭터위치로 옮긴다
                    Camera.main.transform.position = new Vector3(transform.position.x, Camera.main.transform.position.y, transform.position.z)
                        + Camera.main.GetComponent<RTS_Cam.RTS_Camera>().targetOffset;

                    // 카메라의 회색화면을 끈다
                    deadEffect.TurnOff();

                    // 기타 설정을 처리
                    myChampionData.UIIcon.reviveCoverImage.enabled = false;
                    myChampionData.theAIPath.canMove = true;

                    if (PhotonNetwork.player.GetTeam().ToString().Contains("red"))
                    {
                        transform.position = myChampionData.redPos;
                        myChampAtk.aStarTargetObj.transform.position = myChampionData.redPos;
                    }
                    else if (PhotonNetwork.player.GetTeam().ToString().Contains("blue"))
                    {
                        transform.position = myChampionData.bluePos;
                        myChampAtk.aStarTargetObj.transform.position = myChampionData.bluePos;
                    }

                    myChampionAnimation.AnimationAllOff();
                    this.photonView.RPC("SyncRevive", PhotonTargets.Others, null);
                }
            }
        }

        // 어시스트를 확인한다
        CheckAssist();
    }

    private void OnMouseOver()
    {
        if (!SceneManager.GetActiveScene().name.Equals("InGame"))
            return;

        if (team.ToLower().Equals(PhotonNetwork.player.GetTeam().ToString()))
        {
            if (photonView.isMine)
                return;

            cursor.SetCursor(1, Vector2.zero);
            isMouseChanged = true;
        }
        else
        {
            cursor.SetCursor(2, Vector2.zero);
            isMouseChanged = true;
        }
    }

    private void OnMouseExit()
    {
        if (!SceneManager.GetActiveScene().name.Equals("InGame"))
            return;

        if (isMouseChanged)
        {
            cursor.SetCursor(cursor.PreCursor, Vector2.zero);
            isMouseChanged = false;
        }
    }

    private void OnLevelWasLoaded(int level)
    {
        if (UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(level).name.Equals("InGame"))
        {
            if (!cursor)
                cursor = GameObject.FindGameObjectWithTag("MouseCursor").GetComponent<AOSMouseCursor>();

            Invoke("SetHP", 0.5f);
        }
    }

    /// <summary>
    /// 어시스트를 했는지 확인하기 위해 어시스트 리스트를 처리하는 함수
    /// </summary>
    private void CheckAssist()
    {
        // 리스트에 무엇인가 있을때만 체크
        if (assistCheckList.Count > 0)
            for (int i = 0; i < assistCheckList.Count; i++)
                //마지막 공격받은 시간이 10초가 지나면 리스트에서 삭제한다
                if (Time.time - assistCheckList[i].LastDamagedTime > 10.0f)
                    assistCheckList.Remove(assistCheckList[i]);
    }

    /// <summary>
    /// 모든 클라이언트에서 죽은 챔피언이 동일하게 부활하도록 처리하는 함수
    /// </summary>
    [PunRPC]
    public void SyncRevive()
    {
        if (icon != null)
            icon.gameObject.SetActive(true);

        champHP.BasicSetting();
        SoundManager.Instance.ChampSound(SoundManager.Instance.Champion_Respawn);
        isDead = false;
        myChampionData.totalStat.Hp = myChampionData.totalStat.MaxHp;
        myChampionData.totalStat.Mp = myChampionData.totalStat.MaxMp;
        InGameManager inGameManager = GameObject.FindGameObjectWithTag("InGameManager").GetComponent<InGameManager>();

        if (team.Equals("Blue"))
            transform.position = inGameManager.BluePos;
        else
            transform.position = inGameManager.RedPos;

        Transform myAstarTarget = transform.parent.Find("PlayerA*Target");

        if (myAstarTarget != null)
            myAstarTarget.position = new Vector3(transform.position.x, myAstarTarget.localPosition.y, transform.position.z);

        myChampionAnimation.AnimationAllOff();
    }

    /// <summary>
    /// 챔피언에게 체력바를 붙이는 함수
    /// </summary>
    private void SetHP()
    {
        champHP = transform.GetComponent<ChampionHP>();

        if (champHP == null)
            isHpinit = true;
    }

    /// <summary>
    /// 사망 시 Dead 함수를 호출해주는 함수
    /// </summary>
    /// <param name="time">몇 초 뒤에 사망하는가</param>
    public void IamDead(float time = 0)
    {
        Invoke("Dead", time);
    }

    /// <summary>
    /// 사망처리를 하는 함수
    /// </summary>
    private void Dead()
    {
        if (icon != null)
            icon.gameObject.SetActive(false);

        // 플레이어 본인이 죽은 경우 데스 수치를 올리고 죽는 더미를 생성한다
        ChampionSound.instance.IamDeadSound(myChampionData.championName);
        InitChampionStatus();
        champHP.InitProgressBar();

        if (GetComponent<PhotonView>().owner.Equals(PhotonNetwork.player))
        {
            myChampionData.death++;

            // 더미생성
            string dummyName = gameObject.name.Split('_')[0];
            PhotonNetwork.Instantiate("Champion/" + dummyName + "Die", transform.position, transform.rotation, 0);

            reviveTime = (myChampionData.totalStat.Level - 1) * 2.0f + 10.0f;
            myChampionData.UIIcon.reviveCoverImage.enabled = true;

            if (deadEffect == null)
                deadEffect = Camera.main.GetComponentInChildren<DeadEffect>();

            deadEffect.TurnOn();

            // 사망 시 상점 이용이 가능하게 처리한다
            PlayerData.Instance.purchaseState = true;

            // 나를 죽인 이들의 어시스트를 올린다
            AssistRPC();
        }

        myChampAtk.StopAllCoroutines();
        myChampAtk.ResetTarget();

        // 사망시 활성을 끄는 대신 좌표를 옮겨버린다
        transform.position = new Vector3(transform.position.x, -100, transform.position.z);

        // 사망시 마우스 커서가 바뀌어있다면 원상복귀 시킨다
        if (isMouseChanged)
            cursor.SetCursor(cursor.PreCursor, Vector2.zero);
    }

    /// <summary>
    /// 자신을 죽인 플레이어들에게 어시스트를 올려준다
    /// </summary>
    public void AssistRPC()
    {
        // 사망 시 자신을 공격했던 어시스트 대상자들의 클라이언트에 viewID를 보내준다
        foreach (assistData champ in assistCheckList)
            this.photonView.RPC("AssistUP", PhotonView.Find(champ.viewID).owner, champ.viewID);

        assistCheckList.Clear();
    }

    /// <summary>
    /// AssistRPC 함수에서 받은 viewID에 따라 어시스트를 올려준다
    /// </summary>
    /// <param name="viewID">어시스트를 올려줄 챔피언의 viewID</param>
    [PunRPC]
    public void AssistUP(int viewID)
    {
        PhotonView.Find(viewID).gameObject.GetComponent<ChampionData>().AssistUP();
    }

    /// <summary>
    /// 챔피언 사망 시 값을 초기화해주는 함수
    /// </summary>
    public void InitChampionStatus()
    {
        fog.ClearEnteties();
        myChampAtk.TheAIPath.isStopped = false;
        champHP.InitProgressBar();
    }

    /// <summary>
    /// 피격을 당했을 때의 처리를 담당하는 함수
    /// </summary>
    /// <param name="damage">공격자의 공격력</param>
    /// <param name="atkType">공격의 타입</param>
    /// <param name="atker">공격한 오브젝트</param>
    /// <param name="killerName">공격한 오브젝트의 이름</param>
    public bool HitMe(float damage = 0, string atkType = "AD", GameObject atker = null, string killerName = "") // AD, AP, FD(고정 데미지 = Fixed damage)
    {
        if (!GetComponent<PhotonView>().owner.Equals(PhotonNetwork.player))
            return false;

        if (myChampionData.totalStat.Hp < 1)
            return false;

        int atkerViewID = 0;

        // 공격한 이가 챔피언이면 어시스트 리스트에 저장
        if (atker != null)
        {
            if (atker.layer.Equals(LayerMask.NameToLayer("Champion")))
            {
                atkerViewID = atker.GetComponent<PhotonView>().viewID;

                bool isFind = false;

                // 리스트에 같은 적이 저장되어있다면 시간을 갱신
                for (int i = 0; i < assistCheckList.Count; i++)
                {
                    if (assistCheckList[i].viewID.Equals(atkerViewID))
                    {
                        isFind = true;
                        assistCheckList[i].LastDamagedTime = Time.time;
                        break;
                    }
                }

                // 리스트에 없다면 새로 추가
                if (!isFind)
                    assistCheckList.Add(new assistData() { viewID = atkerViewID, LastDamagedTime = Time.time });
            }
        }

        //데미지 계산
        if (atkType.Equals("AD"))
            damage = (damage * 100f) / (100f + myChampionData.totalStat.AttackDef);
        else if (atkType.Equals("AP"))
            damage = (damage * 100f) / (100f + myChampionData.totalStat.AbilityDef);

        //체력을 감소시킴
        myChampionData.totalStat.Hp -= damage;

        //사망한 경우
        if (myChampionData.totalStat.Hp < 1)
        {
            myChampionData.totalStat.Hp = 0;
            bool isChamp = true;
            int atkViewID = -1;

            if (atker == null)
                isChamp = false;

            // 챔피언이 아닌 적에게 맞고 죽은 경우
            else if (!atker.layer.Equals(LayerMask.NameToLayer("Champion")))
            {
                // 자신을 친 챔피언이 없다면 넘어감
                if (assistCheckList.Count == 0)
                    isChamp = false;
                else
                {// 타 챔피언과 싸우던 중이었다면 마지막으로 친 챔피언이 죽인 것으로 처리
                    if (assistCheckList.Count == 1)
                    {
                        atkViewID = assistCheckList[0].viewID;
                        killerName = PhotonView.Find(atkViewID).gameObject.name;
                    }
                    else
                    {
                        float lastDamagedTime = 0;
                        int lastAtkViewID = 0;

                        for (int i = 0; i < assistCheckList.Count; i++)
                        {
                            if (lastDamagedTime == 0)
                            {
                                lastDamagedTime = assistCheckList[i].LastDamagedTime;
                                lastAtkViewID = assistCheckList[i].viewID;
                            }
                            else if (lastDamagedTime < assistCheckList[i].LastDamagedTime)
                            {
                                lastDamagedTime = assistCheckList[i].LastDamagedTime;
                                lastAtkViewID = assistCheckList[i].viewID;
                            }
                        }

                        atkViewID = lastAtkViewID;
                        killerName = PhotonView.Find(atkViewID).gameObject.name;
                    }
                }
            }
            // 챔피언이 죽인거라면 공격한 챔피언의 viewID를 가져옴
            else if (isChamp)
                atkViewID = atker.GetComponent<PhotonView>().viewID;

            //킬매니저에게 누군가 자신을 죽였음을 알림
            KillManager.instance.SomebodyKillChampionRPC(myPhotonView.viewID, atkViewID, isChamp, killerName);
        }
        else if (hitEffect.activeInHierarchy)
            hitEffect.SetActive(false);

        hitEffect.SetActive(true);

        return isDead;
    }

    /// <summary>
    /// 사망 판정 시 사망 함수를 불러주고 기타 처리를 해주는 함수
    /// </summary>
    /// <param name="time">사망이 지연되는 시간</param>
    /// <param name="atkViewID">죽인 이의 viewID</param>
    /// <param name="atkIsChamp">죽인 적이 챔피언인가에 대한 여부</param>
    public void CallDead(float time, int atkViewID, bool atkIsChamp)
    {
        if (!isDead)
        {
            isDead = true;
            IamDead(time); // 사망 함수를 호출

            // 자신을 죽인 이는 어시스트 리스트에서 제거한다
            if (atkIsChamp)
            {
                for (int i = 0; i < assistCheckList.Count; i++)
                {
                    if (assistCheckList[i].viewID.Equals(atkViewID))
                    {
                        assistCheckList.Remove(assistCheckList[i]);
                        break;
                    }
                }
            }

            // 상단바의 정보를 업데이트 한다
            Invoke("RightTopBarRefresh", time);
        }
    }

    /// <summary>
    /// 우측 상단의 상단바 정보를 업데이트하도록 호출해주는 함수
    /// </summary>
    public void RightTopBarRefresh()
    {
        myChampionData.UIRightTop.AllUpdate();
    }


    /// <summary>
    /// RPC로 피격을 동기화시키는 함수
    /// </summary>
    /// <param name="viewID">피격당한 오브젝트의 viewID</param>
    [PunRPC]
    public void HitSync(int viewID)
    {
        GameObject obj = PhotonView.Find(viewID).gameObject;

        //각각 피격당한 대상의 HitMe 함수를 호출함.
        if (obj != null)
        {
            if (obj.tag.Equals("Minion"))
                obj.GetComponent<MinionBehavior>().HitMe(myChampionData.totalStat.AttackDamage, "AD", this.gameObject);
            else if (obj.layer.Equals(LayerMask.NameToLayer("Champion")))
                obj.GetComponent<ChampionBehavior>().HitMe(myChampionData.totalStat.AttackDamage, "AD", this.gameObject, this.name);
            else if (obj.layer.Equals(LayerMask.NameToLayer("Monster")))
                obj.GetComponent<MonsterBehaviour>().HitMe(myChampionData.totalStat.AttackDamage, "AD", this.gameObject);
        }
    }

    /// <summary>
    /// RPC로 피격을 동기화시키는 함수
    /// </summary>
    /// <param name="key">피격당한 오브젝트의 key값</param>
    [PunRPC]
    public void HitSyncKey(string key)
    {
        //피격당한 건물의 HitMe 함수를 호출함
        if (TowersManager.towers[key] != null)
            if (key.Contains("1") || key.Contains("2") || key.Contains("3"))
                TowersManager.towers[key].GetComponent<TowerBehaviour>().HitMe(myChampionData.totalStat.AttackDamage);
            else
                TowersManager.towers[key].GetComponent<SuppressorBehaviour>().HitMe(myChampionData.totalStat.AttackDamage);
    }

    /// <summary>
    /// RPC로 스킬을 동기화시키는 함수
    /// </summary>
    /// <param name="viewID">스킬을 사용한 챔피언의 viewID</param>
    /// <param name="name">스킬을 사용한 챔피언의 이름</param>
    /// <param name="key">사용한 스킬 단축키</param>
    /// <param name="number">이펙트가 몇 회 재생되어야 하는가</param>
    /// <param name="term">몇 초 뒤에 발동하는가</param>
    [PunRPC]
    public void HitSyncEffect(int viewID, string name, string key, int number, float term)
    {
        GameObject obj = PhotonView.Find(viewID).gameObject;
        key += "Effect";

        if (name.Contains("Alistar"))
            obj.GetComponent<AlistarSkill>().InvokeEffect(key, number, term);
        else if (name.Contains("Mundo"))
            obj.GetComponent<MundoSkill>().InvokeEffect(key, number, term);
        else if (name.Contains("Ashe"))
            obj.GetComponent<AsheSkill>().InvokeEffect(key, number, term);
    }

    /// <summary>
    /// RPC로 스킬을 동기화시키는 함수
    /// </summary>
    /// <param name="viewID">스킬을 사용한 챔피언의 viewID</param>
    /// <param name="name">스킬을 사용한 챔피언의 이름</param>
    /// <param name="key">사용한 스킬 단축키</param>
    /// <param name="vec">스킬이 발동되는 목표 지점</param>
    /// <param name="number">이펙트가 몇 회 재생되어야 하는가</param>
    /// <param name="term">몇 초 뒤에 발동하는가</param>
    [PunRPC]
    public void HitSyncEffectVector(int viewID, string name, string key, Vector3 vec, int number, float term)
    {
        GameObject obj = PhotonView.Find(viewID).gameObject;
        key += "VecEffect";

        if (name.Contains("Mundo"))
            obj.GetComponent<MundoSkill>().InvokeVecEffect(key, number, term, vec);
        else if (name.Contains("Ashe"))
            obj.GetComponent<AsheSkill>().InvokeVecEffect(key, number, term, vec);
    }

    /// <summary>
    /// RPC로 상태 이상이 동반되는 스킬을 동기화시키는 함수
    /// </summary>
    /// <param name="viewID">피격당한 오브젝트의 viewID</param>
    /// <param name="damage">데미지</param>
    /// <param name="atkType">어택 타입</param>
    /// <param name="cc">상태 이상의 종류</param>
    /// <param name="senderViewID">스킬을 사용한 챔피언의 viewID</param>
    [PunRPC]
    public void HitSyncCCSkill(int viewID, float damage, string atktype, string cc, int senderViewID)
    {
        GameObject obj = PhotonView.Find(viewID).gameObject;
        GameObject atker = PhotonView.Find(senderViewID).gameObject;

        if (obj != null)
        {
            //각각 피격당한 대상의 상태이상을 발생시킴
            if (obj.tag.Equals("Minion"))
            {
                obj.GetComponent<MinionBehavior>().HitMe(damage, atktype, atker);

                if (!string.IsNullOrEmpty(cc))
                {
                    if (cc.Contains("Jump"))
                        obj.transform.DOJump(obj.transform.position, 3, 1, 1f);

                    if (cc.Contains("Push"))
                        obj.GetComponentInChildren<MinionAtk>().PushMe(Vector3.up * 3 + obj.transform.position
                            + (((obj.transform.position - atker.transform.position).normalized) * 5), 1f);
                }
            }
            else if (obj.layer.Equals(LayerMask.NameToLayer("Champion")))
            {
                obj.GetComponent<ChampionBehavior>().HitMe(damage, atktype, atker, atker.name);

                if (!string.IsNullOrEmpty(cc))
                {
                    if (cc.Contains("Jump"))
                        obj.transform.DOJump(obj.transform.position, 3, 1, 1f);

                    if (cc.Contains("Push"))
                        obj.GetComponentInChildren<ChampionAtk>().PushMe(Vector3.up * 3 + obj.transform.position
                            + (((obj.transform.position - atker.transform.position).normalized) * 5), 1f);
                }
            }
            else if (obj.layer.Equals(LayerMask.NameToLayer("Monster")))
            {
                obj.GetComponent<MonsterBehaviour>().HitMe(damage, atktype, atker);

                if (!string.IsNullOrEmpty(cc))
                {
                    if (cc.Contains("Jump"))
                        obj.transform.DOJump(obj.transform.position, 3, 1, 1f);

                    if (cc.Contains("Push"))
                        obj.GetComponentInChildren<MonsterAtk>().PushMe(Vector3.up * 3 + obj.transform.position
                            + (((obj.transform.position - atker.transform.position).normalized) * 5), 1f);
                }
            }
        }
    }

    /// <summary>
    /// RPC로 화살을 발사하는 함수
    /// </summary>
    /// <param name="targetPos">타겟의 위치</param>
    /// <param name="moveTime">타겟에게 도달할 때까지 걸리는 시간</param>
    [PunRPC]
    public void CreateArrow(Vector3 targetPos, float moveTime)
    {
        if (this != null)
        {
            //화살이 부족한 경우에만 풀링을 돌림
            if (arrow.Count < 1)
            {
                GameObject arrowObj;

                for (int i = 0; i < 5; ++i)
                {
                    arrowObj = Instantiate(arrowPrefab, new Vector3(0, 0, 0), Quaternion.identity, transform.parent);
                    arrowObj.SetActive(false);
                    this.arrow.Add(arrowObj);
                }
            }

            GameObject frontArrow = arrow[0];
            arrow.RemoveAt(0);
            arrow.Add(frontArrow);
            frontArrow.SetActive(true);
            frontArrow.transform.position = transform.position;
            frontArrow.transform.LookAt(targetPos);
            frontArrow.transform.DOMove(targetPos, moveTime, false).OnKill(() => { frontArrow.SetActive(false); });
            TargetProjectile projectile = frontArrow.GetComponent<TargetProjectile>();

            if (projectile != null)
                projectile.ActiveFalse(moveTime);
        }
    }

    /// <summary>
    /// 다른 클라이언트에도 화살을 생성하기 위해 RPC를 돌리는 함수
    /// </summary>
    /// <param name="targetPos">타겟의 위치</param>
    /// <param name="moveTime">타겟에게 도달할 때까지 걸리는 시간</param>
    public void ArrowRPC(Vector3 targetPos, float moveTime)
    {
        CreateArrow(targetPos, moveTime);
        this.photonView.RPC("CreateArrow", PhotonTargets.Others, targetPos, moveTime);
    }

    /// <summary>
    /// RPC로 와드를 모든 클라이언트에게 동기화시키는 함수
    /// </summary>
    /// <param name="team">와드를 설치한 챔피언의 팀</param>
    /// <param name="champLv">와드를 설치한 챔피언의 레벨</param>
    /// <param name="wardVec">와드를 설치한 위치</param>
    [PunRPC]
    public void SyncWard(string team, int champLv, Vector3 wardVec)
    {
        GameObject ward = Minion_ObjectPool.current.GetPooledWard(team);
        wardVec.y = 1;
        ward.transform.position = wardVec;

        if (!team.Equals(this.team))
            ward.GetComponent<MeshRenderer>().enabled = false;

        ward.SetActive(true);
        ward.GetComponent<Ward>().MakeWard(team, champLv);
    }

    /// <summary>
    /// 다른 클라이언트에게 피격을 동기화시키기 위해 RPC를 돌리는 함수
    /// </summary>
    /// <param name="viewID">피격당한 오브젝트의 viewID</param>
    public void HitRPC(int viewID)
    {
        this.photonView.RPC("HitSync", PhotonTargets.Others, viewID);
    }

    /// <summary>
    /// 다른 클라이언트에게 피격을 동기화시키기 위해 RPC를 돌리는 함수
    /// </summary>
    /// /// <param name="key">피격당한 오브젝트의 key값</param>
    public void HitRPC(string key)
    {
        this.photonView.RPC("HitSyncKey", PhotonTargets.Others, key);
    }

    /// <summary>
    /// 다른 클라이언트에게 와드를 동기화시키기 위해 RPC를 돌리는 함수
    /// </summary>
    /// <param name="team">와드를 설치한 챔피언의 팀</param>
    /// <param name="champLv">와드를 설치한 챔피언의 레벨</param>
    /// <param name="wardVec">와드를 설치한 위치</param>
    public void WardRPC(string team, int champLv, Vector3 wardVec)
    {
        this.photonView.RPC("SyncWard", PhotonTargets.All, this.team, myChampionData.myStat.Level, wardVec);
    }
}