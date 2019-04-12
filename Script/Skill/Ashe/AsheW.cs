using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 애쉬의 W 스킬에 들어있는 스크립트
/// </summary>
public class AsheW : MonoBehaviour
{
    public AsheSkill mySkill;

    private SystemMessage sysmsg;
    private bool isFirstAtk = true;

    void OnLevelWasLoaded(int level)
    {
        if (UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(level).name.Contains("InGame"))
            if (!sysmsg)
                sysmsg = GameObject.FindGameObjectWithTag("SystemMsg").GetComponent<SystemMessage>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 다른 챔피언과 충돌한 적 있는지 검사
        if (isFirstAtk)
        {
            bool isTrig = false;

            // 현재 충돌한 적에 따라 각각의 HitMe 함수를 호출한다
            if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
            {
                if (!other.gameObject.Equals(mySkill.gameObject))
                {
                    ChampionBehavior champBehav = other.GetComponent<ChampionBehavior>();

                    if (champBehav.team != mySkill.TheChampionBehaviour.team)
                    {
                        isTrig = true;
                        float damage = CalculateDamage();

                        if (champBehav != null)
                            champBehav.HitMe(damage, "AD", mySkill.gameObject, mySkill.name);
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

                    if (monBehav.HitMe(damage, "AD", mySkill.gameObject))
                        mySkill.TheChampionAtk.ResetTarget();
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
                        minBehav.HitMe(damage, "AD", mySkill.gameObject);
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
    /// 스킬 이펙트를 활성화시키는 함수
    /// </summary>
    /// <param name="dest">스킬이 날아갈 목적지</param>
    public void SkillOn(Vector3 dest)
    {
        isFirstAtk = true;
        transform.position = mySkill.transform.position;
        ActiveFalse(0.5f);
        transform.DOMove(dest, 0.5f);
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

    /// <summary>
    /// 데미지를 계산하는 함수
    /// </summary>
    /// <returns>계산된 데미지 값</returns>
    private float CalculateDamage()
    {
        return mySkill.skillData.wDamage[mySkill.TheChampionData.skill_W - 1]
+ mySkill.Acalculate(mySkill.skillData.wAstat, mySkill.skillData.wAvalue, true);
    }
}