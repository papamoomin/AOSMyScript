using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 애쉬의 E 스킬에서 매가 사라진 이후의 시야를 담당하는 스크립트
/// </summary>
public class AsheEHawkWard : MonoBehaviour
{
    private void OnEnable()
    {
        Invoke("ActiveFalse", 5f);
    }

    /// <summary>
    /// 시야의 활성 상태를 꺼주는 함수
    /// </summary>
    private void ActiveFalse()
    {
        gameObject.SetActive(false);
    }
}
