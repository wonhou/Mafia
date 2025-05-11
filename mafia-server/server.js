// server.js
const WebSocket = require('ws');
const axios = require('axios');
const MafiaGame = require('./game');
const { createPlayers } = require('./players');

const wss = new WebSocket.Server({ port: 3000 });
const players = createPlayers();
const game = new MafiaGame(players, broadcast);
const socketMap = new Map(); // key: playerId, value: WebSocket
const nicknameSet = new Set();
const rooms = {};  // 모든 방 저장용

function generateRoomId() {
  return Math.random().toString(36).substring(2, 8);  // 예: "a2b9k1"
}

function broadcast(data) {
  const json = JSON.stringify(data);
  wss.clients.forEach((client) => {
    if (client.readyState === WebSocket.OPEN) {
      client.send(json);
    }
  });
}

function broadcastPlayerList() {
  broadcast({
    type: "update_players",
    players: [...socketMap.keys()]
  });
}

wss.on('connection', (ws) => {
  console.log('✅ 클라이언트 연결됨');

  // 유저가 "start_game" 요청 시 게임 시작
  ws.on('message', async (message) => {
    try {
      const msg = JSON.parse(message.toString());

      // register 처리 부분
      if (msg.type === 'register') {
        const id = msg.playerId;
        if (nicknameSet.has(id)) {
          ws.send(JSON.stringify({
            type: 'register_failed',
            message: '이미 등록된 닉네임입니다.'
          }));
          return;
        }
        socketMap.set(id, ws);
        nicknameSet.add(id);
        console.log(`✅ ${id} 등록됨`);
      
        // 접속 목록 전파
        broadcastPlayerList();
        return;
      }

      if (msg.type === 'create_room') {
        const roomId = generateRoomId();
        rooms[roomId] = {
          name: msg.roomName || "무제방",
          players: [msg.playerId]
        };

        const ws = socketMap.get(msg.playerId);
        if (ws && ws.readyState === WebSocket.OPEN) {
          ws.send(JSON.stringify({
            type: 'room_created',
            roomId: roomId,
            roomName: rooms[roomId].name
          }));
        }

        console.log(`${msg.playerId} 가 방을 생성함: ${rooms[roomId].name} (${roomId})`);
        return;
      } 

      if (msg.type === 'start_game') {
        game.startGame();

        // 유저에게 역할 전달
        game.players.forEach((player) => {
          const ws = socketMap.get(player.id);
          if (ws && ws.readyState === WebSocket.OPEN && !player.isAI) {
            ws.send(JSON.stringify({
              type: "your_role",
              role: player.role
            }));
          }
        });

        // AI에게 init 메시지 전달
        const allPlayers = game.players.map(p => ({
          id: p.id,
          isAI: p.isAI
        }));

        game.players.forEach((player) => {
          if (player.isAI) {
            axios.post(`http://localhost:4000/init`, {
              type: "init",
              playerId: player.id,
              role: player.role,
              allPlayers: allPlayers,
              settings: {
                totalPlayers: game.players.length,
                numMafia: game.players.filter(p => p.role === "mafia").length,
                numDoctor: game.players.filter(p => p.role === "doctor").length,
                numPolice: game.players.filter(p => p.role === "police").length
              }
            }).then(() => {
              console.log(`✅ ${player.id} 초기화 메시지 전송됨`);
            }).catch((err) => {
              console.error(`❌ ${player.id} 초기화 실패:`, err.message);
            });
          }
        });

        await game.startNight();
        broadcast({ type: 'game_started', day: game.day, state: game.state });
      }

      if (msg.type === 'vote') {
        const { from, target } = msg;
        game.receiveVote(from, target);
      }    

    } catch (err) {
      console.error("❌ 메시지 처리 중 오류:", err.message);
    }
  });

  ws.on('close', () => {
    for (const [id, socket] of socketMap.entries()) {
      if (socket === ws) {
        socketMap.delete(id);
        nicknameSet.delete(id);
        console.log(`❌ 연결 종료: ${id}`);
        broadcastPlayerList(); // 플레이어 목록 갱신
        break;
      }
    }
  
    // 온라인 목록 업데이트
    broadcast({
      type: "update_players",
      players: [...socketMap.keys()]
    });
  });
});

console.log('🚀 서버 실행 중 (port 3000)');