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
    Editor/                        ← sadece Unity Editor'de derlenir, build'e girmez
      LevelEditorWindow.cs
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
- `cells[]`: her hücre için — pozisyon, hücre tipi (Empty, Wall, CarSlot, GarageSpawner, LockedBox), araç rengi (varsa), `garageColors[]` (GarageSpawner: sıralı spawn renkleri, dizi uzunluğu = stok sayısı)
- `difficultyTag`: enum (Normal, Hard, SuperHard) — sadece UI gösterimi için, oynanışı etkilemez

### CellEntry şeması (önemli alanlar)
```csharp
public class CellEntry
{
    public Vector2Int position;
    public CellType type;
    public CarColor color;            // CarSlot: araç rengi | LockedBox: gizli araç rengi
    public FacingDirection facingDirection; // Yalnızca GarageSpawner
    public CarColor[] garageColors;   // Yalnızca GarageSpawner: sıralı spawn renkleri
                                      // garageColors.Length == stok sayısı (ayrı field yok)
}
```

**Not:** `garageStockCount` field'ı kaldırıldı. Stok sayısı artık `garageColors.Length` ile belirlenir — data senkronizasyon sorunu olamaz.

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

Her ikisi de ortak bir prensipten türer: **"bir hücre boşaldığında, kendi kurallarına göre tepki veren davranış."**

### ILaneObstacle Interface

```csharp
public interface ILaneObstacle
{
    void Initialize(Vector2Int gridPos, Board board, CellEventChannel onCellVacatedChannel);
    bool IsActive { get; }
}
```

**Tasarım notları:**
- `OnAdjacentCellVacated(GridCell)` interface'ten kaldırıldı. Her MonoBehaviour implementasyonu kendi `OnEnable`/`OnDisable`'ında `CellEventChannel`'a subscribe/unsubscribe eder (self-subscription pattern). Board obstacle'ları iterate etmez.
- Filtre mantığı implementasyonun içinde kalır: LockedBox 4 komşuyu kontrol eder, GarageSpawner sadece kendi `facingDirection`'ını.
- `IsActive` Board'un ileride obstacle durumunu sorgulamasına izin verir.

### Obstacle Undo Mekanizması: ObstacleTriggerPayload + Closure Pattern

Obstacle'ların Undo booster ile geri alınabilmesi için **closure tabanlı** bir yan etki kayıt sistemi kullanılır.

```csharp
// Data/ObstacleTriggerPayload.cs
public struct ObstacleTriggerPayload
{
    public Vector2Int Position;   // debug/log için
    public System.Action UndoAction;  // bu tetiklenmeyi geri alan closure
}
```

Her obstacle tetiklendiğinde `OnObstacleTriggered` (ObstacleEventChannel) event'i fırlatır; payload içindeki `UndoAction` closure'ı "bu tetiklenmeyi tam olarak geri al" mantığını kapsüller. UndoBooster bu closure'ları `_pendingObstacleUndos` listesinde biriktirir; undo çalıştırılınca ters sırayla invoke eder.

Board, `SpawnGarageSpawner` ve `SpawnLockedBox`'ta `_onObstacleTriggeredChannel` referansını ilgili obstacle nesnesine enjekte eder.

**Subscriber order race condition ve çözümü:**  
`GameEventChannel.Raise()` listener'ları ters kayıt sırasında çalıştırır. Board hem `OnCarSelected`'a hem de GarageSpawner `OnCellVacated`'a subscribe ise, Board'un `HandleCarSelected` bitmeden önce GarageSpawner tetiklenebilir ve `OnObstacleTriggered` fırlatılabilir — UndoBooster'ın `RecordSnapshot()`'u henüz çalışmamışsa bu closure kaybolur.

**Çözüm:** Board, `HandleCarSelected`'ın en başına (herhangi bir hücre state değişmeden önce) ayrı bir `OnBeforeCarRemoved` event'i fırlatır. UndoBooster `RecordSnapshot`'ını `OnCarSelected` yerine **`OnBeforeCarRemoved`**'a bağlar. Bu sayede snapshot, tüm side-effect'lerden önce alınır.

```
OnBeforeCarRemoved fırlar
  → UndoBooster.RecordSnapshot() çalışır (snapshot alındı, pendingList temizlendi)
Board hücreyi boşaltır → OnCellVacated fırlar
  → GarageSpawner.Trigger() çalışır → OnObstacleTriggered fırlar
    → UndoBooster.OnObstacleTriggered() closure'ı listeye ekler ✓
```

### LockedBox
- Board'daki bir hücrenin yerinde durur; hücre `isWalkable = false`.
- **LevelData'da:** `CellEntry { type=LockedBox, color=Red }` — `color` alanı kutunun patlamasıyla ortaya çıkacak arabanın rengini tanımlar.
- **Tetiklenme:** 4 komşudan (yukarı/aşağı/sağ/sol) herhangi biri boşalınca — yönsüz (omnidirectional).
- **Filtre:** `vacatedCell.Position == _gridPos + offset` — saf Vector2Int aritmetiği.
- **Tetiklenince:** `_triggered = true` → `board.RevealLockedBox(pos)` → Board pool'dan araç çıkarır → `SetActive(false)` (görsel) → `OnObstacleTriggered.Raise(payload)` — payload'da `UndoLastReveal` closure.
- **Tek seferlik:** `_triggered = true` sonrası event'ler yoksayılır, `IsActive` false döner.
- **Undo (UndoLastReveal):** `_triggered = false` → `board.RemoveCarAtAndBlock(_gridPos)` (araç silinir, hücre `isWalkable=false` kalır) → `SetActive(true)`. `SetActive(true)` Unity'nin `OnEnable`'ını tetikler; `OnEnable` içinde `_onCellVacatedChannel.Subscribe(OnCellVacated)` çağrısı yapılır — bu sayede kutu yeniden tetiklenebilir hale gelir. İlk aktivasyonda `OnEnable` channel null olduğu için güvenlidir; `Initialize` kendi subscribe'ını ayrıca yapar.
- **Kritik:** `board.RemoveCarAt` (GarageSpawner için) yerine `board.RemoveCarAtAndBlock` kullanılır. `RemoveCarAt`, `isWalkable=true` set eder; LockedBox hücresi kutu aktifken hiçbir zaman walkable olmamalıdır.

### GarageSpawner
- Bir lane'in herhangi bir noktasında bulunabilir; kendi hücresi (`_gridPos`) **daima `isWalkable = false`**.
- **LevelData'da:** `CellEntry { type=GarageSpawner, facingDirection=Down, garageColors=[Blue, Red, Blue] }`. `color` alanı kullanılmaz; stok sayısı = `garageColors.Length`.
- **Tetiklenme:** SADECE `facingCell` (`_gridPos + facingDirection.ToVector()`) boşalınca.
- **Spawn pozisyonu:** Araç `_gridPos`'a DEĞİL, `_facingCell`'e spawn olur.
- **Çoklu spawn:** Her `_facingCell` boşalmasında tetiklenir — tüm `garageColors` tükenene kadar. Sıradaki araç rengi `_garageColors[_currentSpawnIndex]` ile belirlenir; her tetiklemede `_currentSpawnIndex++`.
- **Tetiklenince:** `spawnColor = _garageColors[_currentSpawnIndex]` → `_currentSpawnIndex++` → `board.SpawnFromGarage(_facingCell, spawnColor)` → `OnObstacleTriggered.Raise(payload)` — payload'da `UndoLastSpawn` closure.
- **Undo (UndoLastSpawn):** `_currentSpawnIndex--` → `board.RemoveCarAt(_facingCell)` (araç silinir, hücre `isWalkable=true` olur — normal boş hücre).
- `IsActive = _garageColors != null && _currentSpawnIndex < _garageColors.Length`. Tüm araçlar spawn edilince `IsActive = false`; `_facingCell` normal boş hücre gibi davranır.

### Board — Undo için Ek API
```csharp
// Araç pool'a iade edilir, hücre isWalkable=true olur (GarageSpawner undo)
public bool RemoveCarAt(Vector2Int pos)

// Araç pool'a iade edilir, hücre isWalkable=false KALIR (LockedBox undo)
public bool RemoveCarAtAndBlock(Vector2Int pos)

// UndoBooster'ın geri koyduğu araç; isWalkable=false set edilir, OnBoardStateChanged fırlatır
public bool PlaceCarBack(Vector2Int pos, CarColor color)
```

### Neden Ortak Interface
`ILaneObstacle` sayesinde ileride yeni bir engel tipi mevcut sistemlere dokunmadan yeni bir sınıf olarak eklenebilir.

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

### Cascade Match Davranışı (Mimari Not)

**Holder-içi cascade:** `ResolveMatches()` `while (FindMatch() >= 0)` döngüsüyle çalışır. Bir match patladıktan sonra kalan araçlar kaydığında yeni bir match oluşursa bu da otomatik patlar — holder-içi cascade **desteklenir**.

**Board→Holder cascade imkansızdır (tasarım gereği):** Araçlar board'dan holder'a **birer birer** gelir. Bir match patlaması board state'ini değiştirmez; board yeni araç göndermez. Dolayısıyla "match → board'dan yeni araç gelir → tekrar match" gibi çok adımlı bir dış döngü mimaride mevcut değildir. Bu bir kısıtlama değil, bilinçli bir sınırdır — oyun döngüsünün deterministik kalmasını sağlar.

---

## 8. Event Bus (GameEvents)

Sistemler birbirini doğrudan çağırmaz, aşağıdaki event'ler üzerinden haberleşir:

```
OnBeforeCarRemoved(Car car)        // Board, HandleCarSelected başında (hücre değişmeden önce) fırlatır
                                   // → UndoBooster snapshot'ı bu event'te alır (race condition önlemi)
OnCarSelected(Car car)             // oyuncu/booster bir araç seçti
OnCarReachedHolder(Car car)        // araç holder entry'e ulaştı
OnCellVacated(GridCell cell)       // araç board'dan ayrıldı → PathfindingService + ILaneObstacle tetiklenir
OnBoardStateChanged()              // yeni araç eklendi (reveal/spawn) → PathfindingService tetiklenir
OnObstacleTriggered(ObstacleTriggerPayload)
                                   // obstacle tetiklendi; payload.UndoAction closure ile geri alınabilir
OnMatchOccurred(CarColor color)
OnHolderFull()
OnGameOver()
OnLevelComplete()
OnBoosterUsed(BoosterType type)
```

UI, ses sistemi, analytics, spawner mekanikleri bu event'leri dinler; birbirlerinden habersizdir. Yeni bir sistem eklerken mevcut event'lere abone ol, mevcut sınıfları değiştirme.

### Dinleyicisi Olmayan Event'ler (Bilerek Hazırlanmış Bağlantı Noktaları)

Aşağıdaki event'ler raise edilmekte ama şu an hiçbir sistem subscribe olmamaktadır. **Silinmemeli** — ilerideki milestone'lar için hazır bırakılmıştır:

| Event | Raise Eden | Hedef Milestone |
|---|---|---|
| `OnBoosterUsed(BoosterType)` | `GameManager.UseBooster()` | M9 — HUD'da kalan booster sayısını güncelle |
| `OnMatchOccurred(CarColor)` | `Holder.ResolveMatches()` | M9/M11 — match VFX, ses efekti, combo sayacı |
| `OnHolderFull()` | `Holder` (Game Over öncesi) | M9/M11 — "holder dolu" uyarı animasyonu, ses |

### Subscriber Sırası (Kritik)

`GameEventChannel.Raise()` listener listesini **ters kayıt sırasında** çalıştırır (son kaydeden ilk çalışır). Aynı event'e birden fazla sistem subscribe olduğunda çalışma sırası buradan belirlenir. Sıranın önemli olduğu durumlarda ayrı bir "pre-event" channel tanımlamak tercih edilir (bkz. `OnBeforeCarRemoved`).

### Somut İmplementasyon: ScriptableObject Event Channel

```
Core/Events/
  GameEventChannel.cs        // generic base: ScriptableObject, List<Listener>, Raise(T payload)
  CarEventChannel.cs         // GameEventChannel<Car>
  CellEventChannel.cs        // GameEventChannel<GridCell>
  ColorEventChannel.cs       // GameEventChannel<CarColor>
  VoidEventChannel.cs        // parametresiz event'ler (OnGameOver, OnLevelComplete, OnBoardStateChanged)
  BoosterEventChannel.cs     // GameEventChannel<BoosterType>
  ObstacleEventChannel.cs    // GameEventChannel<ObstacleTriggerPayload>
```

Her event channel bir `.asset` dosyası olarak `Assets/_Project/Data/EventChannels/` altında saklanır. Yayınlayan ve dinleyen taraflar birbirini hiç tanımaz, sadece aynı `.asset` dosyasına referans verir.

**Kural:** C#'ın native `event`/`Action` yapısı sadece bir sınıfın kendi iç mantığında kullanılabilir. Sistemler arası haberleşme HER ZAMAN event channel üzerinden olur.

---

## 9. Booster Sistemi

### GameState

`GameState`, tek bir level oynanışının anlık durumunu tutan basit bir veri sınıfı (POCO, MonoBehaviour değil):

```
GameState
├── boosterCounts: Dictionary<BoosterType, int>   // oyuncunun elindeki booster sayısı
├── isGameOver: bool
└── isLevelComplete: bool
```

`GameManager` bu sınıfın tek sahibi. Booster'lar `GameState`'i sadece `Execute(board, state)` parametresi üzerinden alır, kendi referanslarını tutmaz.

### IBooster

```csharp
public interface IBooster
{
    bool Execute(Board board, GameState state);
}
```

`Execute` **`bool` döner**: `true` → booster gerçekten etki etti, `GameManager` stoku azaltır ve `OnBoosterUsed` fırlatır. `false` → hiçbir şey yapmadı (geçerli hedef yok, sıra dolu vb.), stok **harcanmaz**. GameManager bu dönen değere bakarak stok azaltma/event fırlatma kararını verir.

### Undo Booster

**Memento + closure hibrit pattern:**

1. `OnBeforeCarRemoved` event'ine subscribe olur; her araç seçiminden önce `RecordSnapshot()` çalışır (araç pozisyonu + rengi kaydedilir, `_pendingObstacleUndos` listesi sıfırlanır).
2. `OnObstacleTriggered` event'ine subscribe olur; tetiklenen obstacle'ların `UndoAction` closure'ları `_pendingObstacleUndos` listesine eklenir.
3. `Execute()` çağrısında: obstacle undo'lar ters sırayla çalışır → holder'dan son araç çıkarılır → araç board'a geri konulur (`PlaceCarBack`).

`OnBeforeCarRemoved` kullanılmasının nedeni: `OnCarSelected`'a subscribe olunursa, Board'un kendi `HandleCarSelected` handler'ı (aynı event'te) GarageSpawner'ı tetikleyip `OnObstacleTriggered` fırlattıktan sonra RecordSnapshot çalışabilir; bu durumda `Clear()` closure'ı siler. `OnBeforeCarRemoved`, herhangi bir hücre değişmeden önce ateşlenir — snapshot sırası garantilenir.

```
Execute() akışı:
  1. _pendingObstacleUndos (ters sıra) → her closure.Invoke()
  2. _holder.TryRemoveLastAdded()
  3. board.PlaceCarBack(_snapshotPos, _snapshotColor)
```

### Shuffle Booster

Board'daki tüm dolu hücreleri toplar, Fisher-Yates ile renkleri karıştırır, araçları pool'a iade edip yeni renklerde yeniden spawn eder. `OnBoardStateChanged` fırlatır → PathfindingService recalculate. Her zaman `true` döner.

### Super Undo Booster

**Aktif seçim mekanizması:** `Execute()` çağrısında `_holder.SetNextCarInterceptor(PlaceInReserve)` set edilir. Bir sonraki araç `OnCarReachedHolder`'a ulaştığında Holder onu normal akışa sokmak yerine `PlaceInReserve`'e yönlendirir.

`PlaceInReserve(Car car)`:
- `car.IsReachable = false` — `GameInputHandler` tıklamayı reddeder.
- `car.GetComponent<Collider>().enabled = false` — fizik raycast'e yakalanmaz (çift koruma).
- Araç `_reserveSlotTransform.position`'a taşınır.

`ReleaseReserve()`:
- `collider.enabled = true` — pool'a geri döndüğünde collider temiz olsun.
- `_holder.ForceAddCar(car)` — araç normal holder akışına (InsertIntoSlot + ResolveMatches) girer.

`Execute()` zaten aktifken (`_isActive = true`) veya rezervde araç varken `false` döner, stok harcanmaz.

### Magnet Booster

**Kuyruk tabanlı sıralı gönderim:** Tüm hedef araçlar aynı anda değil, **birer birer** gönderilir; bir önceki araç holder'a ulaşınca sıradaki gönderilir.

```
Durum değişkenleri:
  _magnetQueue: Queue<Car>       — gönderilmeyi bekleyen araçlar
  _inFlightCars: HashSet<Car>    — kuyruğa alınan araçların takip seti
```

Akış:
1. `Execute()`: hedef rengi belirle (Holder'daki renk sayısına göre, yoksa board'daki erişilebilir renkler), tüm hedefleri `_magnetQueue` ve `_inFlightCars`'a ekle, `SendNext()` çağır.
2. `SendNext()`: kuyruktan bir araç çıkar, `OnCarSelected.Raise()` ile gönder.
3. `OnMagnetCarReachedHolder(Car car)`: `_inFlightCars.Contains(car)` ile bu araç bizim mi kontrol et; evet ise `_inFlightCars.Remove(car)` → `SendNext()`.

`OnCarReachedHolder` tüm araçlar için fırladığından `_inFlightCars` filtresi zorunludur — başka bir araç holder'a ulaşırsa yanlış `SendNext()` tetiklenmez.

Sıra devam ederken `Execute()` yeniden çağrılırsa `false` döner, stok harcanmaz.

**Renk seçim önceliği:** Holder'daki en çok tekrar eden renk; holder boşsa board'daki en çok erişilebilir renk. Seçilen renkte erişilebilir araç yoksa sonraki adaya geçilir; hiçbir aday yoksa `false` döner.

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
