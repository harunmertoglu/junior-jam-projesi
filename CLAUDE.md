# Claude için çalışma kuralları

## Proje: Sonbahardan Kışa (3D platformer, game jam)
Kaynak: "Sonbahardan Kışa — Oyun Tasarım Dokümanı" (GDD). Kararlar oradan gelir; kapsam dışı bir şey önermeden önce GDD'ye bak.

- **Jam:** 48 saat, 3-4 kişilik ekip, tema Sonbahar. Unity 6 URP, PC/Windows build (itch.io). Astro Bot tarzı. Oyun 5-10 dk.
- **Hikâye:** Sonbahar ruhu olan yağmurluklu küçük kız 5-6 adayı geçip kış adasına ulaşır. Görünmeyen yaz ruhu (3. adadan itibaren) engel koyar.
- **Karakter:** Unity'nin hazır şekillerinden kodla kurulur, rig yok, animasyonun tamamı koddan. ~1,1 m boy, büyük kafa, kırmızı yağmurluk, sarı bot, iki örgü. Ayak altında yuvarlak gölge.
  - Animasyonlar: koşma, havada kollar yukarı, kayarken denge, dash'te öne uzama, squash & stretch, sallanan örgüler, göz kırpma.
- **Kontroller:** Klavye+fare ve gamepad. Hareket kameraya göre.
  - Hareket WASD/oklar | sol stick · Kamera fare | sağ stick · Zıpla Space | A (basılı = yüksek)
  - Dash/geri dön Shift, E, sağ tık | X, RB · Duraklat Esc | Start
- **Hareket değerleri:** koşma 6 m/sn, zıplama 2,2 m, coyote 0,12 sn, jump buffer 0,12 sn, düşüş çıkıştan hızlı.
- **Kamera:** arkadan/yukarıdan takip, oyuncu çevirebilir, dokunulmazsa yavaşça koşu yönüne döner, duvara girmez.
- **Dash ve geri dönüş:**
  1. Dash 0,18 sn'de 6 m ileri, bu sürede yerçekimi yok.
  2. Başlangıçta yerde yaprak halkası, yol boyunca kahverengi yaprak izi.
  3. Tuşa tekrar basınca anında başlangıç noktasına döner, iz savrulup dağılır.
  4. Geri dönüş noktası süresi bitince yanıp sönerek kaybolur. Süre adaya göre: 10, 9, 8, 7, 6, 5 sn.
  5. Yere basmadan en fazla 1 dash. Havadaki noktaya dönülürse yere inmeden yeni dash yok.
  6. Taşınan ilerleme nesnesi geri dönüşte kızla gelir.
  7. Düşüp checkpoint'e dönünce geri dönüş noktası silinir.
- **Mekanikler:**
  - Islak zemin (ada 1): kaydırır, kışta buz = daha kaygan.
  - Solan yaprak (ada 2): basınca 0,9 sn'de solar, kırılıp düşer, 3 sn sonra geri gelir.
  - Yükselip alçalan alev (ada 3), eriyen platform (ada 4), alev duvarı (ada 5).
  - İlerleme nesnesi (tohum, şalter) yolu/kapıyı açar (ada 3).
  - Checkpoint (ada 1).
  - **Ölüm yok:** can/sağlık yok, düşünce ya da ateşe değince son checkpoint'ten anında devam.
- **Mevsim:** 0 (sonbahar) – 1 (kış) arası tek değer, her ada ~0,2 artırır. Renk, kar örtüsü, parçacık, ışık/sis ve oynanış (buz, geri dönüş süresi, engel hızı) buna bağlı.
- **Arayüz:** ana menü, duraklatma, ayarlar (ses, fare hassasiyeti), bitiş ekranı. Oyun içinde HUD yok. Kayıt sistemi yok.
- **Kesinlikle yok:** boss, yaz ruhunun görünmesi, toplanabilir/puan/can, diyalog, kayıt, 6'dan fazla ada, WebGL.

## Dil
- Her zaman **Türkçe** konuş. Kod, değişken isimleri ve teknik terimler İngilizce kalabilir.

## Ben kimim
- Öğrenciyim, yaklaşık 4 aylık Unity tecrübem var. Kendi sistemlerimi kurabiliyorum: static event'li InputManager, singleton state manager, coroutine'ler, kamera sistemi gibi.
- 4-5 ay sonra Unreal Engine ve C++'a geçeceğim; ileride büyük bir şirkette çalışmak istiyorum.
- Bu yüzden **kodu anlamam, işin her şeyden önemli.** Anlamadığım bir kodu projeye koymak istemiyorum, çünkü kontrol edemiyorum.

## Kod yazım tarzım (buna birebir uy)
- Alanlarda ve metotlarda çoğu zaman erişim belirteci yazmam:
  ```csharp
  Rigidbody playerRigidbody;
  [SerializeField] float playerWalkSpeed;
  void Awake() { }
  ```
- Uzun ve açık isimler kullanırım: `playerRigidbody`, `playerVisualTransform`, `cameraDirectionForward`
- Event handler isimlerim: `MoveInputs`, `AimInputs`, `JumpInputs`. Parametre adı `value` ya da `this.moveInput = moveInput` şeklinde.
- Alanları `[Header("Movement")]` ile gruplarım.
- İşleri anlamlı metotlara bölerim: `SetPlayerMovement()`, `UpdateVisualRotation()`, `SetCameraDirection()`
- `if` bloklarında her zaman süslü parantez kullanırım.
- Az ve kısa Türkçe yorum yazarım. XML doc (`/// <summary>`) ve `[Tooltip]` kullanmam.

### Kullandığım yapılar (bunları rahatça kullanabilirsin)
- `public static event Action<T>` + `?.Invoke()`
- `public static X Instance;` singleton
- Ternary: `a ? b : c`
- `Lerp` / `Slerp`, coroutine'ler, `Mathf.Clamp`

### Kullanma (ya da önce açıkla)
- `=>` ile yazılmış metotlar ve property'ler
- `{ get; private set; }` property'leri
- `[DefaultExecutionOrder]`, `[RequireComponent]` gibi attribute'lar
- Gereksiz ileri teknikler (ör. animasyon için yay fiziği). **Her zaman çalışan en basit çözümü seç.**

## Mimari
- Event-driven çalışırım: `InputActions → InputManager → sistemler`
- InputManager girdileri **static event** olarak dağıtır. Sistemler `OnEnable`'da abone olur, `OnDisable`'da abonelikten çıkar.
- Sistemler gelen girdiyi bir alana yazar; `Update`/`FixedUpdate` o alanı kullanır.
- Sistemler birbirlerinin sonuçlarını da event ile dinler (ör. `CubeMovement.OnJumped` → animasyon, ses).
- Fizik ile görsel ayrıdır: Rigidbody ve collider ana objede, mesh `Visual` child'ında durur.
- Hareket **Rigidbody fiziğiyle** yönetilir (dash gibi mekanikler de gelecek). `CharacterController` kullanma.
- Yeni Input System kullanırım. Input Actions'ı proje geneli (`InputSystem.actions`) ya da Inspector'dan atarım.

## Sahne yapısı ve ekip kuralları
- **Tüm adalar TEK gameplay sahnesinde:** `Assets/TestScene/TestScene.unity` (ileride adı değişebilir). Sahibi Selim, başkası doğrudan değiştirmez. Diğer sahneler sadece başlangıç/bitiş sinematikleri.
- Ada geçişi sahne yüklemez: `IslandExit` oyuncuyu sonraki adanın başlangıç noktasına taşır, son ada bitiş sinematiğini yükler.
- Mevsim oyuncunun konumuna göre yumuşakça değişir: adaların ortasına `SeasonPoint` konur (sonbahar 0, orta ada 0.5, kış 1), `SeasonManager` en yakın iki noktayı uzaklığa göre karıştırıp değeri saniyede en fazla 0.25 kaydırır. Görünüm (gökyüzü, güneş, sis, ortam, renk tonu Volume'ları, parçacıklar) `SeasonVisuals`'tan gelir: `Assets/Prefabs/SeasonAtmosphere.prefab` sahneye bir kez konur. Mevsimi okuyan kod değeri saklamasın, her kullanımda `SeasonManager.Instance` üzerinden okusun.
- Environment tasarımı `UpdateScene1` / `UpdateScene2` gibi kopya sahnelerde yapılır, tek kök obje altında **prefab** olarak TestScene'e bir kez konur.
- Karakter `Assets/3rdPartyAssets/Character/RaincoatGirl.prefab`. Karakter değişiklikleri sahnede değil prefab'da yapılır.
- Unity sahne ve prefab dosyaları git'te birleştirilemez: her sahne/prefab dosyasının aynı anda tek sahibi olsun. Kod dosyaları paylaşılabilir.

## Nasıl çalışmalısın
- **Bir seferde tek sistem yaz.** Dört script'i birden atma; ben anladıktan sonra diğerine geç.
- Yeni bir yapı, fonksiyon ya da matematik kullanacaksan **önce kısaca ne işe yaradığını anlat.**
- Açıklamaları sade yap: önce **amaç** ("ne yapmaya çalışıyoruz"), sonra **örnek sayılarla** nasıl çalıştığı. Mümkünse Unity'de elle deneyebileceğim küçük bir test öner.
- Bir şeyi kendim yazmak istersem kodu yazma; ipucu ver ya da yazdığımı incele.
- Kodumda hata ya da eksik görürsen söyle (ör. `OnDisable`'da unutulmuş bir abonelik).
- Kodu Unity'de test etmediysen bunu açıkça belirt.
- Unity'de yapmam gereken kurulum adımlarını (hangi objeye ne eklenecek) madde madde yaz.
