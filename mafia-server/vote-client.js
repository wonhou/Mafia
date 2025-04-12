const WebSocket = require('ws');
const readline = require('readline');

const ws = new WebSocket('ws://localhost:3000');

ws.on('open', () => {
  console.log('✅ 서버에 연결됨');
  
  // 처음에 ID 등록
  ws.send(JSON.stringify({ type: "register", playerId: "user1" }));
});

ws.on('message', (data) => {
    const msg = JSON.parse(data);
    console.log("📨 받은 메시지:", msg);
  
    // ✅ start_vote 메시지가 오면 → 그때 입력받기!
    if (msg.type === 'start_vote') {
      askForVote();
    }
  });

function askForVote() {
  const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout
  });

  rl.question("🗳️ 투표할 대상 ID 입력: ", (target) => {
    const voteMsg = {
      type: "vote",
      from: "user1",
      target: target
    };
    ws.send(JSON.stringify(voteMsg));
    console.log(`📤 투표 보냄: user1 → ${target}`);
    rl.close();
  });
}
