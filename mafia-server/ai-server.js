const express = require('express');
const bodyParser = require('body-parser');
const app = express();
const port = 4000;

app.use(bodyParser.json());

const memory = {}; // AI들의 상태를 저장할 수 있는 객체 (선택적 사용)

// 1. 초기화 요청 처리
app.post('/init', (req, res) => {
  const { playerId, role, allPlayers, settings } = req.body;

  console.log(`🤖 ${playerId} 초기화됨`);
  console.log(`- 역할: ${role}`);
  console.log(`- 플레이어 목록: ${allPlayers.map(p => p.id).join(', ')}`);

  // AI 상태 저장 (선택 사항)
  memory[playerId] = {
    role,
    allPlayers,
    settings
  };

  res.sendStatus(200);
});

// 2. 밤 행동 요청 처리
app.post('/night-action', (req, res) => {
  const { playerId, role, alivePlayers, day } = req.body;

  // 무조건 랜덤 행동
  const target = alivePlayers[Math.floor(Math.random() * alivePlayers.length)];

  let action;
  if (role === 'mafia') action = 'kill';
  else if (role === 'doctor') action = 'save';
  else if (role === 'police') action = 'investigate';
  else action = 'none';

  console.log(`🤖 ${playerId} (${role}) → ${action} ${target}`);
  res.json({ action, target });
});

// (필요 시 앞으로 낮 행동이나 채팅 요청도 여기 추가할 수 있음)

app.listen(port, () => {
  console.log(`🤖 AI 서버 실행 중 (port ${port})`);
});
