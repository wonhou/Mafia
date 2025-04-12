const axios = require('axios');

class MafiaGame {
  constructor(players, broadcastFunc) {
    this.players = players;
    this.broadcast = broadcastFunc;
    this.state = 'waiting';
    this.day = 0;
    this.votes = {};
    this.chatHistory = []; // 전체 누적 대화 기록
  }

  startGame() {
    this.assignRoles();
    this.state = 'night';
    this.day = 1;
  }

  assignRoles() {
    const roles = [
      'mafia', 'mafia',
      'police',
      'doctor',
      'citizen', 'citizen', 'citizen', 'citizen'
    ];

    const shuffled = roles.sort(() => Math.random() - 0.5);

    this.players.forEach((player, index) => {
      player.role = shuffled[index];
      player.alive = true;
      console.log(`🃏 ${player.id} → ${player.role}`);
    });
  }

  async startNight() {
    this.state = 'night';
    console.log(`🌙 밤 ${this.day} 시작`);

    const aliveAIs = this.players.filter(p => p.isAI && p.alive);
    const nightActions = [];

    for (const ai of aliveAIs) {
      const payload = {
        playerId: ai.id,
        role: ai.role,
        alivePlayers: this.players.filter(p => p.alive).map(p => p.id),
        day: this.day
      };

      try {
        const res = await axios.post(`http://localhost:4000/night-action`, payload);
        nightActions.push({ playerId: ai.id, action: res.data });
      } catch (err) {
        console.error(`❌ ${ai.id} 응답 실패:`, err.message);
      }
    }

    this.handleNightActions(nightActions);
  }

  handleNightActions(nightActions) {
    console.log(`🩸 밤 ${this.day} 행동 처리 중...`);

    let mafiaTargets = [];
    let doctorTarget = null;
    let policeTarget = null;

    nightActions.forEach(({ playerId, action }) => {
      if (action.action === 'kill') mafiaTargets.push(action.target);
      else if (action.action === 'save') doctorTarget = action.target;
      else if (action.action === 'investigate') policeTarget = action.target;
    });

    const killCounts = {};
    mafiaTargets.forEach(id => {
      killCounts[id] = (killCounts[id] || 0) + 1;
    });

    let targetToKill = null;
    let maxVotes = 0;
    for (const [target, count] of Object.entries(killCounts)) {
      if (count > maxVotes) {
        maxVotes = count;
        targetToKill = target;
      }
    }

    if (targetToKill && targetToKill === doctorTarget) {
      console.log(`💉 의사가 ${targetToKill}을 살렸습니다!`);
      targetToKill = null;
    }

    if (targetToKill) {
      const victim = this.players.find(p => p.id === targetToKill);
      if (victim) {
        victim.alive = false;
        console.log(`☠️ ${victim.id} 님이 사망했습니다`);
      }
    } else {
      console.log('🌙 이번 밤에는 아무도 죽지 않았습니다');
    }

    if (policeTarget) {
      const investigated = this.players.find(p => p.id === policeTarget);
      console.log(`🔎 경찰이 조사한 대상: ${policeTarget} (${investigated?.role})`);
    }

    this.broadcast({
      type: "night_result",
      killed: targetToKill ?? null,
      saved: doctorTarget ?? null,
      investigated: policeTarget ?? null
    });

    this.state = 'day';
    this.startDay();
  }

  async startDay() {
    this.state = 'day';
    console.log(`🌞 낮 ${this.day} 시작`);

    this.votes = {};

    this.broadcast({
      type: 'day_start',
      message: `낮 ${this.day}이 시작되었습니다. 투표를 준비하세요.`
    });

    await this.sendChatRequests(); // AI에게 채팅 요청

    this.broadcast({
      type: 'start_vote',
      alivePlayers: this.players.filter(p => p.alive).map(p => p.id)
    });

    const aliveAIs = this.players.filter(p => p.isAI && p.alive);
    aliveAIs.forEach(ai => {
      const targets = this.players.filter(p => p.alive && p.id !== ai.id);
      const randomTarget = targets[Math.floor(Math.random() * targets.length)];
      if (randomTarget) {
        this.receiveVote(ai.id, randomTarget.id);
      }
    });
  }

  async sendChatRequests() {
    const aliveAIs = this.players.filter(p => p.isAI && p.alive);

    for (const ai of aliveAIs) {
      try {
        const res = await axios.post(`http://localhost:4000/chat-request`, {
          playerId: ai.id,
          history: this.chatHistory,
          day: this.day
        });

        const message = res.data.message;
        this.chatHistory.push({ sender: ai.id, message });

        this.broadcast({
          type: "chat",
          sender: ai.id,
          message
        });

        console.log(`💬 ${ai.id}: ${message}`);
      } catch (err) {
        console.error(`❌ ${ai.id} 채팅 실패:`, err.message);
      }
    }
  }

  receiveVote(from, target) {
    this.votes[from] = target;
    console.log(`🗳️ ${from} → ${target}`);

    const totalVotesNeeded = this.players.filter(p => p.alive).length;
    const voteCount = Object.keys(this.votes).length;

    if (voteCount >= totalVotesNeeded) {
      this.resolveVote();
    }
  }

  resolveVote() {
    const voteResult = {};
    Object.values(this.votes).forEach(target => {
      voteResult[target] = (voteResult[target] || 0) + 1;
    });

    let maxVotes = 0;
    let targetToKill = null;
    for (const [target, count] of Object.entries(voteResult)) {
      if (count > maxVotes) {
        maxVotes = count;
        targetToKill = target;
      }
    }

    if (targetToKill) {
      const victim = this.players.find(p => p.id === targetToKill);
      if (victim) {
        victim.alive = false;
        console.log(`⚰️ 투표로 ${victim.id}가 처형되었습니다`);
      }
    }

    this.broadcast({
      type: 'vote_result',
      executed: targetToKill ?? null
    });

    const winner = this.checkWinCondition();
    if (winner) {
      this.broadcast({ type: 'game_over', winner });
      return;
    }

    this.day++;
    this.startNight();
  }

  checkWinCondition() {
    const aliveMafia = this.players.filter(p => p.alive && p.role === 'mafia').length;
    const aliveCitizens = this.players.filter(p => p.alive && p.role !== 'mafia').length;

    if (aliveMafia === 0) return 'citizen';
    if (aliveMafia >= aliveCitizens) return 'mafia';
    return null;
  }
}

module.exports = MafiaGame;
