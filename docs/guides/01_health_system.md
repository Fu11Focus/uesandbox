# Крок 1 Фази 1: Health System

**Мета:** зробити так, щоб гравець і Колосс мали здоров'я, могли отримувати шкоду і помирати. Це фундамент під усе інше — зброю, Колосса, зону.

**Очікуваний час:** 2–4 години (з розумінням, не просто копіюванням).

---

## Що ми робимо концептуально

Замість того щоб запихати змінну `Health` в кожен блупринт окремо (в BP_ThirdPersonCharacter, BP_Colossus, майбутню бочку, майбутнього бота...), ми зробимо **ActorComponent** — окремий "мікро-об'єкт", який можна почепити на будь-який Actor.

**Чому так?** Це принцип, який у програмуванні називається **композиція замість спадкування**. Уяви що потім захочеш дати HP ящикам з лутом — просто чіпляєш компонент, не пишеш нічого заново. Якщо ж HP був би в Character — то ящику довелось би стати Character'ом (з ногами!), що абсурдно.

**Що таке ActorComponent:**
- Це клас який наслідується від `UActorComponent` — базовий "модуль", який можна додати до Actor
- Має власні змінні і функції
- Живе стільки скільки живе його Actor
- Actor може мати кілька компонентів одного типу (але нам тут один)

У UE таких компонентів сотні готових: `StaticMeshComponent` (меш), `AudioComponent` (звук), `CameraComponent` (камера) і т.д. Ми робимо свій — `BPC_HealthComponent`.

---

## Крок 1.1 — Створити BPC_HealthComponent

### Де і як

1. У Content Browser зайди в `Content/Characters/Shared/` (якщо папки нема — створи, правий клік → New Folder)
2. Правий клік → **Blueprint Class**
3. У вікні "Pick Parent Class" натисни **All Classes**, в пошуку введи `ActorComponent`
4. Обери **ActorComponent** (не SceneComponent! різниця нижче) → Select
5. Назви `BPC_HealthComponent`

**`BPC_` — моє умовне позначення "Blueprint Component".** У проєкті вже є `BPC_ColossusAI` — там це "Blueprint Controller". Загалом UE не має суворих префіксів, але `BP_` = Blueprint Actor, `BPC_` = Component/Controller, `BB_` = Blackboard, `BT_` = BehaviorTree, `BTT_` = BT Task, `BTS_` = BT Service. Тримаймось цього.

### ActorComponent vs SceneComponent — коротко

- **ActorComponent** — логіка без трансформа. Не має позиції, не візуалізується. HP, AI-стан, інвентар — усе ActorComponent.
- **SceneComponent** — має Transform (позиція/поворот/масштаб). Меші, камери, колізії — SceneComponent.

HP не має позиції в просторі → ActorComponent.

---

## Крок 1.2 — Змінні компонента

Відкрий `BPC_HealthComponent` подвійним кліком. У вкладці **My Blueprint** (ліва панель) знайди секцію **Variables** і додай:

| Ім'я | Тип | Default | Instance Editable | Replication |
|------|-----|---------|-------------------|-------------|
| `MaxHealth` | Float | 100.0 | ✅ | None |
| `CurrentHealth` | Float | 100.0 | ❌ | RepNotify |
| `bIsDead` | Boolean | false | ❌ | Replicated |

**Як додати змінну:** кнопка `+` біля "Variables" → обираєш тип → пишеш ім'я → компілюєш (треба натиснути **Compile** зверху!) → потім можна задати Default Value у правій панелі Details.

**Як зробити Instance Editable:** у Details правої панелі галочка "Instance Editable" — це дозволяє змінити значення на кожному актор-інстансі в редакторі (наприклад, Колосс = 1000 HP, гравець = 100 HP).

**Як задати RepNotify:** у Details правої панелі → секція Replication → Variable Replication = **RepNotify** (для `CurrentHealth`) або **Replicated** (для `bIsDead`).

### Що таке Replication і навіщо воно

У мультіплеєрі є **Server** (той хто хостить) і **Clients** (ті хто приєднався). Усі гравці бачать одну гру — це означає що стан повинен синхронізуватись. Але **не кожна змінна реплікується автоматично** — за замовчуванням вона живе тільки там де створена. Щоб її бачили всі, треба явно сказати "ця змінна реплікується".

Два типи:
- **Replicated** — змінна копіюється з сервера на клієнтів. Клієнт побачить нове значення, але **нічого автоматично не трапиться**.
- **RepNotify** — те ж саме, АЛЕ додатково викликається спеціальна функція `OnRep_<VariableName>` на клієнті. Саме там ми оновлюємо UI, граємо звук шкоди, тощо.

**Правило:** якщо при зміні значення треба щось зробити візуально — RepNotify. Якщо просто читаєш значення — Replicated.

**Важливо:** змінювати реплікованi змінні можна **тільки на сервері**. Якщо клієнт напряму напише `CurrentHealth = 50` — у нього воно зміниться локально, але сервер про це не знатиме, і наступний реплікейт перезапише назад. Правильний шлях: клієнт викликає **Server RPC** на сервері, сервер робить логіку, реплікейт повертається на клієнт. Про це далі.

### Щоб компонент взагалі реплікувався

У самому `BPC_HealthComponent`, у вкладці **Class Defaults** (кнопка зверху), знайди **Component Replicates** і постав галочку. Без цього жодна змінна в компоненті не буде реплікуватись, навіть якщо позначено RepNotify.

---

## Крок 1.3 — Функція TakeDamage (події)

У UE у класу `Actor` є вбудована **подія** (Event) `AnyDamage`. Вона викликається коли хтось застосовує `Apply Damage` до цього актора — саме таку ноду ми вчора ставили на Overlap Колосса. Ми **НЕ** хочемо обробляти `AnyDamage` у кожному BP_Character і BP_Colossus окремо — це знову дублювання. Ми обробимо його в одному місці — у BP_Character / BP_Colossus, і звідти викличемо функцію компонента.

### Функція компонента: `ApplyDamage`

У `BPC_HealthComponent` створи **New Function** (плюс біля Functions у My Blueprint) → назви `ApplyDamage`.

Вхідний параметр:
- `DamageAmount` (Float)

Логіка функції (у graph вікні функції):

```
Get CurrentHealth  →  Subtract (DamageAmount)  →  Clamp (Min=0, Max=MaxHealth)  →  Set CurrentHealth
                                                                                         ↓
                                                                                    Branch (CurrentHealth <= 0 AND NOT bIsDead)
                                                                                         ↓ True
                                                                                    Set bIsDead = true
                                                                                         ↓
                                                                                    Call Event "OnDeath" (створимо зараз)
```

**Event OnDeath:** у секції **Event Dispatchers** (ліва панель, під Variables) натисни `+`, назви `OnDeath`. Event Dispatcher — це "сигнал", на який можуть підписатись інші блупринти. Ідея: HealthComponent нічого не знає про свого власника (чи це гравець, чи Колосс), але коли HP падає в нуль — він "кричить" `OnDeath`, і хто хоче — слухає.

У функції `ApplyDamage` після встановлення `bIsDead = true` викликай `OnDeath` (перетягни з лівої панелі в graph → обери Call).

### Функція повинна виконуватись тільки на сервері

Перед всією логікою постав перевірку `Switch Has Authority` → гілка **Authority** (це означає "ми на сервері"). На клієнті нічого не робимо.

**Чому:** якщо клієнт спробує змінити HP — це перезапишеться з сервера. Тож логіка шкоди має жити ТІЛЬКИ на сервері. Клієнти побачать зміну через RepNotify автоматично.

---

## Крок 1.4 — OnRep_CurrentHealth

Коли ти поставив `CurrentHealth` як **RepNotify**, UE автоматично створить функцію `OnRep_CurrentHealth` (знайдеш її в секції Functions у My Blueprint — можливо в категорії "Override" або просто в списку).

У ній поки що нічого не роби — тільки постав коментар `// Update UI here later`. Ми сюди повернемось коли робитимемо health bar.

**Чому вона порожня зараз важлива:** сама наявність RepNotify і той факт що функція існує — вже достатньо для того щоб клієнт "знав" що значення змінилось. Пізніше ми додамо виклик оновлення UI.

---

## Крок 1.5 — Повісити компонент на BP_ThirdPersonCharacter

1. Відкрий `Content/ThirdPerson/Blueprints/BP_ThirdPersonCharacter`
2. У вкладці Components (ліва верхня панель) натисни **+ Add** → в пошуку введи `HealthComponent` → обери свій `BPC_HealthComponent`
3. Компонент з'явиться в списку. У Details (права панель) виставиш `MaxHealth = 100`, `CurrentHealth = 100`.

### Прокинути AnyDamage → компонент

В EventGraph BP_ThirdPersonCharacter:

1. Правий клік → знайди `Event AnyDamage` → додай
2. Від виходу `Damage` перетягни лінію → `ApplyDamage` (функцію твого `BPC_HealthComponent`). Щоб знайти функцію — спочатку тобі треба **Get BPC_HealthComponent** (він з'явиться в контексті після компонента на актора), з нього вже `ApplyDamage`.

Схематично:
```
Event AnyDamage  →  [target: Self.BPC_HealthComponent] ApplyDamage(Damage)
```

### Підписатись на OnDeath

У `Event BeginPlay` BP_ThirdPersonCharacter:
1. Get `BPC_HealthComponent`
2. Від нього `Bind Event to OnDeath`
3. Створи Custom Event `HandlePlayerDeath` і прив'яжи його до binding
4. У `HandlePlayerDeath` поки що просто `Print String "Player died"` і `Destroy Actor`

**Bind Event** — це "коли OnDeath буде викликано — виклич ось цю функцію". У C++ це називається delegate/event. У Blueprint — Event Dispatcher + Bind.

---

## Крок 1.6 — Повісити компонент на BP_Colossus

Точно так само як для гравця, але:
- `MaxHealth = 1000` (Колосс жирний)
- `CurrentHealth = 1000`
- На OnDeath → `HandleColossusDeath` → `Print String "Colossus defeated"` + `Destroy Actor`

Також треба забрати з BP_Colossus ноду `Apply Damage` (25 dmg на оверлап), яку ми ставили вчора, і замінити її на **нанесення шкоди гравцю**. Зараз вона вже викликає `Apply Damage` з таргетом `OtherActor` — це правильно, бо `ApplyDamage` на Actor запускає подію `AnyDamage` на тому акторі. Тобто Колосс б'є гравця → гравець отримує `AnyDamage` → його `BPC_HealthComponent.ApplyDamage(25)` → HP мінус → можливо смерть.

Отже, Колосс вже вміє бити. Тепер потрібно щоб і гравець міг бити Колосса. Для цього в наступному кроці ми зробимо зброю. Поки — щоб перевірити що HP-система взагалі працює — можна додати `Event BeginPlay → Delay 5 sec → ApplyDamage to Self (1000)` в BP_Colossus для тесту. Після перевірки прибрати.

---

## Крок 1.7 — Тест

1. Натисни Play
2. Через 5 секунд Колосс має сам собі завдати шкоди і помилитись (Print String "Colossus defeated")
3. Якщо ти підбіжиш до Колосса до того як він помре — він вдарить тебе, через 4 удари (25×4=100) Print String "Player died"

Якщо обидва Print'и спрацьовують — HP-система працює.

---

## Про що **не турбуйся** зараз

- **UI health bar** — зробимо окремим кроком (це UMG/Widget, окрема тема)
- **Respawn** — зараз після смерті гравець просто зникає, це нормально
- **Damage types** — поки один загальний тип
- **Healing** — не потрібно до аптечок

---

## Якщо щось не працює

Типові проблеми:
- **Компонент не реплікується:** перевір `Component Replicates = true` в Class Defaults
- **Змінна не оновлюється у клієнта:** перевір Replication тип змінної
- **AnyDamage не стріляє:** переконайся що застосовуєш `Apply Damage` (з GameplayStatics), а не щось інше
- **Функція `ApplyDamage` в компоненті плутається з нодою `Apply Damage` з GameplayStatics:** це нормально що назви схожі. У контексті компонента викликається твоя, у світовому контексті — глобальна. Якщо хочеш — перейменуй свою в `TakeDamageInternal` або `DealDamage` щоб не плутатись.

---

## Що ти маєш вивчити з цього кроку

Після того як закінчиш — ти маєш могти відповісти на ці питання (якщо ні — повертайся до відповідних секцій або запитай мене):

1. Чим відрізняється Actor від ActorComponent? Чому HP — компонент?
2. Що таке Replicated і чим відрізняється від RepNotify?
3. Чому логіка шкоди живе на сервері, а не на клієнті?
4. Що таке Event Dispatcher і навіщо OnDeath зроблено саме так?
5. Що робить нода `Switch Has Authority`?

---

Коли зробиш — покажи що вийшло (можна скриншотом або описом), і я перевірю + відповім на запитання що виникли. Або застрягнеш на якомусь кроці — питай одразу, не мучся.
