using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 알리스타의 E 스킬에 들어있는 스크립트
/// </summary>
public class AlistarE : MonoBehaviour
{
    public AlistarSkill mySkill;

    private SystemMessage sysmsg;

    private void OnTriggerEnter(Collider other)
    {
        //충돌한 적의 종류에 따라 각각의 HitMe 함수를 호출한다
        if (other.tag.Equals("Minion"))
        {
            MinionBehavior minBehav = other.GetComponent<MinionBehavior>();

            if (!other.gameObject.name.Contains(mySkill.TheChampionBehaviour.team))
            {
                float damage = CalculateDamage();

                if (minBehav != null)
                {
                    int viewID = minBehav.GetComponent<PhotonView>().viewID;
                    minBehav.HitMe(damage, "AP", mySkill.gameObject);
                }
            }
        }
        else if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
        {
            ChampionBehavior champBehav = other.GetComponent<ChampionBehavior>();

            if (champBehav.team != mySkill.TheChampionBehaviour.team)
            {
                float damage = CalculateDamage();

                if (champBehav != null)
                {
                    int viewID = champBehav.GetComponent<PhotonView>().viewID;
                    champBehav.HitMe(damage, "AP", mySkill.gameObject, mySkill.name);
                }
            }
        }
        else if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Monster")))
        {
            MonsterBehaviour monBehav = other.GetComponent<MonsterBehaviour>();
            float damage = CalculateDamage();

            if (monBehav != null)
            {
                int viewID = monBehav.GetComponent<PhotonView>().viewID;
                monBehav.HitMe(damage, "AP", mySkill.gameObject);
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
    /// 데미지를 계산하는 함수
    /// </summary>
    /// <returns>계산된 데미지 값</returns>
    private float CalculateDamage()
    {
        return (mySkill.skillData.eDamage[mySkill.TheChampionData.skill_E - 1]
                + mySkill.Acalculate(mySkill.skillData.eAstat, mySkill.skillData.eAvalue)) / 10f;
    }
}