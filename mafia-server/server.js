// server.js
const WebSocket = require('ws');
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
    const msg = JSON.parse(message.toString());

    if (msg.type === 'register') {
      const id = msg.playerId;
      socketMap.set(id, ws);
      console.log(`✅ ${id} 등록됨`);
      return;
    }

    if (msg.type === 'start_game') {
      game.startGame();

      // 역할 알려주기
      game.players.forEach((player) => {
        const ws = socketMap.get(player.id); // 여기!
        if (ws && ws.readyState === WebSocket.OPEN && !player.isAI) {
          ws.send(JSON.stringify({
            type: "your_role",
            role: player.role
          }));
        }
      });
      await game.startNight(); // 🌙 밤 턴 시작
      broadcast({ type: 'game_started', day: game.day, state: game.state });
    }
  
    if (msg.type === 'vote') {
      const { from, target } = msg;
      game.receiveVote(from, target);
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