// server.js
const WebSocket = require('ws');
const axios = require('axios');
const MafiaGame = require('./game');
const { createPlayers } = require('./players');

const wss = new WebSocket.Server({ port: 3000 });
const players = createPlayers();
const game = new MafiaGame(players, broadcast);
const socketMap = new Map(); // key: playerId, value: WebSocket


wss.on('connection', (ws) => {
  console.log('✅ 클라이언트 연결됨');

  // 유저가 "start_game" 요청 시 게임 시작
  ws.on('message', async (message) => {
    try {
      const msg = JSON.parse(message.toString());

      if (msg.type === 'register') {
        const id = msg.playerId;
        socketMap.set(id, ws);
        console.log(`✅ ${id} 등록됨`);
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
        console.log(`❌ 연결 종료: ${id}`);
        break;
      }
    }
  });
});

function broadcast(data) {
  const json = JSON.stringify(data);
  wss.clients.forEach((client) => {
    if (client.readyState === WebSocket.OPEN) {
      client.send(json);
    }
  });
}

console.log('🚀 서버 실행 중 (port 3000)');