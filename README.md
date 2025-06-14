성능 개선 (LoRA)

https://bob-data.tistory.com/41
https://bob-data.tistory.com/45?category=1150698
참고하고 지우기

-------------  시스템 프롬프트  -------------

참고 자료
https://www.promptingguide.ai/kr
```
Zero-shot: 모델에게 아무런 예제도 주지 않고 문제를 해결하도록 시키는 방식
  특징:
    1. 실시간 응답 속도가 빠름
    2. 문제가 단순하거나 명확 또는 모델이 충분히 훈련되어있다면 이 방법만으로 해결 가능
    
Few-shot: 여러 개의 예제를 보여준 후 문제 해결
  특징:
    1. 패턴이 있을 경우, 인공지능의 이해를 가속화함
    2. 적은 수의 예시만 주고도 효과가 존재
    
Chain-of-Thought(CoT) Prompting: 문제를 한 번에 답하지 않고, 중간 단계의 추론 과정을 하나하나 따라가며 답을 도출하는 방식
  ex) 프롬프트: "답만 말하지 말고, 생각 과정을 말하면서 풀어봐"
  특징:
    1. 모델이 단계적으로 생각하면 더 정확한 답을 낼 확률이 높아짐 = 추론 능력 향상
    2. 산술, 논리, 조건문 등 여러 단계가 필요한 문제에 유리 = 복잡한 문제 해결 가능
    3. 어떤 과정을 거쳐 답을 냈는지 사람이 이해할 수 있음 = 설명 가능

Self-Consistency: 하나의 문제에 대해 여러 번 다른 경로로 추론을 시도한 뒤, 그 결과들 중 가장 많이 나온 답을 선택하는 방법
  특징:
    1. 정확도 향상
    2. 하나의 문제에 대하여 여러 번 추론해야되므로 느림

Generated Knowledge Prompting(GKP): 모델이 스스로 생성한 지식을 이용해 프롬프트를 강화하고, 그 지식을 기반으로 문제 해결을 유도하는 기법
  ex) 프롬프트: "AI 너, 먼저 네가 아는 걸 꺼내봐. 그 다음 그걸 바탕으로 문제를 풀어봐."
  특징:
    1. 사고의 일관성 향상 (지식 생성 -> 추론)
    2. 복잡한 문제 해결에 강함
    3. Chain-of-Thought보다 더 명확한 지식 기반 생성 (CoT = 생각의 흐름 유도, GKP = 지식의 집중)

Prompt Chaining: 여러 개의 프롬프트를 단계적으로 연결해서, 하나의 복잡한 작업이나 문제를 점진적으로 해결하는 방식
  ex) 단일 프롬프트: ~에서 해시태그를 만들어줘 (X)
      프롬프트 1: ~를 요약해줘 (Prompt 1(요약))
      프롬프트 2: (요약)에서 핵심 키워드 5개만 뽑아줘 (Prompt 2(추출))
      프롬프트 3: (추출)을 기반으로 해시태그 만들어줘 (Prompt 3(생성)) (O)
  특징:
    1. 이전 단계의 출력 결과를 입력으로 받는 형태 = 사용자가 설계 가능

Tree of Thoughts (ToT): 여러 개의 사고 경로(thoughts)를 생성하고, 이를 트리 구조로 확장하며 평가/선택해나가는 방식
  ToT 프레임워크:
  ![image](https://github.com/user-attachments/assets/dc275287-9829-4311-9125-4e5e671de12b)
  과정:
  ![image](https://github.com/user-attachments/assets/1a89e0c6-54c5-408c-8b94-621225cc205d)

  성능:
  ![image](https://github.com/user-attachments/assets/13d6a39f-152a-46a4-8b78-8af2ab1be8fe)
  이미지 출처: Yao et el. (2023)

  구조와 단계:
  1. 문제를 하위 단계로 분할
  -> 예: 전체 계획을 여러 단계로 나눔

  2. 각 단계에서 가능한 '생각'들을 생성
  -> 여러 해결 방향이나 아이디어(Thought)들을 동시에 제시
  
  3. 생각들을 트리 구조로 확장
  -> 각 생각(노드)이 다음 단계의 분기점이 됨
  
  4. 각 노드를 평가하여 유망한 경로만 선택
  -> BFS, DFS, 또는 휴리스틱 기반 평가
  
  ex) 숫자 야구 게임:
    1단계: 문제 이해 및 초기 생각 (Root):
      Thought 0:
        우선 0~9 숫자 중 4자리 숫자를 조합해야 하고, 중복은 없으니 가능한 조합 수는 5040개 (10P4).
        첫 시도는 무작위 숫자 추측으로 시작한다.
    2단계: 첫 추측 및 결과 수집 (1단계 노드):
      Thought 1.1:
      추측: 1234 → 결과: 1 strike, 2 ball
      해석:
      - 숫자 1개는 자리가 정확히 맞음
      - 숫자 2개는 존재하지만 자리 틀림
      -> 적어도 3개 숫자는 정답 안에 포함됨
    3단계: 가능한 후보군 생성 및 분기 (2단계 노드):
      Thought 2.1:
        다음 추측: 1243
        -> 결과: 2 strike, 2 ball
      Thought 2.2:
        다음 추측: 1325
        -> 결과: 1 strike, 1 ball
    4단계: 유망한 경로 평가 및 확장:
      Thought 3.1 (from 2.1):
        1243 결과가 가장 유망 (strike + ball = 4)
        -> 거의 모든 숫자가 정답 안에 포함된다고 판단
      Thought 3.2 (from 2.2):
        1325는 정보량이 낮으므로 가지치기
        다음 추측: 1432
        -> 결과: 4 strike → 정답

  트리 구조
    Root (무작위 추측)
    ├── 1234 → (1s 2b)
    │   ├── 1243 → (2s 2b)
    │   │   └── 1432 → ✅ 정답
    │   └── 1325 → (1s 1b) → 제거
    └── 다른 조합 → 평가 낮음 → 제거

  특징:
    1. 하나의 사고 흐름이 아닌, 여러 가능한 생각 경로들을 탐색
    2. 각각 생각들을 트리 형태로 분기 시키고, 그 중 가장 유망한 경로를 선택 또는 조합
    3. CoT같은 기술은 단일 추론 경로 = 훨씬 안정적이고 창의적인 문제 해결 가능
    
  https://github.com/kyegomez/tree-of-thoughts (ToT 예제)
  
Retrieval Augmented Generation (RAG): 검색 한 결과를 포함한 후 응답 생성

Automatic Reasoning and Tool-use (ART): LLM이 스스로 추론하고, 필요할 경우 도구(tool)를 직접 선택해 사용하면서 문제를 해결하는 능력

Automatic Prompt Engineer (APE): LLM이 스스로 좋은 프롬프트를 자동으로 설계하거나 추천해서,
다운스트림 작업(분류, 요약, 생성 등)의 성능을 최적화하는 자동 프롬프트 탐색 기법
  특징:
    1. 사람이 설계한 단계별로 생각하게 만드는 프롬프트보다 성능이 더 좋음
    ![image](https://github.com/user-attachments/assets/1ea9e268-2696-4bc0-8ce3-7559a8da8b44)
    이미지 출처: https://arxiv.org/abs/2211.01910
  과정:
    Generate: 프롬프트 후보들을 생성
    Measure: 각 후보의 출력 정확도를 측정
    Select: 성능이 가장 좋은 프롬프트 선택
  
Active-Prompt: 질문마다 가장 효과적인 예시들을 선별해서 넣는 Few-Shot Prompting 방식
  특징:
    1. Few-shot과 가장 큰 차이는 Few-shot은 항상 같은 몇개의 예시를 제공하는데 비해,
    Active-Prompt는 문제마다 다른 예시를 선택해서 보여준다는 점

Directional Stimulus Prompting: 답이 애매한 문제에 대해, 모델이 우리가 원하는 쪽으로 생각하도록 유도하는 방식
  문제와 비슷한 예시와 원하는 답을 줌으로써 유도함
  특징:
    1. 주로 이분법적 분류 문제에서 사용됨
    
Program-Aided Language Models(PAL): LLM이 자연어 문제를 풀기 위해 직접 코드를 작성하고 실행해,
그 결과를 바탕으로 최종 답변을 생성하는 방식

ReAct(Reasoning + Acting) Prompting: LLM이 문제를 해결할 때 사고의 흐름과 도구 사용을 교대로 수행하도록 유도하는 프롬프트 방식

Reflexion: 모델이 스스로의 실패를 인식하고, 피드백을 반영해서 성과를 개선하는 기법
  예시 코드:
    ```python
    response_1 = llm(prompt)
    feedback = llm(f"이 답변의 문제점은 무엇인가?\n답변: {response_1}")
    response_2 = llm(f"이 피드백을 참고해 다시 시도해줘.\n{feedback}")
    ```
```

-------------  사용자 프롬프트  -------------
```
Adversarial Prompting in LLMs(적대적 프롬프팅)
ex)
![image](https://github.com/user-attachments/assets/1837d7a3-89de-45b0-bf8a-07da3cd2ec38)

Factuality(사실성): 허구

Biases(편향)
```
