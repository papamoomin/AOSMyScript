using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

/// <summary>
/// 웨이포인트 자신의 영역에 미니언이 들어왔을 시 미니언의 이동 목표를 다음 웨이포인트로 바꿔주는 스크립트
/// </summary>
public class MinionWaypoint : MonoBehaviour
{
    public GameObject RedPoint;
    public GameObject BluePoint;

    private void OnTriggerEnter(Collider other)
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        //웨이포인트에 진입한게 미니언인지 확인
        if (other.tag.Equals("Minion"))
        {
            MinionAtk min = other.GetComponent<MinionBehavior>().minAtk;

            //미니언의 타겟이 웨이포인트인 경우, 각 팀에 따라 해당하는 웨이포인트로 이동 목표를 바꿔준다.
            if (other.name.Contains("Blue"))
            {
                bool isArriveWaypoint = false;

                if (min.nowTarget == null)
                    isArriveWaypoint = true;
                else if (min.nowTarget.tag.Equals("WayPoint"))
                    isArriveWaypoint = true;

                if (isArriveWaypoint)
                {
                    min.nowTarget = RedPoint;
                    other.GetComponent<AIDestinationSetter>().target = RedPoint.transform;
                }

                min.moveTarget = RedPoint;
            }
            else if (other.name.Contains("Red"))
            {
                bool isArriveWaypoint = false;
                
                if (min.nowTarget == null)
                    isArriveWaypoint = true;
                else if (min.nowTarget.tag.Equals("WayPoint"))
                    isArriveWaypoint = true;

                if (isArriveWaypoint)
                {
                    min.nowTarget = BluePoint;
                    other.GetComponent<AIDestinationSetter>().target = BluePoint.transform;
                }

                min.moveTarget = BluePoint;
            }
        }
    }
}