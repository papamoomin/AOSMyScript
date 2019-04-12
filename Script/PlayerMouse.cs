using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary> 마우스, 키보드 등을 이용한 플레이어의 입력 담당 스크립트 </summary>
public class PlayerMouse : MonoBehaviour
{
    public ChampionAtk myChampAtk;
    public GameObject myTarget;
    public bool isAtkCommand = false;
    public bool isWardCommand = false;

    private ChampionData myChampData;
    private Ray r;
    private RaycastHit[] hits;
    private MouseFxPooling fxPool;
    private MinimapClick minimapClick;
    private PlayerData playerData;
    private Vector3 v;
    private Vector3 dest;
    private string playerTeam;
    private string myChampName;
    private float soundTimer = 5.0f;

    //값 초기화
    private void Start()
    {
        SetPlayerTeam();

        playerData = PlayerData.Instance;
        myChampName = PlayerData.Instance.championName;
        myChampData = GetComponent<ChampionData>();
    }

    //값 초기화
    private void OnLevelWasLoaded(int level)
    {
        if (UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(level).name.Equals("InGame"))
        {
            minimapClick = GameObject.FindGameObjectWithTag("MinimapClick").GetComponent<MinimapClick>();
            fxPool = GameObject.FindGameObjectWithTag("MouseFxPool").GetComponent<MouseFxPooling>();
        }
    }

    private void Update()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Equals("InGame"))
        {
            //플레이어가 죽었다는 어떤 입력에도 반응하지 않게 리턴
            if (PlayerData.Instance.isDead)
                return;

            //키보드 입력을 받았는지 확인하고 처리
            InsertKeyboard();

            //마우스 입력을 받았는지 확인하고 처리
            if (Input.GetMouseButtonDown(1))
                MouseRightButtonClick();
            else if (Input.GetMouseButtonDown(0))
                MouseLeftButtonClick();
        }
    }

    /// <summary>
    /// Player의 팀을 설정하는 함수
    /// </summary>
    private void SetPlayerTeam()
    {
        playerTeam = PhotonNetwork.player.GetTeam().ToString();

        if (playerTeam.Equals("red"))
            playerTeam = "Red";
        else if (playerTeam.Equals("blue"))
            playerTeam = "Blue";
        else
            print("PlayerMouse.cs :: 26 :: Player has not Team T_T");
    }

    /// <summary>
    /// 키보드 입력을 받는 함수
    /// </summary>
    private void InsertKeyboard()
    {
        //어택 단축키를 입력받았을 때 어택을 토글하고 와드를 해제
        if (Input.GetKeyDown(KeyCode.A))
        {
            isAtkCommand = !isAtkCommand;
            isWardCommand = false;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {//와드 단축키를 입력받았을 때 와드를 구매했다면 와드를 토글
            if (PlayerData.Instance.accessoryItem.Equals(1))
            {
                isWardCommand = !isWardCommand;

                //와드가 true가 되었을 때, 남지 않았다면 다시 어택과 함께 해제시켜준다.
                if (isWardCommand)
                {
                    if (myChampAtk.wardAmount < 1)
                    {
                        isWardCommand = false;
                        isAtkCommand = false;
                    }
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.S)) //정지한다.
            myChampAtk.Stop();
    }

    /// <summary>
    /// 플레이어 이동 시 이동 효과음을 출력하는 함수
    /// </summary>
    private void PlayWalkSound()
    {
        soundTimer -= Time.deltaTime;

        //우클릭을 아무리 많이 눌러도 처음 효과음이 재생된 후 일정 시간은 다시 재생되지 않도록 함
        if (soundTimer <= 0)
        {
            ChampionSound.instance.WalkSound(myChampName);
            soundTimer = 5.0f;
        }
    }

    /// <summary>
    /// 어택 명령이나 와드 설치 명령의 키보드 단축키 선택을 해제시켜주는 함수
    /// </summary>
    private void OffAtkAndWardCommand()
    {
        if (isAtkCommand)
            isAtkCommand = false;

        if (isWardCommand)
            if (myChampAtk.wardAmount > 0)
                isWardCommand = false;
    }

    /// <summary>
    /// 어택 단축키를 누른 후 마우스 왼쪽을 클릭했을 때 작동하는 함수
    /// </summary>
    private void AtkKeyAndLeftClick()
    {
        isAtkCommand = false;
        Vector3 h = Vector3.zero;
        GameObject target = null;
        bool isTouchGround = false;
        bool isTouchEnemy = false;
        v = Input.mousePosition;
        r = Camera.main.ScreenPointToRay(v);
        hits = Physics.RaycastAll(r);

        //레이를 쏴 충돌한게 있다면, 충돌한 개체마다 확인
        foreach (RaycastHit hit in hits)
        {
            //땅과 충돌했을 때
            if (hit.collider.tag.Equals("Terrain"))
            {
                h = hit.point;
                h.y = 1;
                //땅과 충돌했음을 체크한다.
                isTouchGround = true;
            }
            else //적과 충돌했는지 확인한다.
                isTouchEnemy = CheckTouchEnemy(hit);

            //적과 레이가 충돌했다면 타겟을 충돌한 오브젝트로 설정한다.
            if (isTouchEnemy)
            {
                target = hit.collider.gameObject;
                break;
            }
        }

        //레이를 쏴 검사한 결과 적을 클릭한 경우 적을 타겟팅
        if (isTouchEnemy)
        {
            myChampAtk.isWillAtkAround = false;

            if (!myChampAtk.isTargetting)
            {
                myChampAtk.isTargetting = true;
                myChampAtk.isWarding = false;
            }

            myChampAtk.atkTargetObj = target;
            fxPool.GetPool("Force", myChampAtk.atkTargetObj.transform.position, myChampAtk.atkTargetObj);
        }
        else if (isTouchGround)
        {//땅을 클릭한 경우 
            fxPool.GetPool("Force", h);
            myTarget.transform.position = h;
            myChampAtk.isWillAtkAround = true;

            //개체가 타겟팅된 경우는 아니므로 타겟팅 false 처리해줌.
            if (myChampAtk.isTargetting)
            {
                myChampAtk.isTargetting = false;
                myChampAtk.isWarding = false;
                myChampAtk.atkTargetObj = null;
            }
        }

        //귀환중에 클릭을 했다면 귀환캔슬
        myChampData.RecallCancel();
    }

    /// <summary>
    /// 와드 단축키를 누른 후 마우스 왼쪽을 클릭했을 때 작동하는 함수
    /// </summary>
    private void WardKeyAndLeftClick()
    {

        Vector3 h = Vector3.zero;
        v = Input.mousePosition;
        r = Camera.main.ScreenPointToRay(v);
        hits = Physics.RaycastAll(r);

        //레이를 쏴 땅과 충돌했는지 확인
        foreach (RaycastHit hit in hits)
        {
            //땅과 충돌한 경우
            if (hit.collider.tag.Equals("Terrain"))
            {
                h = hit.point;
                h.y = 1;
                bool isNotCollision = true;
                //오버랩스피어를 이용해 클릭한 지점 주변의 콜라이더를 저장
                Collider[] cols = Physics.OverlapSphere(h, 1f);

                //오버랩스피어로 가져온 콜라이더 중 벽이 있다면 와드를 설치할 수 없는 곳이니 체크.
                foreach (Collider a in cols)
                {
                    if (a.tag.Equals("WallCollider"))
                    {
                        isNotCollision = false;
                        break;
                    }
                }

                //문제가 없다면 와드를 설치한다.
                if (isNotCollision)
                {
                    isWardCommand = false;
                    myChampAtk.WantBuildWard(h);
                }
            }
        }
    }

    /// <summary>
    /// 마우스 오른쪽을 클릭했을 때 작동하는 함수
    /// </summary>
    private void MouseRightButtonClick()
    {
        PlayWalkSound();
        OffAtkAndWardCommand();
        Vector3 h = Vector3.zero;
        GameObject target = null;
        bool isTouchGround = false;
        bool isTouchEnemy = false;
        v = Input.mousePosition;
        r = Camera.main.ScreenPointToRay(v);
        hits = Physics.RaycastAll(r);

        //레이를 쏴 충돌한 것을 저장하여 확인
        foreach (RaycastHit hit in hits)
        {
            //땅을 충돌한 경우를 체크
            if (hit.collider.tag.Equals("Terrain"))
            {
                h = hit.point;
                h.y = 1;
                isTouchGround = true;

                if (!fxPool && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "InGame")
                    fxPool = GameObject.FindGameObjectWithTag("MouseFxPool").GetComponent<MouseFxPooling>();

                fxPool.GetPool("Default", h);
            }
            else //적과 충돌한 경우를 체크
                isTouchEnemy = CheckTouchEnemy(hit);

            //적과 충돌한 경우 타겟을 적으로 둔다.
            if (isTouchEnemy)
            {
                target = hit.collider.gameObject;
                fxPool.GetPool("Force", target.transform.position, target);
                break;
            }
        }

        // 상점 켜져있으면 뒤로 움직이지않게
        GameObject shop = GameObject.FindGameObjectWithTag("ShopCanvas");

        if (shop != null)
        {
            GraphicRaycaster shopGR = shop.GetComponent<GraphicRaycaster>();
            PointerEventData ped = new PointerEventData(null);
            ped.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            shopGR.Raycast(ped, results);

            foreach (RaycastResult result in results)
                if (result.gameObject.transform.GetComponentInParent<GraphicRaycaster>().Equals(shopGR))
                {
                    isTouchEnemy = false;
                    isTouchGround = false;
                    break;
                }
        }

        // 옵션 켜져있으면 움직이지않게
        var optionCanvas = GameObject.FindGameObjectWithTag("OptionCanvas");

        if (optionCanvas != null)
        {
            GraphicRaycaster optionGR = optionCanvas.GetComponent<GraphicRaycaster>();
            PointerEventData ped = new PointerEventData(null);
            ped.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            optionGR.Raycast(ped, results);

            foreach (RaycastResult result in results)
                if (result.gameObject.transform.GetComponentInParent<GraphicRaycaster>().Equals(optionGR))
                {
                    isTouchEnemy = false;
                    isTouchGround = false;
                    break;
                }
        }

        //적을 클릭한 경우
        if (isTouchEnemy)
        {
            myTarget.transform.position = transform.position;
            myChampAtk.isWillAtkAround = false;

            if (!myChampAtk.isTargetting)
            {
                myChampAtk.isTargetting = true;
                myChampAtk.atkTargetObj = target;
            }
        }
        else if (isTouchGround)
        {//땅을 클릭한 경우
            myTarget.transform.position = h;
            myChampAtk.isWillAtkAround = false;

            if (myChampAtk.isTargetting)
            {
                myChampAtk.isTargetting = false;
                myChampAtk.atkTargetObj = null;
                myChampAtk.TheAIPath.canMove = true;
                myChampAtk.TheAIPath.canSearch = true;
            }
        }
    }

    /// <summary>
    /// 마우스 왼쪽 버튼을 클릭했을 때 작동하는 함수
    /// </summary>
    private void MouseLeftButtonClick()
    {
        if (isAtkCommand)
            AtkKeyAndLeftClick();
        else if (isWardCommand)
            WardKeyAndLeftClick();
    }

    /// <summary>
    /// 현재 마우스로 적을 클릭했는지 여부를 확인해주는 함수
    /// </summary>
    /// <param name="hit">클릭 시 Raycast로 얻은 RaycastHit</param>
    /// <returns>적을 클릭했는지 여부를 리턴</returns>
    private bool CheckTouchEnemy(RaycastHit hit)
    {
        bool isTouchEnemy = false;
        if (hit.collider.tag.Equals("Minion"))
        {//미니언과 충돌했을 때
         //적팀인 미니언이라면 적과 충돌했음을 체크한다.
            if (!hit.collider.name.Contains(playerTeam))
                if (hit.collider.GetComponent<FogOfWarEntity>().isCanTargeting)
                    isTouchEnemy = true;
        }
        else if (hit.collider.gameObject.layer.Equals(LayerMask.NameToLayer("Champion")))
        {//챔피언과 충돌했을 때
         //적팀인 챔피언이라면 적과 충돌했음을 체크한다.
            if (!hit.collider.gameObject.GetComponent<ChampionBehavior>().team.Equals(playerTeam))
                if (hit.collider.GetComponent<FogOfWarEntity>().isCanTargeting)
                    isTouchEnemy = true;
        }
        else if (hit.collider.tag.Equals("Tower"))
        {//타워와 충돌했을 때
            TowerBehaviour towerBehav = hit.collider.gameObject.GetComponent<TowerBehaviour>();

            //적팀인 타워라면 적과 충돌했음을 체크한다.
            if (towerBehav.isCanAtkMe)
                if (!towerBehav.team.Equals(playerTeam))
                    isTouchEnemy = true;
        }
        else if (hit.collider.tag.Equals("Suppressor") || hit.collider.tag.Equals("Nexus"))
        {//억제기나 넥서스와 충돌했을 때
            SuppressorBehaviour supBehav = hit.collider.gameObject.GetComponent<SuppressorBehaviour>();

            //적팀이라면 적과 충돌했음을 체크한다.
            if (supBehav.isCanAtkMe)
                if (!supBehav.team.Equals(playerTeam))
                    isTouchEnemy = true;
        }
        else if (hit.collider.gameObject.layer.Equals(LayerMask.NameToLayer("Monster"))) //몬스터와 충돌했다면 적과 충돌했음을 체크한다.
            if (hit.collider.GetComponent<FogOfWarEntity>().isCanTargeting)
                isTouchEnemy = true;
        return isTouchEnemy;
    }
}