using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미니언이 리스폰되는 위치에 다른 미니언이 있는지를 판단, 처리를 맡은 콜라이더를 담당하는 스크립트
/// </summary>
public class RespownCollider : MonoBehaviour
{
    public bool isTrigger = false;

    private List<GameObject> triggerList = new List<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        // 미니언들이 가진 RespownChecker가 들어와있는지 확인. 들어와있으면 현재 이 위치에 다른 미니언이 있다고 표시를 해준다.
        if (other.tag.Equals("RespownChecker"))
        {
            triggerList.Add(other.gameObject);

            if (!isTrigger)
                isTrigger = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 미니언이 영역 밖으로 나갔는지 확인
        if (other.tag.Equals("RespownChecker"))
        {
            if (triggerList.Contains(other.gameObject))
                triggerList.Remove(other.gameObject);

            if (triggerList.Count < 1)
                isTrigger = false;
        }
    }
}
