# The House — 2D Side-Scrolling Adventure

Исследовательская игра от третьего лица на движке **Godot 4.6 Mono (C#)**. Игрок бродит по дому из нескольких комнат, общается с мамой и шаровидным компаньоном-ИИ, выполняет простой квест.

---

## Что реализовано

### Геймплей
- Перемещение влево/вправо (A/D или стрелки), спринт (Shift), взаимодействие (E)
- Гравитация и коллизия с полом и стенами комнат
- Граничные стены в каждой комнате — игрок не может выйти за пределы уровня

### Комнаты
| Сцена | Файл | Описание |
|---|---|---|
| Холл | `scripts/HallwayScene.cs` | Стартовая комната, 3000px, двери в три комнаты, яблоко на столе, лампы |
| Кухня | `scripts/KitchenScene.cs` | Кухонная стойка, окно, мама-NPC |
| Спальня | `scripts/BedroomScene.cs` | Кровать, шкаф, свеча |
| Гостиная | `scripts/LivingRoomScene.cs` | Диван, камин, книжная полка |

### Переходы между комнатами
Плавный fade-out → смена сцены → fade-in через `SceneTransitionManager`. Позиция спауна задаётся строкой (`"SpawnFromKitchen"` и т.п.) и подхватывается `GameManager.TargetSpawnPoint`.

### Инвентарь
6 слотов, переключение колесом мыши или клавишами 1–6. Подбор предметов через взаимодействие (E).

### Квест «Помоги маме»
1. Поговори с мамой на кухне — квест выдаётся с диалогом.
2. Подбери яблоко в холле.
3. Вернись к маме — диалог завершения, яблоко исчезает из инвентаря.
4. Quest-трекер в правом верхнем углу показывает статус.

### Диалоги
Диалоговое окно внизу экрана, переключение реплик клавишей E или кликом мыши.

### Компаньон-Сфера
Парит за игроком с синусоидальным покачиванием, рисуется процедурно тремя окружностями.

### Настройки
- Громкость (Master / Music / SFX)
- Полноэкранный режим
- Выбор разрешения: 1280×720 / 1600×900 / 1920×1080 / 2560×1440
- Переназначение клавиш: Move Left, Move Right, Run, Interact
- Все настройки сохраняются в `user://settings.cfg`

---

## Что НЕ реализовано / известные ограничения

| # | Чего нет | Примечание |
|---|---|---|
| 1 | **Звук** | Движок поддерживает, но аудио-ассеты не добавлялись |
| 2 | **Анимации спрайтов (покадровые)** | Используется 3-view design sheet (front / side / back). Ходьба имитируется боб-анимацией (смещение по Y), настоящего walk-cycle нет |
| 3 | **Больше одного квеста** | Есть только «Принеси яблоко» |
| 4 | **Сохранение игры** | Подобранные предметы хранятся в памяти (`GameManager.PickedUpItems`) и сбрасываются при закрытии |
| 5 | **Второй тип интеракций** | Все интерактивные объекты либо двери, либо предметы, либо NPC — без головоломок |
| 6 | **Мобильная сборка** | Управление и экспорт под Android/iOS не настраивались |
| 7 | **Локализация** | Весь текст захардкожен в коде |
| 8 | **Меню паузы** | Паузы в игре нет |

---

## Структура проекта

```
project.godot           # Конфиг Godot: viewport 1920×1080, input map, autoloads
TheHouse.csproj         # .NET 9 проект

scripts/
  GameDialogues.cs      # ← ВСЕ диалоги игры — менять здесь
  PlayerController.cs   # Движение, анимация спрайта, инвентарь, взаимодействие
  RoomBase.cs           # Базовый класс комнаты: геометрия, стены, спаун игрока/сферы
  HallwayScene.cs       # Холл: планировка, предметы, двери
  KitchenScene.cs       # Кухня: мебель, мама
  BedroomScene.cs       # Спальня
  LivingRoomScene.cs    # Гостиная
  MotherNPCController.cs # Логика мамы-NPC, машина состояний квеста
  InteractableBase.cs   # Абстрактный Area2D для всех интерактивных объектов
  DoorInteraction.cs    # Дверь — переход в другую сцену
  ItemPickup.cs         # Подбираемый предмет
  OrbFollower.cs        # Компаньон-сфера
  InventoryManager.cs   # Autoload: данные инвентаря + HUD
  QuestManager.cs       # Autoload: состояния квестов + UI
  DialogueManager.cs    # Autoload: очередь реплик + диалоговое окно
  SettingsManager.cs    # Autoload: настройки, полноэкранность, привязки клавиш
  SceneTransitionManager.cs # Autoload: fade-переходы
  GameManager.cs        # Autoload: глобальное состояние (ближайший объект, спаун)
  MainMenuUI.cs         # Главное меню + панель настроек

scenes/
  Build*.cs             # Headless-билдеры сцен (генерируют .tscn файлы)
  *.tscn                # Скомпилированные сцены

sprites/
  hero_spritesheet.png  # Спрайт игрока: 3 вида (front/side/back) 1274×880
  mother_sprite.png     # Спрайт мамы
```

---

## Как менять диалоги

**Весь текст диалогов** хранится в одном файле:

```
scripts/GameDialogues.cs
```

Каждый диалог — статический метод, возвращающий список реплик:

```csharp
public static List<DialogueLine> MotherGiveQuest() => new()
{
    new("Mother", "Oh, there you are. I've been looking for you."),
    new("Mother", "Could you bring me the apple from the hallway table?"),
    new("Player", "Sure, I'll get it for you."),
    new("Orb",    "An apple? Simple enough."),
};
```

`DialogueLine` принимает **(speaker, text)**. Speaker — произвольная строка, отображается жёлтым над текстом.

Чтобы добавить новый диалог:
1. Добавьте метод в `GameDialogues.cs`
2. Вызовите его из нужного скрипта:
   ```csharp
   DialogueManager.Instance.StartDialogue(GameDialogues.ВашМетод());
   // или с колбэком по завершении:
   DialogueManager.Instance.StartDialogue(GameDialogues.ВашМетод(), onComplete: () => { /* ... */ });
   ```

---

## Как добавить новую комнату

1. Создайте `scripts/NewRoomScene.cs`, унаследуйтесь от `RoomBase`:
   ```csharp
   public partial class NewRoomScene : RoomBase
   {
       protected override int RoomWidth => 1920;
       protected override void SetupRoom()
       {
           SpawnPoints["DefaultSpawn"]     = new Vector2(960f, SpawnY);
           SpawnPoints["SpawnFromHallway"] = new Vector2(300f, SpawnY);
           AddDoor(new Rect2(60f, FloorY - 345f, 150f, 345f),
                   "Return to Hallway", "res://scenes/hallway.tscn", "SpawnFromNewRoom");
       }
   }
   ```
2. Создайте `scenes/BuildNewRoom.cs` — headless-билдер по аналогии с существующими.
3. Запустите билдер: `dotnet build && godot --headless --script scenes/BuildNewRoom.cs`
4. Добавьте дверь из холла в новую комнату в `HallwayScene.cs`.

---

## Как добавить предмет

В любом `SetupRoom()`:
```csharp
var torch = new ItemData("torch", "Факел", "Освещает тёмные комнаты.", new Color(0.9f, 0.5f, 0.1f));
AddItem(torch, new Vector2(500f, FloorY - 21f));
```

`ItemData(id, name, description, iconColor)` — id должен быть уникальным.

---

## Сборка с нуля

```bash
dotnet build TheHouse.csproj
godot --headless --import
godot --headless --script scenes/BuildPlayer.cs
godot --headless --script scenes/BuildOrb.cs
godot --headless --script scenes/BuildHallway.cs
godot --headless --script scenes/BuildKitchen.cs
godot --headless --script scenes/BuildBedroom.cs
godot --headless --script scenes/BuildLivingRoom.cs
godot --headless --script scenes/BuildMainMenu.cs
```

После этого запускайте через редактор Godot или `godot scenes/main_menu.tscn`.

---

## Технологии

- **Godot 4.6.2 Mono** + **C# / .NET 9**
- Вся геометрия комнат процедурная (ColorRect ноды, без внешних ассетов)
- Спрайты: `sprites/` (PNG с удалённым белым фоном через PIL)
- Настройки: `ConfigFile` → `user://settings.cfg`
