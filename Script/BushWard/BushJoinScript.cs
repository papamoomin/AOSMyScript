using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> 
/// 부쉬에 들어갔을 때 아군과 적팀을 구분하고, 시야의 유무를 판단하는 스크립트. 
/// </summary>
public class BushJoinScript : MonoBehaviour
{
    private List<GameObject> playerTeamList = new List<GameObject>();
    private List<GameObject> enemyTeamList = new List<GameObject>();
    private string playerTeam;
    private string enemyTeam;

    private void Start()
    {
        SetPlayerTeam();
    }

    private void OnTriggerEnter(Collider other)
    {
        string team = "";

        //챔피언일 때 팀 저장
        if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Champion"))) 
            team = other.gameObject.GetComponent<ChampionBehavior>().team;

        //미니언일때 팀 저장
        if (other.tag.Equals("Minion")) 
        {
            if (other.gameObject.name.Contains("Blue"))
                team = "Blue";
            else if (other.gameObject.name.Contains("Red"))
                team = "Red";
        }

        //와드일 때 팀 저장
        if (other.tag.Equals("Ward")) 
            team = other.gameObject.GetComponent<Ward>().team;

        //위 세 가지 경우가 아니면 신경 안써도 되는 개체니까 리턴한다.
        if (team.Equals("")) 
            return;

        //아군이 들어왔다.
        if (team.Equals(playerTeam)) 
        {
            FogOfWarEntity fogEntity = other.GetComponent<FogOfWarEntity>();

            //적팀이 있는 경우 적이 있는가를 나타내는 변수를 true로 한다.
            if (enemyTeamList.Count > 0) 
                fogEntity.isInTheBushMyEnemyToo = true;

            //아군이 원래 이 부시에 없었다.
            if (playerTeamList.Count < 1) 
            {
                //적들에게도 자신의 적이 있는가 나타내는 변수를 true로 한다.
                for (int i = 0; i < enemyTeamList.Count; ++i) 
                {
                    FogOfWarEntity enemyFogEntity = enemyTeamList[i].GetComponent<FogOfWarEntity>();
                    enemyFogEntity.isInTheBushMyEnemyToo = true;
                    enemyFogEntity.Check();
                }
            }

            playerTeamList.Add(other.gameObject);
            fogEntity.isInTheBush = true;
        }
        else if (team.Equals(enemyTeam))
        { //적팀이 들어왔다.
            FogOfWarEntity fogEntity = other.GetComponent<FogOfWarEntity>();

            //아군이 있는 경우 적팀에게 아군이 있는가를 체크한다.
            if (playerTeamList.Count > 0) 
                fogEntity.isInTheBushMyEnemyToo = true;

            //적팀이 원래 이 부시에 없었다.
            if (enemyTeamList.Count < 1) 
            {
                //아군에게도 적팀이 있는가를 체크한다.
                for (int i = 0; i < playerTeamList.Count; ++i) 
                {
                    FogOfWarEntity nowFogEntity = playerTeamList[i].GetComponent<FogOfWarEntity>();
                    nowFogEntity.isInTheBushMyEnemyToo = true;
                    nowFogEntity.Check();
                }
            }

            enemyTeamList.Add(other.gameObject);
            other.GetComponent<FogOfWarEntity>().isInTheBush = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        string team = "";

        //챔피언일 때 팀 저장
        if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Champion"))) 
            team = other.gameObject.GetComponent<ChampionBehavior>().team;

        //미니언일 때 팀 저장
        if (other.tag.Equals("Minion")) 
        {
            if (other.gameObject.name.Contains("Blue"))
                team = "Blue";
            else if (other.gameObject.name.Contains("Red"))
                team = "Red";
        }

        //와드일 때 팀 저장
        if (other.tag.Equals("Ward")) 
            team = other.gameObject.GetComponent<Ward>().team;

        //위 세 가지 경우가 아니면 신경 안써도 되는 개체니까 리턴한다.
        if (team.Equals("")) 
            return;

        //아군이 나갔다
        if (team.Equals(playerTeam)) 
        {
            FogOfWarEntity fogEntity = other.GetComponent<FogOfWarEntity>();
            fogEntity.isInTheBush = false;
            fogEntity.isInTheBushMyEnemyToo = false;
            fogEntity.isInTheBush = false;
            playerTeamList.Remove(other.gameObject);

            //아군이 부쉬에 아무도 남지 않았는지 확인 후, 아군이 남지 않았다면 적팀에게 아군이 없음을 체크한다.
            if (playerTeamList.Count < 1) 
                for (int i = 0; i < enemyTeamList.Count; ++i) 
                    enemyTeamList[i].GetComponent<FogOfWarEntity>().isInTheBushMyEnemyToo = false;
            
        }
        else if (team.Equals(enemyTeam))
        {//적팀이 나갔다
            FogOfWarEntity fogEntity = other.GetComponent<FogOfWarEntity>();
            fogEntity.isInTheBush = false;
            fogEntity.isInTheBushMyEnemyToo = false;
            fogEntity.isInTheBush = false;
            enemyTeamList.Remove(other.gameObject);

            //적팀이 부쉬에 아무도 남지 않았는지 확인 후, 적팀이 남지 않았다면 아군에게 적팀이 없음을 체크한다.
            if (enemyTeamList.Count < 1) 
                for (int i = 0; i < playerTeamList.Count; ++i) 
                    playerTeamList[i].GetComponent<FogOfWarEntity>().isInTheBushMyEnemyToo = false;
        }
    }

    /// <summary> 플레이어가 어느팀이냐에 따라 아군과 적팀을 구분하는 함수 </summary>
    private void SetPlayerTeam()
    {
        playerTeam = PhotonNetwork.player.GetTeam().ToString();

        if (playerTeam.Contains("red"))
        {
            playerTeam = "Red";
            enemyTeam = "Blue";
        }
        else if (playerTeam.Contains("blue"))
        {
            playerTeam = "Blue";
            enemyTeam = "Red";
        }
        else
        {// 예외 처리
            playerTeam = "Red";
            enemyTeam = "Blue";
        }
    }
}