# Sisifos - Sahne Kurulum Rehberi

Bu rehber, 3D side-scroller karakter kontrolcüsü ve Cinemachine kamera sisteminin nasıl kurulacağını açıklar.

## Gereksinimler

- Unity Input System (zaten yüklü)
- Cinemachine 3.x (zaten yüklü)
- Terrain veya mesh collider'lı zemin

---

## 1. Player Prefab Kurulumu

### 1.1 Boş GameObject Oluştur
1. **Hierarchy** > **Create Empty** > "Player" olarak adlandır
2. **Tag** olarak "Player" seç

### 1.2 Karakter Modeli
1. Player'ın altına karakter modelini ekle
2. Animator component'i modele eklenmiş olmalı

### 1.3 Character Controller Ekle
1. Player objesine **Character Controller** component ekle
2. Ayarlar:
   - **Center**: (0, 1, 0)
   - **Radius**: 0.3
   - **Height**: 2

### 1.4 Scriptleri Ekle
```
Player (GameObject)
├── CharacterController
├── SlopeCharacterController
├── PlayerInputHandler
└── Player Input (Component)
```

1. **SlopeCharacterController** ekle
2. **PlayerInputHandler** ekle
3. **Player Input** component ekle:
   - **Actions**: `InputSystem_Actions` asset'ini sürükle
   - **Default Map**: "Player"
   - **Behavior**: "Invoke Unity Events" veya "Send Messages"

### 1.5 SlopeCharacterController Ayarları

| Ayar | Önerilen Değer | Açıklama |
|------|----------------|----------|
| Walk Speed | 4 | Yürüme hızı |
| Run Speed | 8 | Koşma hızı |
| Jump Force | 8 | Zıplama gücü |
| Gravity | -20 | Yerçekimi |
| Ground Layer | "Ground" | Zemin layer'ı |
| Max Slope Angle | 45 | Maksimum tırmanılabilir eğim |
| Lock Z Axis | ✓ | Side-scroller için aktif |

---

## 2. Cinemachine Kamera Kurulumu

### 2.1 Cinemachine Kamera Oluştur
1. **Hierarchy** > **Cinemachine** > **Cinemachine Camera**
2. "CM_SideScroller" olarak adlandır

### 2.2 Kamera Ayarları
1. **Tracking Target**: Player objesini sürükle
2. **Add Component** > **Cinemachine Follow** ekle
3. **Cinemachine Follow** ayarları:
   - **Follow Offset**: (0, 5, -15)
   - **Damping**: (0.5, 0.5, 0.5)

### 2.3 DynamicCameraController Ekle
1. CM_SideScroller objesine **DynamicCameraController** ekle
2. Ayarları:
   - **Player**: Player objesini sürükle
   - **Virtual Camera**: Otomatik bulunur
   - **Camera Distance**: 15
   - **Camera Height**: 3
   - **Look Ahead Distance**: 3

---

## 3. Terrain ve Ground Layer

### 3.1 Layer Oluştur
1. **Edit** > **Project Settings** > **Tags and Layers**
2. Yeni layer ekle: "Ground"

### 3.2 Terrain Ayarları
1. Terrain objesini seç
2. **Layer**: "Ground" olarak ayarla
3. Terrain Collider'ın aktif olduğundan emin ol

### 3.3 SlopeCharacterController Ground Layer
1. Player'daki SlopeCharacterController'ı seç
2. **Ground Layer**: "Ground" layer'ını seç

---

## 4. Camera Zone Trigger (Opsiyonel)

Sinematik anlar için kamera bölgeleri oluşturabilirsiniz:

1. Boş GameObject oluştur > "CameraZone_Vista" olarak adlandır
2. **Box Collider** ekle, **Is Trigger** aktif
3. **CameraZoneTrigger** script ekle
4. Preset ayarları:
   - **Distance**: 25 (geniş açı için)
   - **Height**: 5
   - **Enter Transition Time**: 2 (yavaş geçiş)

---

## Hızlı Test

1. Play moduna gir
2. **A/D** veya **Sol/Sağ ok** ile hareket et
3. **Space** ile zıpla
4. **Shift** ile koş
5. Terrain eğimlerinde karakterin rotasyonunu gözlemle

---

## Sorun Giderme

| Sorun | Çözüm |
|-------|-------|
| Karakter hareket etmiyor | Input Actions asset'inin atandığını kontrol et |
| Karakter yere düşüyor | Ground Layer ayarını kontrol et |
| Kamera takip etmiyor | Player'ın Tag'inin "Player" olduğunu kontrol et |
| Rotation çalışmıyor | Ground raycast'ın zemine ulaştığını kontrol et (Scene view'da görüntülenir) |

---

## 5. Kutu Sürükleme (Box Dragging) Sistemi

### 5.1 Layer Oluştur
1. **Edit** > **Project Settings** > **Tags and Layers**
2. Yeni layer ekle: "Box" veya "Draggable"

### 5.2 Kutu Prefab Hazırla
1. **Hierarchy** > **3D Object** > **Cube** oluştur
2. "DraggableBox" olarak adlandır
3. Şu component'leri ekle:
   - **Rigidbody** (Is Kinematic: ✓)
   - **DraggableBox** script
4. Layer: "Box" olarak ayarla
5. Prefab olarak kaydet

### 5.3 DraggableBox Ayarları

| Ayar | Önerilen Değer | Açıklama |
|------|----------------|----------|
| Attach Point Offset | (0, 0.5, 0) | Halatın bağlandığı nokta |
| Follow Speed | 8 | Takip yumuşaklığı |
| Rope Length | 1.5 | Kutular arası mesafe |
| Stay Grounded | ✓ | Kutu yere yapışık kalır |
| Ground Layer | "Ground" | Zemin layer'ı |
| Interaction Range | 2 | Bağlanma mesafesi |

### 5.4 Player'a BoxCarrier Ekle
1. Player objesine **BoxCarrier** component ekle
2. Player objesine **RopeVisualizer** component ekle (LineRenderer otomatik eklenir)
3. Ayarlar:
   - **Max Boxes**: 5 (veya 0 sınırsız için)
   - **Box Layer**: "Box" layer'ını seç
   - **Rope Visualizer**: RopeVisualizer component'ini sürükle

### 5.5 PlayerInputHandler'a BoxCarrier Bağlantısı
1. PlayerInputHandler'daki **Box Carrier** alanına BoxCarrier component'ini sürükle
2. InputSystem_Actions'a "Interact" action ekle:
   - **Action Type**: Button
   - **Binding**: E (Keyboard) veya South Button (Gamepad)

---

## Hızlı Test - Kutu Sürükleme

1. Sahneye 3-4 adet DraggableBox prefab'ı yerleştir
2. Play moduna gir
3. Kutuya yaklaş ve **E** tuşuna bas - kutu bağlanır
4. Hareket et - kutu arkandan gelir
5. Başka kutulara yaklaşıp **E** ile zincirleme bağla
6. **E** tuşuna basarak son kutuyu çöz

