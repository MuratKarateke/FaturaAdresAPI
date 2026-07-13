# FaturaAdresAPI
Öne Çıkan Özellikler
PDF İşleme: PdfPig kütüphanesi ile fatura içindeki metinlerin hatasız okunması.

Akıllı Adres Ayrıştırma: Regex (Düzenli İfadeler) ile fatura metni içinden ilçe adının cımbızla çekilmesi.

Veritabanı Entegrasyonu: Entity Framework Core kullanılarak SQL Server ile tam uyumlu veri akışı.

Şube Atama Mantığı: Veritabanındaki TBL_HIZMETBOLGELERI tablosu ile ilçeye özel şube atama algoritması.

Güvenli Kod: Null Reference kontrolleri ile hatasız ve kararlı çalışma yapısı.

Kullanılan Teknolojiler
.NET 8 / C#

Entity Framework Core (ORM)

Microsoft SQL Server

PdfPig (PDF Parsing)

Swagger (API Dokümantasyonu)

Nasıl Çalışır?
Kullanıcı, POST /api/Fatura/yukle endpoint'ine PDF faturasını gönderir.

API, faturanın tüm metnini okur.

Regex kullanarak metin içindeki ilçe adını tespit eder.

Veritabanından o ilçeye hizmet veren şubeyi sorgular.

Fatura bilgilerini TBL_FATURALAR tablosuna ŞubeID ile birlikte kalıcı olarak kaydeder.

İletişim & Geliştirici
Geliştirici: [Murat Karateke]

GitHub: [https://github.com/MuratKarateke]
