const axios = require('axios'); // HTTP 요청용

// game.js
class MafiaGame {
  constructor(players, broadcastFunc) {
    this.players = players; // [{id, role, isAI, alive}]
    this.broadcast = broadcastFunc;
    this.state = 'waiting';
    this.day = 0;
    this.votes = {};
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

    // 역할을 무작위로 섞자
    const shuffled = roles.sort(() => Math.random() - 0.5);

    this.players.forEach((player, index) => {
      player.role = shuffled[index];
      console.log(`🃏 ${player.id} → ${player.role}`);
    });
  }

  startDay() {
    this.state = 'day';
    console.log(`🌞 낮 ${this.day} 시작`);
  
    this.votes = {}; // 투표 초기화
  
    const alivePlayers = this.players.filter(p => p.alive);
    const aliveAIs = alivePlayers.filter(p => p.isAI);
  
    this.broadcast({
      type: 'day_start',
      message: `낮 ${this.day}이 시작되었습니다. 투표를 준비하세요.`
    });
  
    this.broadcast({
      type: 'start_vote',
      alivePlayers: alivePlayers.map(p => p.id)
    });
  
    // ✅ AI 투표 랜덤 처리
    aliveAIs.forEach(ai => {
      // 자기 자신 제외한 플레이어들 중에서 타겟 선택
      const targets = alivePlayers.filter(p => p.id !== ai.id);
      const randomTarget = targets[Math.floor(Math.random() * targets.length)];
  
      if (randomTarget) {
        console.log(`🤖 ${ai.id}가 랜덤으로 ${randomTarget.id}에게 투표함`);
        this.receiveVote(ai.id, randomTarget.id); // 서버 내부에서 처리
      }
    });
  }
  

  handleNightActions(nightActions) {
    console.log(`🩸 밤 ${this.day} 행동 처리 중...`);

    let mafiaTargets = [];
    let doctorTarget = null;
    let policeTarget = null;

    // 1. 각 행동 파악
    nightActions.forEach(actionData => {
      const { playerId, action } = actionData;

      if (action.action === 'kill') {
        mafiaTargets.push(action.target); // 여러 마피아가 같은 타겟 고를 수도 있음
      } else if (action.action === 'save') {
        doctorTarget = action.target;
      } else if (action.action === 'investigate') {
        policeTarget = action.target;
      }
    });

    // 2. 마피아 표를 가장 많이 받은 사람 확인 (아직 죽이지는 않음!)
    const killCounts = {};
    mafiaTargets.forEach(id => {
      killCounts[id] = (killCounts[id] || 0) + 1;
    });

    // 우선순위 타겟 찾기
    let targetToKill = null;
    let maxVotes = 0;

    for (const [target, count] of Object.entries(killCounts)) {
      if (count > maxVotes) {
        maxVotes = count;
        targetToKill = target;
      }
    }

    // 의사가 보호한 대상은 살려준다
    if (targetToKill && targetToKill === doctorTarget) {
      console.log(`💉 의사가 ${targetToKill}을 살렸습니다!`);
      targetToKill = null; // 살았음
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

    // 경찰 조사 로그 (콘솔에만)
    if (policeTarget) {
      const investigated = this.players.find(p => p.id === policeTarget);
      console.log(`🔎 경찰이 조사한 대상: ${policeTarget} (${investigated?.role})`);
    }

    // 밤 결과를 모두에게 브로드캐스트
    this.broadcast({
      type: "night_result",
      killed: targetToKill ?? null,
      saved: doctorTarget ?? null,
      investigated: policeTarget ?? null
    });

  const winner = this.checkWinCondition();
  if (winner) {
    this.broadcast({ type: 'game_over', winner });
    return;
  }

    // 낮으로 전환
    this.state = 'day';
    this.startDay(); // 낮 턴으로 전환
  }

  checkWinCondition() {
    const aliveMafia = this.players.filter(p => p.alive && p.role === 'mafia').length;
    const aliveCitizens = this.players.filter(p => p.alive && p.role !== 'mafia').length;
  
    if (aliveMafia === 0) {
      console.log("🎉 시민 승리!");
      return 'citizen';
    }
  
    if (aliveMafia >= aliveCitizens) {
      console.log("😈 마피아 승리!");
      return 'mafia';
    }
  
    return null; // 계속 진행

  }

  async startNight() {
    this.state = 'night';
    console.log(`🌙 밤 ${this.day} 시작`);

    // 살아있는 AI 플레이어만 가져오기
    const aliveAIs = this.players.filter(p => p.isAI && p.alive);

    // 행동 요청 보낼 대상
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
        console.log(`🤖 ${ai.id} 응답:`, res.data);
        nightActions.push({ playerId: ai.id, action: res.data });
      } catch (err) {
        console.error(`❌ ${ai.id} 응답 실패`, err.message);
      }
    }

    this.handleNightActions(nightActions);
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
}

module.exports = MafiaGame;