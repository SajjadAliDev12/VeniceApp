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
(Screenshots of the POS interface, kitchen display, and management panels will be added here)

---

## 📄 License
This project is currently provided for educational and demonstration purposes. Licensing and distribution terms may be updated in future releases.
