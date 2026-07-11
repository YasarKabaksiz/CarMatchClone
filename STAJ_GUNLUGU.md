# Staj Günlüğü

Bu dosya, staj defteri hazırlarken referans alınmak üzere günlük çalışma özetlerini tutar. Her gün sonunda yeni bir bölüm eklenir (en yeni gün en üstte).

**Not:** Staj boyunca birden fazla proje/görev üzerinde çalışılabilir. Her gün hangi proje üzerinde çalışıldığı, gün başlığının altında ayrıca belirtilir (şu an: **Car Match Clone** projesi).

---

## [TARİH GİRİLECEK] — Gün 5

### Yapılan İşler

Bir önceki günün son commit'i olan `feat(save): M8 - JSON tabanlı save/load sistemi...`'dan itibaren devam edildi.

**1. Coin Sistemi**
- Level tamamlandığında sabit 20 coin ödülü verilmesi sağlandı
- `OnCoinRewardEarned` event channel'ı eklendi: Level Complete popup'ında yalnızca o turda kazanılan coin deltası (`+20`) gösteriliyor, HUD'da ise `SaveManager`'dan gelen toplam bakiye gösteriliyor
- `GameState.Coins` alanı zaten M8'de placeholder olarak eklenmişti; bu milestone'da dolduruldu ve save/load döngüsüne dahil edildi (uygulama kapatılsa bile coin bakiyesi korunuyor)

**2. Milestone 9 — UI/UX Tamamlanması**

*Level İçi HUD:*
- Booster butonları (Undo, Shuffle, SuperUndo, Magnet) ve coin göstergesi HUD'a eklendi
- HUD güncellemeleri tamamen event tabanlı: `OnBoosterRequested`, `OnBoosterCountChanged`, `OnCoinsChanged` channel'larına subscribe ediliyor; HUD hiçbir sistemi doğrudan tanımıyor

*Level Complete / Game Over Popup'ları:*
- Level Complete popup'ı: kazanılan coin deltasını gösterir, "Continue" butonu `OnLevelContinueRequested` event'ini fırlatır
- Game Over popup'ı: "Retry" butonu `OnRetryRequested` event'ini fırlatır
- Event tabanlı akış bölmesi sayesinde popup'lar oyun mantığını doğrudan çağırmıyor — `GameManager` uygun event'i dinleyip tepki veriyor; UI ve mantık arasında sıfır doğrudan bağımlılık
- Race condition düzeltmesi: Level Complete kararı `OnHolderProcessed` event'iyle (animasyon tamamlandıktan sonra) tetikleniyor, araç hareket ederken erken popup açılması sorunu giderildi
- `ResetLevelState` artık Holder'ı da temizliyor (slot'lar, son eklenen meyve referansı, interceptor)

*Ana Menü — İki Aşamalı Geliştirme:*
- İlk aşama: level haritası (grid, kilit/açık durumu), `LevelTransitionData` ScriptableObject ile sahneler arası veri taşıma, `SceneLoader` çift yönlü çalışır hale getirildi
- İkinci aşama (sadelleştirme): gerçek oyun referansı incelenince harita UI'sının erken aşama için fazla karmaşık olduğu görüldü; "Level X — Play" tek butonlu sade menüye dönüştürüldü, `LevelTransitionData` kaldırıldı, `SceneLoader` mantığı doğrudan `GameManager`'a taşındı
- Bu karar "önce çalışır, sonra güzel" prensibini uygulamak için bilinçli olarak alındı

*Kenney UI Pack Entegrasyonu:*
- 9-slice butonlar ve TMP fontlarıyla ekranlar yeniden tasarlandı
- Renk-anlam sistemi benimsendi: **mavi** = ana aksiyon, **kırmızı** = acil/tehlike, **gri** = nötr/iptal, **sarı** = ödül/coin, **yeşil** = oyna/devam

**3. Büyük Konsept Değişikliği — Car → Fruit Reskin ("Taze Pazar" Teması)**

*Karar ve Motivasyon:*
- Araç temasının ücretsiz 3D asset bulma sürecinde kısıtlayıcı olduğu görüldü; Kenney'nin ücretsiz Food Kit ve Pirate Kit ile meyve/sebze temasına geçiş kararı alındı
- Tema değişikliği; staj projesini oynanabilir bir prototipin ötesine taşıyıp görsel kimliğini güçlendirdi

*GUID Koruma Stratejisi ile Risksiz Refactor:*
- Unity'de dosya yeniden adlandırma hatalı yapılırsa prefab/scene referansları kopar; bu riski önlemek için tüm `.cs` dosyaları Unity Editor'ın "Rename" özelliğiyle yeniden adlandırıldı (GUID korundu), ardından `git mv` ile git geçmişi de senkronize edildi
- `Car` → `Fruit`, `CarMover` → `FruitMover`, `CarColor` → `FruitType` (enum değerleri: `Red→Tomato`, `Blue→Coconut`, `Green→Watermelon`, `Yellow→Lemon`, `Purple→Grape`, `Orange→Orange`)
- Tüm event channel'lar, prefablar ve level asset'leri eş zamanlı güncellendi; `ARCHITECTURE.md` ve `CLAUDE.md` dokümantasyonu yenilendi
- Oyun mantığı, pathfinding, save sistemi — **hiçbiri değişmedi**; event-driven mimarinin isimden bağımsız çalışması burada kanıtlandı

*Kenney Food Kit 3D Model Entegrasyonu:*
- 6 meyve türü seçildi ve prefab'lara yerleştirildi: Tomato (scale 6), Coconut (scale 7), Watermelon (scale 2.5, 8 parçalı slice), Lemon (scale 5), Grape (scale 4), Orange (scale 6)
- Wall → Crate, LockedBox → Chest, GarageSpawner → Bowl tematik karşılıkları oluşturuldu
- Holder slot tepsileri (cutting board stili), çevre/zemin düzenlemesi yapıldı

*PropScatterTool — Özel Editor Aracı:*
- `Window → CarMatchClone → Prop Scatter Tool`: seçilen prefabı belirtilen XZ alanına rastgele sayı, konum, rotasyon ve ölçek varyasyonuyla dağıtır
- `PrefabUtility.InstantiatePrefab` ile prefab bağlantısı korunuyor; tüm scatter işlemi tek `Ctrl+Z` ile geri alınabilir (`Undo.CollapseUndoOperations`)

*Animasyon Sistemi — Üç Katmanlı Geliştirme:*
- **Pivot-doğru yuvarlanma**: `transform.Rotate(axis, angle)` mesh'i taban noktasında (Y=0) döndürdüğü için meyve zemine gömülüyor ve büyüyüp küçülüyordu. BoxCollider center'dan hesaplanan `_meshCenterY` değeriyle `DOTween.To()` + pozisyon telafisi formülü (`transform.position = pathPos + V - q*V`) sayesinde mesh merkezi hareket boyunca sabit yükseklikte tutuldu; zemine girme sorunu tamamen giderildi
- **Holder varışında rotasyon düzeltme**: Hareket sırasında biriken rastgele rotasyonu temizlemek için `OnComplete` callback'inde `transform.rotation = Quaternion.identity` sıfırlaması eklendi
- **GarageSpawner Preview Sistemi**: Bowl üzerinde sıradaki meyvenin küçük (%65 ölçek) görsel kopyası (MeshFilter + MeshRenderer kopyalama, Fruit/Collider/FruitMover yok) bekliyor. Tetiklenince: oyun mantığı hemen çalışır (grid state güncellenir, pathfinding yeniden hesaplanır), gerçek meyve geçici olarak gizlenir, preview parabolik ark çizerek facing hücreye zıplar, iniş sonrası gerçek meyve mini-pop ile görünür hale gelir. Undo tetiklenince preview bowl'a geri döner.

### Kullanılan Araçlar/Teknikler
- Unity 3D (URP), C#, DOTween (Sequence, DOPath, DOMoveX/Y/Z, DOScale), ScriptableObject Event Channel Pattern, A* Pathfinding, Object Pooling, JSON Serialization, Git/GitHub (GUID-safe rename), Claude Code (AI-assisted development), Unity Editor Scripting (EditorWindow, PrefabUtility, Undo API), Kenney Food Kit / Kenney UI Pack (CC0)

### Öğrenilenler / Notlar
- **GUID disiplini büyük ölçekli refactor'larda kritik**: Unity'de `.cs` dosyasını dosya sistemi üzerinden yeniden adlandırmak GUID'yi değiştirmez ama git bu değişikliği "sil + yeni dosya" olarak görür. Doğru yol: önce Unity Editor'da rename (GUID korunur) → sonra `git mv` (geçmiş takibi korunur). Bu iki adımın atlanması tüm prefab bağlantılarını koparabilirdi.
- **3D model pivot/scale sorunlarının teşhisi**: "Meyve zemine giriyor" gibi görsel bir şikayet, BoxCollider center değerinin ölçekle çarpılmasıyla matematiksel kök nedene indirgenebilir. Prefab YAML'ı okuyarak `m_Center` ve `m_LocalScale` değerlerini doğrudan görmek, "ne kadar Y offset eklenmeli" sorusunu kesin olarak yanıtladı.
- **Event-driven mimarinin tema bağımsızlığı kanıtlandı**: 5 günlük geliştirmede temanın "araç" yerine "meyve" olması oyun mantığında (pathfinding, matching, booster, save) sıfır değişiklik gerektirdi. Sistemler birbirini event channel üzerinden tanıdığı için isimleri önemsiz.
- **Ücretsiz asset'lerin (Kenney) profesyonel prototipleme değeri**: CC0 lisanslı Kenney Food Kit, Pirate Kit ve UI Pack; telif riski olmadan hızlı görsel prototipleme sağladı. Gerçek kullanım senaryosunda "önce işlev, sonra görsel" prensibi ile entegre edildi.
- **"Önce çalışır, sonra güzel" kararlarını kayıt altına almak**: Ana menü için level haritası yerine sade tek buton tercih edildiğinde bu karar günlüğe yazıldı. Gelecekte geri dönmek ya da staj değerlendirmesinde açıklamak için bu tür pivot noktalarını belgelemek önemli.

---

## [TARİH GİRİLECEK] — Gün 4

### Yapılan İşler

Bir önceki günün son commit'i olan `feat(camera): padding ve verticalBias...`'dan itibaren devam edildi.

**1. Milestone 6 — LockedBox + GarageSpawner (Engel Sistemi)**
- `ILaneObstacle` arayüzüyle genişletilebilir engel altyapısı kuruldu; yeni engel eklemek mevcut kodu değiştirmiyor
- `LockedBox`: 4 komşudan biri boşalınca tetiklenir, içindeki gizli araç ortaya çıkar; hücre kutu aktifken hiçbir zaman walkable olmaz
- `GarageSpawner`: karşısındaki (`facingDirection`) hücre boşalınca sıradaki renkte araç spawn eder; stok biter bitirmez kapanır
- `garageColors[]` dizisi: tek renk yerine sıralı spawn renk listesi — `garageStockCount` field'ı tamamen kaldırıldı, stok sayısı dizinin uzunluğundan otomatik çıkar
- Self-subscription pattern: her obstacle MonoBehaviour kendi `OnEnable`/`OnDisable`'ında event channel'a abone olup çıkar; Board obstacle'ları iterate etmez
- Wall hücreleri için statik görsel prefab eklendi (Instantiate/Destroy, pooling gerekmiyor)

**2. Level Editor Aracı Genişletilmesi**
- LevelEditorWindow'a LockedBox ve GarageSpawner desteği eklendi
- GarageSpawner için "Spawn Renk Sırası" UI bölümü: `+ Renk Ekle` / `- Son Rengi Sil` butonları, numaralı açılır renkler
- Garaj hücresinin görsel gösterimi: ilk renk iç kare rengi, "↓ N" etiketi ile stok sayısı

**3. Milestone 7 — Booster Sistemi (Undo, Shuffle, Super Undo, Magnet)**
- `IBooster` arayüzü `bool Execute(Board, GameState)` döner: `true` → stok azalır, `false` → stok harcanmaz
- **Undo Booster**: memento+closure hibrit pattern — obstacle geri almalarını `ObstacleTriggerPayload.UndoAction` closure'larıyla kayıt altına alır
- **Shuffle Booster**: Fisher-Yates karıştırma, araçları pool'a iade edip yeni renklerde yeniden spawn
- **Super Undo Booster**: aktif seçim mekanizması; seçilen araç reserve slot'a ayrılır, collider + `IsReachable=false` ile tamamen izole edilir; "R" ile normal akışa iade
- **Magnet Booster**: aynı anda değil kuyruk tabanlı sıralı gönderim — her araç holder'a ulaşınca sıradaki gönderilir; `Queue<Car>` + `HashSet<Car>` filtresiyle birden fazla Magnet chain'in karışması önlendi
- Race condition düzeltmesi: `OnBeforeCarRemoved` event'i eklendi — Board `HandleCarSelected` başında (hücre değişmeden önce) bu event'i fırlatır; UndoBooster snapshot'ını bu event'e bağladı, GarageSpawner'ın closure kaydetmesiyle çakışma önlendi
- LockedBox undo: `RemoveCarAtAndBlock()` API'si eklendi (hücre walkable olmadan araç silinir); `OnEnable` re-subscription ile kutu undo sonrası yeniden tetiklenebilir hale getirildi

**4. Milestone 8 — Save/Load Sistemi**
- `ISaveProvider` arayüzü + `LocalJsonSaveProvider`: `Application.persistentDataPath/save.json` — JSON, JsonUtility
- `SaveManager` MonoBehaviour: lazy-init provider, `[ContextMenu("Kayıt Dosyasını Sil")]` Edit/Play modunda çalışır
- `GameManager` artık save sisteminin tek otoritesi: Awake'te yükler, level tamamlanınca/booster kullanılınca/uygulama arka plana alınınca kaydeder
- `LevelData[]` dizisi ile gerçek level ilerlemesi: `_currentLevelIndex` ile hangi level yükleneceği belirleniyor, `Board._levelData` Inspector'dan boş bırakıldı
- `GameState.Coins` alanı eklendi (M9+ para sistemi için placeholder)
- `[ContextMenu("Reset Save Data")]` — sadece Play modunda çalışır, Edit modunda `Application.isPlaying` guard ile sahne kirlenmesi önlendi

**5. Level Complete Timing Düzeltmesi**
- Eski akışta Level Complete, araç hareket etmeye başlamadan (animasyon bitmeden) tetikleniyordu
- `OnHolderProcessed` event'i eklendi: Holder, araç işlemeyi tamamlayınca (InsertIntoSlot + ResolveMatches + IsFull kontrolü bittikten sonra) bu event'i fırlatır
- Level Complete kararı `Board`'dan `GameManager.HandleHolderProcessed()`'a taşındı; board boş VE reserve slot boş ise tetiklenir
- `Board.IsBoardEmpty()` public API'ye taşındı
- `Board._onLevelCompleteChannel` kaldırıldı — Board artık level tamamlanma kararı vermiyor

**6. Level Geçişi Bug Düzeltmeleri**
- `Board.RebuildGrid()` sonunda `_onBoardStateChangedChannel.Raise()` eklendi — level geçişinde PathfindingService'in yeni grid'i tanıması sağlandı
- `GameInputHandler._inputLocked = true` olan level complete sonrası input kilidi açılmıyordu: `OnNewLevelLoaded` event'i eklendi, `GameInputHandler` buna subscribe olup kilitleri sıfırlıyor
- Subscriber ters sıra race condition: `OnLevelCompleteChannel` aboneliği `GameInputHandler`'dan kaldırıldı — `OnNewLevelLoaded` yeterli, ayrıca kilitleme gereksizdi
- SuperUndo ReserveSlot kontrolü: board boşalma kontrolüne `HasReservedCar` testi eklendi

**7. Event Bus Sağlık Kontrolü**
- 13 event channel asset tarandı: raise eden ve dinleyen sistemler tabloya döküldü
- `OnBoosterUsed`, `OnMatchOccurred`, `OnHolderFull` — dinleyicisi olmayan ama kasıtlı bırakılan event'ler ARCHITECTURE.md'ye belgelendi (M9/M11 için hazır bağlantı noktaları)

### Kullanılan Araçlar/Teknikler
- Unity 3D (URP), C#, DOTween, ScriptableObject Event Channel Pattern, A* Pathfinding, Object Pooling, JSON Serialization (JsonUtility), Git/GitHub, Claude Code (AI-assisted development), Unity Editor Scripting (EditorWindow)

### Öğrenilenler / Notlar
- **Closure tabanlı event payload'ları**: `ObstacleTriggerPayload.UndoAction` ile her tetiklenme anına ait geri alma mantığı, tetiklenmeyle birlikte paketlenerek taşındı — sistemler arası bağımlılık olmadan undo zinciri kuruldu
- **Race condition'ları event sıralaması ile çözme**: `GameEventChannel.Raise()` listener'ları ters kayıt sırasında çalıştırır; bu yüzden UndoBooster'ın snapshot alması `OnCarSelected` yerine ayrı bir `OnBeforeCarRemoved` event'ine bağlandı — sıra bağımsız garantisi sağlandı
- **Aynı event'e hem raise hem subscribe (self-raise pattern)**: `GameManager` kendi fırlattığı `OnLevelComplete`'i kendi dinleyebilir; bu, diğer sistemlerin (M9 UI) aynı event'e bağlanmasını kolaylaştırır
- **"Raise var ama dinleyen yok" durumu normaldir**: `OnBoosterUsed`, `OnMatchOccurred`, `OnHolderFull` şu an dinlenmese de silinmemeli — event-driven mimaride yeni sistemler (HUD, ses, VFX) mevcut event'lere bağlanır, mevcut kodu değiştirmeden genişleme yapılır
- **Edit modu güvenliği**: `[ContextMenu]` metodları Edit modunda çağrıldığında sahneye GameObjects spawn edebilir; `Application.isPlaying` guard ile bu önlendi

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
