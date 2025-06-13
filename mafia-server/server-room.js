const WebSocket = require('ws');
const axios = require('axios');
const { createPlayers } = require('./players');
const MafiaGame = require('./game');

const wss = new WebSocket.Server({ port: 3000 });

const rooms = {};                     // roomId -> { name, players: [playerIds], game }
const socketMap = new Map();          // playerId -> WebSocket
const nicknameSet = new Set();        // playerId 중복 방지
const playerNameSet = new Set();      // 닉네임 중복 방지
const playerRoomMap = new Map(); // playerId -> roomId
const playerNameMap = new Map();      // playerId -> playerName
const clients = {};  // playerId → WebSocket

function sendTo(playerId, message) {
  const client = clients[playerId];
  if (client && client.readyState === WebSocket.OPEN) {
    client.send(JSON.stringify(message));
  }
}

function broadcastToRoom(roomId, data) {
  const json = JSON.stringify(data);
  const room = rooms[roomId];
  if (!room) return;
  room.players.forEach((playerId) => {
    const ws = socketMap.get(playerId);
    if (ws && ws.readyState === WebSocket.OPEN) {
      ws.send(json);
    }
  });
}

function broadcastPlayerList() {
  const playerList = [];
  for (const playerId of socketMap.keys()) {
    let foundRoomId = null;
    for (const [roomId, room] of Object.entries(rooms)) {
      if (room.players.includes(playerId)) {
        foundRoomId = roomId;
        break;
      }
    }
    playerList.push({
      id: playerId,
      name: playerNameMap.get(playerId) || "???",
      roomId: foundRoomId
    });
  }
  const message = JSON.stringify({ type: "update_players", players: playerList });
  wss.clients.forEach(client => {
    if (client.readyState === WebSocket.OPEN) {
      client.send(message);
    }
  });
}

function exitPlayerFromRoom(playerId, roomId, options = {}) {
  const {
    notifySocket = null,
    sendLeftRoomMessage = false,
    wasClosed = false
  } = options;

  const room = rooms[roomId];
  if (!room) return;

  const wasOwner = room.players[0] === playerId;

  room.players = room.players.filter(id => id !== playerId);
  delete room.readyPlayers?.[playerId];
  broadcastToRoom(roomId, { type: 'player_left', playerId });

  if (room.players.length === 0) {
    delete rooms[roomId];
    console.log(`🗑️ 방 삭제됨 (${wasClosed ? "disconnect" : "leave"}): ${roomId}`);
  } else if (wasOwner) {
    const nonAIPlayers = room.players.filter(id => !id.startsWith('ai_'));
    const newOwner = nonAIPlayers.length > 0
      ? nonAIPlayers[Math.floor(Math.random() * nonAIPlayers.length)]
      : null;

    if (newOwner) {
      broadcastToRoom(roomId, { type: 'new_owner', playerId: newOwner });
      console.log(`👑 방장 변경됨: ${newOwner} → ${roomId}`);

      const playerList = room.players.map((id, index) => ({
        id,
        name: playerNameMap.get(id) || "???",
        slot: index,
        isOwner: index === 0,
        isAlive: room.game?.players.find(p => p.id === id)?.alive ?? true
      }));

      broadcastToRoom(roomId, {
        type: 'room_info',
        roomId,
        roomName: room.name,
        players: playerList,
        isOwner: false // 개별 처리 필요 없고, 각 플레이어가 클라이언트에서 판단함
      });
    } else {
      room.players.forEach(aiId => {
        const aiSocket = socketMap.get(aiId);
        if (aiSocket?.readyState === WebSocket.OPEN) {
          aiSocket.send(JSON.stringify({ type: 'room_destroyed', roomId }));
        }
      });
        // 게임 중이면 인스턴스 제거 처리
        if (room.game) {
          console.log(`🧹 진행 중이던 게임 인스턴스 제거: ${roomId}`);
          room.game?.terminate();
          room.game = null;
        }

      delete rooms[roomId];
      console.log(`🗑️ 방 삭제됨 (AI만 남음): ${roomId}`);
    }
  }

  if (sendLeftRoomMessage && notifySocket?.readyState === WebSocket.OPEN) {
    notifySocket.send(JSON.stringify({ type: 'left_room', roomId }));
  }

  broadcastPlayerList();
}

wss.on('connection', (ws) => {
  let currentRoom = null;
  let currentPlayerId = null;

  console.log('✅ 클라이언트 연결됨');

  ws.on('message', async (message) => {
    try {
      const msg = JSON.parse(message);
      console.log("💌 수신된 메시지:", msg);

      if (msg.type === 'register') {
        const name = msg.playerName;

        if (!name || typeof name !== 'string' || name.trim() === '') {
          ws.send(JSON.stringify({ type: 'error', message: '❌ 닉네임이 비어 있습니다.' }));
          return;
        }

        if (playerNameSet.has(name)) {
          ws.send(JSON.stringify({ type: 'register_failed', message: '❌ 이미 사용 중인 닉네임입니다.' }));
          return;
        }

        const id = "user_" + Math.random().toString(36).substring(2, 8);

        currentPlayerId = id;
        socketMap.set(currentPlayerId, ws);
        nicknameSet.add(currentPlayerId);
        playerNameSet.add(name);
        playerNameMap.set(currentPlayerId, name);
        clients[id] = ws;

        ws.send(JSON.stringify({ type: 'register_success', playerId: currentPlayerId, playerName: name }));
        console.log(`🟢 등록됨: [${currentPlayerId}] ${name}`);

        broadcastPlayerList();
        return;
      }

      if (msg.type === 'chat') {
        const roomId = playerRoomMap.get(currentPlayerId);
        const room = rooms[roomId];
        if (!room || !room.game) return;

        const senderId = msg.senderId || currentPlayerId;
        const senderName = msg.senderName || playerNameMap.get(senderId) || "???";
        const role = room.game.getRoleOf(senderId);
        const isNight = room.game.isNight();

        const chatMessage = {
          type: 'chat',
          sender: senderName,
          senderId: senderId,
          message: msg.text
        };

        console.log(`💬 [${senderName}] 채팅: ${msg.text}`);

        if (isNight) {
          if (role === 'mafia') {
            // 밤에는 마피아끼리만 보냄
            room.players.forEach(id => {
              const r = room.game.getRoleOf(id);
              if (r === 'mafia') {
                sendTo(id, chatMessage);
              }
            });
          } else {
            return;
          }
        } else {
          // 낮에는 전체에게 공개
          broadcastToRoom(roomId, chatMessage);
        }
        return;
      }

      if (msg.type === "create_room") {
        const roomId = "Room_" + Math.random().toString(36).substring(2, 5);
        const roomName = msg.roomName || "Untitled Room";

        rooms[roomId] = {
          id: roomId,
          name: roomName,
          players: [currentPlayerId],
          readyPlayers: {
            [currentPlayerId]: true
          }
        };

        currentRoom = roomId;

        console.log(`🟢 방 생성됨: [${roomId}] ${roomName}`);

        ws.send(JSON.stringify({
          type: "room_created",
          roomId: roomId,
          roomName: roomName
        }));
        return;
      }

      if (msg.type === 'join_room') {
        const { roomId, playerId } = msg;
        const room = rooms[roomId];

        if (!room) {
          ws.send(JSON.stringify({ type: 'error', message: '❌ 방이 존재하지 않습니다.' }));
          return;
        }

        if (!room.players.includes(playerId)) {
          room.players.push(playerId);
        }

        currentRoom = roomId;
        currentPlayerId = playerId;
        playerRoomMap.set(playerId, roomId);

        const playerList = room.players.map((id, index) => ({
          id,
          name: playerNameMap.get(id) || "???",
          slot: index,
          isOwner: index === 0
        }));

        const isOwner = room.players[0] === playerId;

        console.log(`🏠 ${playerId} 입장 → ${roomId} (${room.name})`);
        console.log("🧑‍🤝‍🧑 현재 방 플레이어 목록:");
        playerList.forEach(p => {
          console.log(`  - 슬롯 ${p.slot}: ${p.name} (${p.id}) ${p.isOwner ? "👑 방장" : ""}`);
        });
        console.log(`📌 ${playerId}는 방장인가? → ${isOwner ? "✅ 예" : "❌ 아니오"}`);

        ws.send(JSON.stringify({
          type: 'room_info',
          roomId,
          roomName: room.name,
          players: playerList,
          isOwner
        }));

        return;
      }

      if (msg.type === 'leave_room') {
        const { roomId, playerId } = msg;
        exitPlayerFromRoom(playerId, roomId, {
          notifySocket: ws,
          sendLeftRoomMessage: true,
          wasClosed: false
        });
        playerRoomMap.delete(playerId);
        return;
      }

      if (msg.type === 'list_rooms') {
        console.log("📦 list_rooms 요청 수신됨");
        const roomList = Object.entries(rooms).map(([roomId, room]) => ({
          roomId,
          roomName: room.name,
          playerCount: room.players.length
        }));

        ws.send(JSON.stringify({
          type: 'room_list',
          rooms: roomList
        }));
        return;
      }

      if (msg.type === "list_players") {
        const players = [];

        Object.entries(clients).forEach(([id, ws]) => {
          players.push({
            id,
            name: playerNameMap.get(id),
            roomId: playerRoomMap.get(id) ?? null
          });
        });

        const response = {
          type: "update_players",
          players
        };
        
        ws.send(JSON.stringify(response));
      }

      if (msg.type === 'start_game') {
        console.log("🟨 start_game 수신됨");
        const room = rooms[currentRoom];
        if (!room) return;

        // ✅ 준비 상태 체크: 방장을 제외한 모든 유저가 Ready 상태여야 함
        room.readyPlayers = room.readyPlayers || {};
        const ownerId = room.players[0];
        const nonOwnerPlayers = room.players.filter(id => id !== room.owner && !id.startsWith('ai_'));
        const allReady = nonOwnerPlayers.every(id => room.readyPlayers[id] === true);

        if (!allReady) {
          console.log("⛔ Ready하지 않은 유저가 있어서 게임 시작 불가");
          return;
        }

        // 부족한 플레이어 수만큼 AI 추가
        const currentPlayerIds = room.players;
        const playerCount = currentPlayerIds.length;

        const neededAIs = Math.max(0, 8 - playerCount);
        const aiCandidates = ['ai_1','ai_2','ai_3','ai_4','ai_5','ai_6','ai_7'];
        const usedIds = new Set(currentPlayerIds);
        const availableAIs = aiCandidates.filter(id => !usedIds.has(id)).slice(0, neededAIs);

        room.players.push(...availableAIs);

        // 모든 플레이어 구성
        const allPlayers = room.players.map(id => ({
          id,
          isAI: id.startsWith('ai_')
        }));

        // ai 닉네임
        for (const aiId of availableAIs) {
          playerNameMap.set(aiId, aiId);  // 또는 playerNameMap.set(aiId, "AI_" + aiId.split("_")[1]);
        }

        // 게임 인스턴스 생성 및 시작
        const game = new MafiaGame(allPlayers, data => broadcastToRoom(currentRoom, data), (playerId, msg) => sendTo(playerId, msg));
        room.game = game;

        // Ready 상태 초기화
        for (const id of room.players) {
          room.readyPlayers[id] = false;
        }

        broadcastToRoom(currentRoom, {
          type: 'update_ready',
          players: Object.entries(room.readyPlayers).map(([id, isReady]) => ({
            playerId: id,
            isReady
          }))
        });
        
        const playerList = room.players.map((id, index) => ({
          id,
          name: playerNameMap.get(id) || "???",
          slot: index,
          isOwner: index === 0,
          isAlive: game.players.find(p => p.id === id)?.alive ?? true
        }));

        broadcastToRoom(currentRoom, {
          type: 'room_info',
          roomId: currentRoom,
          roomName: room.name,
          players: playerList,
          isOwner: false // 클라이언트가 각자 판단
        });

        console.log("🎮 게임 시작!");
        game.startGame();
        return;
      }


      if (msg.type === 'set_ready') {
        const room = rooms[currentRoom];

        if (!room) return;

        room.readyPlayers = room.readyPlayers || {};
        room.readyPlayers[currentPlayerId] = msg.isReady;

        console.log(`✅ ${currentPlayerId} Ready 상태: ${msg.isReady}`);

        // 모든 유저에게 Ready 상태 브로드캐스트
        const update = {
          type: 'update_ready',
          players: Object.entries(room.readyPlayers).map(([id, isReady]) => ({
            playerId: id,
            isReady
          }))
        };

        broadcastToRoom(currentRoom, update);
      }

      if (msg.type === 'night_start') {
        const room = rooms[currentRoom];
        if (!room || !room.game) return;

        console.log("🌙 밤 시작됨!");
        room.game.startNight();  // AI 마피아/경찰/의사 행동 처리
      }

      if (msg.type === 'day_start') {
        const room = rooms[currentRoom];
        if (!room || !room.game) return;

        console.log("☀️ 낮 시작됨!");
        room.game.startDay();  // AI가 채팅 발언 출력 (콘솔)
      }

      if (msg.type === 'vote_start') {
        const room = rooms[currentRoom];
        if (!room || !room.game) return;

        console.log("🗳️ 투표 시작됨!");
        room.game.startVote();  // AI가 투표 대상 정하고 처리
      }

      if (msg.type === 'night_action') {
        const room = rooms[currentRoom];
        if (!room || !room.game) return;
        // 행동 저장 (유저용)
        room.game.receiveHumanNightAction(currentPlayerId, msg.action, msg.target);
      }

    } catch (err) {
      console.error("❌ 메시지 처리 오류:", err.message);
      console.error(err.stack);
    }
  });

  ws.on('close', () => {
    if (!currentPlayerId) return;

    socketMap.delete(currentPlayerId);
    nicknameSet.delete(currentPlayerId);

    const leavingName = playerNameMap.get(currentPlayerId);
    if (leavingName) {
      playerNameSet.delete(leavingName);
      playerNameMap.delete(currentPlayerId);
    }

    playerRoomMap.delete(currentPlayerId);
    delete clients[currentPlayerId];
    console.log(`🔴 연결 종료: ${currentPlayerId}`);

    if (currentRoom && rooms[currentRoom]) {
      exitPlayerFromRoom(currentPlayerId, currentRoom, {
        sendLeftRoomMessage: false,
        wasClosed: true
      });
    }
    
    broadcastPlayerList();
  });
});

console.log("🚀 메인 서버 실행 중 (port 3000)");

rooms["TestRoom1"] = {
  id: "TestRoom1",
  name: "Test1",
  players: ["test_user_1", "test_user_2", "test_user_3"]
};

rooms["TestRoom2"] = {
  id: "TestRoom2",
  name: "Test2",
  players: ["test_user_4", "test_user_5"]
};

rooms["TestRoom3"] = {
  id: "TestRoom3",
  name: "Test3",
  players: ["test_user_6", "test_user_7", "test_user_8", "test_user_9"]
};

rooms["TestRoom4"] = {
  id: "TestRoom4",
  name: "Test4",
  players: ["test_user_10"]
};

rooms["TestRoom5"] = {
  id: "TestRoom5",
  name: "Test5",
  players: ["test_user_11", "test_user_12"]
};

[
  "test_user_1", "test_user_2", "test_user_3", "test_user_4",
  "test_user_5", "test_user_6", "test_user_7", "test_user_8",
  "test_user_9", "test_user_10", "test_user_11", "test_user_12"
].forEach(id => {
  playerNameMap.set(id, id);
});
