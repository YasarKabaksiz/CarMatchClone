# Staj Günlüğü

Bu dosya, staj defteri hazırlarken referans alınmak üzere günlük çalışma özetlerini tutar. Her gün sonunda yeni bir bölüm eklenir (en yeni gün en üstte).

**Not:** Staj boyunca birden fazla proje/görev üzerinde çalışılabilir. Her gün hangi proje üzerinde çalışıldığı, gün başlığının altında ayrıca belirtilir (şu an: **Car Match Clone** projesi).

---

## [TARİH GİRİLECEK] — Gün 3

### Yapılan İşler

Bir önceki günün son commit'i olan `feat(pooling): object pooling sistemi...`'den itibaren devam edildi.

**1. Milestone 4A — Holder + Match Sistemi**
- Event Channel altyapısı tamamlandı (CarEventChannel, ColorEventChannel, VoidEventChannel)
- `Holder`: 7 slotlu, aynı renkten 3'lü eşleşme, cascade match desteği
- `MatchChecker`: saf algoritma katmanı
- `GameManager`: GameState sahibi, Game Over / Level Complete akışı
- Renkli test level'ı ile pathfinding'in hücre boşalınca yeniden hesaplandığı doğrulandı

**2. Milestone 4B — CarMover (Animasyonlu Hareket)**
- DOTween ile path boyunca gerçek araç hareketi eklendi
- Event akışı: `OnCarSelected` → hareket → `OnCarReachedHolder` → Holder'a giriş
- Eşzamanlı hareket kilidi (`_moveLocked`) ile aynı anda tek araç hareket edebilmesi sağlandı

**3. Renk Sistemi (5 Renk)**
- Renk başına ayrı prefab (Car_Red, Car_Blue, Car_Green, Car_Yellow, Car_Purple), ayrı pool'lar
- `Board`'da CarColor → Prefab mapping kuruldu

**4. Sabit 8x7 Grid Mimarisine Geçiş**
- Board artık her zaman sabit 8 satır x 7 sütun (56 hücre) — gerçek oyunun board yapısına uygun
- `CellType.Wall` eklendi (geçilemez, statik görsel prefab — Instantiate/Destroy, pooling gerekmiyor)
- Exit noktaları artık otomatik hesaplanıyor (level tasarımcısının elle işaretlemesine gerek kalmadı, tasarım basitleştirildi)

**5. Unity Editor İçi Level Editor Aracı**
- `Window → CarMatchClone → Level Editor`: grid'i görsel olarak boyama (CarSlot/Empty/Wall + renk), Save/Save As
- Level tasarım sürecini Inspector'da elle 56 hücre doldurmaktan kurtaran özel araç geliştirildi

**6. Kamera Sistemi İnce Ayarı**
- `CameraController`: board+holder boyutuna göre otomatik framing (tilt açılı, padding ve verticalBias parametreleriyle)
- Gerçek oyunun ekran görüntüleriyle karşılaştırılarak görsel oran kalibre edildi

**7. Bug Düzeltmeleri**
- Namespace/sınıf isim çakışması (Board, CarColor) — tam nitelikli isim ile çözüldü
- Holder'daki araç tekrar seçilebiliyordu — `IsReachable=false` ile düzeltildi
- Game Over sonrası input durmuyor sorunu — `_inputLocked` flag ile düzeltildi
- Level Editor'de varsayılan hücre tipi Wall'dan Empty'ye çevrildi (level tasarlarken hataya açık bir tuzak gideirldi)
- Grid yönü (7x8 yerine 8x7) düzeltildi

### Kullanılan Araçlar/Teknikler
- Unity 3D (URP), C#, DOTween, A* Pathfinding, ScriptableObject Event Channel Pattern, Object Pooling, Git/GitHub, Claude Code (AI-assisted development), Unity Editor Scripting (EditorWindow)

### Öğrenilenler / Notlar
- Event-driven mimarinin (event channel'lar) sistemler arası bağımlılığı nasıl azalttığı
- Object pooling'in ne zaman gerekli, ne zaman gereksiz olduğu (statik vs. dinamik objeler)
- Unity Editor scripting ile özel araç (Level Editor) geliştirmenin level tasarım sürecini nasıl hızlandırdığı
- Kademeli milestone bazlı geliştirmenin (küçük, test edilebilir adımlar) hata ayıklamayı kolaylaştırdığı

---

## [TARİH GİRİLECEK] — Gün 2

### Yapılan İşler

**1. Referans Oyunun Analizi**
- Klonlanacak oyun (Car Match: Traffic Puzzle) App Store'dan incelendi, ekran görüntüleri ve gameplay videosu üzerinden mekanikler çıkarıldı
- Board yapısı (lane/kuyruk tabanlı sistem → sonra grid+pathfinding sistemine dönüştü), özel elemanlar (kilitli kutu, numaralı garaj/spawner) analiz edildi
- Aynı türden açık kaynak bir klon projesi (BusJam) araştırılarak endüstride kullanılan yaklaşım (grid + A* pathfinding) doğrulandı

**2. Proje Planlama ve Mimari Tasarım**
- `ARCHITECTURE.md` hazırlandı: grid sistemi, A* pathfinding, event-driven mimari (ScriptableObject Event Channel pattern), object pooling, save/load sistemi, milestone sırası
- `CLAUDE.md` hazırlandı: kod stili, kesin kurallar, Conventional Commits formatı (feat/fix/wip/refactor/chore/test)

**3. Unity Proje Kurulumu**
- Unity LTS + URP 3D template ile proje oluşturuldu
- Klasör yapısı, Input System, DOTween kuruldu
- Git + GitHub entegrasyonu (.gitignore, .gitattributes, Git LFS), VS Code + Claude Code entegrasyonu

**4. Milestone 1 — Grid + Pathfinding Sistemi**
- `GridCell`, `Board`, `PathfindingService` sınıfları (A* algoritması ile erişilebilirlik hesaplama)
- Tek düz lane test senaryosu ile doğrulandı

**5. Milestone 2 — Şekilli Board Desteği**
- `LevelData` ScriptableObject sistemi (Dictionary tabanlı grid, keyfi şekiller — L, U, Z)
- Çoklu exit noktası desteği (`exitPositions[]`)
- Test level'ları (Straight, L, U, Z) oluşturulup pathfinding doğrulandı

**6. Kamera Sistemi (İlk Versiyon)**
- `CameraController`: board boyutuna göre otomatik framing (tilt açılı, padding destekli)

**7. Milestone 3 — Object Pooling**
- `ObjectPool` + `ObjectPoolManager`: prefab başına ayrı pool, warm-up mantığı
- `Board`, `Instantiate()` yerine pooling kullanacak şekilde güncellendi

**8. Bug Düzeltmeleri**
- Namespace/sınıf isim çakışması (Board sınıfı/namespace'i) — alias ve sonra tam nitelikli isim ile çözüldü

### Kullanılan Araçlar/Teknikler
- Unity 3D (URP), C#, A* Pathfinding, ScriptableObject Event Channel Pattern, Object Pooling, Git/GitHub, Claude Code (AI-assisted development)

### Öğrenilenler / Notlar
- Mobil puzzle oyunlarında yaygın kullanılan grid + pathfinding mimarisinin nasıl çalıştığı
- Event-driven mimari prensiplerinin (sistemler birbirini doğrudan tanımamalı) neden önemli olduğu
- Git/GitHub kurulumu, Unity projelerinde .gitignore ve Git LFS kullanımı

---

## [TARİH GİRİLECEK] — Gün 1

### Yapılan İşler
- Staj yerindeki ekiple tanışma
- Yapılacak proje netleştirildi: mobil bir oyunun (Car Match: Traffic Puzzle) klonlanması görevi verildi
- Genel süreç ve izlenecek yol hakkında görüşüldü

---
