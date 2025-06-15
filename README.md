# ![Image](https://github.com/user-attachments/assets/61a0dc9a-23b9-4357-9972-f03e702470d9) AI와 함께하는 마피아 게임: Mafia With AI
      - 경찰: 투표한 사람의 직업을 알 수 있음
      - 의사: 투표한 사람은 이번 밤 투표에서 살릴 수 있음
  - 모든 투표는 아래와 같은 규칙을 따름
    - 각 페이즈별 남아있는 시간 동안 마음껏 투표 대상을 바꿀 수 있으며, 내가 한 투표를 취소할 수도 있음
    - 기본적으로 투표하지 않는다면 이는 무효표를 던진 것으로 간주
    - 밤 투표의 경우 마피아끼리만 자신이 누구에게 투표했는지를 실시간으로 반영
   
### 게임 진행 흐름도
1. `시작`: 각 플레이어는 자신의 직업을 배정받습니다.
2. `밤 진행`: 마피아, 경찰, 의사는 각각 투표를 진행하게 됩니다.
3. `밤 투표 결과 발표`: 의사, 마피아, 경찰의 투표 결과를 확인합니다.
4. `낮 투표`: 낮이 시작될 때 마피아의 투표로 사망자가 나오며, 경찰은 조사 결과가, 의사는 누굴 살렸는지 나오게 됩니다. 각 플레이어와 AI는 대화를 통해 의심스러운 사람을 투표할 수 있습니다.
5. `최종 투표 결과 발표`: 최종 투표 결과를 확인합니다.
6. `2~5` 과정을 반복하여 최종 승리자가 나올 때까지 낮과 밤이 진행됩니다. 
7. `종료`: 어느 측이 승리했는지를 발표하게 되며, 대기 상태로 다시 돌아가게 됩니다.

# 3. 게임 실행 방법

### 필요 파일
![Image](https://github.com/user-attachments/assets/7792856e-d263-4c93-ade9-bef00f69f37f)

유니티와 node.js파일이 필요합니다. 노드 JS로 server-room.js를 실행시켜 줘야 합니다.

### 첫 화면
![Image](https://github.com/user-attachments/assets/eb3d18e5-007b-457d-b8af-e32efc8905ab)

유저 로그인 기능이 구현되어 있습니다.

### 로비 화면
![Image](https://github.com/user-attachments/assets/8b74f33d-0a14-4835-971f-704847f0cdb3)

로그인 후에는 로비 화면이 나오게 되며 다른 플레이어가 만든 방을 확인할 수 있고 자기가 방을 만들수도 있습니다.

### 방 화면
![Image](https://github.com/user-attachments/assets/ff099506-3c93-4cf5-a648-dc5061c3cef7)

다른 유저들의 준비가 완료되면 호스트(방장)는 게임을 시작할 수 있습니다.

### 방 만드는 화면
![Image](https://github.com/user-attachments/assets/2c2c80fa-d5d4-414a-8f8d-4a6ce996639b)

자기가 방을 만들 때는 로비 화면에서 CREATE ROOM을 통해 만들 수 있습니다.

### 자기가 방장인 경우
![Image](https://github.com/user-attachments/assets/6ea6199c-05ec-451f-8bf2-b599f63233fb)

START버튼이 활성화 되며, 8명의 인원이 다 차지 않아도 시작이 가능합니다.

### 게임 시작
![Image](https://github.com/user-attachments/assets/0097d13d-d74d-4588-83ce-82f73e0ab2d1)

처음에 자기 직업을 배정받으며 왼쪽 위에는 몇번째 낮/밤 인지, 유저 이름이 나타나 있습니다.

### 투표시
![Image](https://github.com/user-attachments/assets/95e78e35-49cf-4fe7-b116-bb393dac4689)

투표 시 해당 아이디에 구별하기 쉽게 나타나 있습니다.

### 낮
![Image](https://github.com/user-attachments/assets/5f37950b-5d29-40ed-9b12-214c06e53972)

다음과 같이 AI와 대화를 통해 마피아를 추리합니다.

### 낮 투표 시작
![Image](https://github.com/user-attachments/assets/51ead9bf-786f-4988-9249-9c727caad13f)

투표가 시작되면 처형할 사람을 고릅니다. AI또한 처형할 사람을 선택하게 됩니다.

### 투표 종료 후 2일차 밤 시작
![Image](https://github.com/user-attachments/assets/97917749-f71a-4cb3-8c48-9a60f57273f8)

투표가 종료된 후 가장 마피아로 의심되는 사람이 처형되고, 다시 밤이 시작됩니다. 이와 같은 과정을 거쳐 마피아가 마피아와 시민의 수가 같아지거나 모든 마피아를 제거하면 게임이 끝나게 됩니다.
