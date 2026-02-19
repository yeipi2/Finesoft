using fn_backend.Models;
using fs_backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Identity
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

       
        public DbSet<Client> Clients { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketComment> TicketComments { get; set; }
        public DbSet<TicketAttachment> TicketAttachments { get; set; }
        public DbSet<TicketHistory> TicketHistories { get; set; }
        public DbSet<Quote> Quotes { get; set; }
        public DbSet<QuoteItem> QuoteItems { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<InvoicePayment> InvoicePayments { get; set; }
        public DbSet<TicketActivity> TicketActivities { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========== CONFIGURACIÓN DE RELACIONES ==========

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Client)
                .WithMany(c => c.Projects)     // ← apunta a la propiedad que agregaste
                .HasForeignKey(p => p.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketComment>()
                .HasOne(tc => tc.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(tc => tc.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TicketAttachment>()
                .HasOne(ta => ta.Ticket)
                .WithMany(t => t.Attachments)
                .HasForeignKey(ta => ta.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.Ticket)
                .WithMany(t => t.History)
                .HasForeignKey(th => th.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Quote>()
                .HasOne(q => q.Client)
                .WithMany()
                .HasForeignKey(q => q.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QuoteItem>()
                .HasOne(qi => qi.Quote)
                .WithMany(q => q.Items)
                .HasForeignKey(qi => qi.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Client)
                .WithMany()
                .HasForeignKey(i => i.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Quote)
                .WithMany()
                .HasForeignKey(i => i.QuoteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InvoiceItem>()
                .HasOne(ii => ii.Invoice)
                .WithMany(i => i.Items)
                .HasForeignKey(ii => ii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InvoiceItem>()
                .HasOne(ii => ii.Ticket)
                .WithMany()
                .HasForeignKey(ii => ii.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InvoicePayment>()
                .HasOne(ip => ip.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(ip => ip.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TicketActivity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
                entity.Property(e => e.HoursSpent).HasColumnType("decimal(5,2)");
                entity.Property(e => e.IsCompleted).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.CreatedByUserId).IsRequired().HasMaxLength(450);

                entity.HasOne(e => e.Ticket)
                    .WithMany(t => t.Activities)
                    .HasForeignKey(e => e.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========== CONFIGURACIÓN DE PERMISOS ==========
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Module).IsRequired().HasMaxLength(50);
                entity.Property(p => p.Action).IsRequired().HasMaxLength(50);
                entity.Property(p => p.Code).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Description).IsRequired().HasMaxLength(255);
                entity.HasIndex(p => p.Code).IsUnique();
            });

            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(rp => rp.Id);
                entity.HasOne(rp => rp.Permission)
                      .WithMany()
                      .HasForeignKey(rp => rp.PermissionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
            });

            // ⭐ SEED DE PERMISOS - Esto insertará los permisos automáticamente
            SeedPermissions(modelBuilder);
        }

        // ========== MÉTODO QUE INSERTA LOS 45 PERMISOS AUTOMÁTICAMENTE ==========
        // ========== MÉTODO QUE INSERTA LOS PERMISOS AUTOMÁTICAMENTE ==========
        private void SeedPermissions(ModelBuilder builder)
        {
            var permissions = new List<Permission>();
            int id = 1;

            // ========== DASHBOARD (1 permiso) ==========
            permissions.Add(new Permission
            {
                Id = id++,
                Module = "Dashboard",
                Action = "Ver",
                Code = "dashboard.view",
                Description = "Ver dashboard"
            });

            // ========== TICKETS (9 permisos) ==========
            permissions.Add(new Permission { Id = id++, Module = "Tickets", Action = "Ver", Code = "tickets.view", Description = "Ver lista de tickets" });
            permissions.Add(new Permission { Id = id++, Module = "Tickets", Action = "VerDetalle", Code = "tickets.view_detail", Description = "Ver detalles de ticket" });
            permissions.Add(new Permission { Id = id++, Module = "Tickets", Action = "Crear", Code = "tickets.create", Description = "Crear tickets" });
            permissions.Add(new Permission { Id = id++, Module = "Tickets", Action = "Editar", Code = "tickets.edit", Description = "Editar tickets" });
            permissions.Add(new Permission { Id = id++, Module = "Tickets", Action = "Eliminar", Code = "tickets.delete", Description = "Eliminar tickets" });
            permissions.Add(new Permission { Id = id++, Module = "Tickets", Action = "Comentar", Code = "tickets.comment", Description = "Agregar comentarios" });
            permissions.Add(new Permission { Id = id++, Module = "Tickets", Action = "Actividades", Code = "tickets.activity", Description = "Gestionar actividades" });
            permissions.Add(new Permission { Id = id++, Module = "Tickets", Action = "Estadisticas", Code = "tickets.stats", Description = "Ver estadísticas" });
            permissions.Add(new Permission { Id = id++, Module = "Tickets", Action = "Asignar", Code = "tickets.assign", Description = "Asignar tickets a usuarios" });

            // ========== CLIENTES (5 permisos) ==========
            permissions.Add(new Permission { Id = id++, Module = "Clientes", Action = "Ver", Code = "clients.view", Description = "Ver lista de clientes" });
            permissions.Add(new Permission { Id = id++, Module = "Clientes", Action = "VerDetalle", Code = "clients.view_detail", Description = "Ver detalles de cliente" });
            permissions.Add(new Permission { Id = id++, Module = "Clientes", Action = "Crear", Code = "clients.create", Description = "Crear clientes" });
            permissions.Add(new Permission { Id = id++, Module = "Clientes", Action = "Editar", Code = "clients.edit", Description = "Editar clientes" });
            permissions.Add(new Permission { Id = id++, Module = "Clientes", Action = "Eliminar", Code = "clients.delete", Description = "Eliminar clientes" });

            // ========== EMPLEADOS (5 permisos) - ⭐ NUEVOS ⭐ ==========
            permissions.Add(new Permission { Id = id++, Module = "Empleados", Action = "Ver", Code = "employees.view", Description = "Ver lista de empleados" });
            permissions.Add(new Permission { Id = id++, Module = "Empleados", Action = "VerDetalle", Code = "employees.view_detail", Description = "Ver detalles de empleado" });
            permissions.Add(new Permission { Id = id++, Module = "Empleados", Action = "Crear", Code = "employees.create", Description = "Crear empleados" });
            permissions.Add(new Permission { Id = id++, Module = "Empleados", Action = "Editar", Code = "employees.edit", Description = "Editar empleados" });
            permissions.Add(new Permission { Id = id++, Module = "Empleados", Action = "Eliminar", Code = "employees.delete", Description = "Eliminar empleados" });

            // ========== PROYECTOS (5 permisos) ==========
            permissions.Add(new Permission { Id = id++, Module = "Proyectos", Action = "Ver", Code = "projects.view", Description = "Ver lista de proyectos" });
            permissions.Add(new Permission { Id = id++, Module = "Proyectos", Action = "VerDetalle", Code = "projects.view_detail", Description = "Ver detalles de proyecto" });
            permissions.Add(new Permission { Id = id++, Module = "Proyectos", Action = "Crear", Code = "projects.create", Description = "Crear proyectos" });
            permissions.Add(new Permission { Id = id++, Module = "Proyectos", Action = "Editar", Code = "projects.edit", Description = "Editar proyectos" });
            permissions.Add(new Permission { Id = id++, Module = "Proyectos", Action = "Eliminar", Code = "projects.delete", Description = "Eliminar proyectos" });

            // ========== COTIZACIONES (6 permisos) ==========
            permissions.Add(new Permission { Id = id++, Module = "Cotizaciones", Action = "Ver", Code = "quotes.view", Description = "Ver lista de cotizaciones" });
            permissions.Add(new Permission { Id = id++, Module = "Cotizaciones", Action = "VerDetalle", Code = "quotes.view_detail", Description = "Ver detalles de cotización" });
            permissions.Add(new Permission { Id = id++, Module = "Cotizaciones", Action = "Crear", Code = "quotes.create", Description = "Crear cotizaciones" });
            permissions.Add(new Permission { Id = id++, Module = "Cotizaciones", Action = "Editar", Code = "quotes.edit", Description = "Editar cotizaciones" });
            permissions.Add(new Permission { Id = id++, Module = "Cotizaciones", Action = "Eliminar", Code = "quotes.delete", Description = "Eliminar cotizaciones" });
            permissions.Add(new Permission { Id = id++, Module = "Cotizaciones", Action = "ConvertirFactura", Code = "quotes.convert", Description = "Convertir a factura" });

            // ========== FACTURAS (6 permisos) ==========
            permissions.Add(new Permission { Id = id++, Module = "Facturas", Action = "Ver", Code = "invoices.view", Description = "Ver lista de facturas" });
            permissions.Add(new Permission { Id = id++, Module = "Facturas", Action = "VerDetalle", Code = "invoices.view_detail", Description = "Ver detalles de factura" });
            permissions.Add(new Permission { Id = id++, Module = "Facturas", Action = "Crear", Code = "invoices.create", Description = "Crear facturas" });
            permissions.Add(new Permission { Id = id++, Module = "Facturas", Action = "Editar", Code = "invoices.edit", Description = "Editar facturas" });
            permissions.Add(new Permission { Id = id++, Module = "Facturas", Action = "Eliminar", Code = "invoices.delete", Description = "Eliminar facturas" });
            permissions.Add(new Permission { Id = id++, Module = "Facturas", Action = "RegistrarPago", Code = "invoices.payment", Description = "Registrar pagos" });

            // ========== REPORTES (3 permisos) ==========
            permissions.Add(new Permission { Id = id++, Module = "Reportes", Action = "Ver", Code = "reports.view", Description = "Ver reportes" });
            permissions.Add(new Permission { Id = id++, Module = "Reportes", Action = "Exportar", Code = "reports.export", Description = "Exportar reportes" });
            permissions.Add(new Permission { Id = id++, Module = "Reportes", Action = "Financieros", Code = "reports.financial", Description = "Ver reportes financieros" });

            // ========== USUARIOS (6 permisos) ==========
            permissions.Add(new Permission { Id = id++, Module = "Usuarios", Action = "Ver", Code = "users.view", Description = "Ver lista de usuarios" });
            permissions.Add(new Permission { Id = id++, Module = "Usuarios", Action = "Crear", Code = "users.create", Description = "Crear usuarios" });
            permissions.Add(new Permission { Id = id++, Module = "Usuarios", Action = "Editar", Code = "users.edit", Description = "Editar usuarios" });
            permissions.Add(new Permission { Id = id++, Module = "Usuarios", Action = "Eliminar", Code = "users.delete", Description = "Eliminar usuarios" });
            permissions.Add(new Permission { Id = id++, Module = "Usuarios", Action = "CambiarPassword", Code = "users.change_password", Description = "Cambiar contraseñas" });
            permissions.Add(new Permission { Id = id++, Module = "Usuarios", Action = "AsignarRoles", Code = "users.assign_roles", Description = "Asignar roles" });

            // Insertar todos los permisos en la base de datos
            builder.Entity<Permission>().HasData(permissions);
        }
    }
}