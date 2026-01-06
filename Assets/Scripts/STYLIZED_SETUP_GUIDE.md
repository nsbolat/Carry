# Stylized Visual System - Kurulum Rehberi

## Oluşturulan Dosyalar

### 1. StylizedLit Shader
**Konum:** `Assets/Shaders/StylizedLit.shader`

Bu shader aşağıdaki özellikleri içerir:
- **Soft Shadows:** Yumuşak gölge geçişleri
- **Shadow Color:** Gölgelere renk (mor/mavi tonları)
- **Rim Light:** Kenar ışığı efekti
- **Distance Fog:** Mesafeye bağlı sis blend

### 2. StylizedVolumeProfile
**Konum:** `Assets/Settings/StylizedVolumeProfile.asset`

Post-processing efektleri:
- **Color Adjustments:** Sıcak ton, düşük satürasyon
- **Split Toning:** Gölgeler mor, highlight turuncu
- **Bloom:** Sıcak tonlu parlama
- **Vignette:** Kenar karartması
- **Depth of Field:** Gaussian blur
- **Tonemapping:** ACES

---

## Kurulum Adımları

### Adım 1: Volume Profile'ı Uygulama
1. Hierarchy'de `Global Volume` objenizi seçin (veya yeni bir tane oluşturun: `GameObject > Volume > Global Volume`)
2. Volume component'inde **Profile** alanına `StylizedVolumeProfile` asset'ini sürükleyin
3. **Is Global** checkbox'ının işaretli olduğundan emin olun

### Adım 2: Material Oluşturma
1. `Assets > Create > Material` ile yeni bir material oluşturun
2. Shader olarak `Custom/StylizedLit` seçin
3. Önerilen ayarlar:
   - **Shadow Color:** `(0.4, 0.35, 0.5, 1)` (mor-gri)
   - **Shadow Softness:** `0.3`
   - **Rim Intensity:** `0.5`
   - **Fog Color:** `(0.75, 0.65, 0.55, 1)` (sıcak bej)

### Adım 3: Sahne Ayarları
1. `Window > Rendering > Lighting`
2. **Environment** sekmesinde:
   - **Skybox Material:** Basit gradient skybox veya solid color
   - **Ambient Mode:** Flat veya Gradient
   - **Ambient Color:** Sıcak tonlar `(0.6, 0.55, 0.5)`

### Adım 4: Çim Shader Güncellemesi (Opsiyonel)
Mevcut `GrassShader.shader`'da renkleri güncelle:
- **Base Color:** `(0.15, 0.25, 0.12, 1)` (koyu yeşil)
- **Tip Color:** `(0.4, 0.5, 0.25, 1)` (açık yeşil-sarı)

---

## Önerilen Renk Paleti

| Öğe | Renk (RGB) | Hex |
|-----|------------|-----|
| Ana Çim (Base) | (0.15, 0.25, 0.12) | #263F1F |
| Çim Ucu | (0.4, 0.5, 0.25) | #668040 |
| Gölge | (0.4, 0.35, 0.5) | #665980 |
| Sis | (0.75, 0.65, 0.55) | #BFA68C |
| Rim Light | (1, 0.9, 0.8) | #FFE6CC |
| Ambient | (0.6, 0.55, 0.5) | #998C80 |

---

## Hızlı Test
1. Sahneye bir `Cube` ekleyin
2. StylizedLit shader ile material uygulayın
3. Directional Light'ı döndürerek gölge geçişlerini test edin
4. Game view'da post-processing efektlerini kontrol edin
