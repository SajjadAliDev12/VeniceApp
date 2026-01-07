# Venice Sweets POS & Management System

A comprehensive Point of Sale (POS) and management solution designed for a sweets and ice cream shop. This system streamlines the workflow between the cashier and the kitchen using a synchronized architecture.

## 🚀 Key Features
* **Order Management:** Streamlined interface for cashiers to process orders quickly.
* **Kitchen Display System (KDS):** A dedicated real-time read-only screen for the kitchen staff to view and track incoming orders instantly.
* **Hybrid Database Architecture:** Utilizes local SQL Server for stability and supports Azure SQL for cloud data accessibility.
* **Menu Management:** Full control over categories, items, and pricing.

## 🛠️ Technology Stack
* **Language:** C#
* **Framework:** .NET (WPF) for modern UI/UX.
* **ORM:** Entity Framework Core.
* **Database:** Microsoft SQL Server.

## ⚙️ Setup & Installation
1.  **Clone the repository:**
    ```bash
    git clone [https://github.com/SajjadAliDev12/VeniceApp.git]
    ```
2.  **Database Setup:**
    * Locate the `VeniceDataBaseSetup.sql` file in the root folder.
    * Open SQL Server Management Studio (SSMS) and Create New DataBase With The Name `VinceSweetsDB`.
    * run the script to generate the database and tables.
3.  **Configuration:**
    * Open `appsettings.json` and update the `connectionString` to match your local SQL Server instance name.
4.  **Run:**
    * Open the solution in Visual Studio and click **Start**.

    Notice : Printing Module Currently simulates the action with a user alert. Full implementation is on hold pending specific thermal printer drivers and hardware integration.

# 🚧 Project Status: Under Development

## 📸 Screenshots
<img width="1919" height="1079" alt="Screenshot 2026-01-06 160826" src="https://github.com/user-attachments/assets/96b328de-a0e2-47e2-809c-e0fcd514d8b0" />
<img width="1919" height="1079" alt="Screenshot 2026-01-06 160925" src="https://github.com/user-attachments/assets/ed6b8037-f9bc-4b3e-be28-aa0e4b0c2da1" />
<img width="1918" height="1079" alt="Screenshot 2026-01-06 160911" src="https://github.com/user-attachments/assets/6edd554b-b828-4f6b-984e-517591ce79b7" />
<img width="1919" height="1079" alt="Screenshot 2026-01-06 160904" src="https://github.com/user-attachments/assets/c37c055f-b329-4541-998d-f62d6d1ab20a" />
<img width="1919" height="1079" alt="Screenshot 2026-01-06 160836" src="https://github.com/user-attachments/assets/782c2cce-075c-4af9-b30c-628fe76829c1" />
