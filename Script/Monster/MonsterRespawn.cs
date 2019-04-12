using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 몬스터의 생성과 리스폰을 처리하는 스크립트
/// </summary>
public class MonsterRespawn : Photon.PunBehaviour
{
    public Vector3 respawnRotating
    {
        set
        {
            _respawnRotating = value;
        }
        get
        {
            return respawnRotating;
        }
    }
    public GameObject respawnPrefab;
    public string myMonsterName;
    public float birthTime; //첫 등장하는 시간. 0이면 안됨
    public float respawnTime; //죽은 후 부활하는 시간 0이면 부활하지 않음
    public float outTime; //죽이지 않아도 사라지는 시간. 0이면 사라지지 않음

    private MonsterBehaviour myMonsterBehav;
    private GameObject myMonster = null;
    private GameObject myMonsterManager;
    private Vector3 andromeda = new Vector3(200, -100, 200);
    private Vector3 _respawnRotating;
    private bool isDie = true; // 죽은 상태(첫 생성 후 스폰되기 전도 죽은 걸로 침)
    private bool isOut = false; // 아웃된 상태 (전령의 경우 일정 시간이 지나면 죽지 않아도 사라지도록 함)
    private byte monsterBasicSettingEventCode = (byte)190;

    private void Awake()
    {
        myMonsterManager = GameObject.FindGameObjectWithTag("MonsterManager");
        PhotonNetwork.OnEventCall += MonsterBasicSetting;
    }

    private void OnDestroy()
    {
        PhotonNetwork.OnEventCall -= MonsterBasicSetting;
    }

    void Start()
    {
        // 몬스터는 마스터에서만 생성
        if (PhotonNetwork.isMasterClient)
        {
            if (myMonster == null)
            {
                //포톤으로 생성하여 모든 클라이언트가 공유한다.
                myMonster = PhotonNetwork.Instantiate("Monster/" + myMonsterName + "Obj", andromeda, Quaternion.identity, 0);
                object[] datas = new object[] { (int)myMonster.GetComponent<PhotonView>().viewID, (int)GetComponent<PhotonView>().viewID };

                PhotonNetwork.RaiseEvent(monsterBasicSettingEventCode, datas, true, new RaiseEventOptions()
                {
                    CachingOption = EventCaching.AddToRoomCacheGlobal,
                    Receivers = ReceiverGroup.Others
                });

                myMonster.SetActive(false);
                transform.SetParent(myMonsterManager.transform);
                myMonster.transform.SetParent(this.transform);
                myMonster.transform.DORotate(_respawnRotating, 0);
                myMonster.transform.localPosition = Vector3.zero;
                myMonsterBehav = myMonster.GetComponent<MonsterBehaviour>();
                myMonsterBehav.myCenter = this;
                myMonsterBehav.LateInit();
            }

            //첫 스폰과 아웃 시간을 설정하여 코루틴을 돌린다.
            this.photonView.RPC("StartBirth", PhotonTargets.All, null);

            if (outTime > 0)
                this.photonView.RPC("StartOut", PhotonTargets.All, null);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        // 몬스터가 영역 밖으로 나가면 리턴을 켠다
        if (other.gameObject.Equals(myMonster))
            myMonsterBehav.monAtk.StartReturn();
        else if (myMonsterBehav.friendsList.Contains(other.gameObject))
            myMonsterBehav.monAtk.StartReturn();
    }

    public void OnTriggerEnter(Collider other)
    {
        //주변에 몬스터가 있는 경우 한 그룹으로 묶는다.
        if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Monster")))
            if (!other.gameObject.Equals(myMonster))
                if (!myMonsterBehav.friendsList.Contains(other.gameObject))
                    myMonsterBehav.friendsList.Add(other.gameObject);
    }

    /// <summary>
    /// 첫 생성을 담당하는 코루틴
    /// </summary>
    IEnumerator Birth()
    {
        yield return new WaitForSeconds(birthTime);

        myMonsterBehav.SetStat(0);
        myMonster.SetActive(true);
        isDie = false;
    }

    /// <summary>
    /// 리스폰을 담당하는 코루틴
    /// </summary>
    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);

        myMonsterBehav.SetStat(1);
        myMonsterBehav.ReturnOtherClients(false);
        myMonsterBehav.InitValue();
        myMonster.SetActive(true);
        isDie = false;
    }

    /// <summary>
    /// 아웃을 담당하는 코루틴
    /// </summary>
    IEnumerator Out()
    {
        yield return new WaitForSeconds(outTime);

        myMonster.SetActive(false);
        isOut = true;
    }

    /// <summary>
    /// 몬스터의 기본 설정을 담당하는 함수
    /// </summary>
    /// <param name="eventCode">몬스터 기본 설정을 담당하는 이벤트의 코드 (190)</param>
    /// <param name="content"></param>
    /// <param name="senderId"></param>
    private void MonsterBasicSetting(byte eventCode, object content, int senderId)
    {
        if (eventCode != monsterBasicSettingEventCode)
            return;

        object[] datas = content as object[];
        int viewID = (int)datas[0];
        int parentViewID = (int)datas[1];
        GameObject monster = PhotonView.Find(viewID).gameObject;
        GameObject parentMonster = PhotonView.Find(parentViewID).gameObject;

        if (parentMonster.Equals(this.gameObject))
        {
            myMonster = monster;
            monster.SetActive(false);
            transform.SetParent(myMonsterManager.transform);
            monster.transform.SetParent(parentMonster.transform);
            monster.transform.DORotate(_respawnRotating, 0);
            monster.transform.localPosition = Vector3.zero;
            myMonsterBehav = monster.GetComponent<MonsterBehaviour>();
            myMonsterBehav.myCenter = parentMonster.GetComponent<MonsterRespawn>();
            myMonsterBehav.LateInit();//세부설정 불러줌
        }
    }

    /// <summary>
    /// RPC를 통해 첫 스폰 코루틴을 켜주는 함수
    /// </summary>
    [PunRPC]
    public void StartBirth()
    {
        StartCoroutine("Birth");
    }

    /// <summary>
    /// RPC를 통해 아웃 코루틴을 켜주는 함수
    /// </summary>
    [PunRPC]
    public void StartOut()
    {
        StartCoroutine("Out");
    }

    /// <summary>
    /// 스폰 시 위치와 방향을 잡는 함수
    /// </summary>
    public void SetPosition()
    {
        myMonster.transform.localPosition = Vector3.zero;
        myMonster.transform.DORotate(_respawnRotating, 0.5f);
    }
}