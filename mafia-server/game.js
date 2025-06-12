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
    this.humanNightActions = {};
    this.isAlive = true;
  }

  terminate() {
    this.isAlive = false;
    this.players = [];
    this.broadcast = () => {};  // noop 처리
    this.sendTo = () => {};
    console.log("🛑 MafiaGame 인스턴스가 종료되었습니다");
  }

  startGame() {
    this.assignRoles();
    this.broadcastRoles();
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

  getRoleOf(playerId) {
    const player = this.players.find(p => p.id === playerId);
    return player?.role || null;
  }


  async startNight() {
    this.state = 'night';
    this.humanNightActions = {};
    console.log(`🌙 밤 ${this.day} 시작`);

    // 밤 행동은 15초 후에 처리
    setTimeout(async () => {
      if (!this.isAlive) return;
      const nightActions = await this.collectNightActions();
      if (!this.isAlive) return;
      await this.handleNightActions(nightActions);
      if (!this.isAlive) return;

      this.state = 'day';
      this.startDay();
    }, 15000);
  }


  receiveHumanNightAction(playerId, action, target) {
    console.log(`🧑‍🤝‍🧑 유저 밤 행동 수신: ${playerId} -> ${action} ${target}`);
    this.humanNightActions[playerId] = { action, target };
  }

  async collectNightActions() {
    const nightActions = [];

    // ▶ AI 플레이어들의 행동 수집
    const aliveAIs = this.players.filter(p => p.isAI && p.alive);
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

    // ▶ 사람이 제출한 행동도 포함
    const aliveHumans = this.players.filter(p => !p.isAI && p.alive);
    for (const human of aliveHumans) {
      const saved = this.humanNightActions?.[human.id];
      if (saved) {
        nightActions.push({ playerId: human.id, action: saved });
      } else {
        console.warn(`⚠️ ${human.id}의 밤 행동이 제출되지 않았습니다`);
      }
    }

    return nightActions;
  }


  async handleNightActions(nightActions) {
    if (!this.isAlive) return;
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

        this.broadcast({
          type: "player_eliminated",
          deadPlayers: [victim.id],
          reason: "night"
        });
      }
    } else {
      console.log('🌙 이번 밤에는 아무도 죽지 않았습니다');
    }

      // ✅ saved 메시지 추가 (죽은 사람 없음 + 의사가 살림)
    if (doctorTarget && mafiaTargets.includes(doctorTarget)) {
      this.broadcast({
        type: "player_eliminated",
        deadPlayers: [],
        reason: "saved"
      });
    } else {
      this.broadcast({
        type: "player_eliminated",
        deadPlayers: [],
        reason: "none"
      });
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
    if (!this.isAlive) return;
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
    if (!this.isAlive) return;
    const aliveAIs = this.players.filter(p => p.isAI && p.alive);
    const endTime = Date.now() + 120000;  // 낮 턴 제한 시간: 2분

    // 각 AI당 발언 횟수 2~3회로 제한
    const speakCountMap = {};
    for (const ai of aliveAIs) {
      speakCountMap[ai.id] = 2 + Math.floor(Math.random() * 2);  // 2~3회
    }

    // 랜덤 순서를 만들기 위한 섞기 함수
    const shuffle = arr => arr.sort(() => Math.random() - 0.5);

    const speakOnce = async (ai) => {
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

        if (!this.isAlive) return;
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
    };

    // 메인 루프: 2분 동안 무작위 순서로 AI들이 돌아가며 말함
    while (Date.now() < endTime && this.state === 'day') {
      const shuffled = shuffle(aliveAIs);

      for (const ai of shuffled) {
        if (Date.now() >= endTime || this.state !== 'day') break;

        if (speakCountMap[ai.id] > 0) {
          await speakOnce(ai);
          speakCountMap[ai.id]--;
          
          if (!this.isAlive) return;

          // 말한 후 2~5초 쉬기
          const delay = Math.floor(Math.random() * 3000) + 2000;
          await new Promise(resolve => setTimeout(resolve, delay));
        }
      }

      // 남은 발언 기회 없으면 종료
      const hasMore = Object.values(speakCountMap).some(cnt => cnt > 0);
      if (!hasMore) break;
    }

    this.lastInvestigation = null;
    this.lastSaved = null;
  }


  async startVote() {
    if (!this.isAlive) return;
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
        this.broadcast({
          type: "player_eliminated",
          deadPlayers: [victim.id],
          reason: "vote"
        });
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