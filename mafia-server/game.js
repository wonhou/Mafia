const axios = require('axios');

class MafiaGame {
  constructor(players, broadcastFunc, sendToFunc) {
    this.players = players;
    this.broadcast = broadcastFunc;
    this.sendTo = sendToFunc;
    this.state = 'waiting';
    this.day = 0;
    this.votes = {};
    this.chatHistory = [];
    this.lastInvestigation = null;
    this.lastSaved = null;
  }

  startGame() {
    this.assignRoles();
    this.broadcastRoles();  // ✅ 여기서 각 유저에게 역할 전달
    this.startNight();
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

  broadcastRoles() {
    for (const p of this.players) {
      if (p.id.startsWith("ai_")) continue;  // AI는 제외

      this.sendTo(p.id, {
        type: "your_role",
        role: p.role
      });
    }
  }

  async startNight() {
    this.state = 'night';
    console.log(`🌙 밤 ${this.day} 시작`);

    const nightActions = await this.collectNightActions();
    await this.handleNightActions(nightActions);

    // 🔔 밤 10초 후 낮 시작
    setTimeout(() => {
      this.state = 'day';
      this.startDay();
    }, 10000);
  }

  async collectNightActions() {
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

    return nightActions;
  }

  async handleNightActions(nightActions) {
    console.log(`🩸 밤 ${this.day} 행동 처리 중.`);

    let mafiaTargets = [];
    let doctorTarget = null;
    let policeTarget = null;

    nightActions.forEach(({ action }) => {
      if (action.action === 'kill') mafiaTargets.push(action.target);
      else if (action.action === 'save') doctorTarget = action.target;
      else if (action.action === 'investigate') policeTarget = action.target;
    });

    // 마피아 투표 집계
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

    // 경찰 조사 정보 저장
    const police = this.players.find(p => p.role === 'police' && p.alive);
    if (police && policeTarget) {
      const investigated = this.players.find(p => p.id === policeTarget);
      if (investigated) {
        this.lastInvestigation = {
          policeId: police.id,
          target: investigated.id,
          isMafia: investigated.role === 'mafia'
        };
      }
    }

    // 의사 보호 정보 저장
    const doctor = this.players.find(p => p.role === 'doctor' && p.alive);
    if (doctor && doctorTarget) {
      this.lastSaved = {
        doctorId: doctor.id,
        saved: doctorTarget
      };
    }

    // 결과 브로드캐스트
    this.broadcast({
      type: "night_result",
      killed: targetToKill ?? null,
      saved: doctorTarget ?? null,
      investigated: policeTarget ?? null
    });
  }

  async startDay() {
    this.state = 'day';
    console.log(`🌞 낮 ${this.day} 시작`);
    this.votes = {};

    this.broadcast({
      type: 'day_start',
      message: `낮 ${this.day}이 시작되었습니다. 자유롭게 토론하세요.`
    });

    await this.sendChatPhase();  // 시간 기반 발언

    await this.startVote();
  }

  async sendChatPhase() {
    const aliveAIs = this.players.filter(p => p.isAI && p.alive);
    const endTime = Date.now() + 15000;  // 테스트용 낮 턴 제한 시간: 15초

    const speakLoop = async (ai) => {
      while (Date.now() < endTime && this.state === 'day') {
        const delay = Math.floor(Math.random() * 2000) + 1000; // 1000~3000ms
        await new Promise(resolve => setTimeout(resolve, delay));

        const isPolice = ai.role === 'police';
        const isDoctor = ai.role === 'doctor';

        const investigation = (isPolice && this.lastInvestigation?.policeId === ai.id)
          ? { target: this.lastInvestigation.target, isMafia: this.lastInvestigation.isMafia }
          : null;

        const savedInfo = (isDoctor && this.lastSaved?.doctorId === ai.id)
          ? { saved: this.lastSaved.saved }
          : null;

        try {

          const res = await axios.post(`http://localhost:4000/chat-request`, {
            playerId: ai.id,
            history: this.chatHistory,
            day: this.day,
            investigation,
            savedInfo
          });

          const message = res.data.message;

          if (message && message !== "...") {
            this.chatHistory.push({ sender: ai.id, message });
            this.broadcast({
              type: "chat",
              sender: ai.id,
              message
            });
            console.log(`💬 ${ai.id}: ${message}`);
          }
        } catch (err) {
          console.error(`❌ ${ai.id} 채팅 실패:`, err.message);
        }
      }
    };

    // 동시에 AI 발언 루프 시작
    const loops = aliveAIs.map(ai => speakLoop(ai));
    await Promise.all(loops);  // 모든 루프가 끝날 때까지 대기

    this.lastInvestigation = null;
    this.lastSaved = null;
  }

  async startVote() {
    console.log("🗳️ 투표 시작됨!");

    const alivePlayerIds = this.players.filter(p => p.alive).map(p => p.id);

    this.broadcast({
      type: 'start_vote',
      alivePlayers: alivePlayerIds
    });

    const aliveAIs = this.players.filter(p => p.isAI && p.alive);

    for (const ai of aliveAIs) {
      try {

        const availableTargets = alivePlayerIds.filter(id => id !== ai.id);

        const res = await axios.post(`http://localhost:4000/vote-suggestion`, {
          playerId: ai.id,
          history: this.chatHistory,
          alivePlayers: availableTargets
        });

        const target = res.data.target;
        this.receiveVote(ai.id, target);
      } catch (err) {
        console.error(`❌ 투표 추천 실패 (${ai.id}):`, err.message);
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