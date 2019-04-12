using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미니언이 정글에 들어왔다면 돌려보내는 스크립트
/// </summary>
public class MinionJoinJungle : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        //미니언이 정글에 들어오면 돌려보낸다.
        if (!other.isTrigger)
            if (other.tag.Equals("Minion"))
                other.GetComponent<MinionBehavior>().minAtk.RemoveNowTarget();
    }
}
