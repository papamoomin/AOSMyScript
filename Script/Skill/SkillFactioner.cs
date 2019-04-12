using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬을 사용한 챔피언이 부쉬에 있을 때 적에게 스킬이 보이는지 유무를 설정하는 스크립트
/// </summary>
public class SkillFactioner : MonoBehaviour
{
    public FogOfWarEntity skillChampFogEntity;
    public FogOfWarEntity myFogEntity;

    private void OnEnable()
    {
        if (skillChampFogEntity != null)
            myFogEntity.SetSameTeam(skillChampFogEntity);
    }

    void Update()
    {
        //챔피언이 보이느냐의 유무와 스킬이 보이느냐의 유무를 일치시킴
        myFogEntity.isInTheBush = skillChampFogEntity.isInTheBush;
        myFogEntity.isInTheBushMyEnemyToo = skillChampFogEntity.isInTheBushMyEnemyToo;
    }
}