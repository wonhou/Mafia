const WebSocket = require('ws');

const ws = new WebSocket('ws://localhost:3000');

let playerId = null;

ws.on('open', () => {
  console.log('✅ 서버에 연결됨');

  // 1. 사용자 이름 등록
  ws.send(JSON.stringify({
    type: 'register',
    playerName: '콘솔유저'
  }));
});

ws.on('message', (data) => {
  const msg = JSON.parse(data);
  console.log('📨 수신 메시지:', msg);

  if (msg.type === 'register_success') {
    playerId = msg.playerId;
    console.log('🆔 등록 성공:', playerId);

    // ✅ Unity에서 생성한 roomId를 직접 입력
    const roomId = 'Room_b6p'; // ← Unity에서 생성된 roomId로 수동 입력

    // 2. join_room
    ws.send(JSON.stringify({
      type: 'join_room',
      roomId,
      playerId
    }));
  }

  if (msg.type === 'room_info') {
    console.log('🏠 방 정보:', msg);
  }
});
