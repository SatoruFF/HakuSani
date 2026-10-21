# Haku & Sani: Time Crossing

Godot 4.4 (.NET) · кооператив на двоих (split-screen) или соло с переключением котов.

## Запуск
Открыть папку в `Godot_mono.app` → F5. Или из терминала:
```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot --path ~/Desktop/prog/hakusani
```

## Управление
| | Хаку (прошлое, розовый ошейник) | Сани (будущее, синий ошейник) |
|---|---|---|
| Движение | WASD | Стрелки |
| Прыжок | Пробел | Enter |
| Бег | Shift | `/` |
| Действие | E | `.` |
| Камера | мышь, Q / R | `[` `]` |

Геймпады: стик, A — прыжок, X — действие, B/RT — бег, правый стик — камера. Два геймпада → по одному на кота; один геймпад → Сани.
Соло: Tab / Y — переключить кота. Esc / Start — пауза.

## Структура
- `scripts/Core` — Game (автозагрузка, сохранение, переходы), Inputs, слои.
- `scripts/Cats` — Cat (движение, анимации, призрак-силуэт), CatCamera.
- `scripts/World` — конструктор уровней `B`, материалы, гаджеты (ящик, плита, плот, следы), дом.
- `scripts/Levels` — Пролог (интро в движке), Главы I–IV, Финал. Уровни строятся кодом.
- `assets/medieval`, `assets/city` — модульные киты Quaternius; `assets/audio` — музыка и звуки (авторы в `assets/audio/CREDITS.md`).
- `assets/cats/haku.glb`, `sani.glb` — собраны `tools/build_cats.py` (Blender) из `generalContext/models/source/model.glb`: упрощение меша, ошейник, скелет с IK-ногами, анимации Idle/Walk/Run/Jump/Fall/Sleep/Paw (лапы не скользят: скорость анимации = скорость кота).

Обе эпохи лежат в одних координатах и разделены render/physics-слоями: изменение в прошлом сразу видно «спустя годы» в будущем.

## Пересборка котов
```bash
/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup -P tools/build_cats.py
```

## Отладка
`-- --chapter=N` (0 пролог … 5 финал), `--mode=solo`, `--script=solve` (авто-прохождение), `--shotdir=DIR` (скриншоты).
