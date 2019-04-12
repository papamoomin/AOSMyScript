using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using Pathfinding.RVO;
using Pathfinding;

/// <summary>
/// 미니언 개체를 제어하는 스크립트
/// </summary>
public class MinionBehavior : Photon.PunBehaviour
{
    public enum TeamColor { red, blue }
    public TeamColor team;
    public MinionAtk minAtk;
    public StatClass.Stat stat;
    public GameObject hpBar;
    public SkinnedMeshRenderer mesh;
    public new AudioSource audio;
    public Vector3 spawnPoint;
    public GameObject minionHitEffect;
    public GameObject cannonMuzzle;
    public int key = -1;
    public bool isDead = false;

    protected Animator animator;

    private AIDestinationSetter TheAIDest;
    private AIPath TheAIPath;
    private InGameManager ingameManager;
    private FogOfWarEntity fog;
    private AOSMouseCursor cursor;
    private MinionHP minHP;
    private GameObject pool;
    private bool isFirstLoad = false;
    private bool isMouseChanged = false;

    private void Awake()
    {
        Init();
    }

    private void OnEnable()
    {
        isDead = false;

        //팀 설정
        if (transform.name.Contains("Red"))
            team = TeamColor.red;
        else if (gameObject.name.Contains("Blue"))
            team = TeamColor.blue;

        //처음 불러오는 경우에만 하는 설정
        if (isFirstLoad)
            InitMinionStatus();
    }

    private void OnDestroy()
    {
        CancelInvoke();
    }

    private void Start()
    {
        pool = GameObject.FindGameObjectWithTag("MinionPooling");
        animator = GetComponent<Animator>();
        SetMinion();
        InitMinionStatus();
        isFirstLoad = true;
    }

    private void Update()
    {
        if (stat != null)
        {
            //사망시 가지고 있던 타겟 리스트를 초기화
            if (stat.Hp < 1)
            {
                stat.Hp = 0;

                if (minAtk == null)
                    minAtk = transform.GetComponentInChildren<MinionAtk>();

                minAtk.enemiesList.Clear();
            }
        }
    }

    private void OnMouseOver()
    {
        if (team.ToString().ToLower().Equals(PhotonNetwork.player.GetTeam().ToString().ToLower()))
        {
            cursor.SetCursor(1, Vector2.zero);
            isMouseChanged = true;
        }
        else if (!team.ToString().ToLower().Equals(PhotonNetwork.player.GetTeam().ToString().ToLower()))
        {
            cursor.SetCursor(2, Vector2.zero);
            isMouseChanged = true;
        }
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
    /// 미니언의 기본 스텟값을 설정하는 함수
    /// </summary>
    /// <param name="minionName">데이터를 가져오기 위한 미니언의 종류</param>
    private void SetStat(string minionName)
    {
        stat = StatClass.Instance.characterData[minionName].ClassCopy();
    }

    /// <summary>
    /// 시작 시 오디오 기본 값을 설정하는 함수
    /// </summary>
    private void InitAudio()
    {
        audio = GetComponentInChildren<AudioSource>();
        audio.minDistance = 1.0f;
        audio.maxDistance = 10.0f;
        audio.volume = 1f;
        audio.spatialBlend = 0.5f;
        audio.rolloffMode = AudioRolloffMode.Linear;
    }

    /// <summary>
    /// 해당 스크립트의 전체적인 기본 값을 설정하는 함수
    /// </summary>
    private void Init()
    {
        ingameManager = GameObject.FindGameObjectWithTag("InGameManager").GetComponent<InGameManager>();
        mesh = GetComponentInChildren<SkinnedMeshRenderer>();
        fog = GetComponent<FogOfWarEntity>();
        minHP = transform.GetComponent<MinionHP>();
        minAtk = transform.GetComponentInChildren<MinionAtk>();
        TheAIDest = gameObject.GetComponent<AIDestinationSetter>();
        TheAIPath = gameObject.GetComponent<AIPath>();

        if (!cursor)
            cursor = GameObject.FindGameObjectWithTag("MouseCursor").GetComponent<AOSMouseCursor>();

        InitAudio();
    }

    /// <summary>
    /// 미니언의 타입을 분류하는 함수
    /// </summary>
    protected void SetMinion()
    {
        if (transform.name.Contains("Super"))
            SetStat("Minion_Super");
        else if (transform.name.Contains("Melee"))
            SetStat("Minion_Warrior");
        else if (transform.name.Contains("Magician"))
            SetStat("Minion_Magician");
        else if (transform.name.Contains("Siege"))
            SetStat("Minion_Siege");
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
        InitMinionStatus();
        minHP.InitProgressBar();

        if (minAtk == null)
            minAtk = transform.GetComponentInChildren<MinionAtk>();
        if (TheAIDest == null)
            TheAIDest = gameObject.GetComponent<AIDestinationSetter>();
        if (TheAIPath == null)
            TheAIPath = gameObject.GetComponent<AIPath>();

        minAtk.TheAIPath = null;
        minAtk.moveTarget = null;
        minAtk.nowTarget = null;
        minAtk.StopAllCoroutines();
        TheAIPath.canMove = true;
        TheAIPath.canSearch = true;
        gameObject.SetActive(false);
        gameObject.GetComponent<AIDestinationSetter>().target = null;

        // 죽을 때 마우스 바뀐 상태면 원래대로 돌림
        if (isMouseChanged)
            cursor.SetCursor(cursor.PreCursor, Vector2.zero);
    }

    /// <summary>
    /// 피격을 당했을 때의 처리를 담당하는 함수
    /// </summary>
    /// <param name="damage">공격자의 공격력</param>
    /// <param name="atkType">공격의 타입</param>
    /// <param name="atker">공격한 챔피언(공격자가 챔피언인 경우 외엔 null)</param>
    /// <returns>본래 죽었는가의 여부를 리턴하나, 방식의 변경으로 무조건 false를 리턴</returns>
    public bool HitMe(float damage = 0, string atkType = "AD", GameObject atker = null) // AD, AP, FD(고정 데미지 = Fixed damage)
    {
        if (!PhotonNetwork.isMasterClient)
            return false;

        //데미지 계산
        if (atkType.Equals("AD") || atkType.Equals("ad"))
            damage = (damage * 100f) / (100f + stat.AttackDef);
        else if (atkType.Equals("AP") || atkType.Equals("ap"))
            damage = (damage * 100f) / (100f + stat.AbilityDef);

        //이미 다른 개체에 피격당해 체력이 0 이하라면 더 맞아봐야 소용없으니 리턴
        if (stat.Hp <= 0)
            return false;

        //체력을 감소시킴
        stat.Hp -= damage;

        //사망한 경우
        if (stat.Hp < 1)
        {
            stat.Hp = 0;
            bool isChamp = (atker == null) ? false : true;
            int id;

            if (isChamp)
                id = atker.GetPhotonView().viewID;
            else
                id = -1;

            //킬매니저에게 자신이 죽었음을 알림
            KillManager.instance.SomebodyKillMinionRPC(key, id, isChamp);
        }
        else
        {//사망하지 않은 경우
            minionHitEffect.SetActive(true);
            //킬매니저에게 자신이 맞았음을 알림
            KillManager.instance.ChangeMinionHPRPC(key, stat.Hp);
        }

        return false;
    }

    /// <summary>
    /// 사망 판정 시 사망 함수를 불러주고 기타 처리를 해주는 함수
    /// </summary>
    /// <param name="time">사망이 지연되는 시간</param>
    public void CallDead(float time)
    {
        isDead = true;
        stat.Hp = 0;
        IamDead(time); //사망 함수를 호출
        NearExp(); //자신의 주변에 있는 적들에게 경험치를 줌

        if (minAtk == null)
            minAtk = transform.GetComponentInChildren<MinionAtk>();

        minAtk.enemiesList.Clear();
    }

    /// <summary>
    /// 미니언 사망 시 주변에 경험치를 주는 함수
    /// </summary>
    private void NearExp()
    {
        //근처의 오브젝트를 찾음
        Collider[] nearCollider = Physics.OverlapSphere(transform.position, 15f);
        int exp = 0;

        if (gameObject.name.Contains("Melee"))
            exp = 59;
        else if (gameObject.name.Contains("Magician"))
            exp = 29;
        else if (gameObject.name.Contains("Siege"))
            exp = 92;
        else if (gameObject.name.Contains("Super"))
            exp = 97;

        //적팀 챔피언에게만 경험치 지급
        if (gameObject.name.Contains("Blue"))
        {
            foreach (Collider c in nearCollider)
                if (c.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
                    if (c.GetComponent<PhotonView>().owner.GetTeam().ToString().Equals("red"))
                        c.GetComponent<PhotonView>().RPC("MinionExp", c.GetComponent<PhotonView>().owner, exp);
        }
        else if (gameObject.name.Contains("Red"))
            foreach (Collider c in nearCollider)
                if (c.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
                    if (c.GetComponent<PhotonView>().owner.GetTeam().ToString().Equals("blue"))
                        c.GetComponent<PhotonView>().RPC("MinionExp", c.GetComponent<PhotonView>().owner, exp);
    }

    /// <summary>
    /// RPC로 화살을 생성해 발사하는 함수
    /// </summary>
    /// <param name="targetPos">타겟의 위치</param>
    /// <param name="moveTime">타겟에게 도달할 때까지 걸리는 시간</param>
    [PunRPC]
    public void CreateArrow(Vector3 targetPos, float moveTime)
    {
        if (this != null)
        {
            GameObject Arrow = Minion_ObjectPool.current.GetPooledArrow();
            Arrow.SetActive(true);
            Arrow.GetComponent<MinionProjectile>().projectileDuration = moveTime;
            Vector3 temppos = transform.position;
            temppos.y = 1.6f;
            Arrow.transform.position = temppos;
            Arrow.transform.LookAt(targetPos);
            Arrow.transform.DOMove(targetPos, moveTime, false);
        }
    }

    /// <summary>
    /// RPC로 대포의 탄환을 생성해 발사하는 함수
    /// </summary>
    /// <param name="targetPos">타겟의 위치</param>
    /// <param name="moveTime">타겟에게 도달할 때까지 걸리는 시간</param>
    [PunRPC]
    public void CannonballCreate(Vector3 targetPos, float moveTime)
    {
        if (this != null)
        {
            GameObject Cannonball = Minion_ObjectPool.current.GetPooledCannonball();
            Cannonball.SetActive(true);
            Cannonball.GetComponent<MinionProjectile>().projectileDuration = moveTime;
            Cannonball.transform.position = cannonMuzzle.transform.position;
            Cannonball.transform.LookAt(targetPos);
            Cannonball.transform.DOMove(targetPos, moveTime, false);
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
    /// 다른 클라이언트에도 대포 탄환을 생성하기 위해 RPC를 돌리는 함수
    /// </summary>
    /// <param name="targetPos">타겟의 위치</param>
    /// <param name="moveTime">타겟에게 도달할 때까지 걸리는 시간</param>
    public void CannonballRPC(Vector3 targetPos, float moveTime)
    {
        CannonballCreate(targetPos, moveTime);
        this.photonView.RPC("CannonballCreate", PhotonTargets.Others, targetPos, moveTime);
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
            {
                MinionBehavior minBehav = obj.GetComponent<MinionBehavior>();

                if (minBehav != null)
                    minBehav.HitMe(stat.AttackDamage, "AD", gameObject);
            }
            else if (obj.layer.Equals(LayerMask.NameToLayer("Champion")))
            {
                ChampionSound.instance.IamAttackedSound(audio, obj.name);
                obj.GetComponent<ChampionBehavior>().HitMe(stat.AttackDamage, "AD", gameObject, gameObject.name);
            }
        }
    }

    [PunRPC]
    public void HitSyncKey(string key)
    {
        //피격당한 건물의 HitMe 함수를 호출함
        if (TowersManager.towers[key] != null)
        {
            if (key.Contains("1") || key.Contains("2") || key.Contains("3"))
                TowersManager.towers[key].GetComponent<TowerBehaviour>().HitMe(stat.AttackDamage);
            else
                TowersManager.towers[key].GetComponent<SuppressorBehaviour>().HitMe(stat.AttackDamage);
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
    /// 다른 클라이언트에게 피격을 동기화시키기 위해 RPC를 돌리는 함수
    /// </summary>
    /// /// <param name="key">피격당한 오브젝트의 key값</param>
    public void HitRPC(string key)
    {
        this.photonView.RPC("HitSyncKey", PhotonTargets.Others, key);
    }

    /// <summary>
    /// 미니언의 값을 초기화시키는 함수. 새로 태어날 때와 죽었을 때 호출
    /// </summary>
    private void InitMinionStatus()
    {
        fog.ClearEnteties();
        TheAIPath.isStopped = false;
        stat.Hp = stat.MaxHp;
        minAtk.InitMinionStatus();

        if (minHP.makeProgress)
            minHP.hpbarOn();
    }
}