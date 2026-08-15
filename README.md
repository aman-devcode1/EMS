# EMS - Employee Management System (Backend API)

A secure, role-based RESTful API built with .NET 10, following Clean Architecture.

---

## Project Overview

This project is a fully functional **Employee Management System** backend. It handles employee data, authentication, and authorization for different user roles within an organization.

**Why I built this:** To showcase my ability to build enterprise-grade backend systems using modern .NET practices, including JWT Authentication, Repository Pattern, and Global Exception Handling.

---

## Key Features

- JWT Authentication & Authorization with Access Tokens (15 mins) and Refresh Tokens (7 days)
- Role-Based Access Control for Admin, Manager, and Employee
- Global Exception Handling with custom middleware for standardized JSON error responses
- Clean Architecture with separate layers: Core, Infrastructure, Services, and API
- AutoMapper for automatic DTO-Entity mapping
- Repository & Service Pattern for decoupled and testable code

---

## Tech Stack

| Technology          | Purpose                           |
|---------------------|-----------------------------------|
| .NET 10             | Runtime & Framework               |
| C#                  | Primary Language                  |
| Entity Framework Core | ORM for database operations     |
| SQL Server / LocalDB | Relational Database              |
| JWT                 | Authentication & Authorization    |
| BCrypt.Net          | Password Hashing                  |
| AutoMapper          | Object-Object Mapping             |
| Swagger / OpenAPI   | API Documentation & Testing       |
| Git & GitHub        | Version Control                   |

---

## Architecture

This project follows **Clean Architecture** to keep the code decoupled and maintainable.


Client (Postman / Swagger)
↓
[API Layer] (Controllers, Middleware)
↓
[Service Layer] (Business Logic)
↓
[Repository Layer] (Data Access - EF Core)
↓
[Database] (SQL Server)


- **EMS.Core** – Entities, DTOs, Interfaces, and Common Utilities (No external dependencies)
- **EMS.Infrastructure** – DbContext and Repository Implementations
- **EMS.Services** – Business Logic and AutoMapper Profiles
- **EMS.API** – Controllers, Middleware, and Program.cs (Startup)

---

## API Endpoints

| Method | Endpoint | Description | Access |
|--------|----------|-------------|--------|
| POST   | `/api/auth/register` | Register a new Employee | Public |
| POST   | `/api/auth/login` | Login and get Access/Refresh Tokens | Public |
| POST   | `/api/auth/refresh` | Refresh the Access Token | Authenticated |
| POST   | `/api/auth/revoke` | Revoke a specific Refresh Token | Authenticated |
| POST   | `/api/admin/register` | Register the first Admin (only once) | Public (Secret) |
| POST   | `/api/admin/login` | Admin Login | Public |
| GET    | `/api/employees` | Get paginated list of employees | Admin/Manager |
| GET    | `/api/employees/{id}` | Get employee by ID | Authenticated |

---

## Author

**Aman Kumar**  
- GitHub: [github.com/aman-devcode1](https://github.com/aman-devcode1)   
- Email: aman.devcode1@gmail.com

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.