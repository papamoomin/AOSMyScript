# AOSMyScript

AOS 팀 프로젝트에서 제가 주가 되어 짠 스크립트만 모아둔 저장소입니다.  
<br><br><br>

---

# BushWard

### <b>BushJoinScript</b>
부쉬에 들어갔을 때 아군과 적팀을 구분하고, 시야의 유무를 판단하는 스크립트.  

### <b>Ward</b>
와드 내부 구조를 담당하는 스크립트  


# Champion

### <b>ChampionAtk</b>
챔피언의 공격을 담당하는 스크립트  

### <b>Championehavior</b>
챔피언 개체를 제어하는 스크립트  


# Ingame

### <b>KillManager</b>
오브젝트간의 공격, 사망을 동기화하는 매니저 스크립트  

### <b>PlayerMouse</b>
마우스, 키보드 등을 이용한 플레이어의 입력 담당 스크립트  

### <b>StructureSetting</b>
네비게이션 메쉬를 만드는데에 방해되는 지상의 오브젝트들을, 메쉬가 생성된 후에 활성화해주는 스크립트.  

### <b>WallCollider</b>
챔피언, 몬스터, 미니언이 지나가선 안되는 길을 막아줄 콜라이더를 담당하는 스크립트  


# Minion

### <b>MinionAtk</b>
미니언의 공격을 담당하는 스크립트  

### <b>MinionBehavior</b>
미니언 개체를 제어하는 스크립트  

### <b>MinionJoinJungle</b>
미니언이 정글에 들어왔다면 돌려보내는 스크립트  

### <b>MinionWaypoint</b>
웨이포인트 자신의 영역에 미니언이 들어왔을 시 미니언의 이동 목표를 다음 웨이포인트로 바꿔주는 스크립트  

### <b>RespownCollider</b>
미니언이 리스폰되는 위치에 다른 미니언이 있는지를 판단, 처리를 맡은 콜라이더를 담당하는 스크립트 

### <b>RespownZone</b>
리스폰 콜라이더들을 한 배열에 묶어둔 스크립트  


# Monster

### <b>MonsterAtk</b>
몬스터의 공격을 담당하는 스크립트  

### <b>MonsterBehaviour</b>
몬스터 개체를 제어하는 스크립트  

### <b>MonsterManager</b>
게임 시작 시 몬스터의 영역 오브젝트 생성을 담당하는 스크립트  

### <b>MonsterRespawn</b>
몬스터의 생성과 리스폰을 처리하는 스크립트  


# Skill

### <b>AlistarSkill</b>
알리스타의 스킬을 담당하는 스크립트  

### <b>AsheSkill</b>
애쉬의 스킬을 담당하는 스크립트  

### <b>MundoSkill</b>
문도의 스킬을 담당하는 스크립트  

### <b>SkillFactioner</b>
스킬을 사용한 챔피언이 부쉬에 있을 때 적에게 스킬이 보이는지 유무를 설정하는 스크립트  

### <b>Skills</b>
모든 챔피언의 스킬의 부모 클래스이자, 공통된 부분을 묶어둔 스크립트  

<br>

## - Skill\Alistar

### <b>AlistarE</b>
알리스타의 E 스킬에 들어있는 스크립트  

### <b>AlistarQ</b>
알리스타의 Q 스킬에 들어있는 스크립트  

### <b>AlistarR</b>
알리스타의 R 스킬에 들어있는 스크립트  

### <b>AlistarW</b>
알리스타의 W 스킬에 들어있는 스크립트  

<br>

## - Skill\Ashe

### <b>AsheE</b>
애쉬의 E 스킬에 들어있는 스크립트  

### <b>AsheEHawkWard</b>
애쉬의 E 스킬에서 매가 사라진 이후의 시야를 담당하는 스크립트  

### <b>AsheR</b>
애쉬의 R 스킬에 들어있는 스크립트  

### <b>AsheW</b>
애쉬의 W 스킬에 들어있는 스크립트  

<br>

## - Skill\Mundo

### <b>MundoQ</b>
문도의 Q 스킬에 들어있는 스크립트  

### <b>MundoW</b>
문도의 W 스킬에 들어있는 스크립트  


# Suppressor

### <b>SuppressorBehaviour</b>
넥서스와 억제기 개체를 제어하는 스크립트  

### <b>SuppressorRevive</b>
억제기의 리스폰을 담당하는 스크립트


# Tower

### <b>TowerAtk</b>
타워의 공격을 담당하는 스크립트  

### <b>TowerBehaviour</b>
타워 개체를 제어하는 스크립트  

### <b>TowerProjectile</b>
타워의 투사체를 담당하는 스크립트  

### <b>TowersManager</b>
전장의 모든 타워를 총괄, 연결하는 스크립트  
