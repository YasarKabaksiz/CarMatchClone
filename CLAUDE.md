# CLAUDE.md
## Claude Code için Proje Talimatları

Bu dosya, Claude Code'un her session başında otomatik okuduğu konvansiyon dosyasıdır. ARCHITECTURE.md "ne" ve "neden" sorularını cevaplar; bu dosya "nasıl" sorusunu cevaplar. İkisi çelişirse ARCHITECTURE.md önceliklidir, bana haber ver.

---

## 1. Proje Bağlamı

Bu bir Unity 3D mobil puzzle oyunu (grid tabanlı meyve/sebze eşleştirme — collector/matching türü). Tema: Kenney Food Kit meyve/sebze modelleri (domates, limon, üzüm, karpuz, portakal, hindistan cevizi). Proje kodu adı `CarMatchClone`'dur ancak tema araçtan meyve/sebzeye çevrilmiştir; namespace ve proje adı değişmemektedir.

Detaylı mimari için mutlaka `ARCHITECTURE.md` dosyasını oku ve ona uy. Her görev öncesi ilgili bölümü referans al.

**Unity sürümü:** [LTS sürüm buraya yazılacak, proje kurulunca]
**Render Pipeline:** URP (3D)
**Dil:** C#

---

## 2. Kesin Kurallar (Asla Yapma)

- **Singleton pattern kullanma** (GameManager hariç — o da sadece sahne yaşam döngüsü için, iş mantığı içermez). Sistemler arası referans için event bus (`GameEvents`) kullan.
- **Hardcoded değer bırakma.** Tür sayısı, holder slot sayısı, grid boyutu gibi değerler `LevelData` veya `GameConfig` ScriptableObject'lerinden gelmeli.
- **`Update()` içinde ağır hesaplama yapma** (özellikle pathfinding). Pathfinding sadece board state değiştiğinde (event tetiklenince) çalışmalı, her frame değil.
- **Bir sistemin başka bir sistemin private/internal detayına doğrudan erişmesi yasak.** Örn. UI kodu, Board'un GridCell dizisine doğrudan erişmemeli — public API veya event üzerinden veri almalı.
- **Yeni bir engel/mekanik eklerken mevcut `ILaneObstacle` interface'ini bypass etme.** Yeni class yaz, mevcut LockedBox/GarageSpawner kodunu değiştirme.
- **localStorage/PlayerPrefs'i büyük veri için kullanma** (level state, save data). JSON dosyası veya uygun bir serialization sistemi kullan.

## 3. Kesin Kurallar (Her Zaman Yap)

- Her yeni sistem için **arayüz/interface öncelikli** tasarım (ör. `IBooster`, `ILaneObstacle`) — somut sınıfı değil, interface'i referans al.
- **ScriptableObject tabanlı veri yönetimi** her yerde: LevelData, GameConfig, FruitTypePalette vb.
- Yeni bir görev bitince **kısa bir özet ver**: hangi dosyalar değişti, hangi event'ler eklendi/kullanıldı, test edilmesi gereken senaryo ne.
- **Object pooling** kullan: Fruit prefabları, patlama/VFX objeleri için (mobilde GC baskısı kritik).
- Yeni Input System kullan (eski `Input.GetMouseButtonDown` değil).

---

## 4. Kod Stili

- **Naming:** PascalCase (class, method, public field), camelCase (private field, local variable). Private field'larda `_` prefix kullan (`_currentState` gibi).
- **Dosya = Sınıf:** Her `.cs` dosyasında tek bir public sınıf/interface olsun (dosya adıyla aynı isimde).
- **Namespace:** `CarMatchClone.Core`, `CarMatchClone.Board`, `CarMatchClone.Gameplay` gibi klasör yapısına paralel namespace kullan. **Not:** `CarMatchClone.Board` namespace'i ile `Board` sınıfı aynı isimde. `CarMatchClone.Core` gibi kardeş namespace içinden erişirken alias çalışmıyor (compiler namespace'i kazanıyor); bu durumlarda `CarMatchClone.Board.Board` tam nitelikli ismini kullan.
- **Yorum satırları Türkçe veya İngilizce olabilir**, ama tutarlı ol — bir dosyada karışık kullanma.
- **Magic number yok.** Sabitler `const` veya `[SerializeField]` ile isimlendirilmiş alanlar olarak tanımlansın.

---

## 5. Görev Alma Kuralları (Claude Code'un Kendisi İçin)

- Büyük, belirsiz bir görev alırsan (ör. "oyunu tamamla") **durup netleştirme iste.** Tek seferde tek bir milestone/sistem üzerinde çalış (bkz. ARCHITECTURE.md Bölüm 11).
- Bir görev, ARCHITECTURE.md'de tanımlı olmayan yeni bir mimari karar gerektiriyorsa (ör. yeni bir pattern, yeni bir kütüphane), **önce sor, sessizce karar verme.**
- Mevcut çalışan bir sistemi (önceki milestone'da tamamlanmış) değiştirmen gerekiyorsa, bunu açıkça belirt ve neden gerektiğini açıkla — sessizce yan etkili değişiklik yapma.
- Test edilebilir küçük adımlarla ilerle: bir özelliği yazdıktan sonra, o özelliğin nasıl test edileceğini (hangi sahnede, hangi adımlarla) belirt.

---

## 6. Git / Versiyon Kontrolü

- **Commit mesaj formatı:** `<prefix>(sistem): kısa açıklama` — Conventional Commits tarzı prefix kullanılır:
  - `feat`: yeni bir özellik/sistem eklendi (ör. `feat(board): grid ve GridCell temel yapısı eklendi`)
  - `fix`: bir hata düzeltildi (ör. `fix(pathfinding): garaj kapanınca reachability yanlış hesaplanıyordu`)
  - `wip`: henüz tamamlanmamış, ara commit (ör. `wip(booster): undo booster iskeleti, test edilmedi`)
  - `refactor`: davranış değişmeden kod yeniden düzenlendi (ör. `refactor(holder): match kontrolü ayrı sınıfa taşındı`)
  - `chore`: proje kurulumu, paket/ayar değişikliği, dokümantasyon (ör. `chore: .gitignore ve .gitattributes eklendi`)
  - `test`: test kodu eklendi/değiştirildi
- Her milestone bitiminde ayrı commit. Bir commit'te birden fazla sistemi karıştırma.
- Çalışmayan/test edilmemiş kodu `wip` dışında bir prefixle commit etme — `feat`/`fix` prefixli commit'ler mutlaka test edilmiş olmalı.

---

## 7. Test Beklentisi

- Her yeni sistem için en azından manuel test senaryosu tarif edilmeli ("Play moduna gir, X level'ı yükle, Y meyveye tıkla, Z olmalı").
- Pathfinding gibi kritik sistemlerde, en az 3 farklı board şekliyle (düz, L, U şekilli) test senaryosu önerilmeli.
- Kırılgan/riskli bir değişiklik yapılıyorsa (ör. mevcut Board API'sinde imza değişikliği), bunu açıkça belirt.

---

## 8. İletişim Tarzı

- Kod yazmadan önce **plan kısa özet halinde** sunulmalı (hangi dosyalar oluşturulacak/değişecek).
- Belirsiz bir nokta varsa varsayım yapıp ilerlemek yerine sor.
- Uzun açıklama yerine öz ve teknik ol; detaylar için ARCHITECTURE.md'ye referans ver.
