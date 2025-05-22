const WebSocket = require('ws');
const axios = require('axios');
const { createPlayers } = require('./players');
const MafiaGame = require('./game');

const wss = new WebSocket.Server({ port: 3000 });
const rooms = {}; // roomId -> { name, players: [playerIds], game }
const socketMap = new Map(); // playerId -> ws

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

wss.on('connection', (ws) => {
  let currentRoom = null;
  let currentPlayerId = null;

  ws.on('message', async (message) => {
    try {
      const msg = JSON.parse(message);

      if (msg.type === 'register') {
        currentPlayerId = msg.playerId;
        socketMap.set(currentPlayerId, ws);
        ws.send(JSON.stringify({ type: 'register_success', playerId: currentPlayerId }));
        return;
      }

      if (msg.type === 'create_room') {
        const roomId = generateRoomId();
        rooms[roomId] = {
          name: msg.roomName || '새로운 방',
          players: [msg.playerId],
          game: null
        };
        currentRoom = roomId;

        ws.send(JSON.stringify({ type: 'room_created', roomId, roomName: rooms[roomId].name }));
        return;
      }

      if (msg.type === 'join_room') {
        const { roomId, playerId } = msg;
        const room = rooms[roomId];

        if (!room) {
          ws.send(JSON.stringify({ type: 'error', message: '방이 존재하지 않습니다.' }));
          return;
        }

        if (room.players.includes(playerId)) {
        ws.send(JSON.stringify({ type: 'error', message: '이미 방에 참여한 플레이어입니다.' }));
        return;
        }

        room.players.push(playerId);
        currentRoom = roomId;
        broadcastToRoom(roomId, { type: 'player_joined', playerId });
        return;
      }

      if (msg.type === 'chat') {
        const room = rooms[currentRoom];
      if (room) {
        broadcastToRoom(currentRoom, {
          type: 'chat',
          from: currentPlayerId,
          text: msg.text
          });
        }
        return;
      }

      if (msg.type === 'start_game') {
        const room = rooms[msg.roomId];
        const players = room.players.map(id => ({
          id,
          role: null,
          isAI: id.startsWith('ai_'),
          alive: true
        }));
        const game = new MafiaGame(players, (data) => broadcastToRoom(msg.roomId, data));
        room.game = game;
        game.startGame();

        players.forEach((player) => {
          const socket = socketMap.get(player.id);
          if (!player.isAI && socket && socket.readyState === WebSocket.OPEN) {
            socket.send(JSON.stringify({ type: 'your_role', role: player.role }));
          } else if (player.isAI) {
            axios.post('http://localhost:4000/init', {
              type: 'init',
              playerId: player.id,
              role: player.role,
              allPlayers: players,
              settings: {
                totalPlayers: players.length,
                numMafia: players.filter(p => p.role === 'mafia').length,
                numDoctor: players.filter(p => p.role === 'doctor').length,
                numPolice: players.filter(p => p.role === 'police').length,
              }
            }).then(() => {
              console.log(`✅ ${player.id} AI 초기화 완료`);
            }).catch((err) => {
              console.error(`❌ ${player.id} AI 초기화 실패:`, err.message);
            });
          }
        });

        broadcastToRoom(msg.roomId, { type: 'game_started', day: game.day });
        await game.startNight();
        return;
      }

      if (msg.type === 'vote') {
        const room = rooms[msg.roomId];
        if (room?.game) {
          room.game.receiveVote(msg.from, msg.target);
        }
      }

    } catch (err) {
      console.error("❌ 메시지 처리 오류:", err.message);
    }
  });

  ws.on('close', () => {
    if (currentPlayerId) {
      socketMap.delete(currentPlayerId);
      if (currentRoom && rooms[currentRoom]) {
        rooms[currentRoom].players = rooms[currentRoom].players.filter(id => id !== currentPlayerId);
        broadcastToRoom(currentRoom, { type: 'player_left', playerId: currentPlayerId });
      }
    }
  });
});

console.log("🚀 Room 기반 AI 마피아 서버 실행 중 (port 3000)");
