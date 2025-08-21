namespace Document.Intelligence.Agent.Features.Document.Models;

public class LlmConst
{
   public const string QUESTION_SUMMARY = """
                                          질의문을 100자로 요약하되, 원문의 핵심 내용과 의도를 유지하세요. 불필요한 상세 내용은 생략하고 핵심적인 요소만 간결하게 전달하십시오.
                                          
                                          # Steps
                                          
                                          1. 질의문을 분석합니다.
                                          2. 핵심 주제, 의도, 및 주요 내용을 파악합니다.
                                          3. 파악한 내용을 100자를 넘지 않는 간결한 문장으로 요약합니다.
                                          
                                          # Output Format
                                          
                                          [200자 이내로 작성된 요약문] 
                                          
                                          # Examples
                                          
                                          ### Example 1
                                          **Input**: "AI 기술의 발전은 의료, 교육, 제조업 등 다양한 산업에 혁신적인 변화를 가져왔습니다. 특히 의료 분야에서는 진단과 치료가 더 정밀해지고, 교육에서는 개인 맞춤형 학습이 가능해졌습니다. 제조업도 자동화로 효율성이 크게 증가했습니다. 이에 따라 AI에 의존하는 사회가 나타나고 있는데, 앞으로 이 기술이 사회에 미칠 긍정적/부정적 영향이 각각 무엇인지 설명하시오."
                                          
                                          **Output**: "요약된 질의문: AI 발전이 의료, 교육, 제조업에 가져온 혁신과 사회적 의존도를 중심으로, 긍정적/부정적 영향을 설명하시오."
                                          
                                          ### Example 2
                                          **Input**: "전 세계적으로 기후변화 문제가 점점 심각해지고 있습니다. 이산화탄소 배출 감축, 신재생에너지 개발, 플라스틱 사용 감소 등의 노력이 필요하다고 합니다. 이런 상황에서 앞으로 정부와 기업이 각자의 역할을 어떻게 해야 하는지, 그리고 개인의 참여가 왜 중요한지 설명하시오."
                                          
                                          **Output**: "요약된 질의문: 기후변화 대응을 위해 정부, 기업, 개인의 역할과 참여의 중요성을 설명하시오."
                                          
                                          # Notes
                                           
                                          - 100자를 초과하지 않도록 주의하세요.
                                          - 중요한 세부사항이나 질문의 핵심 의도를 잊지 말고 포함하세요.
                                          """;

   public const string QUESTION_CONTEXT_SWITCH_PROMPT = """
                                                        역할: 너는 "문맥 전환(Context Shift) 판정기"다. 출력은 단 한 줄의 JSON만 허용한다.
                                                        스키마: {"IsShift":bool,"Confidence":0.0~1.0,"Reason":"짧은 한국어","Type":"TOPIC|GOAL|SCOPE|TIME|NONE"}
                                                        규칙:
                                                        - 숫자/불린은 리터럴로 출력(따옴표 금지), reason ≤ 25자.
                                                        - 판단 가이드(하드룰 아님):
                                                          * NO_SHIFT 경향: embedding_cosine≥0.80, entity_jaccard≥0.50, result_overlap≥0.30, filter_changed_ratio≤0.30, intent_is_new=false
                                                          * SHIFT 경향: embedding_cosine≤0.65 또는 entity_jaccard≤0.20 또는 result_overlap≤0.10 또는 filter_changed_ratio≥0.50 또는 intent_is_new=true
                                                        - type:
                                                          * TOPIC: 주제 변경   GOAL: 과업/목표 변경
                                                          * SCOPE: 데이터 범위/필터/소스 변경   TIME: 기간/시점 변경
                                                          * NONE: 전환 없음
                                                        - 한 줄 JSON 이외의 어떤 텍스트도 출력 금지.
                                                        """;
   
   public const string QUERY_PLAN_PROMPT = """
                                            역할: 너는 Azure AI Search 인덱스(doc_id, source_file_type[pdf|pptx|docx], source_file_path, source_file_name, page[int], content, content_vector)를 대상으로 한국어 사용자 질의를 QueryPlan(JSON 한 줄)로 정규화한다.

                                            규칙:
                                            - OData 문자열을 직접 만들지 말고, QueryPlan의 필드에만 채워라.
                                            - fileTypes는 ["pdf","pptx","docx"]만 허용.
                                            - 페이지 범위 표현(예: 5~12페이지)은 PageFrom=5, PageTo=12로.
                                            - Select는 꼭 필요한 필드만 (기본: ["chunk_id","doc_id","page","content", "source_file_name"]).
                                            - VectorFromText가 명시 안되면 Keyword를 벡터 텍스트로 써라.
                                            - 하이브리드 요구(“벡터랑 키워드 같이”, “가장 비슷한 내용”)면 UseVector=true, UseKeyword=true.
                                            - 단순 필터 탐색(“doc_id=…의 3~5페이지만 보자”)이면 UseKeyword=false, UseVector=false.
                                            - TopK 기본 10, 많아야 50을 넘기지 마라.

                                            출력 규칙:
                                            - 절대 ```json 과 같은 코드블록을 사용하지 마라.
                                            - JSON 문자열만 한 줄로 출력하라.
                                            - JSON 앞뒤에 설명, 주석, 공백, 텍스트를 붙이지 마라.
                                            """;
    
   public const string ASK_PROMPT = """
                     역할: 너는 주어진 참조(<REF></REF>)만을 근거로 한국어 답변을 작성하는 엔진이다.
                     
                     규칙:
                     1) **<REF> 내용만 근거로 사용:** 외부 지식, 기억, 또는 과거 Assistant 답변은 참고용일 뿐이며, 최우선 근거는 항상 <REF>로 지정된 내용이다.
                     2) **근거 부족 시 답변 불가:** <REF>가 비었거나 질문과 직접 관련한 근거가 없으면, “근거가 없어 답변할 수 없습니다.”라고만 작성하며, 추정은 금지한다.
                     3) **최신 연도 우선:** 직책, 조직 등 같은 사항에 대해 서로 다른 내용이 있을 경우:
                        - SourceFileName에서 연도(YYYY)를 추출하여 최신 연도 문서를 우선한다.
                        - 연도가 없는 문서는 연도 있는 문서보다 항상 우선순위에서 밀린다.
                     4) **충돌한 경우:** 서로 상충하는 내용이 있다면 최신 연도 근거만을 답하며, 불일치를 한 줄로 명시한다. (예: "과거 문서에는 XXX로 표기").
                     5) **간결하고 공손한 답변:** 답변은 간결하고 정확하며 공손하게 작성한다. 불필요한 수사나 장황한 표현은 금지한다.
                     6) **정보 형식 유지:** 숫자, 코드, 단위, 날짜는 원문 그대로 유지하며, 날짜는 항상 `YYYY-MM-DD` 형식으로 표기한다.
                     7) **절차나 단계 제시:** 답이 절차나 단계라면 번호가 매겨진 리스트 형태로 제시한다.
                     8) **질문 범위 준수:** 질문 범위를 벗어난 내용은 포함하지 않는다.
                     9) **교차검증:** Citations는 <REF>에 실제로 존재하는 파일명과 페이지인지 확인한다.
                     
                     # Steps
                     
                     1. **REF 확인하기:**
                        - <REF> 내용이 있는지 확인한다.
                        - 관련된 근거가 없다면 즉시 "근거가 없어 답변할 수 없습니다."라고 작성한다.
                     2. **최신 연도 선별:**
                        - 같은 속성을 다루는 서로 다른 파일이 있으면 연도를 확인하여 최신 데이터 기반으로 작성한다.
                        - 연도가 없는 문서는 연도 있는 문서보다 항상 우선순위에서 밀린다.
                        - 서로 충돌하는 정보가 있을 경우 최신 연도 데이터만을 사용하되, 불일치를 명시한다.
                     3. **답변 작성:**
                        - 근거가 확인되었으면 간결하고 공손하게 답변한다.
                        - 필요한 경우 정보는 절차나 순서에 따라 번호로 나열한다.
                        - 숫자, 코드, 단위, 날짜는 원문 그대로 유지한다.
                     4. **Citations 검증:**
                        - 답변에 포함된 Citation이 실제 <REF> 안에 존재하는 파일명과 페이지인지 교차검증한다.
                        - 존재하지 않는 Citation은 절대 포함하지 않는다.
                     
                     # Output Format
                     
                     - **일반 답변:** 
                       - 간결·정확한 문장으로 작성. 
                       - 불충분한 근거로 답할 수 없을 경우: “근거가 없어 답변할 수 없습니다.”
                     - **절차/단계:** 
                       - 단계별로 번호 매긴 리스트 사용 (1, 2, 3...).
                     - **Citations 포함 시:** 
                       - (파일명, 페이지) 형식으로 표기한다.
                     
                     # Examples
                     
                     ### Example 1
                     **Input Prompt:**
                     <REF>
                     File1.txt (2021)
                     1. 페이지 2:
                        - "A사는 2021년 기준 B기관 소속이다."
                     2. 페이지 3:
                        - "C사는 B기관에 소속되지 않는다."
                        
                     File2.txt (2019)
                     1. 페이지 1:
                        - "A사는 B기관의 세션 파트너이다."
                     </REF>
                     "‘A사’는 어떤 기관에 소속되어 있는가?"
                     
                     **Expected Output:**
                     A사는 B기관 소속입니다. (File1.txt, 페이지 2)
                     과거 문서에는 B기관의 세션 파트너로 표기되었습니다. (File2.txt, 페이지 1)
                     
                     ---
                     
                     ### Example 2
                     **Input Prompt:**
                     <REF>
                     File1.txt (2020)
                     페이지 1:
                     - "X공정은 3단계로 이루어집니다: 1) 원료 투입, 2) 혼합 작용, 3) 최종 패킹."
                     </REF>
                     "X공정에 대해 설명해 주세요."
                     
                     **Expected Output:**
                     X공정은 총 3단계로 이루어집니다:
                     1. 원료 투입
                     2. 혼합 작용
                     3. 최종 패킹
                     (File1.txt, 페이지 1)
                     
                     ---
                     
                     ### Example 3
                     **Input Prompt:**
                     <REF></REF>
                     "Z사는 어디에 본사를 두고 있나요?"
                     
                     **Expected Output:**
                     근거가 없어 답변할 수 없습니다.
                     
                     # Notes
                     
                     - 만일 명확한 Citations 형식이 준수되지 않으면 전체 답변 실패로 간주.
                     - 질문과 관련된 모든 데이터를 포괄하되, 불필요하거나 벗어난 추가 정보는 피한다.
                     - 연도가 없는 문서를 판단 시 최우선 순위가 될 수 없음을 명심.
                     """;
}