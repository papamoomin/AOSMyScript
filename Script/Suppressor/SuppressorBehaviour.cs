using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 넥서스와 억제기 개체를 제어하는 스크립트
/// </summary>
public class SuppressorBehaviour : MonoBehaviour
{
    public StatClass.Stat towerStat;
    public SuppressorBehaviour myNext;
    public TowerBehaviour myNextTower1;
    public TowerBehaviour myNextTower2;
    public GameObject destroyEffect3;
    public bool isNexus = false;
    public bool isCanAtkMe = false;
    public bool isBomb = false;
    public string team = "Red";
    public int nexusAtkNum = 0;

    private SystemMessage sysmsg;
    private AOSMouseCursor cursor;
    private SuppressorRevive TheSupRevive;
    private new AudioSource audio;
    private SupHP supHp;
    private GameObject destroyEffect;
    private GameObject destroyEffect2;
    private GameObject crystal;
    private GameObject stone;
    private bool isDead = false;
    private float HP = 3300;
    private float defence = 55;

    private void Awake()
    {
        if (!this.gameObject.name.Contains("Sup_Container"))
        {
            destroyEffect = transform.GetChild(transform.childCount - 2).gameObject;
            destroyEffect2 = transform.GetChild(transform.childCount - 1).gameObject;
        }

        if (!sysmsg)
            sysmsg = GameObject.FindGameObjectWithTag("SystemMsg").GetComponent<SystemMessage>();

        //기본 설정
        InitTowerStat();
        InitAudio();
        stone = transform.GetChild(0).gameObject;
        crystal = transform.GetChild(1).gameObject;
        supHp = GetComponent<SupHP>();
    }

    private void OnMouseOver()
    {
        if (team.ToString().ToLower().Equals(PhotonNetwork.player.GetTeam().ToString().ToLower()))
            cursor.SetCursor(1, Vector2.zero);
        else if (!team.ToString().ToLower().Equals(PhotonNetwork.player.GetTeam().ToString().ToLower()))
            cursor.SetCursor(2, Vector2.zero);
    }

    private void OnMouseExit()
    {
        cursor.SetCursor(cursor.PreCursor, Vector2.zero);
    }

    private void OnEnable()
    {
        //억제기가 리스폰되는 경우
        if (!isNexus)
        {
            //넥서스에 할당된 숫자에서 1 빼준다. nexusAtkNum의 값으로 넥서스가 공격 가능한 지 판단
            int num = (myNext.nexusAtkNum % 10);

            if (num > 0)
            {
                if (num == 1)
                {
                    if (myNextTower1.gameObject.activeInHierarchy)
                        myNextTower1.isCanAtkMe = false;

                    if (myNextTower2.gameObject.activeInHierarchy)
                        myNextTower2.isCanAtkMe = false;
                }

                myNext.nexusAtkNum -= 1;
            }
        }

        towerStat.Hp = towerStat.MaxHp;
        HP = towerStat.Hp;
        isDead = false;

        if (!cursor)
            cursor = GameObject.FindGameObjectWithTag("MouseCursor").GetComponent<AOSMouseCursor>();
    }

    private void Update()
    {
        // 넥서스가 파괴됨
        if (isBomb && isNexus && gameObject.activeInHierarchy)
        {
            HitMe(10000);
            isBomb = false;
        }
    }
    
    /// <summary>
    /// 건물의 초기 값을 설정하는 함수
    /// </summary>
    private void InitTowerStat()
    {
        towerStat = new StatClass.Stat();
        towerStat.Hp = HP;
        towerStat.MaxHp = HP;
        towerStat.AttackDef = defence;
        towerStat.AbilityDef = defence;
        towerStat.AttackSpeed = 0.83f;
        towerStat.Level = 1;
    }

    /// <summary>
    /// 음향의 초기 값을 설정하는 함수
    /// </summary>
    private void InitAudio()
    {
        audio = GetComponentInParent<AudioSource>();

        if (!audio)
            audio = gameObject.AddComponent<AudioSource>();

        audio.maxDistance = 20;
        audio.volume = 0.5f;
    }

    /// <summary>
    /// 피격을 당했을 때의 처리를 담당하는 함수
    /// </summary>
    /// <param name="damage">공격자의 공격력</param>
    public bool HitMe(float damage = 0)
    {
        if (towerStat.Hp < 1)
            return false;

        towerStat.Hp -= damage;

        if (towerStat.Hp < 1)
        {
            towerStat.Hp = 0;

            if (!isDead)
                IamDead(0.2f);

            isDead = true;
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
        //넥서스가 터진 경우 승리나 패배를 띄워준다.
        if (isNexus)
        {
            SoundManager.Instance.PlaySound(SoundManager.Instance.Nexus_Destroy);
            destroyEffect.SetActive(true);
            destroyEffect.GetComponent<ParticleSystem>().Play();
            crystal.GetComponent<Crystal>().isdead = true;
            crystal.transform.DOMoveY(-10f, 2.8f);
            Invoke("DelayingDestroyNexus", 5f);
            return;
        }
        else
        {//억제기가 터진 경우 넥서스에 AtkNum을 더한다. 
            myNext.nexusAtkNum += 1;

            //본진의 쌍둥이 타워가 터지면 10, 억제기가 터지면 1을 더해 
            //억제기가 하나 이상, 쌍둥이 타워가 모두 터진 상황이 되면 넥서스를 공격 가능하게 둔다
            if (myNext.nexusAtkNum >= 21)
                myNext.isCanAtkMe = true;

            SoundManager.Instance.EnvironmentFx(audio, SoundManager.Instance.Building_Destroy);

            if (destroyEffect3)
                destroyEffect3.GetComponent<ParticleSystem>().Play();

            //억제기가 하나라도 깨졌다면 쌍둥이 타워를 공격 가능하게 둔다
            if (myNext.nexusAtkNum % 10 > 0)
            {
                if (myNextTower1.gameObject.activeInHierarchy)
                    myNextTower1.isCanAtkMe = true;
                
                if (myNextTower2.gameObject.activeInHierarchy)
                    myNextTower2.isCanAtkMe = true;
            }

            if (TheSupRevive == null)
                TheSupRevive = transform.parent.GetComponent<SuppressorRevive>();

            //일정 시간이 지났다면 억제기를 부활시킨다
            TheSupRevive.WillRevive();

            //건물이 파괴되었다는 시스템 메시지를 띄운다
            if (PhotonNetwork.isMasterClient)
            {
                if (team.ToLower().Contains("red"))
                {
                    sysmsg.Annoucement(11, false, "red");
                    sysmsg.Annoucement(10, false, "blue");
                }
                else
                {
                    sysmsg.Annoucement(11, false, "blue");
                    sysmsg.Annoucement(10, false, "red");
                }
            }
        }

        //억제기의 체력 바를 끈다
        supHp.HpbarOff();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 넥서스 파괴 시 승리, 패배를 띄워주는 함수
    /// </summary>
    private void DelayingDestroyNexus()
    {
        if (team.Equals("red") || team.Equals("Red"))
        {
            if (PhotonNetwork.player.GetTeam().ToString().Equals("red"))
                sysmsg.GameEndUI(true);
            else
                sysmsg.GameEndUI(false);
        }
        else if (team.Equals("blue") || team.Equals("Blue"))
        {
            if (PhotonNetwork.player.GetTeam().ToString().Equals("blue"))
                sysmsg.GameEndUI(true);
            else
                sysmsg.GameEndUI(false);
        }

        audio.PlayOneShot(SoundManager.Instance.Nexus_Destroy);
        gameObject.SetActive(false);
    }
}