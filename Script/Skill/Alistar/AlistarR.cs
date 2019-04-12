using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 알리스타의 R 스킬에 들어있는 스크립트
/// </summary>
public class AlistarR : MonoBehaviour
{
    private Transform skillContainer;

    void Awake()
    {
        skillContainer = transform.parent;
    }

    /// <summary>
    /// 파티클이 끝날 때 호출되는 함수
    /// </summary>
    public void OnParticleSystemStopped()
    {
        gameObject.SetActive(false);
        transform.position = Vector3.zero;
        transform.parent = skillContainer;
    }
}
