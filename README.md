# 🏢 EMS - Employee Management System (Backend API)

A secure, role-based RESTful API built with **.NET 10**, following **Clean Architecture** with **Two-Factor Authentication (OTP)** support.

---

## 🚀 Project Overview

This project is a fully functional **Employee Management System** backend. It handles employee data, authentication, authorization, and secure login flows for different user roles within an organization.

**Why I built this:** To showcase my ability to build enterprise-grade backend systems using modern .NET practices, including JWT Authentication, Repository Pattern, Global Exception Handling, and Two-Factor Authentication (OTP).

---

## ✨ Key Features

### 🔐 Authentication & Security

- **JWT Authentication** with Access Tokens (15 mins) and Refresh Tokens (7 days)
- **Two-Factor Authentication (OTP)** via Email (Brevo API) for Admin/Manager logins
- **Role-Based Access Control** for Admin, Manager, and Employee
- **Secure Password Hashing** using BCrypt.Net
- **Refresh Token Rotation** for enhanced security
- **Account Activation** — User must verify OTP to activate account (`IsActive = true`)

### 🏗️ Architecture & Design

- **Clean Architecture** with separate layers: Core, Infrastructure, Services, and API
- **Repository & Service Patterns** for decoupled and testable code
- **Global Exception Handling** with custom middleware for standardized JSON error responses
- **AutoMapper** for automatic DTO-Entity mapping
- **Dual Database Support** — In-Memory (Development) & SQL Server (Production)

### 📊 Employee Management

- **CRUD Operations** for Employees (Admin/Manager only)
- **Pagination, Search, Sorting, and Filtering** on Employee List
- **Soft Delete** (IsActive Flag) for employee records
- **Employee Registration** with automatic User creation
- **Promote Employee to Manager**

### 📧 OTP (Two-Factor Authentication)

- OTP sent via **Email** using Brevo REST API (Free Tier — 300 emails/day)
- OTP valid for **5 minutes**
- One active OTP per user (previous gets invalidated on resend)
- OTP verified and **immediately deleted** from database
- **Resend OTP** functionality
- **Automatic Expired OTP Cleanup** — Expired OTPs are deleted before new OTP generation

---

## 🛠️ Tech Stack

| Technology | Purpose |
| ------------ | --------- |
| .NET 10 | Runtime & Framework |
| C# | Primary Language |
| Entity Framework Core | ORM for database operations |
| SQL Server / LocalDB | Relational Database (Production) |
| In-Memory Database | Development & Demo |
| JWT | Authentication & Authorization |
| BCrypt.Net | Password Hashing |
| AutoMapper | Object-Object Mapping |
| Brevo API (formerly Sendinblue) | Email Sending (OTP) |
| Swagger / OpenAPI | API Documentation & Testing |
| Git & GitHub | Version Control |

---

## 🧱 Architecture

This project follows **Clean Architecture** to keep the code decoupled and maintainable.

Client (Postman / Swagger / Angular)
↓
[API Layer] (Controllers, Middleware)
↓
[Service Layer] (Business Logic + OTP Service)
↓
[Repository Layer] (Data Access - EF Core)
↓
[Database] (SQL Server / In-Memory)

### Project Structure

- **EMS.Core** – Entities, DTOs, Interfaces, Enums, and Common Utilities (No external dependencies)
- **EMS.Infrastructure** – DbContext, Repository Implementations, and External Services (EmailService)
- **EMS.Services** – Business Logic, AutoMapper Profiles, and OTP Service
- **EMS.API** – Controllers, Middleware, and Program.cs (Startup)

---

## 🔐 Authentication Flow

### 1. Registration (Employee)

1. Employee registers with `FirstName, LastName, Email, Password, PhoneNumber, DateOfBirth, Address, PreviousCompanyRole`
2. Account is created with `IsActive = false`
3. **OTP is sent to registered email** for verification
4. After OTP verification, account is activated (`IsActive = true`) and JWT tokens are issued

### 2. Login (Employee)

1. Employee logs in with `Email, PhoneNumber, Password`
2. Directly gets Access Token + Refresh Token (No OTP required)

### 3. Login (Admin / Manager)

1. Admin/Manager logs in with `Email, PhoneNumber, Password`
2. **OTP is sent to registered email** (Additional security layer)
3. OTP verification required to get Access Token + Refresh Token

### 4. Token Refresh

- Use Refresh Token to get new Access Token (15 mins expiry)
- No OTP required for token refresh

### 5. Logout

- Revoke the Refresh Token to invalidate the session

---

## 📋 API Endpoints

### Authentication (Public)

| Method | Endpoint | Description |
| -------- | ---------- | ------------- |
| POST | `/api/auth/register` | Register a new Employee (OTP sent to email) |
| POST | `/api/auth/login` | Employee Login (No OTP) |
| POST | `/api/auth/refresh` | Refresh the Access Token |
| POST | `/api/auth/revoke` | Revoke a specific Refresh Token |
| POST | `/api/auth/verify-registration-otp` | Verify Registration OTP and get Tokens |

### OTP Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/otp/resend` | Resend OTP to registered email |

### Admin Endpoints (Public - First Time Setup)

| Method | Endpoint | Description |
| -------- | ---------- | ------------- |
| POST | `/api/admin/register` | Register the first Admin (OTP sent) |
| POST | `/api/admin/login` | Initiate Admin Login (OTP sent) |
| POST | `/api/admin/verify-login-otp` | Verify Admin Login OTP and get Tokens |

### Employee Management (Authenticated)

| Method | Endpoint | Description | Access |
| -------- | ---------- | ------------- | -------- |
| GET | `/api/employees` | Get paginated list of employees | Admin/Manager |
| GET | `/api/employees/{id}` | Get employee by ID | Admin/Manager |
| GET | `/api/employees/user/{userId}` | Get employee by User ID | Authenticated |
| POST | `/api/employees` | Create a new employee | Admin |
| PUT | `/api/employees/{id}` | Update employee details | Admin |
| DELETE | `/api/employees/{id}` | Delete an employee | Admin |
| PATCH | `/api/employees/{id}/toggle-status` | Toggle employee active status | Admin/Manager |
| POST | `/api/employees/{id}/promote` | Promote employee to Manager | Admin |

---

## 🗄️ Database Schema

### Key Tables

| Table | Description |
| ------- | ------------- |
| `Users` | User credentials, Role, and 2FA status |
| `Employees` | Employee personal and professional details |
| `RefreshTokens` | Refresh token storage (1 per user) |
| `OtpCodes` | OTP storage with expiration and usage tracking |

### Relationships

- **User ↔ Employee**: One-to-One (UserId → EmployeeId)
- **User ↔ RefreshToken**: One-to-One (Unique per user)
- **User ↔ OtpCodes**: One-to-Many (One OTP at a time)

---

## 🔒 Security Features

| Feature | Implementation |
| --------- | ---------------- |
| Password Storage | BCrypt Hashing (No plaintext passwords) |
| JWT Expiry | Access Token: 15 mins, Refresh Token: 7 days |
| OTP Expiry | 5 minutes validity |
| OTP Rate Limiting | One active OTP per user (previous gets invalidated on resend) |
| OTP Deletion | OTP deleted immediately after verification |
| Expired OTP Cleanup | Automatic cleanup before new OTP generation |
| Role-Based Access | `[Authorize(Roles = "Admin")]` attribute |
| Global Exception Handling | Custom middleware with standardized JSON responses |
| CORS | Configured for Angular frontend |

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or LocalDB (optional — In-Memory supported)
- [Git](https://git-scm.com/downloads)

### Installation

1. Clone the repository

   ```bash
   git clone https://github.com/aman-devcode1/EMS.git
   cd EMS

📄 License
This project is licensed under the MIT License - see the LICENSE file for details.

🤝 Connect with Me
Aman Ghildiyal
GitHub: github.com/aman-devcode1
Email: <aman.devcode1@gmail.com>

⭐ Show Your Support
If you found this project helpful, please give it a ⭐ on GitHub!
