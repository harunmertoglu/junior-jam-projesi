# Claude için çalışma kuralları

## Proje: Junyorjem (3D platformer)
- Yağmurluklu küçük bir kız, sonbahardan kışa doğru ilerlemeye çalışıyor.
- Level mekanikleri:
  - Kaygan alanlar
  - Üstüne atlayınca solup dökülen yapraklar
- Karakterin gücü: İleriye doğru dash atar, arkasında sonbaharı anımsatan kahverengi bir iz bırakır. Tuşa tekrar basınca o iz konumuna geri döner.

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
- Yeni Input System kullanırım. Input Actions'ı proje geneli (`InputSystem.actions`) ya da Inspector'dan atarım.

## Nasıl çalışmalısın
- **Bir seferde tek sistem yaz.** Dört script'i birden atma; ben anladıktan sonra diğerine geç.
- Yeni bir yapı, fonksiyon ya da matematik kullanacaksan **önce kısaca ne işe yaradığını anlat.**
- Açıklamaları sade yap: önce **amaç** ("ne yapmaya çalışıyoruz"), sonra **örnek sayılarla** nasıl çalıştığı. Mümkünse Unity'de elle deneyebileceğim küçük bir test öner.
- Bir şeyi kendim yazmak istersem kodu yazma; ipucu ver ya da yazdığımı incele.
- Kodumda hata ya da eksik görürsen söyle (ör. `OnDisable`'da unutulmuş bir abonelik).
- Kodu Unity'de test etmediysen bunu açıkça belirt.
- Unity'de yapmam gereken kurulum adımlarını (hangi objeye ne eklenecek) madde madde yaz.
