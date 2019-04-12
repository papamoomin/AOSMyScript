using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 문도의 W 스킬에 들어있는 스크립트
/// </summary>
public class MundoW : MonoBehaviour
{
    public MundoSkill mySkill;

    private new AudioSource audio;
    private SystemMessage sysmsg;
    private List<GameObject> enemyList = new List<GameObject>();
    private Stack<GameObject> enemyDeleteStack = new Stack<GameObject>();
    private bool isSoundDelayForInit;
    private float damageTime = 0.5f;

    void OnLevelWasLoaded(int level)
    {
        if (SceneManager.GetSceneByBuildIndex(level).name.Contains("InGame"))
        {
            if (!sysmsg)
                sysmsg = GameObject.FindGameObjectWithTag("SystemMsg").GetComponent<SystemMessage>();

            audio = transform.parent.GetComponentInChildren<AudioSource>();
            Invoke("SoundDelay", 7f);
        }
    }

    private void OnEnable()
    {
        enemyList.Clear();
        damageTime = 0.5f;

        if(isSoundDelayForInit)
        {
            if (audio == null)
                audio = transform.parent.GetComponentInChildren<AudioSource>();

            audio.loop = true;
            audio.clip = ChampionSound.instance.Mundo_W;
            audio.Play();
        }
    }

    private void Update()
    {
        damageTime -= Time.deltaTime;

        // 0.5초마다 반경 내의 적에게 데미지를 준다
        if (damageTime <= 0)
        {
            damageTime = 0.5f;

            for (int i = 0; i < enemyList.Count; ++i)
            {
                if (enemyList[i].Equals(mySkill.gameObject))
                    continue;

                float damage = mySkill.skillData.wDamage[mySkill.TheChampionData.skill_W - 1]
+ mySkill.Acalculate(mySkill.skillData.wAstat, mySkill.skillData.wAvalue, true);

                // 현재 충돌한 적에 따라 각각의 HitMe 함수를 호출한다
                if (enemyList[i].layer.Equals(LayerMask.NameToLayer("Champion")))
                {
                    ChampionBehavior champBehav = enemyList[i].GetComponent<ChampionBehavior>();

                    if (champBehav.team != mySkill.TheChampionBehaviour.team)
                    {
                        if (champBehav != null)
                        {
                            champBehav.HitMe(damage, "AP", mySkill.gameObject, mySkill.name);
                            
                            if (champBehav.myChampionData.totalStat.Hp <= 0)
                                enemyDeleteStack.Push(enemyList[i]);
                        }
                    }
                }
                else if (enemyList[i].layer.Equals(LayerMask.NameToLayer("Monster")))
                {
                    MonsterBehaviour monBehav = enemyList[i].GetComponent<MonsterBehaviour>();

                    if (monBehav != null)
                    {
                        if (monBehav.HitMe(damage, "AP", mySkill.gameObject))
                        {
                            mySkill.TheChampionAtk.ResetTarget();
                            enemyDeleteStack.Push(enemyList[i]);
                        }

                        if (monBehav.stat.Hp <= 0)
                            enemyDeleteStack.Push(enemyList[i]);
                    }
                }
                else if (enemyList[i].tag.Equals("Minion"))
                {
                    MinionBehavior minBehav = enemyList[i].GetComponent<MinionBehavior>();

                    if (!enemyList[i].name.Contains(mySkill.TheChampionBehaviour.team))
                    {
                        if (minBehav != null)
                        {
                            minBehav.HitMe(damage, "AP", mySkill.gameObject);

                            if (minBehav.stat.Hp <= 0)
                                enemyDeleteStack.Push(enemyList[i]);
                        }
                    }
                }

                // 죽은 적은 데미지를 입히는 대상에서 제외시킨다
                while (enemyDeleteStack.Count > 0)
                {
                    GameObject deletingEnemyObj = enemyDeleteStack.Pop();

                    if (enemyList.Contains(deletingEnemyObj))
                        enemyList.Remove(deletingEnemyObj);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        bool isTrig = false;

        if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
            isTrig = true;
        else if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Monster")))
            isTrig = true;
        else if (other.tag.Equals("Minion"))
            isTrig = true;

        // 적이 공격 반경에 들어온 경우 적 리스트에 추가시킨다
        if (isTrig)
            if (!enemyList.Contains(other.gameObject))
                enemyList.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        // 적이 공격 반경에서 나간 경우 적 리스트에서 제외시킨다
        if (enemyList.Contains(other.gameObject))
            enemyList.Remove(other.gameObject);
    }

    void OnDisable()
    {
        if (audio != null)
        {
            audio.loop = false;
            audio.clip = null;
            audio.Stop();
        }
    }

    /// <summary>
    /// 파티클이 끝날 때 호출되는 함수
    /// </summary>
    public void OnParticleSystemStopped()
    {
        gameObject.SetActive(false);
        transform.position = Vector3.zero;
    }

    /// <summary>
    /// 효과음 재생에 딜레이를 주는 함수
    /// </summary>
    private void SoundDelay()
    {
        isSoundDelayForInit = true;
    }
}