using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

/// <summary>
/// 문도의 Q 스킬에 들어있는 스크립트
/// </summary>
public class MundoQ : MonoBehaviour
{
    public MundoSkill mySkill;

    private SystemMessage sysmsg;
    private bool isFirstAtk = true;
    private float distance = 20;

    void OnLevelWasLoaded(int level)
    {
        if (SceneManager.GetSceneByBuildIndex(level).name.Contains("InGame"))
            if (!sysmsg)
                sysmsg = GameObject.FindGameObjectWithTag("SystemMsg").GetComponent<SystemMessage>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 다른 챔피언과 충돌한 적 있는지 검사
        if (isFirstAtk)
        {
            bool isTrig = false;

            // 현재 충돌한 적에 따라 각각의 HitMe 함수를 호출하고 체력을 회복한다
            if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
            {
                if (other.gameObject.Equals(mySkill.gameObject))
                    return;

                ChampionBehavior champBehav = other.GetComponent<ChampionBehavior>();

                if (champBehav.team != mySkill.TheChampionBehaviour.team)
                {
                    isTrig = true;
                    float damage = CalculateDamage();

                    if (champBehav != null)
                    {
                        int x2 = 1;
                        ChampionSound.instance.PlayOtherFx(champBehav.GetComponentInChildren<AudioSource>(), ChampionSound.instance.Mundo_Q_Hit);
                        champBehav.HitMe(damage, "AP", mySkill.gameObject, mySkill.name);
                        mySkill.activeMundoQEffect(champBehav.transform.position);
                        mySkill.Heal(damage * x2);
                    }
                }
            }
            else if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Monster")))
            {
                MonsterBehaviour monBehav = other.GetComponent<MonsterBehaviour>();
                float damage = CalculateDamage();

                if (monBehav != null)
                {
                    isTrig = true;
                    int x2 = 1;

                    if (monBehav.HitMe(damage, "AP", mySkill.gameObject))
                    {
                        x2 = 2;
                        mySkill.TheChampionAtk.ResetTarget();
                    }

                    mySkill.Heal(damage * x2);
                }
            }
            else if (other.tag.Equals("Minion"))
            {
                MinionBehavior minBehav = other.GetComponent<MinionBehavior>();

                if (!other.name.Contains(mySkill.TheChampionBehaviour.team))
                {
                    isTrig = true;
                    float damage = CalculateDamage();

                    if (minBehav != null)
                    {
                        ChampionSound.instance.PlayOtherFx(minBehav.audio, ChampionSound.instance.Mundo_Q_Hit);
                        int x2 = 1;
                        minBehav.HitMe(damage, "AP", mySkill.gameObject);
                        mySkill.activeMundoQEffect(minBehav.transform.position);
                        mySkill.Heal(damage * x2);
                    }
                }
            }

            // 피격당한 게 확인되면 isFirstAtk을 꺼 다른 이에게 충돌할 일이 없도록 처리한다.
            if (isTrig)
            {
                gameObject.SetActive(false);
                isFirstAtk = false;
            }
        }
    }

    /// <summary>
    /// 데미지를 계산하는 함수
    /// </summary>
    /// <returns>계산된 데미지 값</returns>
    private float CalculateDamage()
    {
        return mySkill.skillData.qDamage[mySkill.TheChampionData.skill_Q - 1]
+ mySkill.Acalculate(mySkill.skillData.qAstat, mySkill.skillData.qAvalue, true);
    }

    /// <summary>
    /// 스킬 이펙트를 활성화시키는 함수
    /// </summary>
    /// <param name="dest">스킬이 날아갈 목적지</param>
    public void SkillOn(Vector3 dest)
    {
        isFirstAtk = true;
        transform.position = mySkill.transform.position;
        Vector3 directionVec = (dest - transform.position).normalized;
        ActiveFalse(0.5f);
        transform.DOMove(transform.position + (directionVec * distance), 0.5f);
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
    /// 일정 시간이 지난 후 활성 상태를 꺼주는 함수
    /// </summary>
    /// <param name="time">몇 초 뒤에 꺼질 것인가</param>
    public void ActiveFalse(float time)
    {
        Invoke("_ActiveFalse", time);
    }

    /// <summary>
    /// 활성 상태를 꺼주는 함수
    /// </summary>
    private void _ActiveFalse()
    {
        gameObject.SetActive(false);
    }
}