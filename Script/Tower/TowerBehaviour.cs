using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타워 개체를 제어하는 스크립트
/// </summary>
public class TowerBehaviour : MonoBehaviour
{
    public StatClass.Stat towerStat;
    public TowerBehaviour myNextTower;
    public SuppressorBehaviour myNextSup;
    public AudioSource towerAudio;
    public GameObject towerDestroyEffect;
    public bool isCanAtkMe = false;
    public string team = "Red";
    public float HP;
    public float attackDamage;

    private TowerAtk myTowerAtk = null;
    private SystemMessage sysmsg;
    private AOSMouseCursor cursor;
    private TowerHP towerHP;
    private ParticleSystem destroyEffect;
    private bool isDead = false;
    private bool isFirstLoad = false;
    private bool isMouseChanged = false;
    private float defence = 55;

    private void Awake()
    {
        towerHP = transform.GetComponent<TowerHP>();

        if (!sysmsg)
            sysmsg = GameObject.FindGameObjectWithTag("SystemMsg").GetComponent<SystemMessage>();

        InitTowerStat();

        if (towerDestroyEffect)
        {
            towerDestroyEffect.SetActive(false);
            destroyEffect = towerDestroyEffect.GetComponent<ParticleSystem>();
        }

        InitTowerAudio();
    }

    private void OnEnable()
    {
        if (isFirstLoad)
            towerHP.BasicSetting();

        isFirstLoad = true;
        myTowerAtk = transform.GetComponentInChildren<TowerAtk>();

        if (!cursor)
            cursor = GameObject.FindGameObjectWithTag("MouseCursor").GetComponent<AOSMouseCursor>();
    }

    private void Start()
    {
        isFirstLoad = true;
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
    /// 타워의 체력과 체력바를 초기 설정하는 함수
    /// </summary>
    public void InitTowerHP()
    {
        towerStat.Hp = towerStat.MaxHp;
        towerHP.InitProgressBar();
    }

    /// <summary>
    /// 피격을 당했을 때의 처리를 담당하는 함수
    /// </summary>
    /// <param name="damage">공격자의 공격력</param>
    public bool HitMe(float damage = 0)
    {
        //이미 다른 개체에 피격당해 체력이 없다면 더 맞아봐야 소용없으니 리턴
        if (towerStat.Hp < 1)
            return false;

        //체력을 감소시킴
        towerStat.Hp -= damage;

        //사망한 경우
        if (towerStat.Hp < 1)
        {
            towerStat.Hp = 0;

            if (!isDead)
                IamDead(0.2f);

            isDead = true;

            if (team.ToLower().Equals("red"))
                GameObject.FindGameObjectWithTag("InGameManager").GetComponent<InGameManager>().blueTeamTowerKill++;
            else
                GameObject.FindGameObjectWithTag("InGameManager").GetComponent<InGameManager>().redTeamTowerKill++;
        }

        return isDead;
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
        InitTowerHP();

        if (myTowerAtk == null)
            myTowerAtk = transform.GetComponentInChildren<TowerAtk>();

        //공격 중이었다면 공격을 멈춘다
        myTowerAtk.StopAllCoroutines();
        myTowerAtk.nowTarget = null;

        //건물이 파괴되었다는 시스템 메시지를 띄운다
        if (PhotonNetwork.isMasterClient) 
        {
            if (team.ToLower().Equals("red")) 
            {
                sysmsg.Annoucement(8, false, "red"); 
                sysmsg.Annoucement(9, false, "blue"); 
            }
            else if (team.ToLower().Equals("blue"))
            {
                sysmsg.Annoucement(8, false, "blue");
                sysmsg.Annoucement(9, false, "red");
            }
        }

        if (myNextTower != null)
            myNextTower.isCanAtkMe = true;
        else if (myNextSup != null)
        {
            //만약 현재 타워가 쌍둥이 타워면 넥서스의 공격 가능 유무를 계산한다
            if (myNextSup.tag.Equals("Nexus"))
            {
                myNextSup.nexusAtkNum += 10;

                if (myNextSup.nexusAtkNum >= 21)
                    myNextSup.isCanAtkMe = true;
            }
            else
                myNextSup.isCanAtkMe = true;
        }

        towerDestroyEffect.SetActive(true);
        destroyEffect.Play();
        towerAudio.PlayOneShot(SoundManager.Instance.Building_Destroy);
        gameObject.SetActive(false);

        // 죽을 때 마우스 바뀐 상태면 원래대로 돌림
        if (isMouseChanged)
            cursor.SetCursor(cursor.PreCursor, Vector2.zero);
    }

    /// <summary>
    /// 음향의 초기 값을 설정하는 함수
    /// </summary>
    private void InitTowerAudio()
    {
        towerAudio = GetComponent<AudioSource>();
        towerAudio.minDistance = 1.0f;
        towerAudio.maxDistance = 15.0f;
        towerAudio.volume = 0.5f;
        towerAudio.spatialBlend = 0.6f;
        towerAudio.rolloffMode = AudioRolloffMode.Linear;
    }

    /// <summary>
    /// 건물의 초기 값을 설정하는 함수
    /// </summary>
    private void InitTowerStat()
    {
        towerStat = new StatClass.Stat();
        towerStat.Hp = HP;
        towerStat.MaxHp = HP;
        towerStat.AttackDamage = attackDamage;
        towerStat.AttackDef = defence;
        towerStat.AbilityDef = defence;
        towerStat.AttackSpeed = 0.83f;
        towerStat.Level = 1;
    }
}