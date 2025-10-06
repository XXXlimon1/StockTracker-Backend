# 🔧 StockTracker Backend API

Borsa İstanbul (BIST) portföy takip uygulaması için RESTful API servisi.

![.NET](https://img.shields.io/badge/.NET-6.0-purple)
![C#](https://img.shields.io/badge/C%23-10.0-blue)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-blue)
![License](https://img.shields.io/badge/License-MIT-yellow)

## 📋 İçindekiler

- [Proje Hakkında](#proje-hakkında)
- [Özellikler](#özellikler)
- [Teknolojiler](#teknolojiler)
- [Kurulum](#kurulum)
- [API Dokümantasyonu](#api-dokümantasyonu)
- [Veritabanı Şeması](#veritabanı-şeması)
- [Proje Yapısı](#proje-yapısı)
- [Deployment](#deployment)
- [Katkıda Bulunma](#katkıda-bulunma)
- [Lisans](#lisans)

## 🎯 Proje Hakkında

StockTracker Backend API, Android mobil uygulaması için RESTful servisler sunan .NET Core tabanlı bir backend uygulamasıdır. Kullanıcı yönetimi, portföy takibi, teknik analiz hesaplamaları ve bildirim servisleri sağlar.

### 🌟 Ana Özellikler

- **RESTful API**: Standart HTTP metodları ve status kodları
- **JWT Authentication**: Güvenli kullanıcı kimlik doğrulaması
- **Entity Framework Core**: ORM ile veritabanı işlemleri
- **PostgreSQL**: Üretim ortamı veritabanı
- **Swagger UI**: Otomatik API dokümantasyonu
- **Background Services**: Fiyat güncellemeleri ve uyarı kontrolü

## ✨ Özellikler

### Kullanıcı Yönetimi
- ✅ Kullanıcı kaydı (email, şifre)
- ✅ Giriş (JWT token)
- ✅ Şifre hash'leme (BCrypt)
- ✅ Token yenileme

### Portföy İşlemleri
- ✅ Hisse ekleme, güncelleme, silme
- ✅ Kullanıcı portföyünü listeleme
- ✅ Kâr/zarar hesaplama
- ✅ İşlem geçmişi

### Fiyat Servisleri
- ✅ Yahoo Finance API entegrasyonu
- ✅ Yapı Kredi BIST Indices entegrasyonu
- ✅ Güncel fiyat sorgulama
- ✅ Historik veri çekme
- ✅ Cache mekanizması

### Teknik Analiz
- ✅ RSI (Relative Strength Index) hesaplama
- ✅ MACD (Moving Average Convergence Divergence)
- ✅ Bollinger Bands
- ✅ SMA/EMA (Moving Averages)

### Uyarı Sistemi
- ✅ Fiyat bazlı uyarılar
- ✅ Teknik gösterge bazlı uyarılar
- ✅ Arka plan uyarı kontrolü
- ✅ Firebase push notification entegrasyonu

## 🛠️ Teknolojiler

### Framework & Runtime
- **.NET 6.0** - Ana framework
- **C# 10.0** - Programlama dili
- **ASP.NET Core Web API** - Web servisleri

### Database
- **PostgreSQL 15** - Production database (Neon/Supabase)
- **SQL Server Express** - Development database
- **Entity Framework Core 7.0** - ORM

### Authentication & Security
- **JWT Bearer** - Token-based authentication
- **BCrypt.Net** - Password hashing
- **HTTPS/TLS** - Güvenli iletişim

### External Services
- **Yahoo Finance API** - Hisse fiyatları
- **Yapı Kredi API** - BIST endeksleri
- **Firebase Admin SDK** - Push notifications

### Tools & Libraries
- **Swagger/OpenAPI** - API dokümantasyonu
- **Serilog** - Logging
- **AutoMapper** - Object mapping
- **FluentValidation** - Input validation
- **Hangfire** - Background jobs (opsiyonel)

## 🚀 Kurulum

### Gereksinimler

- .NET 6.0 SDK veya üzeri
- PostgreSQL 15 veya SQL Server 2019+
- Visual Studio 2022 / VS Code / Rider
- Postman (API test için)

### Adımlar

1. **Projeyi klonlayın**
```bash
