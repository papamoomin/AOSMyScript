using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 애쉬의 E 스킬에 들어있는 스크립트
/// </summary>
public class AsheE : MonoBehaviour
{
    public AsheSkill mySkill;
    public FogOfWarEntity myFogEntity;
    public GameObject hawkWard;

    private void OnEnable()
    {
        if (mySkill.TheChampionBehaviour.team.Equals("Red"))
        {
            myFogEntity.faction = FogOfWar.Players.Player00;
            hawkWard.GetComponent<FogOfWarEntity>().faction = FogOfWar.Players.Player00;
        }
        else if (mySkill.TheChampionBehaviour.team.Equals("Blue"))
        {
            myFogEntity.faction = FogOfWar.Players.Player01;
            hawkWard.GetComponent<FogOfWarEntity>().faction = FogOfWar.Players.Player01;
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
    /// 매를 목표 지점으로 이동시키는 함수
    /// </summary>
    /// <param name="dest">목표로 하는 목적지</param>
    public void SkillOn(Vector3 dest)
    {
        transform.position = mySkill.transform.position;
        float length = Vector3.Distance(dest, transform.position);
        float time = length * 0.04f;
        ActiveFalse(dest, time);
        transform.DOMove(dest, time);
    }

    /// <summary>
    /// 매가 목적지에 도착할 시간에 매의 활성 상태를 꺼주고, 그 위치의 시야를 켜는 함수
    /// </summary>
    /// <param name="dest">매가 도착할 목적지</param>
    /// <param name="time">매가 도착할 때까지 걸리는 시간</param>
    public void ActiveFalse(Vector3 dest, float time)
    {
        dest.y = 1.5f;
        hawkWard.transform.position = dest;
        hawkWard.SetActive(false);
        Invoke("SetHawkWard", time);
        Invoke("_ActiveFalse", time + 0.1f);
    }

    /// <summary>
    /// 매의 활성 상태를 꺼주는 함수
    /// </summary>
    private void _ActiveFalse()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 매가 도착하는 부분에 시야를 가진 오브젝트를 활성화시키는 함수
    /// </summary>
    private void SetHawkWard()
    {
        hawkWard.SetActive(true);
    }
}
