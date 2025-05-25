const WebSocket = require('ws');
const axios = require('axios');
const { createPlayers } = require('./players');
const MafiaGame = require('./game');

const wss = new WebSocket.Server({ port: 3000 });

const rooms = {};                     // roomId -> { name, players: [playerIds], game }
const socketMap = new Map();          // playerId -> WebSocket
const nicknameSet = new Set();        // playerId 중복 방지 (과거 방식)
const playerNameSet = new Set();      // playerName 중복 방지
const playerNameMap = new Map();      // playerId -> playerName

function generateRoomId() {
  return Math.random().toString(36).substring(2, 8);
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
  broadcastToRoom(roomId, { type: 'player_left', playerId });

  if (room.players.length === 0) {
    delete rooms[roomId];
    console.log(`🗑️ 방 삭제됨 (${wasClosed ? "disconnect" : "leave"}): ${roomId}`);
  } else if (wasOwner) {
    const newOwner = room.players.find(id => !id.startsWith('ai_'));
    if (newOwner) {
      broadcastToRoom(roomId, { type: 'new_owner', playerId: newOwner });
      console.log(`👑 방장 변경: ${newOwner} → ${roomId}`);
    } else {
      room.players.forEach(aiId => {
        const aiSocket = socketMap.get(aiId);
        if (aiSocket?.readyState === WebSocket.OPEN) {
          aiSocket.send(JSON.stringify({ type: 'room_destroyed', roomId }));
        }
      });
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

      console.log("💌 수신된 메시지:", msg); // ✅ 여기!

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
        nicknameSet.add(currentPlayerId);         // 유지 (optional)
        playerNameSet.add(name);
        playerNameMap.set(currentPlayerId, name);

        ws.send(JSON.stringify({ type: 'register_success', playerId: currentPlayerId, playerName: name }));
        console.log(`🟢 등록됨: [${currentPlayerId}] ${name}`);

        broadcastPlayerList();
        return;
      }

      if (msg.type === "create_room") {
      const roomId = "Room_" + Math.random().toString(36).substring(2, 5); // 예시 ID 생성
      const roomName = msg.roomName || "Untitled Room";

      rooms[roomId] = {
          id: roomId,
          name: roomName,
          players: [currentPlayerId],
      };

    console.log(`🟢 방 생성됨: [${roomId}] ${roomName}`); // ✅ 원하는 형식의 로그

    ws.send(JSON.stringify({
        type: "room_created",
        roomId: roomId,
        roomName: roomName
    }));
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

    console.log(`🔴 연결 종료: ${currentPlayerId}`);

    if (currentRoom && rooms[currentRoom]) {
      exitPlayerFromRoom(currentPlayerId, currentRoom, {
        sendLeftRoomMessage: false,
        wasClosed: true
      });
    }
  });
});

console.log("🚀 메인 서버 실행 중 (port 3000)");
