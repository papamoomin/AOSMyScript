using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 애쉬의 R 스킬에 들어있는 스크립트
/// </summary>
public class AsheR : MonoBehaviour
{
    public AsheSkill mySkill;

    private SystemMessage sysmsg;
    private Vector3 shootVec;
    private bool isFirstAtk = true;

    void OnLevelWasLoaded(int level)
    {
        if (UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(level).name.Contains("InGame"))
            if (!sysmsg)
                sysmsg = GameObject.FindGameObjectWithTag("SystemMsg").GetComponent<SystemMessage>();
    }

    private void Update()
    {
        //맵에서 플레이어들의 시야가 머물 수 있는 거리 밖으로 날아가면 활성 상태를 꺼준다.
        if (transform.position.x < -10 || transform.position.x > 285 || transform.position.z < -10 || transform.position.z > 285)
            gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 다른 챔피언과 충돌한 적 있는지 검사
        if (isFirstAtk)
        {
            bool isTrig = false;

            // 현재 충돌한 콜라이더가 챔피언임을 확인
            if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
            {
                if (other.gameObject.Equals(mySkill.gameObject))
                    return;

                ChampionBehavior champBehav = other.GetComponent<ChampionBehavior>();

                if (champBehav.team != mySkill.TheChampionBehaviour.team)
                {
                    isTrig = true;
                    float damage = mySkill.skillData.rDamage[mySkill.TheChampionData.skill_R - 1]
    + mySkill.Acalculate(mySkill.skillData.rAstat, mySkill.skillData.rAvalue, true);

                    // 피격당한 챔피언의 HitMe 함수를 호출하고 스턴 상태이상을 건다
                    if (champBehav != null)
                    {
                        champBehav.HitMe(damage, "AP", mySkill.gameObject, mySkill.name);
                        champBehav.myChampAtk.PauseAtk(3.5f, true);
                        champBehav.myChampAtk.StunEffectToggle(true, 0);
                        champBehav.myChampAtk.StunEffectToggle(false, 3.5f);
                        Collider[] cols = Physics.OverlapSphere(champBehav.transform.position, 12);

                        // 피격당한 챔피언 주변의 챔피언들에게도 데미지를 입힌다
                        foreach (Collider c in cols)
                        {
                            if (c.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
                            {
                                ChampionBehavior colChampBehav = c.GetComponent<ChampionBehavior>();

                                if (colChampBehav != null)
                                    if (colChampBehav.team != mySkill.TheChampionBehaviour.team)
                                        colChampBehav.HitMe(damage / 2f, "AP", mySkill.gameObject, mySkill.name);
                            }
                        }
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
    /// 파티클이 끝날 때 호출되는 함수
    /// </summary>
    public void OnParticleSystemStopped()
    {
        gameObject.SetActive(false);
        transform.position = Vector3.zero;
    }

    /// <summary>
    /// 스킬 이펙트를 활성화시키는 함수
    /// </summary>
    /// <param name="dest">스킬이 날아갈 목적지</param>
    public void SkillOn(Vector3 dest)
    {
        isFirstAtk = true;
        transform.position = mySkill.transform.position;
        shootVec = transform.position;
        Vector3 realDestVec = (Vector3.Normalize(dest - shootVec) * 400) + shootVec;
        float time = 400 * 0.04f;
        ActiveFalse(time);
        transform.DOMove(realDestVec, time);
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