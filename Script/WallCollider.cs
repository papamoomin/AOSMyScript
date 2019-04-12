using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 챔피언, 몬스터, 미니언이 지나가선 안되는 길을 막아줄 콜라이더를 담당하는 스크립트
/// </summary>
public class WallCollider : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        //미니언, 챔피언, 몬스터가 벽으로 밀쳐진다면 벽을 뚫고 지나가지 못하게 엮으로 밀어준다.
        if (other.gameObject.tag.Equals("Minion"))
        {
            MinionAtk minAtk = other.GetComponent<MinionBehavior>().minAtk;

            if (!minAtk.isPushing)
                return;

            minAtk.PushWall();
        }
        else if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
        {
            ChampionAtk champAtk = other.GetComponent<ChampionBehavior>().myChampAtk;

            if (!champAtk.isPushing)
                return;

            champAtk.PushWall();
        }
        else if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Monster")))
        {
            MonsterAtk monAtk = other.GetComponent<MonsterBehaviour>().monAtk;

            if (!monAtk.isPushing)
                return;

            monAtk.PushWall();
        }
    }
}
