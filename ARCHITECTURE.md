# ARCHITECTURE.md
## Car Match Clone — Teknik Mimari Dokümanı

Bu doküman, projenin teknik anayasasıdır. Claude Code (veya başka bir geliştirici) her görev öncesinde bu dosyayı referans almalı, buradaki desenlerin dışına çıkmamalıdır. Yeni bir sistem eklerken önce burada tanımlı prensiplere uyup uymadığını kontrol et.

---

## 1. Oyun Özeti

- **Tür:** Grid tabanlı "collector / queue matching" puzzle (Match-3 alt türü)
- **Perspektif:** 3D modeller, sabit üstten-yakın perspektif kamera
- **Core Loop:** Board üzerindeki lane'lerden (şerit/kuyruk) araç seçilir → holder'a gider → aynı renkten 3 araç yan yana gelince patlar → board tamamen boşalınca level biter
- **Kaybetme koşulu:** Holder (7 slot) doluyken match oluşmadan yeni araç eklenemez hale gelirse Game Over

---

## 2. Temel Mimari Prensipler

1. **Event-Driven Tasarım:** Sistemler birbirini doğrudan referans almaz, event bus üzerinden haberleşir. Yeni bir sistem eklemek mevcut sistemleri bozmamalı.
2. **Data-Driven Level Tasarımı:** Level içerikleri (ScriptableObject/JSON) kod değişikliği gerektirmeden üretilebilmeli.
3. **Tek Sorumluluk:** Her sınıf tek bir işten sorumlu (Board grid yönetir, Pathfinding erişilebilirlik hesaplar, Holder match kontrolü yapar — birbirine karışmaz).
4. **Gerçek Zamanlı Hesaplama, Önceden Tanımlı Rota Yok:** Araçların "seçilebilir" durumu, board her değiştiğinde otomatik yeniden hesaplanır (bkz. Bölüm 5). Level tasarımcısı rota tanımlamaz, sadece board'un ilk durumunu tanımlar.

---

## 3. Klasör Yapısı

```
Assets/
  _Project/
    Scripts/
      Core/
        GameManager.cs
        GameEvents.cs          (event channel / event bus)
        SceneLoader.cs
      Board/
        Board.cs               (grid state, tüm hücrelerin sahibi)
        GridCell.cs             (tek hücre: pozisyon, walkable, occupant)
        PathfindingService.cs   (erişilebilirlik hesaplama - A*/BFS)
      Gameplay/
        Car.cs
        CarMover.cs             (path boyunca hareket/tween)
        Holder.cs               (7 slot, match kontrolü)
        MatchChecker.cs
      SpecialMechanics/
        ILaneObstacle.cs        (ortak interface)
        LockedBox.cs
        GarageSpawner.cs
      Boosters/
        IBooster.cs
        UndoBooster.cs
        ShuffleBooster.cs
        SuperUndoBooster.cs
        MagnetBooster.cs
      UI/
        HUDController.cs
        LevelCompletePopup.cs
        MainMenuController.cs
      Data/
        LevelData.cs            (ScriptableObject)
        LevelLoader.cs
    Prefabs/
      Cars/
      Obstacles/
      UI/
    Sprites/ & Models/
    Levels/                     (LevelData asset dosyaları)
    Scenes/
      MainMenu.unity
      Gameplay.unity
```

---

## 4. Veri Katmanı (Data Layer)

### LevelData (ScriptableObject)
Her level için:
- `gridWidth`, `gridHeight`
- `cells[]`: her hücre için — pozisyon, hücre tipi (Empty, Wall, CarSlot, GarageSpawner, LockedBox), araç rengi (varsa), garaj stok sayısı (varsa)
- `difficultyTag`: enum (Normal, Hard, SuperHard) — sadece UI gösterimi için, oynanışı etkilemez

### Neden ScriptableObject + basit veri, "waypoint" değil
Board tamamen grid koordinatlarından oluşur. Level tasarımcısı sadece **başlangıç durumunu** tanımlar (hangi hücrede ne var). Yol/rota, oyun çalışırken PathfindingService tarafından anlık hesaplanır. Bu, her level için elle rota tanımlama zahmetini ortadan kaldırır.

---

## 5. Board & Pathfinding Sistemi (En Kritik Katman)

### GridCell
```
GridCell
├── position: Vector2Int
├── isWalkable: bool          // true: içinden geçilebilir, false: duvar/dolu
├── occupant: Car | null       // hücrede duran araç (varsa)
└── obstacle: ILaneObstacle | null  // LockedBox veya GarageSpawner (varsa)
```

### Board
- Tüm `GridCell[,]` matrisini tutar.
- `Exit` sanal bir hedef noktadır (holder'ın bağlandığı nokta/nokta grubu).
- Bir hücre boşaldığında (`OnCellVacated`) event fırlatır.

### PathfindingService
- **Algoritma:** A* (BFS de yeterli olur çünkü tüm hücreler eşit maliyetli, ama A* ileride ağırlıklı/öncelikli yol senaryolarına genişlemeye daha uygun — bu yüzden A* tercih edilir).
- **Ne zaman çalışır:** Her board state değişiminde (araç holder'a ulaştığında, kutu kırıldığında, garaj spawn ettiğinde/kapandığında) — her frame değil.
- **Çıktı:** Her `Car` için `isReachable` (true/false) durumunu günceller. Sadece `isReachable = true` olan araçlar tıklanabilir/interactable olur.

```
PathfindingService
├── RecalculateReachability(Board board): void
│     → board'daki her Car için Exit'e ulaşan bir yol var mı diye A* çalıştırır
│     → sonucu Car.isReachable üzerine yazar
└── HasPathToExit(GridCell start, Board board): bool
```

### Kritik Kural
Bir arabanın seçilebilir olması için **başlangıçtan Exit'e kadar TÜM yol boyunca hücreler walkable olmalı** (sadece bitişik hücre değil). Bu kullanıcı tarafından doğrulanmış bir kuraldır.

---

## 6. Özel Mekanikler (Locked Box & Garage Spawner)

Her ikisi de ortak bir prensipten türer: **"komşu bir hücre boşaldığında tetiklenen davranış."**

```
ILaneObstacle
└── OnAdjacentCellVacated(GridCell triggeringCell): void
```

### LockedBox
- Board'daki normal bir araç slotunun yerinde durur (kendi hücresi `isWalkable = false`).
- Bitişiğindeki **herhangi bir** hücre boşaldığında (komşu araç holder'a gittiğinde) tetiklenir.
- Tetiklenince: kutu görsel olarak patlar, kendi hücresindeki `occupant` içindeki araç ortaya çıkar (reveal), hücre artık o arabanın durumuna göre walkable/occupied olur.
- **Tek seferlik davranış** (bir kez tetiklenir, sonra pasif).

### GarageSpawner
- Bir lane'in **herhangi bir noktasında** (uçta ya da ortada) bulunabilir; kendi hücresi `isWalkable = false` (garaj doluyken içinden geçilemez).
- Önündeki (bitişiğindeki) araç holder'a gidince tetiklenir.
- Tetiklenince: garajın bulunduğu hücreye yeni bir araç spawn olur (garajın stok rengi), stok sayacı 1 azalır.
- Stok `0` olunca: garaj pasifleşir, artık spawn etmez. **Kendi hücresi kalıcı olarak `isWalkable = false` kalır** (duvar gibi davranır).
- Garaj kapandıktan sonra arkasındaki araçlar, PathfindingService'in bulacağı **alternatif bir yoldan** (sağ/sol boşsa oradan dolanarak) Exit'e ulaşabilir. Bu, ekstra kod gerektirmez — çünkü PathfindingService zaten her state değişiminde board'u yeniden tarar.

### Neden Ortak Interface
`ILaneObstacle` sayesinde ileride yeni bir engel tipi eklenmek istendiğinde (örn. "elevator" — görsellerde bahsi geçen ama mekaniği netleşmemiş bir eleman), mevcut sistemlere dokunmadan sadece yeni bir sınıf yazılır.

---

## 7. Holder & Match Sistemi

```
Holder
├── slots: Car[7]
├── AddCar(Car car): void       // slotlardan birine ekler
├── CheckMatch(): void          // aynı renkten 3 yan yana var mı kontrol eder
└── IsFull: bool                // game over kontrolü için
```

- Araç holder'a ulaştığında `Holder.AddCar()` çağrılır.
- `MatchChecker` aynı renkten 3 tanenin yan yana olup olmadığını kontrol eder, varsa patlatır, slotları kaydırır.
- Holder doluyken match oluşmazsa → `GameEvents.OnGameOver` fırlatılır.
- Board'daki tüm hücreler boş kalınca → `GameEvents.OnLevelComplete` fırlatılır.

---

## 8. Event Bus (GameEvents)

Sistemler birbirini doğrudan çağırmaz, aşağıdaki event'ler üzerinden haberleşir:

```
OnCarSelected(Car car)
OnCarReachedHolder(Car car)
OnCellVacated(GridCell cell)
OnMatchOccurred(CarColor color)
OnHolderFull()
OnGameOver()
OnLevelComplete()
OnBoosterUsed(BoosterType type)
```

UI, ses sistemi, analytics, spawner mekanikleri bu event'leri dinler; birbirlerinden habersizdir. Yeni bir sistem eklerken mevcut event'lere abone ol, mevcut sınıfları değiştirme.

### Somut İmplementasyon Kararı: ScriptableObject Event Channel

"Event bus" soyut bir kavram olarak kalmasın diye kesin karar: **ScriptableObject tabanlı Event Channel pattern** kullanılacak (Unity projelerinde endüstri standardı, Inspector'dan bağlanabilir, MonoBehaviour'lar arası sıkı bağımlılık yaratmaz).

```
Core/Events/
  GameEventChannel.cs        // generic base: ScriptableObject, List<Listener>, Raise(T payload)
  CarEventChannel.cs         // GameEventChannel<Car>
  CellEventChannel.cs        // GameEventChannel<GridCell>
  ColorEventChannel.cs       // GameEventChannel<CarColor>
  VoidEventChannel.cs        // parametresiz event'ler için (OnGameOver, OnLevelComplete)
```

Her event channel bir `.asset` dosyası olarak `Assets/_Project/Data/EventChannels/` altında saklanır. Yayınlayan (publisher) ve dinleyen (listener) taraflar birbirini hiç tanımaz, sadece aynı `.asset` dosyasına referans verir. Örn: `Board`, `OnCellVacated` channel'ına `Raise()` çağırır; `PathfindingService` ve `ILaneObstacle` implementasyonları aynı channel'ı dinler — ikisi de birbirinden habersizdir.

**Kural:** C#'ın native `event`/`Action` yapısı sadece bir sınıfın kendi iç mantığında (ör. `CarMover` kendi tamamlanma callback'i için) kullanılabilir. Sistemler arası (cross-system) haberleşme HER ZAMAN event channel üzerinden olur.

---

## 9. Booster Sistemi

### GameState (Önceden Tanımsızdı — Netleştirildi)

`GameState`, tek bir level oynanışının anlık durumunu tutan basit bir veri sınıfı (POCO, MonoBehaviour değil):

```
GameState
├── currentLevelData: LevelData
├── movesUsedCount: int
├── boosterCounts: Dictionary<BoosterType, int>   // oyuncunun elindeki booster sayısı
├── isGameOver: bool
└── isLevelComplete: bool
```

`GameManager` bu sınıfın tek sahibi ve yaşam döngüsünü yönetir (level başlarken oluşturur, level bitince serbest bırakır). Booster'lar ve diğer sistemler `GameState`'i sadece `GameManager` üzerinden okur/günceller, kendi kopyalarını tutmaz.

```
IBooster
└── Execute(Board board, GameState state): void
```

- **Undo:** son hamleyi geri alır (board'un bir önceki state snapshot'ını tutmak gerekir — basit bir memento pattern).
- **Shuffle:** board'daki araçların renk/pozisyonlarını yeniden dağıtır (kilit/garaj durumları korunur), sonrasında `PathfindingService.RecalculateReachability()` tekrar çağrılır.
- **Super Undo:** seçili aracı geçici bir "bekleme" slotuna alır.
- **Magnet:** aynı renkteki erişilebilir arabaları highlight eder/otomatik seçer.

Her booster event bus üzerinden `OnBoosterUsed` fırlatır, kendi mantığını board üzerinde uygular.

---

## 10. Kamera & Hareket

- Kamera sabit, level'ın board boyutuna göre otomatik "frame" (zoom/pozisyon) ayarlanır.
- `CarMover`, PathfindingService'in bulduğu path'i (hücre listesi) alır, araç bu hücreler boyunca tween ile hareket eder (DOTween önerilir).

---

## 11. Save/Load Sistemi

MVP aşaması için basit ama genişletilebilir bir yapı:

```
Core/SaveSystem/
  SaveData.cs          // [Serializable] POCO: coin, mevcut level index, booster envanteri, ayarlar
  SaveManager.cs        // Save()/Load()/HasSave() — JSON serialization (JsonUtility yeterli, MVP için)
```

- **Format:** JSON, `Application.persistentDataPath` altında tek bir dosya (`save.json`).
- **Ne zaman kaydedilir:** Level tamamlanınca, coin/booster değiştiğinde, uygulama arka plana alınınca (`OnApplicationPause`).
- **PlayerPrefs KULLANILMAZ** (CLAUDE.md Bölüm 2'de belirtildiği gibi) — sadece basit tekil ayarlar (ör. ses açık/kapalı) için PlayerPrefs istisnai olarak kabul edilebilir, ilerleme verisi asla PlayerPrefs'te tutulmaz.
- **İleride sunucu senkronizasyonu ihtimali:** `SaveManager` bir interface (`ISaveProvider`) arkasına gizlenir (`LocalJsonSaveProvider` ilk implementasyon), böylece ileride bulut kayıt eklenmek istenirse mevcut sistemi bozmadan yeni bir provider yazılabilir.

```
ISaveProvider
├── Save(SaveData data): void
└── Load(): SaveData
```

`GameManager`, `SaveManager` üzerinden `ISaveProvider`'a erişir; hiçbir sistem dosya yolunu veya JSON detayını doğrudan bilmez.

---

## 12. Object Pooling Sistemi

CLAUDE.md'de kural olarak belirtilen object pooling'in somut karşılığı:

```
Core/Pooling/
  ObjectPool.cs         // generic: Get(), Release(GameObject obj), prefab + başlangıç kapasitesi alır
  ObjectPoolManager.cs  // tüm pool'ların merkezi kayıt noktası (dictionary: prefab → ObjectPool)
```

**Pool'lanacak objeler:**
- `Car` prefabları (renk başına ayrı pool, level yüklenirken kapasiteye göre önceden ısıtılır — "warm-up")
- Patlama/match VFX'leri
- Kutu patlama VFX'i (LockedBox)

**Kural:** Hiçbir sistem `Instantiate()`/`Destroy()`'u doğrudan çağırmaz (test/prototip kodu hariç). Tüm spawn/despawn işlemleri `ObjectPoolManager.Get(prefab)` / `ObjectPoolManager.Release(obj)` üzerinden yapılır. Level değişiminde (yeni level yüklenirken) pool'lar tamamen boşaltılmaz, sadece aktif objeler `Release` edilir — pool'un kendisi sahne boyunca yaşar.

---

## 13. Milestone Sırası (Geliştirme Önceliği)

1. Grid + PathfindingService (tek düz lane, engelsiz) — **en kritik, önce bu sağlamlaşmalı**
2. Çoklu şekilli board (L, U, Z, H şekiller test level'ları ile)
3. Object Pooling sistemi (Car prefabları için — Milestone 2'den sonra, gerçek prefab sayısı artmadan önce kurulmalı)
4. Holder + Match sistemi + Game Over/Level Complete
5. LevelData sistemi (en az 5 test level, ScriptableObject)
6. LockedBox + GarageSpawner (ILaneObstacle üzerinden)
7. Booster sistemi (Undo, Shuffle, Super Undo, Magnet)
8. Save/Load sistemi (level ilerlemesi, coin, booster envanteri)
9. UI/UX (ana menü, level haritası, popup'lar)
10. Monetizasyon + Analytics entegrasyonu
11. Polish (ses, VFX, haptic)
    - **TODO (M4B'den):** Holder entry point sabit; araç entry noktasından gerçek slot pozisyonuna kısa bir instant snap oluyor. Düzeltme: CarMover'ın tween bitmeden önce Holder'dan hedef slot Transform'unu event-based sorgulayabilmesi (slot pozisyonu dinamik olarak döndürülür). Milestone 11 Polish kapsamında ele alınacak.

**Kural:** Bir milestone tamamlanmadan sonrakine geçilmez. Her milestone sonunda test edilir, çalıştığı doğrulanır, ayrı bir Git commit ile kaydedilir.

---

## 14. Claude Code Kullanım Notları

- Her görev tek bir sistemi/milestone'u hedeflemeli, büyük ve belirsiz görev verilmemeli.
- Her görev öncesi bu dosya referans gösterilmeli ("ARCHITECTURE.md'deki Bölüm X'e göre..." şeklinde).
- Yeni bir sistem eklenirken mevcut event bus ve interface'ler kullanılmalı, yeni bağımlılık (tight coupling) yaratılmamalı.
- Değişiklik sonrası: "bu değişiklik ARCHITECTURE.md'deki plana uygun mu, sapma var mı" kontrolü yapılmalı.
