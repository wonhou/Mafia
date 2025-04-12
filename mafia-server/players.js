// players.js
function createPlayers() {
  return [
    { id: 'user1', role: null, isAI: false, alive: true },
    { id: 'ai_1', role: null, isAI: true, alive: true },
    { id: 'ai_2', role: null, isAI: true, alive: true },
    { id: 'ai_3', role: null, isAI: true, alive: true },
    { id: 'ai_4', role: null, isAI: true, alive: true },
    { id: 'ai_5', role: null, isAI: true, alive: true },
    { id: 'ai_6', role: null, isAI: true, alive: true },
    { id: 'ai_7', role: null, isAI: true, alive: true }
  ];
}

module.exports = { createPlayers };