# FineSoft - Sistema de Gestión de Tickets y Facturación

## 📋 Descripción

FineSoft es un sistema web integral para la gestión de tickets de soporte técnico, clientes, empleados, proyectos, cotizaciones y facturación. Desarrollado con tecnología .NET 8 y Blazor.

---

## 🏗️ Arquitectura

```
FineProjectWeb/
├── fn-backend/              # API REST (.NET 8)
│   ├── Controllers/         # Controladores API
│   ├── Services/            # Lógica de negocio
│   ├── Repositories/        # Interfaces de servicios
│   ├── DTO/                # Data Transfer Objects
│   ├── Validators/         # Validadores FluentValidation
│   ├── Middleware/          # Middleware personalizado
│   ├── Filters/            # Filtros de API
│   ├── Identity/            # Configuración de Identity
│   ├── Models/             # Modelos de Entity Framework
│   └── Util/               # Utilidades
│
└── fs-front/               # Frontend Blazor
    ├── Pages/              # Páginas Blazor
    ├── Components/         # Componentes reutilizables
    ├── Services/           # Servicios HTTP
    ├── DTO/               # DTOs del frontend
    └── wwwroot/           # Archivos estáticos
```

---

## 🛠️ Tecnologías

| Componente | Tecnología |
|------------|------------|
| Backend | .NET 8, ASP.NET Core |
| Frontend | Blazor WebAssembly |
| Base de datos | SQL Server, Entity Framework Core |
| Autenticación | JWT Bearer Tokens |
| Validación | FluentValidation |
| Cache | MemoryCache |
| Documentación API | Swagger/OpenAPI |
| PDF | QuestPDF |

---

## ⚙️ Configuración

### Requisitos

- .NET 8 SDK
- SQL Server (Local o Azure)
- Visual Studio 2022 o VS Code

### Variables de Entorno / appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=FineSoft;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Key": "15d026ee497b15d81c3f7a96f8809182220038c440ed6ffb6e491727",
    "Issuer": "fn-backend",
    "Audience": "fs-front",
    "ExpiresMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Ejecutar el proyecto

```bash
# Restaurar paquetes
dotnet restore

# Compilar
dotnet build

# Ejecutar backend
cd fn-backend
dotnet run

# En otra terminal, ejecutar frontend
cd fs-front
dotnet run
```

---

## 🔐 Seguridad

### Autenticación
- JWT Bearer Tokens
- Roles: Admin, Administracion, Supervisor, Empleado, Cliente

### Permisos
El sistema maneja permisos granulares por módulo:

| Módulo | Permisos |
|--------|----------|
| Dashboard | view |
| Tickets | view, view_detail, create, edit, delete, comment, activity, stats, assign |
| Clientes | view, view_detail, create, edit, delete |
| Empleados | view, view_detail, create, edit, delete |
| Proyectos | view, view_detail, create, edit, delete |
| Cotizaciones | view, view_detail, create, edit, delete, convert |
| Facturas | view, view_detail, create, edit, delete, payment |
| Usuarios | view, create, edit, delete, change_password, assign_roles |

### Rate Limiting
- **API Pública**: 30 solicitudes/minuto
- **Usuarios autenticados**: 100 solicitudes/minuto
- **Endpoints sensibles** (Auth): 10 solicitudes/minuto

---

## 📡 Endpoints Principales

### Autenticación
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | /api/auth/login | Iniciar sesión |
| POST | /api/auth/register | Registrarse |
| POST | /api/auth/refresh | Renovar token |

### Clientes
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | /api/clients | Listar clientes |
| GET | /api/clients/{id} | Ver cliente |
| POST | /api/clients | Crear cliente |
| PUT | /api/clients/{id} | Actualizar cliente |
| DELETE | /api/clients/{id} | Eliminar cliente |

### Tickets
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | /api/tickets | Listar tickets |
| GET | /api/tickets/{id} | Ver ticket |
| POST | /api/tickets | Crear ticket |
| PUT | /api/tickets/{id} | Actualizar ticket |
| DELETE | /api/tickets/{id} | Eliminar ticket |

### Empleados
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | /api/employees | Listar empleados |
| GET | /api/employees/{id} | Ver empleado |
| POST | /api/employees | Crear empleado |
| PUT | /api/employees/{id} | Actualizar empleado |

### Proyectos
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | /api/projects | Listar proyectos |
| POST | /api/projects | Crear proyecto |
| PUT | /api/projects/{id} | Actualizar proyecto |
| DELETE | /api/projects/{id} | Eliminar proyecto |

### Cotizaciones
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | /api/quotes | Listar cotizaciones |
| POST | /api/quotes | Crear cotización |
| PUT | /api/quotes/{id} | Actualizar cotización |
| POST | /api/quotes/{id}/convert | Convertir a factura |

### Facturas
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | /api/invoices | Listar facturas |
| POST | /api/invoices | Crear factura |
| PUT | /api/invoices/{id} | Actualizar factura |
| POST | /api/invoices/{id}/payment | Registrar pago |

---

## 🧪 Testing

```bash
# Ejecutar tests
dotnet test
```

---

## 📦 Funcionalidades Principales

### 🎫 Gestión de Tickets
- Creación y seguimiento de tickets
- Asignación a empleados
- Comentarios y adjuntos
- Historial de cambios
- Estados: Abierto, En Progreso, En Revisión, Cerrado

### 👥 Gestión de Clientes
- Registro de clientes con RFC
- Modo de servicio (Mensual/Por evento)
- Tarifa mensual y horas incluidas
- Proyectos asociados

### 👨‍💼 Gestión de Empleados
- Roles: Empleado, Supervisor, Administración
- Departamento y puesto
- Desactivación con desasignación de tickets

### 📊 Dashboard
- Estadísticas de tickets
- Tickets por estado
- Actividad reciente

### 📄 Cotizaciones
- Creación de cotizaciones con items
- Estados: Borrador, Enviada, Aceptada, Rechazada, Vencida
- Conversión a factura

### 💰 Facturación
- Facturas por evento o mensuales
- Registro de pagos
- Generación de PDF
- Estados: Pendiente, Pagada, Vencida, Cancelada

---

## 🔧 Mejores Prácticas Implementadas

1. **Patrón Repository**: Separación de lógica de acceso a datos
2. **Service Layer**: Lógica de negocio en servicios
3. **DTOs**: Transferencia de datos controlada
4. **FluentValidation**: Validación robusta de entrada
5. **Cache**: Optimización de consultas frecuentes
6. **Middleware**: Manejo centralizado de errores
7. **Audit Logging**: Registro de acciones sensibles

---

## 📄 Licencia

MIT License - FineSoft © 2024
