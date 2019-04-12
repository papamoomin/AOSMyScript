using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전장의 모든 타워를 총괄, 연결하는 스크립트
/// </summary>
public class TowersManager : MonoBehaviour
{
    public static Dictionary<string, GameObject> towers = new Dictionary<string, GameObject>();
    public GameObject[] redTowersArray;
    public GameObject[] blueTowersArray;

    private void Start()
    {
        //타워를 딕셔너리에 모두 묶어 저장하여 다른 스크립트에서 사용하기 용이하도록 한다
        towers.Clear();

        towers.Add("rt1", redTowersArray[0]);
        towers.Add("rt2", redTowersArray[1]);
        towers.Add("rt3", redTowersArray[2]);
        towers.Add("rm1", redTowersArray[3]);
        towers.Add("rm2", redTowersArray[4]);
        towers.Add("rm3", redTowersArray[5]);
        towers.Add("rb1", redTowersArray[6]);
        towers.Add("rb2", redTowersArray[7]);
        towers.Add("rb3", redTowersArray[8]);
        towers.Add("rc1", redTowersArray[9]);
        towers.Add("rc2", redTowersArray[10]);
        towers.Add("rts", redTowersArray[11]);
        towers.Add("rms", redTowersArray[12]);
        towers.Add("rbs", redTowersArray[13]);
        towers.Add("r", redTowersArray[14]);

        towers.Add("bt1", blueTowersArray[0]);
        towers.Add("bt2", blueTowersArray[1]);
        towers.Add("bt3", blueTowersArray[2]);
        towers.Add("bm1", blueTowersArray[3]);
        towers.Add("bm2", blueTowersArray[4]);
        towers.Add("bm3", blueTowersArray[5]);
        towers.Add("bb1", blueTowersArray[6]);
        towers.Add("bb2", blueTowersArray[7]);
        towers.Add("bb3", blueTowersArray[8]);
        towers.Add("bc1", blueTowersArray[9]);
        towers.Add("bc2", blueTowersArray[10]);
        towers.Add("bts", blueTowersArray[11]);
        towers.Add("bms", blueTowersArray[12]);
        towers.Add("bbs", blueTowersArray[13]);
        towers.Add("b", blueTowersArray[14]);
    }
}