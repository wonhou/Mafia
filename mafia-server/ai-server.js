const express = require('express');
const app = express();
app.use(express.json());

app.post('/night-action', (req, res) => {
  const { playerId, role, alivePlayers } = req.body;

  // 살아있는 사람 중에서 자기 제외한 아무나 선택
  const targets = alivePlayers.filter(id => id !== playerId);
  const target = targets[Math.floor(Math.random() * targets.length)];

  // 역할에 따라 액션 종류 다르게
  let action = 'none';
  if (role === 'mafia') action = 'kill';
  if (role === 'doctor') action = 'save';
  if (role === 'police') action = 'investigate';

  console.log(`🔮 AI ${playerId} (${role}) → ${action} ${target}`);

  res.json({ action, target });
});

app.listen(4000, () => {
  console.log('🧠 AI 서버 실행 중 (port 4000)');
});