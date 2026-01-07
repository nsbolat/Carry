# Main Menu Setup Guide

Bu döküman, main menu sisteminin Unity Editor'da nasıl kurulacağını açıklar.

## Gerekli Componentler

### 1. GameStateManager
```
Hierarchy:
└── GameStateManager (Empty GameObject)
    └── GameStateManager.cs
```

### 2. UI Canvas
```
Hierarchy:
└── MainMenuCanvas
    ├── Canvas (Screen Space - Overlay)
    ├── CanvasGroup (fade için)
    └── Children:
        ├── Logo (Image - üst merkez)
        ├── StartButton (Button + Image + TextMeshPro)
        └── QuitButton (Button + Image + TextMeshPro)
```

**Canvas Setup:**
1. `GameObject > UI > Canvas` oluştur
2. Canvas'a `CanvasGroup` component ekle
3. `MainMenuUI.cs` component'ini Canvas'a ekle

**Logo:** `UI > Image` → PNG sprite sürükle

**Buttons:** `UI > Button - TextMeshPro` oluştur

### 3. MainMenuController
```
Hierarchy:
└── MainMenuController (Empty GameObject)
    └── MainMenuController.cs
        ├── Menu UI → MainMenuCanvas
        ├── Player Intro → PlayerIntroController
        └── Menu Camera → MenuCameraController
```

### 4. Player Setup
Player GameObject'ine `PlayerIntroController.cs` ekle:
- **Intro Duration:** 5
- **Start Offset:** (-15, 0, 0)
- **Target Position:** (0, 0, 0)

### 5. Dual Camera Setup (YENİ)

**Menu Camera** - Sabit manzara kamerası:
```
Hierarchy:
└── MenuCamera
    ├── CinemachineCamera component
    └── MenuCameraController.cs
```

1. `GameObject > Cinemachine > Cinemachine Camera` oluştur
2. İsim: "MenuCamera"
3. Kamerayı istediğin sabit konuma yerleştir (manzarayı gösteren açı)
4. `MenuCameraController.cs` component'ini ekle
5. Priority: 20 (aktif), 0 (deaktif)

**Player Camera** - Mevcut DynamicCameraController:
- Priority: 10 (menü kamerasından düşük)

**Kamera Akışı:**
1. Oyun başlar → MenuCamera aktif (Priority 20)
2. Start tıkla → UI fade out, karakter yürümeye başlar
3. Karakter hedefe varır → MenuCamera deaktif (Priority 0)
4. Cinemachine otomatik PlayerCamera'ya blend yapar

## Reference Bağlantıları

| Component | Field | Referans |
|-----------|-------|----------|
| MainMenuUI | Canvas Group | Canvas'taki CanvasGroup |
| MainMenuUI | Logo Image | Logo Image |
| MainMenuUI | Start/Quit Button | Butonlar |
| MainMenuController | Menu UI | MainMenuUI component |
| MainMenuController | Player Intro | PlayerIntroController |
| MainMenuController | Menu Camera | MenuCameraController |

## Test

1. Play Mode'a gir
2. Logo ve butonlar görünmeli
3. Start'a tıkla → UI fade out
4. Karakter yürümeye başlar (menü kamerası aktif kalır)
5. Karakter hedefe varır → Gameplay kamerasına smooth geçiş
6. WASD ile hareket et
