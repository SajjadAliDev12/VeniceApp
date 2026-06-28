# 🍦 Venice Sweets POS & Management System

![Project Status](https://img.shields.io/badge/Status-Active_Development-green)
![.NET](https://img.shields.io/badge/.NET-WPF-512BD4?logo=dotnet)
![Language](https://img.shields.io/badge/Language-C%23-239120?logo=csharp)
![Database](https://img.shields.io/badge/Database-SQL_Server-CC2927?logo=microsoft-sql-server)
![ORM](https://img.shields.io/badge/ORM-Entity_Framework_Core-512BD4)

**Venice Sweets** is a modern Point of Sale (POS) and management system designed specifically for sweets and ice cream shops. Built to deliver speed, stability, and real-time synchronization between the cashier and the kitchen.

---

## ✨ Overview

Venice Sweets POS provides a complete solution for managing daily sales operations in a retail food environment. It supports multiple workstations connected to the same database and ensures smooth, consistent behavior across all devices.

The application follows a clean **first-run setup process**, allowing each installation to be configured dynamically without hardcoded data.

---

## 🚀 Key Features

* **🛒 Order Management**
    * Fast and intuitive interface for processing dine-in and takeaway orders.
* **👨‍🍳 Kitchen Display System (KDS)**
    * A dedicated real-time, read-only kitchen screen for tracking incoming orders.
* **👥 User & Role Management**
    * Secure administration panel for managing users and permissions.
* **📜 Menu Management**
    * Full control over product categories, items, pricing, and availability.
* **📊 Sales Reports**
    * Daily, monthly, and yearly sales reports for business insights.
* **💾 Hybrid Database Architecture**
    * Local SQL Server for reliable on-premise performance.
    * Azure SQL support for optional cloud-based accessibility.

---

## 🛠️ Technology Stack

| Component | Technology |
| :--- | :--- |
| **Language** | C# |
| **Framework** | .NET (WPF) |
| **ORM** | Entity Framework Core |
| **Database** | Microsoft SQL Server |

---

## ⚙️ Setup & First Run

### 1️⃣ Clone the Repository
```bash
git clone [https://github.com/SajjadAliDev12/VeniceApp.git](https://github.com/SajjadAliDev12/VeniceApp.git)
````

## 2️⃣ Build and Run
Open the solution in Visual Studio.

Build the project to restore dependencies.

Run the application.

## 3️⃣ First Run Configuration
On first launch, the application will guide you through a setup wizard:

Configure the SQL Server connection.

Automatically create the database tables using EF Core migrations.

Create the Administrator account.

⚠️ Note: No default users, passwords, or system settings are included. All required data is created securely during the first-run setup.

## 4️⃣ Start Using the System
After completing the setup process, the system is ready for daily operations.

---

## 📌 Notes
The project contains no hardcoded credentials or seed data.

All system settings are stored dynamically after the first run.

Designed to operate reliably across multiple devices sharing the same database.

---

## 🚧 Project Status
Under Active Development Features, performance, and usability improvements are continuously in progress.

---

## 📸 Screenshots
<img width="1919" height="1079" alt="Screenshot 2026-06-13 202417" src="https://github.com/user-attachments/assets/6661280b-97ff-41ee-a6fc-3101e0db76a7" />
<img width="1919" height="1079" alt="Screenshot 2026-06-13 202453" src="https://github.com/user-attachments/assets/560ad98e-af92-42df-a043-f0f36f408673" />
<img width="1919" height="1079" alt="Screenshot 2026-06-13 202507" src="https://github.com/user-attachments/assets/f342bda2-1769-4924-8f32-47497bb95d66" />
<img width="1919" height="1079" alt="Screenshot 2026-06-13 202514" src="https://github.com/user-attachments/assets/7be12f5c-de5f-47f6-9e94-816f3c05ba33" />
<img width="1919" height="1079" alt="Screenshot 2026-06-13 202523" src="https://github.com/user-attachments/assets/408e34e0-fbae-4291-9f2d-7075968dcaa7" />
<img width="1919" height="1079" alt="Screenshot 2026-06-13 202528" src="https://github.com/user-attachments/assets/d410cb4c-ab6f-42c7-a08b-9001785a10b5" />
<img width="1834" height="1079" alt="Screenshot 2026-06-13 202555" src="https://github.com/user-attachments/assets/32f9c880-999a-4cc3-8b21-fcae5053c24f" />
<img width="1919" height="1079" alt="Screenshot 2026-06-13 202606" src="https://github.com/user-attachments/assets/5e06d4fb-eb1f-4eff-b90d-6f4b5aadad37" />
<img width="804" height="753" alt="Screenshot 2026-06-13 202641" src="https://github.com/user-attachments/assets/f36c1a1c-5564-456b-bf52-531aec89ad13" />
<img width="812" height="757" alt="Screenshot 2026-06-13 202703" src="https://github.com/user-attachments/assets/5edeaddd-4c15-4a74-a0bd-46a085e558cb" />

---

## 📄 License
This project is currently provided for educational and demonstration purposes. Licensing and distribution terms may be updated in future releases.
