using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 알리스타의 Q 스킬에 들어있는 스크립트
/// </summary>
public class AlistarQ : MonoBehaviour
{
    public AlistarSkill mySkill;

    private SphereCollider myCollider;
    private SystemMessage sysmsg;
    private bool isNowMake = true;
    private int upPower = 150;
    private float skillRange = 0;

    void OnLevelWasLoaded(int level)
    {
        if (UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(level).name.Contains("InGame"))
            if (!sysmsg)
                sysmsg = GameObject.FindGameObjectWithTag("SystemMsg").GetComponent<SystemMessage>();
    }

    private void Awake()
    {
        myCollider = GetComponent<SphereCollider>();
        myCollider.enabled = false;
        skillRange = myCollider.radius;
    }

    private void OnEnable()
    {
        // 풀링할 때 스킬 발동이 될 필요는 없으니 분리해서 처리
        if (isNowMake)
            isNowMake = false;
        else
        {
            myCollider.enabled = true;
            Invoke("OffCollider", 0.2f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //충돌한 적의 종류에 따라 각각의 HitMe 함수를 호출한다
        if (other.tag.Equals("Minion"))
        {
            MinionBehavior minBehav = other.GetComponent<MinionBehavior>();

            if (!other.gameObject.name.Contains(mySkill.TheChampionBehaviour.team))
            {
                minBehav.minAtk.PauseAtk(1f, true);
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
                champBehav.myChampAtk.PauseAtk(1f, true);
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
            monBehav.monAtk.PauseAtk(1f, true);
            float damage = CalculateDamage();

            if (monBehav != null)
            {
                int viewID = monBehav.GetComponent<PhotonView>().viewID;
                monBehav.HitMe(damage, "AP", mySkill.gameObject);
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
+ mySkill.Acalculate(mySkill.skillData.qAstat, mySkill.skillData.qAvalue);
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
    /// 콜라이더를 꺼주는 함수
    /// </summary>
    private void OffCollider()
    {
        myCollider.enabled = false;
    }
}