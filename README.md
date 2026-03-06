## สารบัญ
- [Prerequisites](#prerequisites)
- [1) สร้าง Database + Tables (PostgreSQL)](#1-สร้าง-database--tables-postgresql)
- [2) ตั้งค่า Connection String (appsettings.json)](#2-ตั้งค่า-connection-string-appsettingsjson)
- [3) ติดตั้ง Packages ที่จำเป็น](#3-ติดตั้ง-packages-ที่จำเป็น)
- [4) ติดตั้ง dotnet-ef (แนะนำแบบ Local Tool)](#4-ติดตั้ง-dotnet-ef-แนะนำแบบ-local-tool)
- [5) ตั้งค่า Program.cs](#5-ตั้งค่า-programcs)
- [6) Scaffold สร้าง Model + DbContext จาก Database](#6-scaffold-สร้าง-model--dbcontext-จาก-database)

---

## Prerequisites

- ✅ .NET SDK 8.x  
  ตรวจสอบ:
  ```bash
  dotnet --list-sdks
  dotnet --version
  ```

- ✅ PostgreSQL (แนะนำ 14+)  
  - Host/Port ที่ใช้บ่อย: `localhost:5432`
  - มี user เช่น `postgres` และ password

---

## 1) สร้าง Database + Tables (PostgreSQL)

> ⚠️ ถ้าคุณรัน `CREATE DATABASE` พร้อมกับคำสั่งอื่นในครั้งเดียว อาจเจอ error:  
> `CREATE DATABASE cannot run inside a transaction block`  
>
> รันเป็น 2 ชุด: (1) สร้าง DB ก่อน (2) ต่อไปสร้างตารางใน DB นั้น

### 1.1 สร้าง Database (รันแยก)

```sql
CREATE DATABASE "Workshop_db"
  WITH OWNER = postgres
       ENCODING = 'UTF8'
       CONNECTION LIMIT = -1;
```
จากนั้นรัน:

```sql
CREATE TABLE public."Role"
(
    "RoleId"   integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "RoleName" varchar(50) NOT NULL UNIQUE
);

CREATE TABLE public."User"
(
    "UserId"    integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Firstname" varchar(50) NOT NULL,
    "Lastname"  varchar(50) NOT NULL,
    "Username"  varchar(30) NOT NULL UNIQUE,
    "Email"     varchar(50) UNIQUE,
    "IsActive"  boolean NOT NULL DEFAULT true
);

CREATE TABLE public."RoleUser"
(
    "Id"     integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "UserId" integer NOT NULL,
    "RoleId" integer NOT NULL,

    CONSTRAINT "FK_RoleUser_User"
        FOREIGN KEY ("UserId") REFERENCES public."User" ("UserId")
        ON DELETE CASCADE,

    CONSTRAINT "FK_RoleUser_Role"
        FOREIGN KEY ("RoleId") REFERENCES public."Role" ("RoleId")
        ON DELETE CASCADE,

    CONSTRAINT "UQ_RoleUser_UserId_RoleId" UNIQUE ("UserId", "RoleId")
);

CREATE INDEX IF NOT EXISTS "IX_RoleUser_UserId" ON public."RoleUser" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_RoleUser_RoleId" ON public."RoleUser" ("RoleId");

-- Seed
INSERT INTO public."Role" ("RoleName") VALUES ('Admin'), ('User');

INSERT INTO public."User" ("Firstname", "Lastname", "Username","Email","IsActive")
VALUES ('ผู้ดูแล','ระบบ','admin','admin@mail.com',true),
       ('จอห์น','ดู','john','john@mail.com',true);

INSERT INTO public."RoleUser" ("UserId","RoleId")
VALUES (1,1), (1,2), (2,2);
```

---

## 2) ตั้งค่า Connection String (appsettings.json)

แก้ไฟล์ `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=<<YourHost>>;Port=<<YourPort>>;Database=Workshop_db;Username=postgres;Password=<<YourPassword>>"
  }
}
```
---

## 3) ติดตั้ง Packages ที่จำเป็น

รันในโฟลเดอร์โปรเจค (ที่มี `.csproj`):

```bash
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.24
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.24
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.11
dotnet add package Scrutor --version 7.0.0
```

## 4) ติดตั้ง dotnet-ef (แนะนำแบบ Local Tool)

เพื่อกันปัญหาเครื่องคนอื่นมี `dotnet-ef` คนละเวอร์ชัน (เช่นไปเป็น 10 แล้ว error `System.Runtime 10.0.0.0`)

```bash
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 8.*
dotnet ef --version
```
---

## 5) ตั้งค่า Program.cs

เพิ่ม `using` และลงทะเบียน DbContext:

```csharp
using Microsoft.EntityFrameworkCore;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

## 6) Scaffold สร้าง Model + DbContext จาก Database

รันคำสั่งนี้ในโฟลเดอร์โปรเจค:

```bash
dotnet ef dbcontext scaffold "Host=<<YourHost>>;Port=<<YourPort>>;Database=Workshop_db;Username=postgres;Password=<<YourPassword>>"   Npgsql.EntityFrameworkCore.PostgreSQL   --output-dir Model   --context AppDbContext   --context-dir Data/DbContexts   --force
```

ตรวจสอบการเชื่อมต่อ PostgreSQL เบื้องต้น:
```bash
psql -h localhost -p 5432 -U postgres -d Workshop_db
```
