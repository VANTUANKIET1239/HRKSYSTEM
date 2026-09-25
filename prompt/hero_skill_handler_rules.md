    # RULE PROMPT — TẠO SKILL HANDLER CHO TƯỚNG MỚI

    Sử dụng toàn bộ quy tắc trong tài liệu này mỗi khi thêm tướng hoặc kỹ năng đặc thù mới vào hệ thống battle. Trước khi chỉnh sửa, phải đọc kiến trúc hiện tại của `BattleSimulationEngine`, `SkillHandlerRegistry`, `DefaultSkillHandler`, các effect handler, target selector, entity/DTO/mapper và script seed skill.

    Mục tiêu là tạo handler dễ bảo trì, data-driven, có thể cân bằng bằng database và không làm logic của từng tướng lan vào battle engine.

    ## 1. Trách nhiệm của SkillHandler

    SkillHandler riêng của tướng chỉ chịu trách nhiệm điều phối những quy tắc đặc thù không thể biểu diễn bằng chuỗi effect thông thường, ví dụ:

    - Thứ tự thực thi các nhóm effect.
    - Khóa một mục tiêu cho chuỗi multi-hit.
    - Kiểm tra trạng thái tồn tại trước khi cast.
    - Phân nhánh thành công/thất bại sau khi kỹ năng kết thúc.
    - Dừng chuỗi hit khi mục tiêu chết.
    - Kích nổ, chuyển đổi hoặc tiêu thụ một status theo cấu hình.

    SkillHandler không được tự triển khai lại:

    - Công thức damage/heal/shield.
    - Crit, né tránh, xuyên giáp hoặc giảm sát thương.
    - Hấp thụ khiên.
    - Mark, reflection, lifesteal hoặc death processing.
    - Target selection dùng chung.
    - Quy tắc tick và hết hạn status dùng chung.

    Các phần này phải đi qua effect handler, target selector hoặc battle executor dùng chung.

    ## 2. Không hard-code thông số cân bằng

    Không hard-code trong SkillHandler hoặc EffectHandler các thông số có thể được game designer điều chỉnh:

    - Damage/heal/shield multiplier.
    - Xác suất kích hoạt.
    - Số lượt tồn tại.
    - Số stack và giới hạn stack.
    - Phần trăm buff/debuff.
    - Xuyên giáp hoặc kháng phép.
    - Tỷ lệ kích nổ.
    - Năng lượng hoặc action gauge thay đổi.
    - Số hit và hệ số từng hit.
    - Target type.
    - Damage school.
    - Điều kiện áp dụng effect nếu điều kiện có thể cấu hình.

    Các giá trị phải đi theo luồng:

    ```text
    Database
    → Entity
    → DTO
    → Mapper
    → BattleSkill/BattleSkillEffect
    → SkillHandler/EffectHandler
    → BattleEvent
    → Frontend
    ```

    Không được chỉ chuyển magic number thành `const` trong C#. Constant vẫn là hard-code và không giải quyết khả năng cân bằng bằng dữ liệu.

    ## 3. Những thứ được phép giữ trong code

    Được phép giữ dưới dạng code/constant:

    - Skill code và effect code.
    - Event type.
    - Attribute code.
    - Condition code và execution group code.
    - Quy tắc orchestration riêng của kỹ năng.
    - Các giới hạn an toàn chung của engine như HP không âm, phần trăm clamp từ 0 đến 100.
    - Giá trị kỹ thuật dùng cho index, so sánh hoặc collection.

    Tên hiển thị và mô tả không được dùng làm khóa xử lý logic.

    ## 4. Cấu hình effect

    Mỗi effect phải lấy dữ liệu từ `BattleSkillEffect`:

    - `EffectTypeCode`
    - `TargetTypeCode`
    - `DamageSchoolCode`
    - `BaseValue`
    - `DurationTurns`
    - `ChancePercent`
    - `MaxStacks`
    - `Scalings`
    - `StatModifiers`
    - `DisplayOrder` hoặc `SequenceIndex`
    - Metadata/parameter bổ sung nếu mechanic yêu cầu

    Nếu schema chưa biểu diễn được mechanic, hãy mở rộng schema dùng chung thay vì đặt số trực tiếp trong handler. Có thể sử dụng bảng tham số có cấu trúc:

    ```text
    HRK_SkillEffectParameters
    ├── Id
    ├── SkillEffectId
    ├── ParameterCode
    ├── DecimalValue
    ├── IntValue
    ├── BoolValue
    ├── StringValue
    ├── CreatedOn
    └── UpdatedOn
    ```

    Ví dụ parameter dùng chung:

    ```text
    ARMOR_IGNORE_PERCENT
    CAN_CRIT
    CAN_KILL
    CONSUME_ON_HIT
    REFRESH_ON_REAPPLY
    DETONATION_MULTIPLIER
    REQUIRED_STATUS_CODE
    REMOVE_STATUS_AFTER_EXECUTION
    ACTION_GAUGE_PERCENT
    EXECUTION_GROUP
    SEQUENCE_INDEX
    CONDITION_CODE
    ```

    Ưu tiên schema có cấu trúc. Không đưa toàn bộ gameplay configuration vào một JSON tùy ý nếu dữ liệu có thể được model rõ ràng.

    ## 5. Multi-hit và chuỗi effect

    Không hard-code mảng hệ số hit trong handler.

    Mỗi hit phải là một damage effect riêng hoặc thuộc một execution group riêng trong database:

    ```text
    HIT_1
    ├── DAMAGE
    └── STATUS_APPLY

    HIT_2
    ├── DAMAGE
    └── STATUS_APPLY
    ```

    Handler đọc các group theo `SequenceIndex`/`DisplayOrder` và thực thi lần lượt. Mỗi hit phải tạo event riêng để frontend phát đúng animation.

    Status đi kèm hit chỉ được roll khi hit tương ứng thực sự trúng và đáp ứng điều kiện được cấu hình. Nếu mục tiêu chết, handler phải dừng các hit còn lại, trừ khi dữ liệu khai báo hành vi khác.

    ## 6. Random và replay

    Mọi xác suất trong battle phải dùng `context.Random` hoặc nguồn random được tạo từ `BattleSimulationRequest.RandomSeed`.

    Không sử dụng:

```csharp
Random.Shared
new Random()
Guid.NewGuid()
```

trong logic battle.

Cùng input và random seed phải sinh cùng target, crit, proc, damage và chuỗi event.

## 7. Damage, heal và status phải dùng executor chung

Custom handler phải gọi effect handler/executor dùng chung thay vì sao chép công thức combat.

Executor nên trả về kết quả có cấu trúc, tối thiểu gồm:

```text
WasApplied
WasHit
ActualValue
WasCrit
WasKilled
TargetId
EmittedEvents
```

Không suy luận “đánh trúng” chỉ từ thay đổi HP vì damage có thể bị khiên hấp thụ.

Nếu API hiện tại chưa thực thi được một effect riêng lẻ, hãy mở rộng `DefaultSkillHandler` hoặc tạo `BattleEffectExecutor` dùng chung. Không tạo một bản sao công thức damage trong handler của tướng.

## 8. Target selector

Target phải được resolve từ `TargetTypeCode` trong effect hoặc skill configuration.

- Dùng `BattleTargetSelectorRegistry`.
- Xử lý Taunt và redirect tại một nơi dùng chung.
- Không copy logic Taunt vào từng handler.
- Multi-hit khóa cùng mục tiêu phải resolve một lần và giữ target ID trong execution scope.
- Tie-break phải deterministic bằng position/ID.

Không hard-code `LOWEST_HP_PERCENT`, `ENEMY_SINGLE` hoặc selector khác trong handler nếu database đã cấu hình target.

## 9. Status effect

Status phải lưu đủ snapshot/configuration để tick chính xác ngay cả khi nguồn đã chết hoặc thay đổi chỉ số.

Một status có thể cần:

- `SourceHeroId`
- `SourceSkillId`
- `RemainingTurns`
- `Stacks` và `MaxStacks`
- Giá trị snapshot
- Damage school
- Armor/magic resistance ignore
- CanCrit
- CanKill
- ConsumeOnHit
- StatModifiers

Các field phải mang ý nghĩa dùng chung, không đặt tên theo một tướng cụ thể.

Phải định nghĩa rõ khi áp dụng lại status:

- Stack.
- Refresh duration.
- Replace theo nguồn mạnh hơn.
- Giữ nguyên.

Không dùng fallback gameplay âm thầm. Thiếu configuration bắt buộc phải fail fast với thông báo chỉ rõ skill, effect và parameter bị thiếu.

## 10. Condition effect

Buff/debuff phát sinh sau kỹ năng phải được lưu thành effect có điều kiện, ví dụ:

```text
TARGET_DEFEATED
TARGET_SURVIVED
ACTOR_HP_BELOW_PERCENT
TARGET_HAS_STATUS
TARGET_STATUS_DETONATED
```

Handler chỉ đánh giá condition rồi chuyển effect phù hợp cho executor. Handler không tự tạo `BattleStatusEffect` kèm các modifier hard-code.

## 11. Action bar và energy

Không được dùng lẫn action bar và energy.

- `ENERGY_CHANGE` chỉ thay đổi năng lượng dùng kỹ năng.
- `ACTION_GAUGE_CHANGE` chỉ thay đổi thứ tự/lượt hành động.
- Event phải phản ánh đúng state thực tế được sửa.

Nếu battle engine chưa hỗ trợ action gauge, không được phát `ACTION_BAR_CHANGED` rồi sửa `Energy`. Phải triển khai action gauge đúng nghĩa hoặc đổi mechanic và mô tả thành energy.

## 12. Battle event

Backend phải phát event đủ dữ liệu để frontend không phải đoán:

- `ActorId`
- `TargetId`
- `SkillId`
- `EffectTypeCode`
- `DamageSchoolCode`
- `Value`
- `HpBefore/HpAfter`
- `EnergyBefore/EnergyAfter` hoặc action gauge tương ứng
- `RemainingTurns`
- `StatModifiers`
- `CastSequence`
- `TimelineOffsetMs`
- `PhaseCode`
- `SequenceIndex/HitIndex` nếu là multi-hit

Phân biệt rõ các event như apply, refresh, tick, detonate, consume và expire. Không gom tất cả thành một event chung khiến frontend phải suy luận.

## 13. Frontend

Không bổ sung chuỗi `if/else` lớn vào `playNextServerEvent` hoặc `battle-scene`.

- Tạo event handler riêng và đăng ký qua registry.
- Tạo component hiệu ứng riêng theo tướng/kỹ năng.
- Status dùng chung đặt trong component status effect dùng lại được.
- Animation phải hoạt động cho cả hai team và tự mirror hướng.
- Không dùng overlay đen toàn màn hình.
- Không chặn hover, tooltip hoặc interaction không liên quan.
- Có timeout/fallback để event queue không bị đứng.
- UI không hard-code thông số cân bằng đã có trong response/database.

## 14. Dependency Injection

- Mỗi custom handler implement `ISkillHandler`.
- Đăng ký bằng `IEnumerable<ISkillHandler>` và resolve qua `SkillHandlerRegistry`.
- Chỉ giữ một constructor DI rõ ràng cho mỗi service.
- Không tự `new` registry/handler trong production path.
- Nếu cần factory cho test, factory không được tạo dependency khác cấu hình production.
- Không thêm constructor có cùng số tham số đều resolve được, tránh lỗi ambiguous constructor.

## 15. SQL migration và seed

Mỗi tướng mới phải có script idempotent:

- Lookup khóa ngoại bằng code, không hard-code ID.
- Tạo/cập nhật hero, skill, effect, scaling, modifier, parameter, animation và aura.
- Không tạo dữ liệu trùng khi chạy lại.
- Không `DELETE` rộng ngoài phạm vi tướng/kỹ năng đang seed.
- Xóa/cập nhật an toàn các child record cũ khi cấu trúc effect thay đổi.
- Dùng transaction, rollback và `THROW` khi lỗi.
- Kiểm tra các dependency bắt buộc trước khi insert.
- Chuỗi tiếng Việt phải là Unicode và file lưu UTF-8.

Description trong database phải khớp với configuration thực tế. Không để mô tả ghi 30% trong khi dữ liệu đang là một giá trị khác.

## 16. Validation

Khi load hoặc thực thi skill, validation phải báo lỗi cụ thể, ví dụ:

```text
Skill 'SKILL_CODE', effect 'EFFECT_KEY' is missing parameter 'PARAMETER_CODE'.
```

Phải validate tối thiểu:

- Effect type và target type tồn tại.
- Chance nằm trong khoảng 0–100.
- Duration hợp lệ với status.
- Scaling dùng attribute tồn tại.
- Sequence không trùng và có thứ tự xác định.
- Conditional effect có condition.
- Effect đặc biệt có đủ parameter bắt buộc.

Không tự thay bằng magic number khi validation thất bại.

## 17. Kiểm thử bắt buộc

Mỗi custom handler phải có test cho:

1. Handler chỉ nhận đúng skill code.
2. Damage/heal/status lấy giá trị từ effect configuration.
3. Thay đổi coefficient không cần sửa handler.
4. Thay đổi chance không cần sửa handler.
5. Thay đổi duration không cần sửa handler.
6. Multi-hit dùng đúng số lượng và thứ tự effect trong dữ liệu.
7. Mỗi hit phát event riêng.
8. Condition success/failure chạy đúng.
9. Shield, crit, mark và damage reduction đi qua executor chung.
10. Status apply/refresh/stack/expire đúng convention.
11. Cùng seed sinh cùng kết quả.
12. DI resolve thành công.
13. Frontend kết thúc animation và event queue không bị treo.

Test data-driven phải chứng minh rằng khi thay đổi cấu hình trong test, kết quả thay đổi mà không chỉnh source code của handler.

## 18. Checklist review trước khi hoàn thành

Trước khi bàn giao, tìm lại trong handler tất cả numeric literal và giải thích từng giá trị còn lại. Nếu giá trị ảnh hưởng cân bằng, chuyển nó sang database.

Kiểm tra:

- Không có công thức damage bị copy.
- Không có target selector bị copy.
- Không có `Random.Shared`.
- Không nhầm action bar với energy.
- Không dùng tên hiển thị để xử lý logic.
- Không có cấu hình backend và mô tả frontend lệch nhau.
- Không có constructor DI ambiguous.
- Không có effect/event frontend bị xử lý bằng nhánh đặc biệt trong battle scene nếu có thể dùng registry.

Sau cùng phải chạy:

```text
Backend build
Battle unit/integration tests
Frontend build
```

Kết quả bàn giao phải nêu:

- Danh sách file thêm/sửa.
- Script SQL cần chạy và thứ tự chạy.
- Các parameter được seed.
- Logic nào nằm trong handler và lý do không thể data-driven hoàn toàn.
- Kết quả build/test.

## 19. Mẫu yêu cầu triển khai tướng mới

Khi nhận mô tả một tướng mới, hãy thực hiện theo mẫu sau:

```text
Hãy triển khai tướng [HERO_NAME] theo RULE PROMPT tạo SkillHandler.

Hero code: [HERO_CODE]
Ảnh: [IMAGE_PATH]
Phẩm chất: [RARITY_CODE]
Vai trò: [ROLE]

Kỹ năng cơ bản:
- Code: [BASIC_SKILL_CODE]
- Mô tả: [DESCRIPTION]

Kỹ năng kích hoạt:
- Code: [ULTIMATE_SKILL_CODE]
- Mô tả: [DESCRIPTION]

Yêu cầu:
- Phân rã mô tả thành effect, scaling, modifier, parameter và condition trong database.
- Chỉ tạo custom SkillHandler cho orchestration thực sự đặc thù.
- Không hard-code thông số cân bằng trong handler.
- Tạo SQL idempotent, backend handler/effect/selector cần thiết, frontend animation/event handler và test.
- Build và báo cáo kết quả sau khi hoàn tất.
```

