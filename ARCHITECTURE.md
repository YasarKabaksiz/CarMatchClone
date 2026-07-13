# ARCHITECTURE.md
## Car Match Clone — Teknik Mimari Dokümanı

Bu doküman, projenin teknik anayasasıdır. Claude Code (veya başka bir geliştirici) her görev öncesinde bu dosyayı referans almalı, buradaki desenlerin dışına çıkmamalıdır. Yeni bir sistem eklerken önce burada tanımlı prensiplere uyup uymadığını kontrol et.

---

## 1. Oyun Özeti

- **Tür:** Grid tabanlı "collector / queue matching" puzzle (Match-3 alt türü)
- **Perspektif:** 3D modeller, sabit üstten-yakın perspektif kamera
- **Core Loop:** Board üzerindeki lane'lerden (şerit/kuyruk) meyve/sebze seçilir → holder'a gider → aynı türden 3 meyve yan yana gelince patlar → board tamamen boşalınca level biter
- **Kaybetme koşulu:** Holder (7 slot) doluyken match oluşmadan yeni meyve eklenemez hale gelirse Game Over

---

## 2. Temel Mimari Prensipler

1. **Event-Driven Tasarım:** Sistemler birbirini doğrudan referans almaz, event bus üzerinden haberleşir. Yeni bir sistem eklemek mevcut sistemleri bozmamalı.
2. **Data-Driven Level Tasarımı:** Level içerikleri (ScriptableObject/JSON) kod değişikliği gerektirmeden üretilebilmeli.
3. **Tek Sorumluluk:** Her sınıf tek bir işten sorumlu (Board grid yönetir, Pathfinding erişilebilirlik hesaplar, Holder match kontrolü yapar — birbirine karışmaz).
4. **Gerçek Zamanlı Hesaplama, Önceden Tanımlı Rota Yok:** Meyvelerin "seçilebilir" durumu, board her değiştiğinde otomatik yeniden hesaplanır (bkz. Bölüm 5). Level tasarımcısı rota tanımlamaz, sadece board'un ilk durumunu tanımlar.

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
        Fruit.cs
        FruitMover.cs           (path boyunca hareket/tween)
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
      Fruits/
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
- `cells[]`: her hücre için — pozisyon, hücre tipi (Empty, Wall, CarSlot, GarageSpawner, LockedBox), meyve türü (varsa), `garageColors[]` (GarageSpawner: sıralı spawn türleri, dizi uzunluğu = stok sayısı)
- `difficultyTag`: enum (Normal, Hard, SuperHard) — sadece UI gösterimi için, oynanışı etkilemez

### CellEntry şeması (önemli alanlar)
```csharp
public class CellEntry
{
    public Vector2Int position;
    public CellType type;
    public FruitType color;            // CarSlot: meyve türü | LockedBox: gizli meyve türü
    public FacingDirection facingDirection; // Yalnızca GarageSpawner
    public FruitType[] garageColors;   // Yalnızca GarageSpawner: sıralı spawn türleri
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
├── occupant: Fruit | null    // hücrede duran meyve (varsa)
└── obstacle: ILaneObstacle | null  // LockedBox veya GarageSpawner (varsa)
```

### Board
- Tüm `GridCell[,]` matrisini tutar.
- `Exit` sanal bir hedef noktadır (holder'ın bağlandığı nokta/nokta grubu).
- Bir hücre boşaldığında (`OnCellVacated`) event fırlatır.

### PathfindingService
- **Algoritma:** A* (BFS de yeterli olur çünkü tüm hücreler eşit maliyetli, ama A* ileride ağırlıklı/öncelikli yol senaryolarına genişlemeye daha uygun — bu yüzden A* tercih edilir).
- **Ne zaman çalışır:** Her board state değişiminde (meyve holder'a ulaştığında, kutu kırıldığında, garaj spawn ettiğinde/kapandığında) — her frame değil.
- **Çıktı:** Her `Fruit` için `isReachable` (true/false) durumunu günceller. Sadece `isReachable = true` olan meyveler tıklanabilir/interactable olur.

```
PathfindingService
├── RecalculateReachability(Board board): void
│     → board'daki her Fruit için Exit'e ulaşan bir yol var mı diye A* çalıştırır
│     → sonucu Fruit.isReachable üzerine yazar
└── HasPathToExit(GridCell start, Board board): bool
```

### Kritik Kural
Bir meyvenin seçilebilir olması için **başlangıçtan Exit'e kadar TÜM yol boyunca hücreler walkable olmalı** (sadece bitişik hücre değil). Bu kullanıcı tarafından doğrulanmış bir kuraldır.

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
`GameEventChannel.Raise()` listener'ları ters kayıt sırasında çalıştırır. Board hem `OnFruitSelected`'a hem de GarageSpawner `OnCellVacated`'a subscribe ise, Board'un `HandleFruitSelected` bitmeden önce GarageSpawner tetiklenebilir ve `OnObstacleTriggered` fırlatılabilir — UndoBooster'ın `RecordSnapshot()`'u henüz çalışmamışsa bu closure kaybolur.

**Çözüm:** Board, `HandleFruitSelected`'ın en başına (herhangi bir hücre state değişmeden önce) ayrı bir `OnBeforeFruitRemoved` event'i fırlatır. UndoBooster `RecordSnapshot`'ını `OnFruitSelected` yerine **`OnBeforeFruitRemoved`**'a bağlar. Bu sayede snapshot, tüm side-effect'lerden önce alınır.

```
OnBeforeFruitRemoved fırlar
  → UndoBooster.RecordSnapshot() çalışır (snapshot alındı, pendingList temizlendi)
Board hücreyi boşaltır → OnCellVacated fırlar
  → GarageSpawner.Trigger() çalışır → OnObstacleTriggered fırlar
    → UndoBooster.OnObstacleTriggered() closure'ı listeye ekler ✓
```

### LockedBox
- Board'daki bir hücrenin yerinde durur; hücre `isWalkable = false`.
- **LevelData'da:** `CellEntry { type=LockedBox, color=Tomato }` — `color` alanı kutunun patlamasıyla ortaya çıkacak meyvenin türünü tanımlar.
- **Tetiklenme:** 4 komşudan (yukarı/aşağı/sağ/sol) herhangi biri boşalınca — yönsüz (omnidirectional).
- **Filtre:** `vacatedCell.Position == _gridPos + offset` — saf Vector2Int aritmetiği.
- **Tetiklenince:** `_triggered = true` → `board.RevealLockedBox(pos)` → Board pool'dan meyve çıkarır → `SetActive(false)` (görsel) → `OnObstacleTriggered.Raise(payload)` — payload'da `UndoLastReveal` closure.
- **Tek seferlik:** `_triggered = true` sonrası event'ler yoksayılır, `IsActive` false döner.
- **Undo (UndoLastReveal):** `_triggered = false` → `board.RemoveFruitAtAndBlock(_gridPos)` (meyve silinir, hücre `isWalkable=false` kalır) → `SetActive(true)`. `SetActive(true)` Unity'nin `OnEnable`'ını tetikler; `OnEnable` içinde `_onCellVacatedChannel.Subscribe(OnCellVacated)` çağrısı yapılır — bu sayede kutu yeniden tetiklenebilir hale gelir. İlk aktivasyonda `OnEnable` channel null olduğu için güvenlidir; `Initialize` kendi subscribe'ını ayrıca yapar.
- **Kritik:** `board.RemoveFruitAt` (GarageSpawner için) yerine `board.RemoveFruitAtAndBlock` kullanılır. `RemoveFruitAt`, `isWalkable=true` set eder; LockedBox hücresi kutu aktifken hiçbir zaman walkable olmamalıdır.

### GarageSpawner
- Bir lane'in herhangi bir noktasında bulunabilir; kendi hücresi (`_gridPos`) **daima `isWalkable = false`**.
- **LevelData'da:** `CellEntry { type=GarageSpawner, facingDirection=Down, garageColors=[Coconut, Tomato, Coconut] }`. `color` alanı kullanılmaz; stok sayısı = `garageColors.Length`.
- **Tetiklenme:** SADECE `facingCell` (`_gridPos + facingDirection.ToVector()`) boşalınca.
- **Spawn pozisyonu:** Meyve `_gridPos`'a DEĞİL, `_facingCell`'e spawn olur.
- **Çoklu spawn:** Her `_facingCell` boşalmasında tetiklenir — tüm `garageColors` tükenene kadar. Sıradaki meyve türü `_garageColors[_currentSpawnIndex]` ile belirlenir; her tetiklemede `_currentSpawnIndex++`.
- **Tetiklenince:** `spawnType = _garageColors[_currentSpawnIndex]` → `_currentSpawnIndex++` → `board.SpawnFromGarage(_facingCell, spawnType)` → `OnObstacleTriggered.Raise(payload)` — payload'da `UndoLastSpawn` closure.
- **Undo (UndoLastSpawn):** `_currentSpawnIndex--` → `board.RemoveFruitAt(_facingCell)` (meyve silinir, hücre `isWalkable=true` olur — normal boş hücre).
- `IsActive = _garageColors != null && _currentSpawnIndex < _garageColors.Length`. Tüm meyveler spawn edilince `IsActive = false`; `_facingCell` normal boş hücre gibi davranır.

### Board — Undo için Ek API
```csharp
// Meyve pool'a iade edilir, hücre isWalkable=true olur (GarageSpawner undo)
public bool RemoveFruitAt(Vector2Int pos)

// Meyve pool'a iade edilir, hücre isWalkable=false KALIR (LockedBox undo)
public bool RemoveFruitAtAndBlock(Vector2Int pos)

// UndoBooster'ın geri koyduğu meyve; isWalkable=false set edilir, OnBoardStateChanged fırlatır
public bool PlaceFruitBack(Vector2Int pos, FruitType fruitType)
```

### Neden Ortak Interface
`ILaneObstacle` sayesinde ileride yeni bir engel tipi mevcut sistemlere dokunmadan yeni bir sınıf olarak eklenebilir.

---

## 7. Holder & Match Sistemi

```
Holder
├── slots: Fruit[7]
├── AddFruit(Fruit fruit): void  // slotlardan birine ekler
├── CheckMatch(): void           // aynı türden 3 yan yana var mı kontrol eder
└── IsFull: bool                 // game over kontrolü için
```

- Meyve holder'a ulaştığında `Holder.AddFruit()` çağrılır.
- `MatchChecker` aynı türden 3 tanenin yan yana olup olmadığını kontrol eder, varsa patlatır, slotları kaydırır.
- Holder doluyken match oluşmazsa → `GameEvents.OnGameOver` fırlatılır.
- Board'daki tüm hücreler boş kalınca → `GameEvents.OnLevelComplete` fırlatılır.

### Cascade Match Davranışı (Mimari Not)

**Holder-içi cascade:** `ResolveMatches()` `while (FindMatch() >= 0)` döngüsüyle çalışır. Bir match patladıktan sonra kalan meyveler kaydığında yeni bir match oluşursa bu da otomatik patlar — holder-içi cascade **desteklenir**.

**Board→Holder cascade imkansızdır (tasarım gereği):** Meyveler board'dan holder'a **birer birer** gelir. Bir match patlaması board state'ini değiştirmez; board yeni meyve göndermez. Dolayısıyla "match → board'dan yeni meyve gelir → tekrar match" gibi çok adımlı bir dış döngü mimaride mevcut değildir. Bu bir kısıtlama değil, bilinçli bir sınırdır — oyun döngüsünün deterministik kalmasını sağlar.

---

## 8. Event Bus (GameEvents)

Sistemler birbirini doğrudan çağırmaz, aşağıdaki event'ler üzerinden haberleşir:

```
OnBeforeFruitRemoved(Fruit fruit)  // Board, HandleFruitSelected başında (hücre değişmeden önce) fırlatır
                                   // → UndoBooster snapshot'ı bu event'te alır (race condition önlemi)
OnFruitSelected(Fruit fruit)       // oyuncu/booster bir meyve seçti
OnFruitReachedHolder(Fruit fruit)  // FruitMover tek parça tween'ini tamamladı (slot pozisyonunda);
                                   // Holder match resolve + OnHolderProcessed başlatır
OnCellVacated(GridCell cell)       // meyve board'dan ayrıldı → PathfindingService + ILaneObstacle tetiklenir
OnBoardStateChanged()              // yeni meyve eklendi (reveal/spawn) → PathfindingService tetiklenir
OnObstacleTriggered(ObstacleTriggerPayload)
                                   // obstacle tetiklendi; payload.UndoAction closure ile geri alınabilir
OnMatchOccurred(FruitType fruitType)
OnHolderFull()
OnGameOver()
OnLevelComplete()
OnCoinRewardEarned(int amount)    // Level Complete'te kazanılan coin miktarı (delta); LevelCompletePopup dinler
OnSlotAssigned(SlotAssignedPayload) // Holder, OnFruitSelected anında slotu rezerve edip fırlatır;
                                   // FruitMover payload.Position'ı son waypoint olarak kullanır (tek parça tween)
OnBoosterUsed(BoosterType type)
```

UI, ses sistemi, analytics, spawner mekanikleri bu event'leri dinler; birbirlerinden habersizdir. Yeni bir sistem eklerken mevcut event'lere abone ol, mevcut sınıfları değiştirme.

### Dinleyicisi Olmayan Event'ler (Bilerek Hazırlanmış Bağlantı Noktaları)

Aşağıdaki event'ler raise edilmekte ama şu an hiçbir sistem subscribe olmamaktadır. **Silinmemeli** — ilerideki milestone'lar için hazır bırakılmıştır:

| Event | Raise Eden | Hedef Milestone |
|---|---|---|
| `OnBoosterUsed(BoosterType)` | `GameManager.UseBooster()` | M11 — match VFX, ses efekti |
| `OnMatchOccurred(FruitType)` | `Holder.ResolveMatches()` | M11 — match VFX, ses efekti, combo sayacı |
| `OnHolderFull()` | `Holder` (Game Over öncesi) | M11 — "holder dolu" uyarı animasyonu, ses |

### Subscriber Sırası (Kritik)

`GameEventChannel.Raise()` listener listesini **ters kayıt sırasında** çalıştırır (son kaydeden ilk çalışır). Aynı event'e birden fazla sistem subscribe olduğunda çalışma sırası buradan belirlenir. Sıranın önemli olduğu durumlarda ayrı bir "pre-event" channel tanımlamak tercih edilir (bkz. `OnBeforeFruitRemoved`).

### Somut İmplementasyon: ScriptableObject Event Channel

```
Core/Events/
  GameEventChannel.cs        // generic base: ScriptableObject, List<Listener>, Raise(T payload)
  FruitEventChannel.cs       // GameEventChannel<Fruit>
  CellEventChannel.cs        // GameEventChannel<GridCell>
  ColorEventChannel.cs       // GameEventChannel<FruitType>
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

1. `OnBeforeFruitRemoved` event'ine subscribe olur; her meyve seçiminden önce `RecordSnapshot()` çalışır (meyve pozisyonu + türü kaydedilir, `_pendingObstacleUndos` listesi sıfırlanır).
2. `OnObstacleTriggered` event'ine subscribe olur; tetiklenen obstacle'ların `UndoAction` closure'ları `_pendingObstacleUndos` listesine eklenir.
3. `Execute()` çağrısında: obstacle undo'lar ters sırayla çalışır → holder'dan son meyve çıkarılır → meyve board'a geri konulur (`PlaceFruitBack`).

`OnBeforeFruitRemoved` kullanılmasının nedeni: `OnFruitSelected`'a subscribe olunursa, Board'un kendi `HandleFruitSelected` handler'ı (aynı event'te) GarageSpawner'ı tetikleyip `OnObstacleTriggered` fırlattıktan sonra RecordSnapshot çalışabilir; bu durumda `Clear()` closure'ı siler. `OnBeforeFruitRemoved`, herhangi bir hücre değişmeden önce ateşlenir — snapshot sırası garantilenir.

```
Execute() akışı:
  1. _pendingObstacleUndos (ters sıra) → her closure.Invoke()
  2. _holder.TryRemoveLastAdded()
  3. board.PlaceFruitBack(_snapshotPos, _snapshotFruitType)
```

### Shuffle Booster

Board'daki tüm dolu hücreleri toplar, Fisher-Yates ile türleri karıştırır, meyveleri pool'a iade edip yeni türlerde yeniden spawn eder. `OnBoardStateChanged` fırlatır → PathfindingService recalculate. Her zaman `true` döner.

### Super Undo Booster

**Aktif seçim mekanizması:** `Execute()` çağrısında `_holder.SetNextFruitInterceptor(PlaceInReserve)` set edilir. Bir sonraki meyve seçildiğinde (`OnFruitSelected`) Holder onu normal akışa sokmak yerine `PlaceInReserve`'e yönlendirir — meyve henüz board pozisyonundayken intercept edilir, animasyon başlamaz.

`PlaceInReserve(Fruit fruit)`:
- `fruit.IsReachable = false` — `GameInputHandler` tıklamayı reddeder.
- `fruit.GetComponent<Collider>().enabled = false` — fizik raycast'e yakalanmaz (çift koruma).
- Meyve `_reserveSlotTransform.position`'a taşınır.

`ReleaseReserve()`:
- `collider.enabled = true` — pool'a geri döndüğünde collider temiz olsun.
- `_holder.ForceAddFruit(fruit)` — meyve normal holder akışına (InsertIntoSlot + ResolveMatches) girer.

`Execute()` zaten aktifken (`_isActive = true`) veya rezervde meyve varken `false` döner, stok harcanmaz.

### Magnet Booster

**Kuyruk tabanlı sıralı gönderim:** Tüm hedef meyveler aynı anda değil, **birer birer** gönderilir; bir önceki meyve holder'a ulaşınca sıradaki gönderilir.

```
Durum değişkenleri:
  _magnetQueue:    Queue<Fruit>       — gönderilmeyi bekleyen meyveler
  _inFlightFruits: HashSet<Fruit>    — kuyruğa alınan meyvelerin takip seti
```

Akış:
1. `Execute()`: hedef türü belirle (Holder'daki tür sayısına göre, yoksa board'daki erişilebilir türler), tüm hedefleri `_magnetQueue` ve `_inFlightFruits`'e ekle, `SendNext()` çağır.
2. `SendNext()`: kuyruktan bir meyve çıkar, `OnFruitSelected.Raise()` ile gönder.
3. `OnMagnetFruitReachedHolder(Fruit fruit)`: `_inFlightFruits.Contains(fruit)` ile bu meyve bizim mi kontrol et; evet ise `_inFlightFruits.Remove(fruit)` → `SendNext()`.

`OnFruitReachedHolder` tüm meyveler için fırladığından `_inFlightFruits` filtresi zorunludur — başka bir meyve holder'a ulaşırsa yanlış `SendNext()` tetiklenmez.

Sıra devam ederken `Execute()` yeniden çağrılırsa `false` döner, stok harcanmaz.

**Tür seçim önceliği:** Holder'daki en çok tekrar eden tür; holder boşsa board'daki en çok erişilebilir tür. Seçilen türde erişilebilir meyve yoksa sonraki adaya geçilir; hiçbir aday yoksa `false` döner.

---

## 10. Kamera & Hareket

- Kamera sabit, level'ın board boyutuna göre otomatik "frame" (zoom/pozisyon) ayarlanır.
- `FruitMover`, PathfindingService'in bulduğu path'i (hücre listesi) alır, meyve bu hücreler boyunca tween ile hareket eder (DOTween önerilir).

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
- `Fruit` prefabları (tür başına ayrı pool, level yüklenirken kapasiteye göre önceden ısıtılır — "warm-up")
- Patlama/match VFX'leri
- Kutu patlama VFX'i (LockedBox)

**Kural:** Hiçbir sistem `Instantiate()`/`Destroy()`'u doğrudan çağırmaz (test/prototip kodu hariç). Tüm spawn/despawn işlemleri `ObjectPoolManager.Get(prefab)` / `ObjectPoolManager.Release(obj)` üzerinden yapılır. Level değişiminde (yeni level yüklenirken) pool'lar tamamen boşaltılmaz, sadece aktif objeler `Release` edilir — pool'un kendisi sahne boyunca yaşar.

---

## 13. Milestone Sırası (Geliştirme Önceliği)

1. Grid + PathfindingService (tek düz lane, engelsiz) — **en kritik, önce bu sağlamlaşmalı**
2. Çoklu şekilli board (L, U, Z, H şekiller test level'ları ile)
3. Object Pooling sistemi (Fruit prefabları için — Milestone 2'den sonra, gerçek prefab sayısı artmadan önce kurulmalı)
4. Holder + Match sistemi + Game Over/Level Complete
5. LevelData sistemi (en az 5 test level, ScriptableObject)
6. LockedBox + GarageSpawner (ILaneObstacle üzerinden)
7. Booster sistemi (Undo, Shuffle, Super Undo, Magnet)
8. Save/Load sistemi (level ilerlemesi, coin, booster envanteri)
9. UI/UX (ana menü, level haritası, popup'lar)
10. Monetizasyon + Analytics entegrasyonu
11. Polish (ses, VFX, haptic)

**Kural:** Bir milestone tamamlanmadan sonrakine geçilmez. Her milestone sonunda test edilir, çalıştığı doğrulanır, ayrı bir Git commit ile kaydedilir.

---

## 14. Claude Code Kullanım Notları

- Her görev tek bir sistemi/milestone'u hedeflemeli, büyük ve belirsiz görev verilmemeli.
- Her görev öncesi bu dosya referans gösterilmeli ("ARCHITECTURE.md'deki Bölüm X'e göre..." şeklinde).
- Yeni bir sistem eklenirken mevcut event bus ve interface'ler kullanılmalı, yeni bağımlılık (tight coupling) yaratılmamalı.
- Değişiklik sonrası: "bu değişiklik ARCHITECTURE.md'deki plana uygun mu, sapma var mı" kontrolü yapılmalı.

---

## 15. UI Tasarım Sistemi

### Asset Paketi

**Kenney UI Pack** (CC0 lisans) kullanılıyor. Dosya konumu: `Assets/_Project/Art/UIKit/`

### Renk–Anlam Eşleştirmesi

| Renk | Kullanım Alanı |
|---|---|
| **Mavi** | Ana aksiyon: Continue, Play, booster butonları |
| **Kırmızı** | Acil/olumsuz aksiyon: Retry |
| **Gri** | İkincil/nötr aksiyon: Ana Menüye Dön, kilitli level overlay |
| **Sarı** | Ödül/vurgu: coin göstergesi, Level Complete başlığı |
| **Yeşil** | Şimdilik kullanılmıyor — ileride "satın alındı" gibi onay durumları için ayrılmış |

**Kural:** Bu eşleştirme tüm UI ekranlarında (HUD, popup'lar, ana menü) tutarlı biçimde uygulanmalıdır. Renk–anlam ilişkisi bozulmamalı; yeni bir ekrana yeni renk anlamı eklenmeden önce bu tabloya eklenmeli.

### Tipografi

Kenney UI Pack içindeki TTF dosyası, TextMeshPro Font Asset'e çevrilerek kullanılıyor. Tüm UI metinleri bu TMP Font Asset'i referans alır; Unity'nin varsayılan fontu kullanılmaz.

---

## 16. Tema Notu

Proje başlangıçta **araç temalıydı** (Car Match Clone — renkli arabalar). Geliştirme sürecinde **meyve/sebze temasına** (Kenney Food Kit modelleri: domates, limon, üzüm, karpuz, portakal, hindistan cevizi) geçildi.

Mimari bu geçişi sıfır mantık değişikliğiyle kaldırdı: yeniden adlandırılan sadece sınıf isimleri (`Car→Fruit`, `CarColor→FruitType`), prefablar ve event channel asset'leriydi. Board, Pathfinding, Holder, Booster sistemlerinin hiçbir satır iş mantığı değişmedi.

**Proje/namespace adı `CarMatchClone` değişmedi** — bu artık oyunun konseptini değil, projenin teknik kimliğini temsil eder.
