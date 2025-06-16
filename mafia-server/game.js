const axios = require('axios');

class MafiaGame {
  constructor(roomId, players, broadcastFunc, sendToFunc, roomsRef) {
    this.roomId = roomId;
    this.rooms = roomsRef;
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

  isNight() {
    return this.state === 'night';
  }
  
  terminate() {
    this.isAlive = false;
    this.players = [];
    this.chatHistory = [];
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
    const mafiaIds = this.players
      .filter(p => p.role === 'mafia')
      .map(p => p.id);

    for (const p of this.players) {
      if (p.role === "mafia") {
        this.sendTo(p.id, {
          type: "mafia_info",
          mafiaIds: mafiaIds
        });
      }
    }
  }

  getRoleOf(playerId) {
    const player = this.players.find(p => p.id === playerId);
    return player?.role || null;
  }


  async startNight() {
    if (!this.isAlive) return;
    this.day++;
    this.state = 'night';
    this.humanNightActions = {};
    console.log(`🌙 밤 ${this.day} 시작`);

    const systemMsg = `${this.day}번째 밤입니다. 마피아, 의사, 경찰은 행동을 선택하세요.`;
    this.broadcast({
      type: "night_start",
      message: systemMsg
    });
    this.chatHistory.push({
      sender: "system",
      message: systemMsg
    });

    //ai마피아 행동
    const aiMafias = this.players.filter(p => p.isAI && p.role === 'mafia' && p.alive);
    this.pendingMafiaActions = await Promise.all(
      aiMafias.map(mafia => this.getMafiaAction(mafia))
    );

    setTimeout(() => {
      if (!this.isAlive) return;
      this.broadcast({
        type: "night_end"
      });
    }, 14000); // 1초 여유

    setTimeout(async () => {
      if (!this.isAlive) return;
      const nightActions = await this.collectNightActions();
      if (!this.isAlive) return;
      const gameEnded = await this.handleNightActions(nightActions);
      if (!this.isAlive || gameEnded) return;

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

    if (this.pendingMafiaActions?.length > 0) {
      nightActions.push(...this.pendingMafiaActions.filter(Boolean));
    }

    // 기타 AI 역할은 기존 방식
    const otherAIs = this.players.filter(p =>
      p.isAI && p.alive && p.role !== 'mafia'
    );

    const otherAIActions = await Promise.all(otherAIs.map(async ai => {
      if (!this.isAlive) return null;

      const payload = {
        roomId: this.roomId,
        playerId: ai.id,
        role: ai.role,
        alivePlayers: this.players.filter(p => p.alive).map(p => p.id),
        day: this.day
      };

      try {
        if (!this.isAlive) return null;
        const res = await axios.post("http://localhost:4000/night-action", payload);
        if (!this.isAlive) return null;
        return { playerId: ai.id, action: res.data };
      } catch (err) {
        console.error(`❌ ${ai.id} 응답 실패:`, err.message);
        return null;
      }
    }));

    nightActions.push(...otherAIActions.filter(Boolean));

    // 사람 플레이어의 행동 포함
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

  async getMafiaAction(mafiaPlayer) {
    try {
      if (!this.isAlive) return null;
      const roomId = this.roomId;
      const playerId = mafiaPlayer.id;
      const aliveTargets = this.players
        .filter(p => p.alive && p.id !== playerId)
        .map(p => p.id);

      // 1. 밤 대화 여러 번 수행
      const chatCount = 2 + Math.floor(Math.random() * 2); // 2~3회
      for (let i = 0; i < chatCount; i++) {
        const chatRes = await axios.post("http://localhost:4000/mafia-night-chat", {
          roomId,
          playerId,
          history: [], // 지금은 공백, 향후 최근 기록 반영 가능
          day: this.day,
          alivePlayers: aliveTargets
        });

        if (!this.isAlive) return null;
        const message = chatRes.data?.message?.trim();

        if (message && message !== "..." && message !== "에러") {
          // 마피아끼리 공유
          for (const p of this.players) {
            if (p.role === 'mafia' && p.alive) {
              this.sendTo(p.id, {
                type: "chat",
                sender: playerId,
                message
              });
            }
          }
          console.log(`💬 [밤채팅] ${playerId}: ${message}`);
        }

        // 채팅 사이 딜레이 (1~2초)
        await new Promise(resolve => setTimeout(resolve, 1000 + Math.random() * 1000));
      }
      if (!this.isAlive) return null;
      // 2. 밤 행동 전 딜레이 (3초)
      await new Promise(resolve => setTimeout(resolve, Math.random() * 3000));
      if (!this.isAlive) return null;
      // 3. 타겟 선택 요청
      const res = await axios.post("http://localhost:4000/night-action", {
        roomId,
        playerId,
        role: mafiaPlayer.role,
        alivePlayers: aliveTargets,
        day: this.day
      });
      if (!this.isAlive) return null;

      const target = res.data?.target?.trim();

      console.log(`[NightAction] ${mafiaPlayer.id} → ${target}`);

      if (!target || target === "..." || target === "에러") {
        console.warn(`❌ 마피아 타겟 선택 실패: ${playerId} → 응답 불완전`);
        return null;
      }

      return {
        playerId,
        action: "kill",
        target
      };

    } catch (err) {
      console.warn(`⚠️ 마피아 행동 실패 (${mafiaPlayer.id}): ${err.message}`);
      return null;
    }
  }

  async handleNightActions(nightActions) {
    if (!this.isAlive) return;
    console.log(`🩸 밤 ${this.day} 행동 처리 중.`);

    let mafiaVotes = [];
    let doctorTarget = null;
    let policeTarget = null;

    // 모든 행동 수집
    nightActions.forEach(entry => {
      const { playerId, action } = entry;

      if (action === 'kill' && entry.target) {
        // AI 마피아의 응답 구조: { playerId, action: 'kill', target: 'ai_3' }
        mafiaVotes.push({ playerId, target: entry.target });
      }
      else if (action?.action === 'kill') {
        // 일반적인 응답 구조: { playerId, action: { action: 'kill', target: 'ai_3' } }
        mafiaVotes.push({ playerId, target: action.target });
      }
      else if (action?.action === 'save') {
        doctorTarget = action.target;
      }
      else if (action?.action === 'investigate') {
        policeTarget = action.target;
      }
    });

    // 마피아 타겟별 득표 수 계산
    const killCounts = {};
    mafiaVotes.forEach(({ target }) => {
      killCounts[target] = (killCounts[target] || 0) + 1;
    });

    let maxVotes = Math.max(...Object.values(killCounts));
    const topTargets = Object.entries(killCounts)
      .filter(([_, count]) => count === maxVotes)
      .map(([target]) => target);

    let targetToKill = null;

    if (topTargets.length === 1) {
      targetToKill = topTargets[0];
    } else {
      const mafiaPlayerIds = mafiaVotes.map(v => v.playerId);
      const aiCount = mafiaPlayerIds.filter(id => id.startsWith('ai_')).length;
      const humanCount = mafiaPlayerIds.length - aiCount;

      if (aiCount === 2 && humanCount === 0) {
        //AI 마피아 타겟 분산 → 무작위 선택
        targetToKill = topTargets[Math.floor(Math.random() * topTargets.length)];
      } else if (aiCount === 1 && humanCount === 1) {
        const humanTarget = mafiaVotes.find(v => !v.playerId.startsWith('ai_'))?.target;
        if (humanTarget) {
          //혼합 마피아 → 사람 마피아 타겟 우선
          targetToKill = humanTarget;
        }
      } else {
        //사람 마피아 의견 불일치 → 처형 무효
      }
    }

    // 실제 사망 처리
    if (targetToKill && targetToKill !== doctorTarget) {
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
    } else if (doctorTarget && mafiaVotes.map(v => v.target).includes(doctorTarget)) {
      console.log(`💉 의사가 ${doctorTarget}을 살렸습니다!`);
      this.broadcast({
        type: "player_eliminated",
        deadPlayers: [],
        reason: "saved",
        savedId: doctorTarget
      });
    } else {
      console.log('🌙 이번 밤에는 아무도 죽지 않았습니다');
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

        try {
          await axios.post(`http://localhost:4000/night-summary`, {
            roomId: this.roomId,
            role: 'police',
            playerId: police.id,
            day: this.day,
            data: {
              target: investigated.id,
              isMafia: investigated.role === 'mafia'
            }
          });
        } catch (err) {
          console.warn(`⚠️ AI 서버 연결 실패 (police: ${police.id}): ${err.message}`);
        }
      }
    }

    // 의사 보호 정보 저장
    const doctor = this.players.find(p => p.role === 'doctor' && p.alive);
    if (doctor && doctorTarget) {
      this.lastSaved = {
        doctorId: doctor.id,
        saved: doctorTarget
      };

      try {
        await axios.post(`http://localhost:4000/night-summary`, {
          roomId: this.roomId,
          role: 'doctor',
          playerId: doctor.id,
          day: this.day,
          data: { target: doctorTarget }
        });
      } catch (err) {
        console.warn(`⚠️ AI 서버 연결 실패 (doctor: ${doctor.id}): ${err.message}`);
      }
    }

    // 마피아 night-summary 저장
    const mafiaPlayers = this.players.filter(p => p.role === 'mafia' && p.alive);
    for (const mafia of mafiaPlayers) {
      try {
        await axios.post(`http://localhost:4000/night-summary`, {
          roomId: this.roomId,
          role: 'mafia',
          playerId: mafia.id,
          day: this.day,
          data: { target: targetToKill }
        });
      } catch (err) {
        console.warn(`⚠️ AI 서버 연결 실패 (mafia: ${mafia.id}): ${err.message}`);
      }
    }

    // 결과 브로드캐스트
    this.broadcast({
      type: "night_result",
      killed: targetToKill ?? null,
      saved: doctorTarget ?? null,
      investigated: policeTarget ?? null
    });

    // 승리 조건 체크
    const winner = this.checkWinCondition();
    if (winner) {
      this.isAlive = false;
      this.players = this.players.filter(p => !p.id.startsWith("ai_"));
      if (this.rooms && this.rooms[this.roomId]) {
        this.rooms[this.roomId].players = this.rooms[this.roomId].players.filter(id => !id.startsWith("ai_"));
      }
      this.broadcast({ type: 'game_over', message: winner });
      console.log(`🏁 게임 종료! 승리: ${winner}`);
      return true;
    }
  }


  async startDay() {
    if (!this.isAlive) return;

    this.chatHistory = this.chatHistory.filter(msg =>
      !(msg.sender === 'system' && msg.message.includes('밤'))
    );

    this.state = 'day';
    console.log(`🌞 낮 ${this.day} 시작`);
    this.votes = {};

    this.broadcast({
      type: 'day_start',
      message: `${this.day}번째 낮입니다. 자유롭게 토론하세요.`
    });

    this.chatHistory.push({
      sender: "system",
      message: `${this.day}번째 낮입니다. 자유롭게 토론하세요.`
    });

    await this.sendChatPhase();  // 시간 기반 발언

    await this.startVote();
  }

  async sendChatPhase() {
    if (!this.isAlive) return;
    const aliveAIs = this.players.filter(p => p.isAI && p.alive);
    const endTime = Date.now() + 10000;  // 낮 턴 제한 시간: 2분

    // // 각 AI당 발언 횟수 2~3회로 제한
    // const speakCountMap = {};
    // for (const ai of aliveAIs) {
    //   speakCountMap[ai.id] = 2 + Math.floor(Math.random() * 2);  // 2~3회
    // }

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
          roomId: this.roomId,
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

        await speakOnce(ai);
        if (!this.isAlive) return;

        const delay = Math.floor(Math.random() * 3000) + 2000;
        await new Promise(resolve => setTimeout(resolve, delay));
        // if (speakCountMap[ai.id] > 0) {
        //   await speakOnce(ai);
        //   speakCountMap[ai.id]--;
          
        //   if (!this.isAlive) return;

        //   // 말한 후 2~5초 쉬기
        //   const delay = Math.floor(Math.random() * 3000) + 2000;
        //   await new Promise(resolve => setTimeout(resolve, delay));
        // }
      }

      // 남은 발언 기회 없으면 종료
      //const hasMore = Object.values(speakCountMap).some(cnt => cnt > 0);
      //if (!hasMore) break;
    }

    this.lastInvestigation = null;
    this.lastSaved = null;
  }


  async startVote() {
    if (!this.isAlive) return;
    console.log("🗳️ 투표 시작됨!");

    const alivePlayerIds = this.players.filter(p => p.alive).map(p => p.id);
    this.broadcast({
      type: 'vote_start',
      alivePlayers: alivePlayerIds
    });

    const aliveAIs = this.players.filter(p => p.isAI && p.alive);

    for (const ai of aliveAIs) {
      try {
        const availableTargets = alivePlayerIds.filter(id => id !== ai.id);

        const res = await axios.post(`http://localhost:4000/vote-suggestion`, {
          roomId: this.roomId,
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

    // 사람 플레이어는 vote_end 신호까지 기다림
    setTimeout(() => {
      if (!this.isAlive) return;
      this.broadcast({ type: "vote_end" });

      // 무조건 1초 후 vote 처리
      setTimeout(() => {
        if (!this.isAlive) return;
        this.resolveVote();
      }, 1000);
    }, 15000);
  }

  receiveVote(from, target) {
    this.votes[from] = target;
    console.log(`🗳️ ${from} → ${target}`);
  }

  resolveVote() {
    if (!this.isAlive) return;
    console.log("🗳️ [resolveVote] 투표 집계 시작");

    const voteResult = {};
    Object.values(this.votes).forEach(target => {
      if (!target) return;
      voteResult[target] = (voteResult[target] || 0) + 1;
    });

    console.log("📊 집계된 투표 결과:", voteResult);

    const entries = Object.entries(voteResult);
    if (entries.length === 0) {
      console.log("⚠️ 아무도 투표하지 않음");
      this.broadcast({ type: 'vote_result', executed: null });
      this.startNight();
      return;
    }

    const maxVotes = Math.max(...entries.map(([_, count]) => count));
    const topVoted = entries.filter(([_, count]) => count === maxVotes);

    console.log("🏅 최다 득표 수:", maxVotes);
    console.log("🧮 최다 득표자 목록:", topVoted.map(([id, _]) => id));

    let targetToKill = null;
    if (maxVotes > 0 && topVoted.length === 1) {
      targetToKill = topVoted[0][0];
      console.log(`🎯 유일한 최다 득표자: ${targetToKill}`);
    } else {
      console.log("⚖️ 동점 발생 → 아무도 처형하지 않음");
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
      executed: targetToKill
    });

    const winner = this.checkWinCondition();
    if (winner) {
      this.isAlive = false;
      // 🔻 게임 종료 직전에 AI 유저들 제거
      this.players = this.players.filter(p => !p.id.startsWith("ai_"));

      if (this.rooms && this.rooms[this.roomId]) {
        this.rooms[this.roomId].players = this.rooms[this.roomId].players.filter(id => !id.startsWith("ai_"));
      }

      this.broadcast({ type: 'game_over', message: winner });
      console.log(`🏁 게임 종료! 승리: ${winner}`);
      // ✅ 게임 종료 후 Ready 상태 초기화
      for (const id of this.players) {
        if (!id.startsWith("ai_")) {
          this.rooms[this.roomId].readyPlayers[id] = false;
        }
      }

      // ✅ 클라이언트들에게 Ready 상태 알려주기
      this.broadcast({
        type: 'update_ready',
        players: this.players.map(id => ({
          playerId: id,
          isReady: id === this.players[0]  // 방장은 자동 ready
        }))
      });
      return;
    }

    this.startNight();
  }

  checkWinCondition() {
    const aliveMafia = this.players.filter(p => p.alive && p.role === 'mafia').length;
    const aliveNonMafia = this.players.filter(p => p.alive && p.role !== 'mafia').length;

    // 우선순위: 마피아가 시민 이상일 경우 마피아 승
    if (aliveMafia >= aliveNonMafia && aliveMafia > 0) return 'mafia';

    // 마피아가 모두 죽었을 경우 시민 승
    if (aliveMafia === 0) return 'citizen';

    return null;
  }
}

module.exports = MafiaGame;