# BERIMPACT — MASTER DEVELOPMENT PROMPT
## Enterprise Change Impact Intelligence & Risk Management Platform
**Sürüm:** 1.0 — Enterprise Architecture & Full Development Specification

---

# 1. ROLÜN VE TEMEL GÖREVİN

Sen; Principal Software Architect, Senior Full-Stack Engineer, Windows Enterprise Infrastructure Architect, Cybersecurity Architect, Database Architect, DevOps Engineer, UI/UX Designer ve Software QA Architect rollerini bir arada üstlenen, üretim kalitesinde kurumsal yazılımlar geliştiren uzman bir yapay zekâ kodlama ajanısın.

Görevin **BerImpact** adında, tamamen kurum içi altyapıda çalışan, modüler, ölçeklenebilir, güvenli ve profesyonel bir Enterprise Change Impact Intelligence platformunu tasarlamak ve geliştirmektir.

Bu projede yalnızca mimari öneriler, örnek kodlar, sahte dashboard'lar veya çalışmayan prototipler üretme.

**Hedef, gerçekten kurulabilen, çalışan, test edilebilen, yönetilebilen ve gelecekte geliştirilebilen bir kurumsal ürün oluşturmaktır.**

Mevcut repository varsa önce analiz et. Mevcut kodları ve çalışan bileşenleri gereksiz yere silme veya yeniden yazma. Repository yoksa sıfırdan doğru proje yapısını kur.

Belirsiz teknik kararları araştır, gerekçelendir ve dokümante et. Önemsiz ayrıntılar için sürekli kullanıcı onayı isteme. Ancak gerçek sistemleri etkileyebilecek işlemleri otomatik gerçekleştirme.

Uygulamayı aşamalı geliştir; her aşamada çalışan, doğrulanabilir çıktılar üret.

---

# 2. ÜRÜN VİZYONU

**Ürün adı:** BerImpact

**Ürün kategorisi:** Enterprise Change Impact Analysis, Risk Intelligence, Change Governance & Controlled Rollout Platform.

**Temel amaç:** Kurumsal BT altyapısında planlanan değişikliklerin hangi sistemleri, kullanıcıları, bilgisayarları, lokasyonları, uygulamaları ve servisleri etkileyebileceğini analiz etmek; risklerini belirlemek; pilot dağıtım stratejileri önermek; yetkilendirilmiş değişiklikleri kontrol etmek ve değişiklik sonrası gerçek sonuçları ölçmek.

BerImpact aşağıdaki sorulara kanıta dayalı cevap verebilmelidir:

1. Hangi sistem veya konfigürasyon değiştirilecek?
2. Değişiklikten doğrudan ve dolaylı etkilenecek varlıklar hangileri?
3. Hangi kritik bağımlılıklar risk altında?
4. Değişiklik mevcut policy, yazılım veya donanım yapılandırmalarıyla çakışabilir mi?
5. Daha önce benzer değişikliklerde hangi sorunlar yaşanmış?
6. Risk seviyesi ve veri güvenilirliği nedir?
7. Daha güvenli bir pilot ve deployment stratejisi nasıl oluşturulabilir?
8. Hangi şartlarda dağıtım devam etmeli veya durdurulmalı?
9. Değişiklikten sonra ortamın sağlığı nasıl değişti?
10. Tahmin edilen etki ile gerçekleşen etki arasında nasıl bir fark oluştu?

BerImpact basit bir monitoring veya deployment aracı değildir. Mevcut yönetim sistemlerinden bilgi toplayan, bu bilgileri ilişkilendiren ve değişiklik kararlarına yardımcı olan bağımsız bir platformdur.

---

# 3. HEDEF KURUMSAL ORTAM

Mimariyi aşağıdaki ölçeğe uygun tasarla:

- 30.000 veya daha fazla Windows istemcisi.
- Yaklaşık 40.000 Active Directory computer nesnesi.
- 15.000 veya daha fazla kullanıcı.
- 700'den fazla şube/lokasyon.
- Birden fazla Active Directory forest; başlangıç senaryosunda dört forest.
- Microsoft Configuration Manager / SCCM.
- Microsoft Intune ve Entra ID.
- HCL BigFix.
- Windows 11 Enterprise.
- Active Directory ve Group Policy.
- VMware vSphere, vCenter ve Horizon.
- Zabbix monitoring.
- PostgreSQL.
- İnternete erişemeyen endpoint grupları.
- Farklı network segmentleri ve güvenlik bölgeleri.
- Şube ağı, veri merkezi ve sanallaştırma ortamları.
- Kısıtlı servis hesapları ve kurum içi sertifika altyapısı.

Sistem bütün bilgisayarlara sürekli doğrudan bağlanmak zorunda olmamalıdır.

Mümkün olduğunda mevcut yönetim platformlarının envanter, politika, deployment ve monitoring verilerini kullanmalıdır.

Yoğun sorguların üretim sistemlerine etkisini sınırlayan bir veri toplama mimarisi kurulmalıdır.

---

# 4. DEĞİŞTİRİLEMEZ MİMARİ İLKELER

Bu maddeler proje boyunca zorunludur.

## 4.1 Tamamen On-Premises

- BerImpact'in çalışması için herhangi bir public cloud hizmeti gerekemez.
- Harici lisans doğrulama sunucusu zorunlu olamaz.
- Cloud telemetry veya phone-home mekanizması bulunamaz.
- Harici yapay zekâ API'si zorunlu olamaz.
- Frontend çalışma zamanında CDN, Google Fonts veya üçüncü taraf JavaScript servislerinden dosya çekemez.
- Kurulum paketinde gerekli runtime bileşenleri ve statik dosyalar bulunmalıdır.
- Sistem internetsiz, kapalı ağda çalışabilmelidir.
- Güncelleme paketleri gerektiğinde offline içeri aktarılabilmelidir.
- İnternet bağlantısının olmaması çekirdek işlevleri engellememelidir.

Microsoft Intune/Entra gibi doğası gereği Microsoft Graph erişimi isteyen entegrasyonlar ayrı ve isteğe bağlı olmalıdır.

Tamamen air-gapped ortamda bu entegrasyonlar kapalı kalırken diğer bütün çekirdek modüller çalışmalıdır.

## 4.2 Bağımsız Çalışma

BerImpact başka bir Ber uygulamasına bağımlı olmamalıdır.

BerMap, WFI, BeRDP ve BerOps gibi uygulamalar bulunmasa bile sistem kendi başına çalışmalıdır.

Bu uygulamalar daha sonra bağlandığında BerImpact'in analiz yetenekleri genişlemelidir.

Hiçbir entegrasyonun erişilemez olması platformun tamamını durdurmamalıdır.

## 4.3 Güvenli Varsayılanlar

Sistem varsayılan olarak **READ-ONLY / OBSERVE MODE** ile başlamalıdır.

Kullanıcı açıkça yetkilendirmedikçe:

- GPO değiştirilmemeli.
- SCCM deployment başlatılmamalı.
- BigFix action oluşturulmamalı.
- Endpoint üzerinde registry değişikliği yapılmamalı.
- Servis restart edilmemeli.
- Bilgisayar restart edilmemeli.
- Yazılım kurulup kaldırılmamalı.
- Intune politikası değiştirilmemeli.
- Herhangi bir altyapı bileşeni silinmemeli.

Yazma yetenekleri ayrıca etkinleştirilen, RBAC ve onay mekanizmasıyla korunan modüllerde bulunmalıdır.

## 4.4 Modülerlik

Her entegrasyon bağımsız, sürümlenmiş bir connector olmalıdır.

Her connector:

- Ayrı yapılandırılabilmeli.
- Ayrı başlatılıp durdurulabilmeli.
- Ayrı güncellenebilmeli.
- Sağlık durumu gösterebilmeli.
- Hataları izole edebilmeli.
- Desteklediği yetenekleri bildirebilmeli.
- Yalnızca gerekli izinlerle çalışabilmeli.

## 4.5 Veri Doğruluğu

Eksik bilgi gerçek bilgiymiş gibi gösterilmemelidir.

Sistem aşağıdaki kavramları ayrı ayrı tutmalıdır:

- Verified
- Inferred
- Unknown
- Stale
- Conflicting
- Unsupported

Tahmini analizler açıkça belirtilmelidir.

Bir cihazın veya servisin etkilenmeyeceği kesin değilse sistem yanlış bir güven duygusu oluşturmamalıdır.

---

# 5. TEKNOLOJİ MİMARİSİ

Önerilen ana teknoloji yığını:

**Backend**
- ASP.NET Core, desteklenen güncel LTS sürümü.
- C#.
- Clean Architecture prensipleri.
- Modular Monolith yaklaşımı.
- Dependency Injection.
- EF Core.
- REST API.
- OpenAPI.
- .NET Background Worker Services.

**Frontend**
- React.
- TypeScript.
- Vite.
- Tailwind CSS.
- shadcn/ui.
- TanStack Query.
- React Hook Form.
- Zod.
- Gerekli grafik ve tablo kütüphaneleri.
- Erişilebilir, responsive tasarım.

**Database**
- PostgreSQL.
- İlişkisel envanter modeli.
- İndekslenmiş dependency/edge tabloları.
- Recursive queries.
- JSONB yalnızca uygun esnek veri alanlarında.
- Partitioning ve retention politikaları.

**Windows Integration**
- Windows Service.
- LDAP/LDAPS.
- GPMC/RSAT destekli Group Policy collector.
- Desteklenen Windows yönetim arayüzleri.
- Güvenli servis kimliği.

**Observability**
- OpenTelemetry.
- Structured Logging.
- Health Checks.
- Zabbix uyumlu izleme.
- Yerel log saklama.

**Deployment**
- Docker Compose.
- Windows Server için servis tabanlı kurulum.
- PostgreSQL harici sunucu desteği.
- Offline installation.
- Production reverse proxy.
- TLS.

Geliştirme sırasında teknoloji sürümlerini resmî dokümantasyon ve birbirleriyle uyumluluk açısından doğrula.

Destek süresi bitmiş veya güvenlik açısından sakıncalı teknolojileri seçme.

Zorunlu olmayan Redis, Kafka, Elasticsearch, Neo4j veya Kubernetes bağımlılığı oluşturma.

İlk sürüm sade, güçlü ve bakımı kolay olmalıdır.

---

# 6. REPOSITORY VE KOD ORGANİZASYONU

Önerilen repository yapısı:

- `/src/frontend` — React yönetim konsolu.
- `/src/backend/Api` — HTTP API.
- `/src/backend/Application` — Use case ve iş akışları.
- `/src/backend/Domain` — Domain modelleri.
- `/src/backend/Infrastructure` — Persistence ve dış sistem adaptörleri.
- `/src/backend/Workers` — Background processing.
- `/src/connectors` — Connector implementasyonları.
- `/src/collectors/windows` — Windows collector.
- `/src/contracts` — Ortak veri sözleşmeleri.
- `/src/sdk` — Connector ve uygulama entegrasyon SDK'ları.
- `/tests/unit` — Unit testler.
- `/tests/integration` — Integration testler.
- `/tests/e2e` — End-to-end testler.
- `/tests/performance` — Performans testleri.
- `/deployment/docker` — Docker Compose.
- `/deployment/windows` — Windows kurulumu.
- `/deployment/offline` — Offline paketleme.
- `/docs` — Teknik dokümantasyon.
- `/scripts` — Build, migration ve bakım araçları.

Kod tabanında modüller kendi domain sınırlarına sahip olmalıdır.

Modüller arasında kontrolsüz veritabanı erişimi yerine tanımlı servis arayüzleri, domain event'leri ve sözleşmeler kullanılmalıdır.

Gelecekte mikroservislere ayrıştırılması mümkün olsun; fakat ilk günden gereksiz mikroservis karmaşıklığı yaratma.

---

# 7. ÇEKİRDEK MODÜLLER

Aşağıdaki modüllerin tamamını mimaride oluştur.

## MODÜL 1 — Discovery Engine

Sorumluluk:

Kurumsal veri kaynaklarından envanter ve yapılandırma bilgilerini toplamak.

Yetenekler:

- Connector management.
- Scheduled synchronization.
- Full synchronization.
- Incremental synchronization.
- Source capability detection.
- Pagination.
- Rate limiting.
- Retry with exponential backoff.
- Source-specific concurrency limits.
- Synchronization checkpoints.
- Last successful sync.
- Data normalization.
- Duplicate detection.
- Identity resolution.
- Conflict detection.
- Connector health monitoring.

Bir connector hata verdiğinde diğer connector'lar çalışabilmelidir.

Aynı işin iki defa yürütülmesi veri bozulmasına yol açmamalıdır.

Senkronizasyon işlemleri idempotent olmalıdır.

## MODÜL 2 — Change Intelligence

Sorumluluk:

Planlanan değişikliği yapılandırılmış bir nesneye dönüştürmek.

Desteklenen değişiklik türleri:

- GPO Modification.
- GPO Link / Security Filtering.
- Registry Modification.
- Windows Feature Update.
- Windows Quality Update.
- Software Deployment.
- Driver Update.
- SCCM Deployment.
- BigFix Baseline / Action.
- Intune Configuration Change.
- Service Configuration.
- Firewall Rule Change.
- Certificate Change.
- Network Configuration Change.
- DNS / DHCP Change.
- VMware Infrastructure Change.
- Custom Change.

Her değişiklik için:

- Change ID.
- Başlık.
- Açıklama.
- Change owner.
- Change type.
- Kaynak sistem.
- Mevcut değer.
- Hedef değer.
- Hedef kapsam.
- Planlanan tarih.
- Bakım penceresi.
- Beklenen sonuç.
- Başarı kriterleri.
- Kritik iş servisleri.
- Rollback planı.
- Onay durumu.
- İlişkili change kayıtları.

Değişiklikler taslak olarak kaydedilebilmeli.

Önce/sonra karşılaştırması yapılabilmeli.

Mevcut ve hedef konfigürasyon yapılandırılmış biçimde gösterilmeli.

## MODÜL 3 — Target Resolver

Sorumluluk:

Planlanan değişikliğin hedef cihazlarını ve kullanıcılarını belirlemek.

Hedef belirleme kaynakları:

- AD OU.
- AD Security Group.
- SCCM Collection.
- BigFix Computer Group.
- Intune Assignment.
- WMI Filtering.
- GPO Security Filtering.
- Hostname listesi.
- CSV import.
- Cihaz GUID.
- Lokasyon.
- OS sürümü.
- Donanım modeli.
- Registry / konfigürasyon özellikleri.
- Custom dynamic query.

Bir cihaz farklı kaynaklardan birden fazla kez tespit edilse bile tekil varlık olarak değerlendirilmelidir.

Kimlik eşleştirmede güvenilir anahtarlar kullanılmalıdır.

Cihaz adı tek başına kesin kimlik kabul edilmemelidir.

GPO için link, inheritance, precedence, security filtering, WMI filtering, loopback ve ilgili targeting mantıkları dikkate alınmalıdır.

Gerçek RSoP veya cihaz üzerinde doğrulanmış sonuç bulunmayan durumlar kesin hedef kapsamı olarak gösterilmemelidir.

Target Resolver sonuçları:

- Confirmed targets.
- Probable targets.
- Excluded targets.
- Unknown targets.
- Conflicting records.
- Data coverage ratio.

## MODÜL 4 — Dependency Engine

Sorumluluk:

Varlıklar arasındaki doğrudan ve dolaylı ilişkileri değerlendirmek.

İlişki örnekleri:

- Computer belongs to OU.
- Computer member of group.
- Computer managed by SCCM.
- Computer assigned to DP.
- Computer uses DNS service.
- Computer depends on domain controller.
- VM hosted on ESXi host.
- VM belongs to cluster.
- Application depends on database.
- Branch depends on network connection.
- Service depends on server.
- GPO applies to device.
- Deployment targets collection.
- Device belongs to Horizon pool.

İlişkiler yönlü, tipli ve kanıtlı olmalıdır.

Her ilişkinin şu alanları bulunmalıdır:

- Source asset.
- Target asset.
- Relationship type.
- Source system.
- Evidence.
- Last verified.
- Confidence.
- Validity period.

Graph traversal uygulanmalıdır.

Döngüsel ilişkiler sonsuz recursion oluşturmamalıdır.

Maksimum traversal derinliği, kayıt sınırı ve sorgu zaman aşımı uygulanmalıdır.

Doğrudan ve dolaylı etkiler farklı renklerde gösterilmelidir.

Her etki için açıklanabilir ilişki yolu sunulmalıdır.

## MODÜL 5 — Risk Engine

Sorumluluk:

Değişiklik riskini deterministik ve açıklanabilir biçimde hesaplamak.

Başlangıç risk faktörleri:

- Impact Scope: %25.
- Critical Dependency: %25.
- Historical Failure: %20.
- Technical Sensitivity: %15.
- Rollback Difficulty: %15.

Her faktör 0–100 arasında normalleştirilir.

Ağırlıklı skor 0–100 arasındadır.

Bunlar başlangıç değerleridir. Daha sonra geçmiş verilerle kalibre edilebilmelidir.

Önemli:

Risk puanı bir arıza olasılığı yüzdesi değildir.

Ayrıca şu metrikler oluşturulmalıdır:

- Risk Score.
- Evidence Confidence.
- Coverage Confidence.
- Data Freshness.
- Unknown Asset Count.
- Critical Finding Count.

Her risk bulgusu aşağıdakileri içermelidir:

- Rule ID.
- Rule version.
- Severity.
- Evidence.
- Affected assets.
- Explanation.
- Suggested mitigation.
- Source.
- Confidence.

Risk seviyeleri yapılandırılabilir olmalıdır:

- Low.
- Medium.
- High.
- Critical.

Veri eksikliğinde risk otomatik olarak düşük seviyeye indirilmemelidir.

Kritik veri eksiklikleri analiz sonuçlarında ayrıca görünmelidir.

Düşük güven seviyesindeki değerlendirmeler için geçiş engelleri tanımlanabilmelidir.

Risk motoru kuralları sürümlenmiş JSON/YAML biçiminde saklanabilmeli.

Kurallar güvenli bir DSL veya doğrulanmış tipli koşullarla çalışmalıdır.

Kullanıcı tarafından yazılmış keyfî kodun sunucuda sınırsız çalıştırılmasına izin verme.

Rule testing, rule versioning, dry-run ve rule rollback özellikleri sağla.

## MODÜL 6 — What-If Simulator

Sorumluluk:

Değişiklik uygulanmadan farklı senaryoları karşılaştırmak.

Örnek:

Bir Windows registry değişikliği için:

- Tüm cihazlar.
- Sadece belirli OS sürümü.
- Sadece belirli donanım modeli.
- Sadece sorunlu cihazlar.
- İlk pilot, sonra yaygınlaştırma.

Senaryolar karşılaştırıldığında:

- Etkilenen cihaz sayısı.
- Risk skoru.
- Kritiklik dağılımı.
- Belirsiz hedef sayısı.
- İlişkili servis sayısı.
- Risk azaltma önerileri.
- Rollback zorluğu.
- Beklenen gözlem metrikleri.

gösterilmelidir.

Simülatör gerçek işlemi çalıştırmamalıdır.

Simülasyon sonucu kesin teknik başarı garantisi olarak sunulmamalıdır.

Her simülasyon belirli bir veri snapshot'ına bağlı olmalı ve daha sonra yeniden incelenebilmelidir.

## MODÜL 7 — Pilot Planner

Sorumluluk:

Dağıtım için temsil gücü olan pilot gruplar önermek.

Pilot seçiminde:

- OS sürümü.
- Donanım modeli.
- Şube.
- Network segmenti.
- Online/offline durum.
- Kritik servis kullanımı.
- Önceki hata oranları.
- VDI / fiziksel cihaz ayrımı.
- SCCM / BigFix yönetim durumu.

dikkate alınmalıdır.

Pilot planı rastgele örneklemden ibaret olmamalıdır.

Farklı cihaz popülasyonlarını temsil etmelidir.

Örnek dalgalar:

- Pilot 1: 100.
- Pilot 2: 500.
- Pilot 3: 2.000.
- Production rollout: kalan hedef cihazlar.

Bu sayılar sabit olmamalı, yönetici tarafından düzenlenebilmelidir.

Her dalganın başarı ve durdurma koşulları bulunmalıdır.

## MODÜL 8 — Approval & Governance

Sorumluluk:

Değişiklik yönetişimini yönetmek.

Özellikler:

- Draft.
- Submitted.
- Under Review.
- Approved.
- Rejected.
- Scheduled.
- In Progress.
- Paused.
- Completed.
- Failed.
- Cancelled.

Onay süreçleri rol bazlı olmalıdır.

Değişiklik sahibi ve onaylayan kullanıcılar ayrılabilmelidir.

Kritik değişiklikler için çok aşamalı onay desteklenmelidir.

Kullanıcı yorumları, ekler, onay tarihleri ve karar gerekçeleri saklanmalıdır.

Onay verilmiş bir değişikliğin kritik içeriği değişirse eski onay geçersiz kılınmalıdır.

Tüm geçişler audit log'a yazılmalıdır.

## MODÜL 9 — Deployment Guard

Sorumluluk:

Yetkilendirilmiş dağıtımları kontrol etmek.

Üç çalışma modu:

**Observe Mode:** Sadece okuma ve analiz.

**Govern Mode:** Planlama, onay ve izleme; deployment mevcut yönetim araçlarından yapılır.

**Orchestrate Mode:** Açıkça yetkilendirilmiş aksiyonların desteklenen entegrasyonlar üzerinden yürütülmesi.

Orchestrate Mode varsayılan olarak kapalı olmalıdır.

Özellikler:

- Preflight checks.
- Approval verification.
- Maintenance window verification.
- Target lock / change version check.
- Pilot execution tracking.
- Health thresholds.
- Pause conditions.
- Resume approval.
- Rollback eligibility.
- Execution audit.
- Idempotency controls.
- Emergency stop.

Yazma yeteneği yalnızca kaynak platformun desteklediği güvenli API işlemleri üzerinden sağlanmalıdır.

Desteklenmeyen işlemler için sahte başarı durumu oluşturma.

Otomatik rollback yalnızca ön koşulları doğrulanmış ve açıkça desteklenen değişikliklerde kullanılmalıdır.

Firmware, BIOS, OS feature update ve geri alınması güvenilir olmayan değişikliklerde özel uyarılar bulunmalıdır.

## MODÜL 10 — Outcome Intelligence

Sorumluluk:

Değişiklik öncesi ve sonrası durumu karşılaştırmak.

İzlenecek alanlar:

- Deployment success.
- Failure count.
- Device health.
- Service health.
- Windows event anomalies.
- Network connectivity.
- GPO processing errors.
- Critical application availability.
- CPU / RAM / disk / network.
- SCCM client health.
- BigFix client health.
- Zabbix alerts.
- WFI findings.

Baseline ile karşılaştırma yapılmalıdır.

Değişiklik sonrasında görülen her hatayı otomatik olarak değişikliğe bağlama.

Zamansal ilişki ile doğrulanmış nedenselliği ayrı değerlendir.

Aşağıdaki sınıflandırmaları kullan:

- Confirmed related.
- Likely related.
- Possibly related.
- Unrelated.
- Insufficient evidence.

Değişiklik tahmini ile gerçekleşen sonuç raporlanmalıdır.

---

# 8. ENTERPRISE CONNECTOR SDK

Tüm connector'lar ortak bir SDK standardı kullanmalıdır.

Her connector için aşağıdaki işlemler tasarlanmalıdır:

- Manifest.
- GetCapabilities.
- ValidateConfiguration.
- TestConnection.
- Discover.
- FullSync.
- IncrementalSync.
- GetHealth.
- GetSyncStatus.
- NormalizeRecords.
- MapIdentities.
- ExportDiagnostics.

Yazma yetenekleri yalnızca ayrıca etkinleştirilen güvenli arayüzlerde bulunmalıdır.

Connector metadata:

- Connector ID.
- Name.
- Version.
- Provider.
- Supported source versions.
- Supported capabilities.
- Required permissions.
- Required network ports.
- Authentication type.
- Read/write classification.
- Sync strategy.
- Health status.

Connector yükleme mekanizmasını güvenli tasarla.

İmzalanmamış veya güvenilirliği doğrulanmamış üçüncü taraf eklentilerin otomatik çalıştırılmasına izin verme.

İlk sürümde dışarıdan rastgele DLL yükleyen bir plugin sistemi oluşturma.

---

# 9. İLK GELİŞTİRİLECEK CONNECTOR'LAR

## 9.1 Active Directory / GPO

Çoklu forest destekle.

Özellikler:

- Forest discovery.
- Domain discovery.
- OU hierarchy.
- Security groups.
- User/computer objects.
- GPO inventory.
- GPO links.
- Link order.
- Enforced.
- Block inheritance.
- Security filtering.
- WMI filtering.
- GPO configuration snapshots.
- GPO difference analysis.
- Effective policy evidence when available.

AD/GPO toplayıcısı Windows üzerinde ayrı servis olarak çalışabilsin.

Desteklenen Microsoft yönetim API'leri ve GPMC yöntemleri kullanılsın.

Doğrudan GPO değiştirme veya SYSVOL dosyalarına müdahale ilk sürümde bulunmasın.

## 9.2 Microsoft Configuration Manager

Desteklenen Administration Service / SMS Provider arayüzleri üzerinden güvenli veri okuma sağla.

Özellikler:

- Site discovery.
- Collections.
- Collection membership.
- Devices.
- Client state.
- Applications.
- Packages.
- Deployments.
- Deployment status.
- Distribution Points.
- Boundary Groups.
- Task Sequences.
- Software updates.
- Compliance states.

Kaynak sürümünde desteklenmeyen özellikleri capability olarak kapat.

SCCM site database'ine doğrudan yazma.

Desteklenmeyen database veya provider işlemlerini zorla kullanma.

## 9.3 HCL BigFix

REST API ve yetkilendirilmiş Session Relevance sorgularını kullan.

Özellikler:

- Computer inventory.
- Computer groups.
- Fixlets.
- Baselines.
- Actions.
- Analysis results.
- Relevance results.
- Action status.
- Failure history.

Relevance sorgularını timeout, sonuç sınırı ve performans güvenliğiyle çalıştır.

İlk sürümde yalnızca okuma yetenekleri aktif olsun.

## 9.4 Zabbix

Resmî JSON-RPC API entegrasyonu sağla.

Özellikler:

- Hosts.
- Host groups.
- Items.
- Problems.
- Triggers.
- Events.
- Historical metrics.
- Availability.
- Health indicators.

Zabbix kaynak sistemindeki yükü kontrol et.

Metrikleri gerektiğinde özetleyerek sakla.

## 9.5 BerMap

BerMap'in mevcut API'leri keşfedilebildiğinde sürümlenmiş adapter oluştur.

Özellikler:

- Asset relationships.
- Dependency topology.
- Branch mapping.
- Service dependency information.
- Related infrastructure objects.

BerImpact, BerMap olmadan da çalışmalıdır.

BerMap veritabanına doğrudan bağımlılık kurma.

## 9.6 WFI

WFI sisteminden:

- Endpoint failure events.
- Historical incidents.
- Error classifications.
- Root-cause hypotheses.
- Remediation outcomes.

alınabilmelidir.

WFI ile entegrasyon opsiyonel olsun.

## 9.7 VMware vCenter / Horizon

İlk aşamada vCenter bağlantısı; sonraki aşamada Horizon desteği.

Veriler:

- Virtual machines.
- ESXi hosts.
- Clusters.
- Datastores.
- Resource associations.
- VM power states.
- Horizon pools.
- Sessions.
- Agent versions.

Desteklenen resmî API'ler kullanılmalıdır.

## 9.8 Microsoft Intune / Entra

Bu connector tamamen opsiyoneldir.

Microsoft Graph üzerinden gerekli en düşük okuma izinleriyle çalışmalıdır.

Dış ağ erişimi olmayan ortamlarda uygulamanın çalışmasını engellememelidir.

Graph API throttling, pagination ve permission kontrolleri uygulanmalıdır.

## 9.9 Generic Integration Connector

Diğer sistemlerin bağlanabilmesi için:

- REST API.
- Webhook.
- JSON import.
- CSV import.
- Scheduled import.

desteklenmelidir.

Her veri türü şema doğrulamasından geçmelidir.

---

# 10. VERİTABANI TASARIMI

PostgreSQL üzerinde normalleştirilmiş, indekslenmiş ve migration destekli veri modeli geliştir.

Önerilen ana tablolar:

**Identity & Security**
- users
- roles
- permissions
- role_permissions
- user_role_assignments
- service_accounts

**Integrations**
- connectors
- connector_instances
- connector_capabilities
- connector_sync_jobs
- connector_sync_checkpoints
- connector_health
- integration_events

**Inventory**
- assets
- asset_identities
- asset_attributes
- asset_sources
- asset_relationships
- asset_snapshots
- asset_conflicts
- locations

**Changes**
- change_requests
- change_versions
- change_targets
- change_attachments
- change_status_history
- change_comments

**Risk**
- risk_rules
- risk_rule_versions
- risk_assessments
- risk_findings
- risk_evidence
- risk_overrides

**Simulation**
- simulations
- simulation_scenarios
- simulation_results
- simulation_comparisons

**Governance**
- approval_workflows
- approval_steps
- approval_decisions

**Deployment**
- rollout_plans
- rollout_waves
- rollout_gates
- executions
- execution_results
- rollback_plans

**Monitoring**
- health_observations
- health_baselines
- outcome_assessments

**Platform**
- audit_events
- system_settings
- background_jobs
- notifications

Zorunlu tasarım ilkeleri:

- UUID veya uygun kararlı kimlikler.
- Foreign key ilişkileri.
- Unique constraints.
- Optimistic concurrency.
- Migration versioning.
- UTC timestamp.
- Source provenance.
- Soft deletion yalnızca gerektiğinde.
- Audit için değiştirilemez olay modeli.
- Retention policies.
- Performans indeksleri.
- Büyük tablolar için partitioning stratejisi.

Değişiklik analizinde kullanılan snapshot ve kural sürümleri daha sonra aynı analizin hangi girdilerle üretildiğinin incelenebilmesi için saklanmalıdır.

Üretim tablolarında hassas verileri gereksiz yere çoğaltma.

---

# 11. API TASARIMI

REST API sürümlenmiş olmalıdır.

Başlangıç endpoint grupları:

**System**
- `/api/v1/health`
- `/api/v1/system/status`
- `/api/v1/system/capabilities`

**Integrations**
- `/api/v1/connectors`
- `/api/v1/connectors/{id}/test`
- `/api/v1/connectors/{id}/sync`
- `/api/v1/connectors/{id}/health`

**Inventory**
- `/api/v1/assets`
- `/api/v1/assets/{id}`
- `/api/v1/assets/{id}/relationships`
- `/api/v1/assets/{id}/dependencies`

**Changes**
- `/api/v1/changes`
- `/api/v1/changes/{id}`
- `/api/v1/changes/{id}/diff`
- `/api/v1/changes/{id}/targets`
- `/api/v1/changes/{id}/assessments`

**Risk**
- `/api/v1/risk/rules`
- `/api/v1/risk/assessments/{id}`
- `/api/v1/risk/findings`

**Simulation**
- `/api/v1/simulations`
- `/api/v1/simulations/{id}/run`
- `/api/v1/simulations/{id}/compare`

**Governance**
- `/api/v1/approvals`
- `/api/v1/approvals/{id}/decision`

**Rollout**
- `/api/v1/rollouts`
- `/api/v1/rollouts/{id}/waves`
- `/api/v1/rollouts/{id}/gates`

**Reports**
- `/api/v1/reports`
- `/api/v1/reports/{id}`

**Audit**
- `/api/v1/audit/events`

Bunlar başlangıç sözleşmesidir. Gerekli ek endpoint'leri tutarlı REST prensiplerine göre oluştur.

API gereksinimleri:

- OpenAPI specification.
- Request validation.
- Response schemas.
- Pagination.
- Sorting.
- Filtering.
- Consistent error responses.
- Correlation IDs.
- Rate limits.
- API authorization.
- API versioning.
- Idempotency keys for mutations.
- Background job status.
- Standardized HTTP response codes.

Uzun süren analizleri HTTP request içinde bloklayarak çalıştırma.

Job oluştur ve durumunu API üzerinden takip et.

---

# 12. ARKA PLAN İŞLEMLERİ VE PERFORMANS

Platform en az 30.000 endpoint ölçeğinde çalışacak biçimde tasarlanmalıdır.

Hedefler:

- 100.000+ asset kaydına büyüyebilen veri modeli.
- Yüz binlerce veya milyonlarca ilişki kaydı için indeksleme.
- Çoklu connector paralelliği.
- Kaynak bazlı concurrency sınırları.
- Background processing.
- Bounded queues.
- Incremental processing.
- Job cancellation.
- Job timeout.
- Checkpoint recovery.
- Backpressure.
- Duplicate job suppression.
- Cache invalidation.
- Optimized graph queries.

Kritik nokta:

Senkronizasyon işleri AD, SCCM, BigFix, Zabbix veya SQL sunucularına kontrolsüz yük bindirmemelidir.

Performans testlerinde:

- 30.000 endpoint.
- 100.000 asset.
- 1.000.000 relationship.
- Çoklu forest.
- Eşzamanlı analiz işleri.
- Geçici connector kesintileri.

senaryolarını üret.

Bu rakamlar test hedefidir; performansın başarılı olduğu ancak gerçek benchmark ile doğrulanabilir.

İlk sürümde kesin gecikme SLA'sı uydurma. Ölçümleri elde ettikten sonra kapasite ve performans hedeflerini kalibre et.

---

# 13. WEB KONSOLU VE UI/UX TASARIMI

BerImpact modern bir enterprise yönetim konsoluna sahip olmalıdır.

Tasarım dili:

- Akbank kırmızısı ve beyazı.
- Kırmızı, tasarım token'ı üzerinden yapılandırılabilir olsun.
- Açık tema varsayılan olabilir.
- Tam işlevli dark mode.
- Modern sidebar.
- Responsive dashboard.
- Profesyonel kartlar ve veri tabloları.
- Tutarlı ikon sistemi.
- Veri yoğun ekranlarda okunabilirlik.
- Erişilebilir kontrast.
- Keyboard navigation.
- Loading skeleton.
- Empty state.
- Error state.
- Retry state.
- Toast notifications.

Light ve dark mode tüm ekranlarda eksiksiz çalışmalıdır.

Tarayıcı yenilendiğinde kullanıcı tercihi korunmalıdır.

Yalnızca dekoratif bir dashboard oluşturma.

Her buton, filtre, arama kutusu, menü, form ve aksiyon gerçek backend davranışına bağlanmalıdır.

## 13.1 Dashboard

Gösterilecek alanlar:

- Total Assets.
- Active Changes.
- Pending Approvals.
- High-Risk Changes.
- Critical Findings.
- Connector Health.
- Recent Assessments.
- Rollout Status.
- Data Freshness.
- Recent Outcomes.

## 13.2 Change Center

Yeni değişiklik oluşturma sihirbazı:

1. Change Type.
2. Source Selection.
3. Configuration Difference.
4. Target Selection.
5. Impact Assessment.
6. Risk Review.
7. Pilot Plan.
8. Approval Submission.

Kullanıcı değişikliği taslak olarak kaydedebilmeli ve daha sonra devam edebilmelidir.

## 13.3 Impact Explorer

İnteraktif dependency graph tasarla.

Özellikler:

- Zoom.
- Pan.
- Search.
- Expand node.
- Collapse node.
- Asset details.
- Relationship details.
- Evidence details.
- Risk coloring.
- Dependency path visualization.
- Direct / indirect impact differentiation.
- Filtering.
- Large graph clustering.

Grafik çok büyük olduğunda tüm düğümleri aynı anda tarayıcıya yükleme.

Server-side graph expansion kullan.

## 13.4 Risk Analysis

Risk puanı, faktörleri, kurallar ve kanıtlar gösterilmeli.

Kullanıcı bir bulguya tıkladığında:

- Bulgunun sebebini.
- İlgili varlıkları.
- Kullanılan kaynakları.
- Kural sürümünü.
- Veri güncelliğini.
- Önerilen azaltıcı tedbirleri.

görebilmeli.

## 13.5 Simulation Lab

İki veya daha fazla senaryoyu karşılaştır.

Grafikler ve karşılaştırma tabloları kullan.

Değişiklik uygulamasından önce farklı hedef popülasyonları test edilebilmeli.

## 13.6 Rollout Center

Dalgalar, hedef sayıları, durumlar ve sağlık eşikleri gösterilsin.

Yazma yetkisi olmayan kullanıcılar deployment aksiyonlarını çalıştıramasın.

Observe Mode'da aksiyonların devre dışı olma nedeni açıkça gösterilsin.

## 13.7 Integration Hub

Connector listesi:

- Name.
- Status.
- Last Sync.
- Asset Count.
- Error Count.
- Capabilities.
- Version.
- Health.

Yeni connector kurulum sihirbazı sağla.

Bağlantı testi, erişim kontrolü ve hata detayları göster.

Hassas parolaları UI'da geri gösterme.

## 13.8 Administration

- User management.
- Role management.
- Permission management.
- Connector configuration.
- Risk rule configuration.
- Theme.
- Data retention.
- Backup status.
- System health.
- Audit logs.

---

# 14. KİMLİK DOĞRULAMA VE GÜVENLİK

Kurumsal güvenlik zorunludur.

Desteklenmesi gerekenler:

- AD/Kerberos entegrasyonu.
- Kurum içi OIDC alternatifi.
- Grup bazlı rol eşleme.
- Role-Based Access Control.
- Kaynak/kapsam bazlı yetkilendirme.
- Service account isolation.
- Least privilege.
- Secure session management.
- CSRF protection.
- XSS protection.
- Input validation.
- Output encoding.
- TLS.
- Certificate validation.
- Secure secret storage.
- Audit logs.
- Security headers.
- Dependency vulnerability scanning.

Başlangıç rollerini oluştur:

- Platform Administrator.
- Integration Administrator.
- Change Manager.
- Risk Analyst.
- Change Approver.
- Deployment Operator.
- Auditor.
- Read-Only User.

Risk analistinin değişiklik çalıştırma yetkisi olmamalıdır.

Kritik değişikliklerde görev ayrılığı uygulanmalıdır.

Secret saklama için işletim sistemine uygun güvenli bir mekanizma kullanılmalıdır.

Windows üzerinde DPAPI tabanlı koruma, Linux üzerinde dışarıda tutulan korumalı anahtar materyali veya kurum içi secret manager adaptörü değerlendirilebilir.

Şifreleme anahtarı ve şifreli veri aynı korunmasız konumda tutulmamalıdır.

Parolalar loglara yazılmamalıdır.

Hardcoded credential, varsayılan yönetici parolası veya güvenlik kontrollerini atlayan geliştirme kısayolları üretme.

Güvenlik açısından riskli hiçbir varsayılan yapılandırmayı production ortamına taşıma.

---

# 15. LOKAL YAPAY ZEKÂ MODÜLÜ

AI tamamen opsiyonel olacaktır.

İlk sürüm AI olmadan çalışacaktır.

İleride kurum içi lokal model entegrasyonu eklenebilmelidir.

Desteklenebilecek adaptörler:

- Ollama.
- OpenAI-compatible local inference API.
- llama.cpp tabanlı servisler.
- Diğer on-premises model sunucuları.

AI kullanım alanları:

- Risk analizinin doğal dille açıklanması.
- Teknik raporların özetlenmesi.
- Kullanıcıdan doğal dille change taslağı oluşturulması.
- Tarihsel değişikliklerin karşılaştırılması.
- Incident özetlerinin yorumlanması.
- Risk azaltma önerileri.

AI modelinin görevi kritik kararların tek başına sahibi olmak değildir.

AI:

- Yetki kontrolünü geçersiz kılamaz.
- Doğrudan deployment çalıştıramaz.
- Risk skorunu kanıtsız değiştiremez.
- Kaynak sistem kayıtlarını değiştiremez.
- Kayıp bilgileri uyduramaz.

AI önerileri ve doğrulanmış sistem bulguları UI'da açıkça ayrılmalıdır.

Prompt injection ve güvenilmeyen connector verilerine karşı güvenlik önlemleri uygulanmalıdır.

AI devre dışı kalırsa çekirdek sistem çalışmaya devam etmelidir.

---

# 16. RAPORLAMA

Raporlar en az aşağıdaki formatlarda üretilebilmeli:

- PDF.
- HTML.
- CSV.
- JSON.

Rapor türleri:

- Change Impact Report.
- Risk Assessment Report.
- Affected Assets Report.
- Dependency Analysis Report.
- Pilot Recommendation Report.
- Deployment Outcome Report.
- Before/After Comparison.
- Audit Report.
- Connector Health Report.

Rapor içeriğinde:

- Change metadata.
- Analysis timestamp.
- Snapshot ID.
- Risk ruleset version.
- Risk score.
- Confidence.
- Affected assets.
- Critical findings.
- Data limitations.
- Recommended mitigations.
- Approval history.
- Execution results when available.

bulunmalıdır.

Raporların tekrar üretilebilir ve denetlenebilir olması sağlanmalıdır.

---

# 17. KURULUM VE OPERASYON

Üç dağıtım mimarisi tasarla.

## A — Docker Compose

Linux üzerinde:

- Frontend.
- Backend API.
- Background Worker.
- PostgreSQL.
- Reverse proxy.

Bileşenlerini çalıştırabilen deployment oluştur.

Windows-only AD/GPO collector gerektiğinde ayrı Windows sunucusunda çalışabilmelidir.

Persistent volumes, health checks, restart policies ve secret handling uygulanmalıdır.

## B — Windows Server

Windows Server üzerinde:

- BerImpact API Service.
- BerImpact Worker Service.
- Windows Integration Collector.
- Web frontend.
- Ayrı PostgreSQL bağlantısı.

Desteklenmelidir.

Servisler otomatik başlatılabilmelidir.

Kurulum, güncelleme ve kaldırma işlemleri belgelenmelidir.

## C — High Availability

İleride:

- Multiple API instances.
- Multiple Worker instances.
- Load balancer.
- PostgreSQL HA.
- Failover.
- Distributed job coordination.
- Backup / restore.

desteklenebilmelidir.

İlk sürümün kurulumu karmaşık olmamalıdır.

## Kurulum Gereksinimleri

- Setup wizard.
- Prerequisite check.
- Database connection test.
- TLS configuration.
- Initial administrator setup.
- Connector onboarding.
- Database migrations.
- Offline installation support.
- Upgrade procedure.
- Backup.
- Restore.
- Health verification.
- Diagnostic package generation.

Kurulum sonunda uygulama gerçekten ayağa kalkmalı ve health checks başarılı olmalıdır.

---

# 18. GELİŞTİRME VE TEST STANDARTLARI

Kod üretirken:

- Clean code.
- SOLID.
- Dependency injection.
- Typed contracts.
- Async/await.
- CancellationToken.
- Structured logging.
- Exception handling.
- Retry policies.
- Resource cleanup.
- Maintainable architecture.
- Secure defaults.

kullan.

Zorunlu test kategorileri:

**Unit Tests**
- Risk score calculation.
- Risk rule evaluation.
- Target resolution.
- Identity matching.
- Dependency traversal.
- Change state transitions.
- Approval logic.

**Integration Tests**
- PostgreSQL migrations.
- API endpoints.
- Connector contract tests.
- Background jobs.
- Authentication / authorization.
- Data synchronization.

**End-to-End Tests**
- Login.
- Dashboard.
- Connector setup.
- Change creation.
- Target selection.
- Risk assessment.
- Simulation comparison.
- Approval.
- Report generation.

**Security Tests**
- Unauthorized access.
- Privilege escalation.
- Invalid input.
- Secret leakage.
- Audit integrity.
- Session protection.
- Unsafe write prevention.

**Performance Tests**
- Large inventory.
- Multiple concurrent analyses.
- Large dependency graphs.
- Connector timeouts.
- Queue recovery.
- Database load.

Test için mock connector'lar oluşturabilirsin.

Ancak test/mock verileri ile gerçek production verilerini kesin biçimde ayır.

Production dashboard'unda demo verisini canlı sistem bilgisi gibi gösterme.

---

# 19. ZORUNLU REFERANS SENARYOLARI

Sistemin gerçekten işe yaradığını doğrulamak için aşağıdaki senaryoları kullan.

## Senaryo 1 — Windows 11 25H2 Security Baseline

Bir GPO güvenlik ayarı değiştiriliyor.

BerImpact:

- Değişikliği tanımlamalı.
- İlgili GPO'yu belirlemeli.
- GPO linklerini analiz etmeli.
- Hedef bilgisayarları tespit etmeli.
- Olası policy çakışmalarını belirlemeli.
- Kritik cihazları işaretlemeli.
- Risk puanı oluşturmalı.
- Pilot önerisi sunmalı.

## Senaryo 2 — NIC Registry Değişikliği

`TCPChecksumOffloadIPv4` yapılandırması değiştirilecek.

BerImpact:

- Hedef bilgisayar grubunu analiz etmeli.
- NIC modellerine göre sınıflandırmalı.
- Windows sürümlerini karşılaştırmalı.
- Geçmiş sorun kayıtlarını ilişkilendirebilmeli.
- Farklı hedefleme senaryolarını karşılaştırmalı.
- Etkilenen bilgisayarları ve belirsiz cihazları ayırmalı.

## Senaryo 3 — SCCM Feature Update

Windows 11 feature update deployment planlanıyor.

BerImpact:

- Collection üyelerini çözmeli.
- Cihaz OS ve donanım bilgilerini değerlendirmeli.
- Önceki deployment failure verilerini kullanmalı.
- Offline cihazları işaretlemeli.
- Pilot grupları önermeli.
- Başarı kriterlerini tanımlamalı.

## Senaryo 4 — SCCM Distribution Point Maintenance

Bir DP sunucusunda bakım yapılacak.

BerImpact:

- İlgili boundary group ilişkilerini değerlendirmeli.
- Olası istemci etki alanını belirlemeli.
- Alternatif DP erişimini incelemeli.
- Aktif deployment'larla ilişkileri göstermeli.
- Bakım riskini hesaplamalı.
- Alternatif doğrulama kontrolleri önermeli.

## Senaryo 5 — VMware Horizon Agent Upgrade

VDI agent sürümü değiştirilecek.

BerImpact:

- İlgili pool ve VM'leri belirlemeli.
- Agent ve OS sürümlerini karşılaştırmalı.
- Host/cluster bağımlılıklarını değerlendirmeli.
- Geçmiş görüntü veya bağlantı sorunları mevcutsa ilişkilendirmeli.
- Pilot VDI grubu önermeli.

## Senaryo 6 — GPO Çakışması

Aynı ayar farklı GPO'lar üzerinden uygulanıyor.

BerImpact:

- Link order.
- Enforced.
- Block inheritance.
- Security filtering.
- WMI filtering.
- Loopback processing.

bilgilerini gerektiği şekilde değerlendirmeli.

Kesin hesaplanamayan sonucu açıkça belirtmeli.

## Senaryo 7 — Connector Kesintisi

SCCM connector bağlantısı kaybedildi.

Beklenen davranış:

- BerImpact çalışmaya devam eder.
- Connector unhealthy görünür.
- Eski veri silinmez.
- Veri stale olarak işaretlenir.
- Risk analizinde güven seviyesi etkilenir.
- Diğer connector'lar çalışmaya devam eder.

---

# 20. GELİŞTİRME AŞAMALARI

Projeyi aşağıdaki milestone'lara böl.

## PHASE 1 — Foundation

- Repository.
- Backend solution.
- Frontend.
- PostgreSQL.
- Migrations.
- Authentication.
- RBAC.
- Logging.
- Health checks.
- Docker Compose.
- Temel UI.

**Çıktı:** Kurulup giriş yapılabilen çalışan uygulama.

## PHASE 2 — Inventory & Connectors

- Connector SDK.
- AD/GPO connector.
- SCCM connector.
- BigFix connector.
- Zabbix connector.
- Asset normalization.
- Identity resolution.
- Inventory UI.
- Connector Health UI.

**Çıktı:** Gerçek veya test connector'larıyla doğrulanmış envanter yönetimi.

## PHASE 3 — Change & Impact

- Change Center.
- Target Resolver.
- Dependency Engine.
- Change diff.
- Asset relationship visualization.
- Risk Engine.
- Risk Analysis UI.

**Çıktı:** Bir değişikliğin gerçek verilere dayalı etki analizinin oluşturulması.

## PHASE 4 — Simulation & Reporting

- What-If Simulator.
- Scenario comparison.
- Pilot Planner.
- Risk reports.
- PDF/HTML export.
- Snapshot tracking.

**Çıktı:** Değişiklik öncesi analiz raporu ve pilot planı.

## PHASE 5 — Governance & Integration

- Approval workflows.
- BerMap integration.
- WFI integration.
- vCenter integration.
- Audit enhancements.
- Change history.
- Outcome Intelligence.

**Çıktı:** Değişiklik yaşam döngüsünü yöneten genişletilmiş platform.

## PHASE 6 — Controlled Orchestration

- Govern Mode.
- Rollout monitoring.
- Health gates.
- Explicit write permissions.
- Approval-bound execution.
- Deployment connectors.
- Safe pause/resume.
- Supported rollback operations.

**Çıktı:** Sadece yetkilendirilmiş kapsamda kontrollü dağıtım yönetimi.

## PHASE 7 — Enterprise Hardening

- Security review.
- Performance tuning.
- HA preparation.
- Backup/restore validation.
- Offline installation testing.
- Documentation completion.
- Release packaging.

**Çıktı:** Üretim ortamında değerlendirmeye hazır sürüm.

Her phase sonunda:

1. Build başarılı olmalı.
2. Testler çalışmalı.
3. Migration doğrulanmalı.
4. API erişilebilir olmalı.
5. UI çalışmalı.
6. Başarısız veya eksik özellikler belirtilmeli.
7. Değişiklikler dokümante edilmeli.

Bir phase tamamlanmadan tamamlanmış gibi rapor verme.

---

# 21. DOKÜMANTASYON

Aşağıdaki dokümanları üret ve güncel tut:

- README.md
- ARCHITECTURE.md
- DATABASE_SCHEMA.md
- API_REFERENCE.md
- CONNECTOR_SDK.md
- SECURITY.md
- DEPLOYMENT.md
- OFFLINE_INSTALLATION.md
- WINDOWS_INSTALLATION.md
- BACKUP_RESTORE.md
- TROUBLESHOOTING.md
- DEVELOPMENT.md
- TESTING.md
- CHANGELOG.md
- ROADMAP.md

Ayrıca Mermaid ile aşağıdaki diyagramları oluştur:

- System architecture.
- Connector data flow.
- Change lifecycle.
- Risk assessment pipeline.
- Dependency graph model.
- Deployment approval workflow.
- Database entity relationships.

Dokümanlar gerçek kod ve desteklenen özelliklerle uyumlu olmalıdır.

Uygulanmamış özellikleri uygulanmış gibi dokümante etme.

---

# 22. KALİTE VE KABUL KRİTERLERİ

BerImpact aşağıdaki şartları sağladığında ilk kullanılabilir sürüm kabul edilebilir.

- Tamamen on-premises çalışıyor.
- İnternet olmadan çekirdek fonksiyonlar çalışıyor.
- PostgreSQL bağlantısı ve migration'lar başarılı.
- Kullanıcı authentication ve RBAC çalışıyor.
- En az bir gerçek kurumsal connector üzerinden envanter alınabiliyor.
- Birden fazla kaynak sistem verisi normalize edilebiliyor.
- Bir change request oluşturulabiliyor.
- Hedef kapsam analizi yapılabiliyor.
- Risk puanı ve açıklanabilir bulgular oluşturulabiliyor.
- Eksik ve güncel olmayan veriler doğru işaretleniyor.
- En az iki What-If senaryosu karşılaştırılabiliyor.
- Pilot önerisi üretilebiliyor.
- Analiz raporu dışarı aktarılabiliyor.
- UI light ve dark mode ile çalışıyor.
- Kritik API'lerde authorization kontrolü bulunuyor.
- Audit log oluşuyor.
- Connector kesintisi tüm uygulamayı durdurmuyor.
- Read-only mod hiçbir yönetim sisteminde değişiklik yapmıyor.
- Unit, integration ve E2E testleri çalıştırılmış ve sonuçları raporlanmış.

Tamamlanan testlerin gerçek sonuçlarını göster.

Desteklenmeyen, doğrulanmayan veya eksik kalan alanları açıkça belirt.

---

# 23. GELİŞTİRME AJANINA ÇALIŞMA TALİMATLARI

Aşağıdaki iş akışını takip et.

**Birinci adım:** Repository ve çalışma ortamını incele. Var olan teknoloji, kod ve test altyapısını belirle.

**İkinci adım:** Bu gereksinimlere uygun mimari karar kaydı oluştur. Teknik bağımlılıkları, güvenlik sınırlarını ve uygulanabilirlik risklerini değerlendir.

**Üçüncü adım:** Uygulanabilir milestone ve task planını oluştur. Bağımlılık sırasına göre görevleri düzenle.

**Dördüncü adım:** Foundation kodunu üret. Uygulamayı ayağa kaldır, veritabanını oluştur ve ilk API/UI entegrasyonunu test et.

**Beşinci adım:** Connector SDK ve temel envanter modelini geliştir. Gerçek kaynak erişimi bulunmuyorsa sözleşme testleri ve açıkça işaretlenmiş test adaptörleri kullan.

**Altıncı adım:** Change Center, Target Resolver, Dependency Engine ve Risk Engine'i uçtan uca geliştir.

**Yedinci adım:** Simülasyon, pilot planlama, raporlama ve açıklanabilir analiz ekranlarını tamamla.

**Sekizinci adım:** Diğer connector'ları, yönetişim özelliklerini ve sonraki milestone'ları uygula.

**Dokuzuncu adım:** Güvenlik, performans, offline kurulum ve operasyonel testleri gerçekleştir.

**Onuncu adım:** Üretim dağıtım paketleri ve kullanım dokümantasyonunu tamamla.

Her aşamada gerekli build, test ve doğrulama komutlarını çalıştır.

Çalıştırılamayan komutları çalıştırılmış gibi gösterme.

Gerçek sistem erişimi yoksa bağlantı sağlanmış gibi davranma.

Repository'de var olmayan dosyaları değiştirdiğini iddia etme.

Kullanıcı açıkça yetkilendirmedikçe production sistemlerine bağlanma ve değişiklik uygulama.

Yaptığın değişiklikleri somut dosyalar, test çıktıları ve gözlemlenebilir sonuçlarla raporla.

Geliştirme ortamı uygun değilse uygulanabilir kodu ve testleri üret, engelleri açıkça belirt.

---

# 24. EK MÜHENDİSLİK KURALLARI

Bu ilkeler bütün projede korunmalıdır:

1. **No Fake Functionality:** Çalışıyor gibi görünen sahte özellik üretme.
2. **No Hidden Cloud Dependencies:** Çekirdek sistem gizli bulut bağımlılığı taşıyamaz.
3. **No Unnecessary Agents:** İlk sürümde endpoint agent zorunlu değildir.
4. **No Blind Automation:** Riskli işlemler kanıtsız ve onaysız çalıştırılamaz.
5. **No Unexplained Risk Scores:** Her risk hesabı açıklanabilir olmalıdır.
6. **No Silent Data Loss:** Senkronizasyon hataları eski verileri sessizce silmemelidir.
7. **No Hardcoded Secrets:** Gizli bilgiler kaynak koda gömülemez.
8. **No Vendor Lock-In:** Açık standartlar ve modüler adaptörler kullanılmalıdır.
9. **No Unbounded Queries:** Büyük sorgular performans sınırlarına sahip olmalıdır.
10. **No Single Integration Dependency:** Bir connector'ın kesilmesi bütün platformu etkilememelidir.
11. **No Unverified Claims:** Test edilmeyen özellikler çalışır olarak raporlanamaz.
12. **No Unauthorized Production Writes:** Gerçek sistemler üzerinde onaysız değişiklik yapılmaz.

Kod ve mimari kararlarında güvenilirlik, güvenlik, sürdürülebilirlik ve gerçek kurumsal kullanım önceliklidir.

---

# 25. NİHAİ HEDEF

BerImpact, kurumsal BT yöneticisinin bir değişiklik yapmadan önce açacağı ilk konsollardan biri olmalıdır.

Bir yönetici:

1. BerImpact'e giriş yapar.
2. Yapmak istediği değişikliği seçer veya tanımlar.
3. Hangi sistemlerin etkileneceğini görür.
4. Risk seviyesini ve kanıtlarını inceler.
5. Alternatif hedefleme senaryolarını karşılaştırır.
6. Güvenli pilot planını oluşturur.
7. Gerekli onayları tamamlar.
8. İzin verilen çalışma modunda değişikliği yönetir veya mevcut araçlarla uygular.
9. Değişiklik sonrası sonuçları izler.
10. Tahmin ile gerçek sonucu karşılaştıran rapor üretir.

Platformun en önemli özelliği, bütün bunları yaparken mevcut sistemlerin çalışmasını bozmaması ve güvenli varsayılanlara sahip olmasıdır.

**BerImpact yalnızca değişiklikleri izleyen değil, değişikliklerin teknik ve operasyonel etkisini öngörmeye çalışan, analizlerini kanıtlarla açıklayan ve BT değişiklik süreçlerini güvenli hâle getiren bir Enterprise Intelligence ürünüdür.**

---

# 26. ŞİMDİ BAŞLA

Bu doküman proje için ana geliştirme gereksinimidir.

Öncelikle mevcut repository'yi ve çalışma ortamını analiz et.

Ardından:

- Nihai mimariyi netleştir.
- Proje iskeletini oluştur.
- Veritabanı tasarımını uygula.
- Backend ve frontend temelini geliştir.
- Authentication, authorization ve audit altyapısını kur.
- Connector SDK'yı oluştur.
- İlk çalışan uçtan uca akışı gerçekleştir.
- Build ve test sonuçlarını doğrula.
- Sonraki geliştirme aşamasına geç.

Yalnızca uzun bir plan hazırlayıp durma.

**Gerçek uygulamayı geliştir. Her kilometre taşını çalışan kod, doğrulanabilir test ve güncel dokümantasyonla tamamla.**

BerImpact'in ilk sürümü profesyonel bir kurumsal ürünün sağlam temeli olmalı; sonraki sürümlerinde çoklu connector, gelişmiş risk analizi ve kontrollü deployment yetenekleri güvenli biçimde genişletilebilmelidir.

**PROJE ADI: BERIMPACT**

**MİMARİ: ON-PREMISES, MODULAR, SECURE, SCALABLE**

**ÖNCELİK: FUNCTIONALITY + RELIABILITY + SECURITY + EXPLAINABILITY**

**HEDEF: PRODUCTION-QUALITY ENTERPRISE SOFTWARE**
