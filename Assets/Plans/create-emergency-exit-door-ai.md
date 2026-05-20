# 프로젝트 개요
- 게임 제목: (Main Scene 기반 복도 환경)
- 상위 개념: 복도 벽면(`WallFloor1_12 (1)`)의 빈 공간에 맞는 AI 생성 비상구 문 추가
- 플레이어: 1인칭 또는 3인칭 탐험
- 아트 방향성: 저폴리(Low-poly), 플랫 셰이딩(Flat-shaded) 인테리어 스타일

# 게임 메카닉
## 핵심 루프
- 환경 탐사 및 경로 확인
## 컨트롤 및 입력
- (기존 시스템 유지)

# UI
- (해당 없음)

# 주요 에셋 및 컨텍스트
- **대상 벽체**: `Main Corridor/Walls/WallFloor1_12 (1)` (Instance ID: -96390)
- **참조 스타일**: `Assets/Imported/LowPolyInterior/Prefabs/Walls/Door_02.prefab`
- **생성될 에셋**: AI로 생성된 3D 비상구 문 메시 (`Assets/Models/Generated/EmergencyExitDoor.fbx` 등)
- **비상구 조명**: `Assets/Imported/Fire Extinguisher Pack/prefabs/ms_emergency_exitlight.prefab` (이미 프로젝트 내 존재)

# 구현 단계
1. **AI 모델 선정**: `Unity.AssetGeneration.GetModels`를 호출하여 3D 메시 생성(`Model3d`)이 가능한 모델(예: Hunyuan 3D 등)을 확인합니다.
2. **비상구 문 생성**:
   - `GenerateAsset` 도구를 사용하여 비상구 스타일의 문 메시를 생성합니다.
   - 프롬프트 예시: "Low-poly 3D emergency exit door, metallic green, with a push bar, minimal detail, matches low-poly interior style."
   - 스타일 일관성을 위해 기존 `Door_02`의 이미지를 참조(`referenceImageInstanceId`)로 활용할 수 있습니다.
3. **에셋 임포트 및 머티리얼 설정**:
   - 생성된 메시를 프로젝트에 임포트하고, 프로젝트의 저폴리 스타일에 맞는 머티리얼을 설정합니다.
4. **씬 배치 및 정렬**:
   - 생성된 문을 `WallFloor1_12 (1)`의 자식으로 배치합니다.
   - 문의 로컬 위치와 스케일을 조정하여 벽체의 빈 공간(로컬 폭 약 0.92)에 정확히 맞춥니다.
   - 벽체의 스케일(1.5, 1, 1)이 문에도 영향을 주므로, 문이 너무 납작해 보이지 않도록 조정이 필요할 수 있습니다.
5. **비상구 조명 추가**:
   - 기존의 `ms_emergency_exitlight` 프리팹을 문 바로 위쪽 벽면에 배치하여 완성도를 높입니다.

# 검증 및 테스트
- **치수 확인**: 문이 벽체의 구멍을 빈틈없이 메우는지 확인합니다.
- **스타일 확인**: 생성된 문의 디자인과 텍스처가 주변의 `LowPolyInterior` 에셋들과 시각적으로 조화를 이루는지 확인합니다.
- **계층 구조**: 오브젝트가 `Main Corridor/Walls` 하위 구조에 올바르게 정리되었는지 확인합니다.
