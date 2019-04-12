using System.Collections;
using UnityEngine;

/// <summary>
/// 억제기의 리스폰을 담당하는 스크립트
/// </summary>
public class SuppressorRevive : MonoBehaviour
{
    private GameObject mySon;
    private SystemMessage sysMsg;
    private SupHP suphp;

    private void Awake()
    {
        mySon = transform.GetChild(0).gameObject;
        sysMsg = GameObject.FindGameObjectWithTag("SystemMsg").GetComponent<SystemMessage>();

        if (!suphp)
            suphp = GetComponentInChildren<SupHP>();
    }

    /// <summary>
    /// 억제기의 리스폰을 위해 Revive 코루틴을 호출하는 함수
    /// </summary>
    public void WillRevive()
    {
        StartCoroutine("Revive");
    }

    /// <summary>
    /// 억제기의 리스폰을 담당하는 코루틴
    /// </summary>
    /// <returns></returns>
    IEnumerator Revive()
    {
        // 억제기가 파괴되면 코루틴이 즉시 호출되어 300초를 기다린 후 리스폰한다
        yield return new WaitForSeconds(300f);

        sysMsg.Annoucement(9, true);
        mySon.SetActive(true);
        suphp.respawn();
    }
}