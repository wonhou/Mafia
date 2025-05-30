const WebSocket = require("ws");

const SERVER_URL = "ws://localhost:3000";

const aiList = [
  { id: "ai_1", name: "봇_1" },
  { id: "ai_2", name: "봇_2" },
  { id: "ai_3", name: "봇_3" },
  { id: "ai_4", name: "봇_4" },
  { id: "ai_5", name: "봇_5" },
];

aiList.forEach((ai, index) => {
  const socket = new WebSocket(SERVER_URL);

  socket.on("open", () => {
    console.log(`🤖 ${ai.name} 접속 완료`);
    socket.send(JSON.stringify({
      type: "register",
      playerName: ai.name
    }));
  });

  socket.on("message", (data) => {
    const msg = JSON.parse(data);
    if (msg.type === "update_players") {
      console.log(`📡 [${ai.name}] 현재 접속자 수: ${msg.players.length}`);
    }
  });

  socket.on("close", () => {
    console.log(`❌ ${ai.name} 연결 종료`);
  });

  socket.on("error", (err) => {
    console.error(`🔥 ${ai.name} 에러 발생:`, err.message);
  });
});