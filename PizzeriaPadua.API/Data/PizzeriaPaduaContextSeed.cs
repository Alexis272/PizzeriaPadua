using Microsoft.EntityFrameworkCore;
using PizzeriaPadua.Core.Data;
using PizzeriaPadua.Core.Enums;

namespace PizzeriaPadua.API.Data;

public class PizzeriaPaduaContextSeed
{
    public async Task Seed(PizzeriaPaduaContext context, ILogger<PizzeriaPaduaContextSeed> logger)
    {
        try
        {
            if (!await context.StoreSettings.AnyAsync())
            {
                context.StoreSettings.Add(new StoreSettings
                {
                    OpeningTime = "11:00",
                    ClosingTime = "23:00",
                    Address = "Av. Principal #120, Centro, Padua|777 321 4567||1,2,3,4,5,6,0|https://maps.google.com",
                    CompanyName = "Pizzería Padua S.A. de C.V.",
                    Rfc = "PPA260101MX1",
                    Currency = "MXN",
                    DefaultTaxRate = 0.16m,
                    RoundingRule = "Ninguno",
                    TipsEnabled = true,
                    AllowCloseWithOpenAccounts = false,
                    MaxCashierDiscountPercent = 10m,
                    ReturnPolicyText = "Conserve su ticket para cualquier aclaración o factura el mismo día de consumo."
                });
                await context.SaveChangesAsync();
            }

            if (!await context.Branches.AnyAsync())
            {
                await context.Branches.AddRangeAsync(
                    new Branch
                    {
                        CompanyName = "Pizzería Padua S.A. de C.V.",
                        Rfc = "PPA260101MX1",
                        Name = "Sucursal Matriz Centro",
                        Code = "SUC-01",
                        Address = "Av. Principal #120, Col. Centro",
                        Phone = "777 321 4567",
                        Currency = "MXN",
                        DefaultTaxRate = 0.16m,
                        SeriesPrefix = "PAD",
                        IsActive = true
                    },
                    new Branch
                    {
                        CompanyName = "Pizzería Padua S.A. de C.V.",
                        Rfc = "PPA260101MX1",
                        Name = "Sucursal Plaza Norte",
                        Code = "SUC-02",
                        Address = "Blvd. del Sol #450, Local 12",
                        Phone = "777 987 6543",
                        Currency = "MXN",
                        DefaultTaxRate = 0.16m,
                        SeriesPrefix = "PNT",
                        IsActive = true
                    }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.CashRegisters.AnyAsync())
            {
                await context.CashRegisters.AddRangeAsync(
                    new CashRegister
                    {
                        BranchId = 1,
                        Name = "Caja 1 - Mostrador Principal",
                        Code = "C1",
                        PrinterName = "Epson TM-T20III (80mm)",
                        SeriesPrefix = "PAD-C1",
                        CurrentFolio = 105,
                        IsActive = true,
                        Status = "Abierta"
                    },
                    new CashRegister
                    {
                        BranchId = 1,
                        Name = "Caja 2 - Terraza y Rápida",
                        Code = "C2",
                        PrinterName = "Star TSP100 (80mm)",
                        SeriesPrefix = "PAD-C2",
                        CurrentFolio = 200,
                        IsActive = true,
                        Status = "Cerrada"
                    },
                    new CashRegister
                    {
                        BranchId = 2,
                        Name = "Caja 1 - Plaza Norte",
                        Code = "N1",
                        PrinterName = "Epson TM-T20III Norte",
                        SeriesPrefix = "PNT-C1",
                        CurrentFolio = 100,
                        IsActive = true,
                        Status = "Cerrada"
                    }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.ShiftConfigs.AnyAsync())
            {
                await context.ShiftConfigs.AddRangeAsync(
                    new ShiftConfig { BranchId = 1, Name = "Matutino", StartTime = "10:00", EndTime = "17:00", SuggestedInitialFund = 1500m, IsActive = true },
                    new ShiftConfig { BranchId = 1, Name = "Vespertino", StartTime = "17:00", EndTime = "23:00", SuggestedInitialFund = 2000m, IsActive = true },
                    new ShiftConfig { BranchId = 1, Name = "Completo", StartTime = "10:00", EndTime = "23:00", SuggestedInitialFund = 1500m, IsActive = true }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.Employees.AnyAsync())
            {
                await context.Employees.AddRangeAsync(
                    new Employee
                    {
                        Name = "Administrador General",
                        LastName = "Padua",
                        Phone = "7771000001",
                        Position = "Administrador General",
                        Username = "admin",
                        Password = "padua2026",
                        PinCode = "1234",
                        Role = "Administrador",
                        BranchId = 1,
                        CanModifyPrice = true,
                        CanAuthorizeDiscount = true,
                        CanCancelPaidSale = true,
                        CanProcessRefund = true,
                        CanManageCatalog = true,
                        CanOpenCloseCash = true,
                        CanRegisterCashMovement = true,
                        CanAdjustCashDifference = true,
                        CanViewFinancialReports = true,
                        CanExportReports = true,
                        MaxManualDiscountPercent = 100m,
                        IsActive = true
                    },
                    new Employee
                    {
                        Name = "Laura Méndez",
                        LastName = "Gómez",
                        Phone = "7771000002",
                        Position = "Gerente de Sucursal",
                        Username = "gerente",
                        Password = "gerente2026",
                        PinCode = "2222",
                        Role = "Gerente",
                        BranchId = 1,
                        CanModifyPrice = true,
                        CanAuthorizeDiscount = true,
                        CanCancelPaidSale = true,
                        CanProcessRefund = true,
                        CanManageCatalog = true,
                        CanOpenCloseCash = true,
                        CanRegisterCashMovement = true,
                        CanAdjustCashDifference = true,
                        CanViewFinancialReports = true,
                        CanExportReports = true,
                        MaxManualDiscountPercent = 50m,
                        IsActive = true
                    },
                    new Employee
                    {
                        Name = "Roberto Salazar",
                        LastName = "Ruiz",
                        Phone = "7771000003",
                        Position = "Supervisor de Turno",
                        Username = "supervisor",
                        Password = "super2026",
                        PinCode = "3333",
                        Role = "Supervisor",
                        BranchId = 1,
                        CanModifyPrice = true,
                        CanAuthorizeDiscount = true,
                        CanCancelPaidSale = true,
                        CanProcessRefund = true,
                        CanManageCatalog = false,
                        CanOpenCloseCash = true,
                        CanRegisterCashMovement = true,
                        CanAdjustCashDifference = true,
                        CanViewFinancialReports = true,
                        CanExportReports = false,
                        MaxManualDiscountPercent = 25m,
                        IsActive = true
                    },
                    new Employee
                    {
                        Name = "Carlos Rivera",
                        LastName = "Soto",
                        Phone = "7771000004",
                        Position = "Cajero Matutino",
                        Username = "cajero1",
                        Password = "caja2026",
                        PinCode = "4444",
                        Role = "Cajero",
                        BranchId = 1,
                        AllowedCashRegisterId = 1,
                        CanModifyPrice = false,
                        CanAuthorizeDiscount = false,
                        CanCancelPaidSale = false,
                        CanProcessRefund = false,
                        CanManageCatalog = false,
                        CanOpenCloseCash = true,
                        CanRegisterCashMovement = false,
                        CanAdjustCashDifference = false,
                        CanViewFinancialReports = false,
                        CanExportReports = false,
                        MaxManualDiscountPercent = 10m,
                        IsActive = true
                    },
                    new Employee
                    {
                        Name = "Sofía Torres",
                        LastName = "Vega",
                        Phone = "7771000005",
                        Position = "Cajera Vespertina",
                        Username = "cajero2",
                        Password = "caja2026",
                        PinCode = "4455",
                        Role = "Cajero",
                        BranchId = 1,
                        AllowedCashRegisterId = 1,
                        CanModifyPrice = false,
                        CanAuthorizeDiscount = false,
                        CanCancelPaidSale = false,
                        CanProcessRefund = false,
                        CanManageCatalog = false,
                        CanOpenCloseCash = true,
                        CanRegisterCashMovement = false,
                        CanAdjustCashDifference = false,
                        CanViewFinancialReports = false,
                        CanExportReports = false,
                        MaxManualDiscountPercent = 10m,
                        IsActive = true
                    },
                    new Employee
                    {
                        Name = "Elena Vargas",
                        LastName = "León",
                        Phone = "7771000006",
                        Position = "Auditor Interno",
                        Username = "auditor",
                        Password = "audit2026",
                        PinCode = "5555",
                        Role = "Auditor",
                        BranchId = 1,
                        CanModifyPrice = false,
                        CanAuthorizeDiscount = false,
                        CanCancelPaidSale = false,
                        CanProcessRefund = false,
                        CanManageCatalog = false,
                        CanOpenCloseCash = false,
                        CanRegisterCashMovement = false,
                        CanAdjustCashDifference = false,
                        CanViewFinancialReports = true,
                        CanExportReports = true,
                        MaxManualDiscountPercent = 0m,
                        IsActive = true
                    }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.Categories.AnyAsync())
            {
                await context.Categories.AddRangeAsync(
                    new Category { Name = "PIZZAS CLÁSICAS", Description = "Recetas tradicionales horneadas a la piedra con mozzarella fresco", Subcategory = "Tradicionales", DefaultTaxRate = 0.16m },
                    new Category { Name = "PIZZAS ESPECIALES", Description = "Creaciones gourmet de la casa Padua con ingredientes selectos", Subcategory = "Gourmet", DefaultTaxRate = 0.16m },
                    new Category { Name = "COMBOS Y PAQUETES", Description = "Paquetes familiares y en pareja listos para compartir", Subcategory = "Paquetes", DefaultTaxRate = 0.16m },
                    new Category { Name = "ENTRADAS Y HORNO", Description = "Alitas, pan de ajo, bastones de queso y calzones", Subcategory = "Complementos", DefaultTaxRate = 0.16m },
                    new Category { Name = "BEBIDAS", Description = "Refrescos, limonadas artesanales, aguas y cervezas", Subcategory = "Bebidas", DefaultTaxRate = 0.16m },
                    new Category { Name = "POSTRES", Description = "Tiramisú italiano, pizza de avellana y cannolis", Subcategory = "Dulces", DefaultTaxRate = 0.16m }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.Products.AnyAsync())
            {
                const string pizzaCustomizations = "Tamaño:Mediana 30cm ($0),Grande 36cm (+$45),Familiar 42cm (+$85)|Masa:Tradicional Artesanal ($0),Crujiente Delgada ($0),Orilla Rellena de Queso (+$45)|Extras:Extra Mozzarella (+$25),Pepperoni Extra (+$30),Champiñones Frescos (+$20),Tocino Ahumado (+$30),Aceitunas Negras (+$20)";

                await context.Products.AddRangeAsync(
                    new Product
                    {
                        Name = "PIZZA PEPPERONI SUPREMA",
                        ShortName = "PIZ PEPPERONI",
                        Sku = "PIZ-CLA-001",
                        Barcode = "750100100001",
                        Description = "Doble porción de pepperoni americano, salsa pomodoro de la casa y queso mozzarella gratinado.",
                        Price = 189m,
                        Cost = 68m,
                        TaxRate = 0.16m,
                        CategoryId = 1,
                        Stock = 60,
                        ImageUrl = "/img/pizza-pepperoni.svg",
                        IsFeatured = true,
                        Badge = "Más vendida",
                        CustomizationOptions = pizzaCustomizations
                    },
                    new Product
                    {
                        Name = "PIZZA HAWAIANA ARTESANAL",
                        ShortName = "PIZ HAWAIANA",
                        Sku = "PIZ-CLA-002",
                        Barcode = "750100100002",
                        Description = "Jamón de pierna horneado, piña asada al horno de piedra, toque de tocino y mozzarella.",
                        Price = 195m,
                        Cost = 72m,
                        TaxRate = 0.16m,
                        CategoryId = 1,
                        Stock = 45,
                        ImageUrl = "/img/pizza-hawaiana.svg",
                        IsFeatured = true,
                        CustomizationOptions = pizzaCustomizations
                    },
                    new Product
                    {
                        Name = "PIZZA MARGHERITA DI PADUA",
                        ShortName = "PIZ MARGHERITA",
                        Sku = "PIZ-CLA-003",
                        Barcode = "750100100003",
                        Description = "Mozzarella fior di latte, jitomate San Marzano, hojas de albahaca fresca y aceite de oliva extra virgen.",
                        Price = 179m,
                        Cost = 62m,
                        TaxRate = 0.16m,
                        CategoryId = 1,
                        Stock = 40,
                        ImageUrl = "/img/pizza-margherita.svg",
                        Badge = "Receta italiana",
                        CustomizationOptions = pizzaCustomizations
                    },
                    new Product
                    {
                        Name = "PIZZA CUATRO QUESOS",
                        ShortName = "PIZ 4 QUESOS",
                        Sku = "PIZ-CLA-004",
                        Barcode = "750100100004",
                        Description = "Mezcla cremosa de mozzarella, gorgonzola, parmesano reggiano y provolone ahumado.",
                        Price = 215m,
                        Cost = 82m,
                        TaxRate = 0.16m,
                        CategoryId = 1,
                        Stock = 35,
                        ImageUrl = "/img/pizza-quesos.svg",
                        CustomizationOptions = pizzaCustomizations
                    },
                    new Product
                    {
                        Name = "PIZZA PADUA ESPECIAL RESERVA",
                        ShortName = "PIZ PADUA ESP",
                        Sku = "PIZ-ESP-001",
                        Barcode = "750100200001",
                        Description = "Prosciutto italiano, salami, salchicha italiana, pimientos asados, champiñones y aceitunas negras.",
                        Price = 249m,
                        Cost = 96m,
                        TaxRate = 0.16m,
                        CategoryId = 2,
                        Stock = 40,
                        ImageUrl = "/img/pizza-padua.svg",
                        IsFeatured = true,
                        Badge = "Especialidad",
                        CustomizationOptions = pizzaCustomizations
                    },
                    new Product
                    {
                        Name = "PIZZA CARNES FRÍAS BRAVA",
                        ShortName = "PIZ CARNES",
                        Sku = "PIZ-ESP-002",
                        Barcode = "750100200002",
                        Description = "Pepperoni, jamón serrano, tocino crujiente, chorizo español y carne sazonada a las finas hierbas.",
                        Price = 259m,
                        Cost = 102m,
                        TaxRate = 0.16m,
                        CategoryId = 2,
                        Stock = 35,
                        ImageUrl = "/img/pizza-carnes.svg",
                        IsFeatured = true,
                        Badge = "Favorita",
                        CustomizationOptions = pizzaCustomizations
                    },
                    new Product
                    {
                        Name = "PIZZA PASTOR Y PIÑA",
                        ShortName = "PIZ PASTOR",
                        Sku = "PIZ-ESP-003",
                        Barcode = "750100200003",
                        Description = "Carne al pastor marinada en casa, piña asada, cebolla morada, cilantro fresco y salsa habanero suave.",
                        Price = 235m,
                        Cost = 88m,
                        TaxRate = 0.16m,
                        CategoryId = 2,
                        Stock = 35,
                        ImageUrl = "/img/pizza-pastor.svg",
                        CustomizationOptions = pizzaCustomizations
                    },
                    new Product
                    {
                        Name = "COMBO FAMILIAR PADUA",
                        ShortName = "CMB FAMILIAR",
                        Sku = "CMB-PAD-001",
                        Barcode = "750100300001",
                        Description = "2 Pizzas Grandes de especialidad + Orden de Pan de Ajo Relleno + Refresco Familiar 2L.",
                        Price = 469m,
                        Cost = 175m,
                        TaxRate = 0.16m,
                        CategoryId = 3,
                        Stock = 30,
                        IsCombo = true,
                        ImageUrl = "/img/combo-familiar.svg",
                        IsFeatured = true,
                        Badge = "Ahorra $95",
                        CustomizationOptions = "Pizza 1:Pepperoni,Hawaiana,Padua Especial,Carnes Frías|Pizza 2:Pepperoni,Hawaiana,Margherita,4 Quesos|Refresco 2L:Coca-Cola,Sprite,Fanta,Sidral"
                    },
                    new Product
                    {
                        Name = "COMBO DÚO PAREJA",
                        ShortName = "CMB DUO",
                        Sku = "CMB-PAD-002",
                        Barcode = "750100300002",
                        Description = "1 Pizza Mediana a elegir + 6 Alitas al Horno + 2 Bebidas Individuales.",
                        Price = 299m,
                        Cost = 112m,
                        TaxRate = 0.16m,
                        CategoryId = 3,
                        Stock = 30,
                        IsCombo = true,
                        ImageUrl = "/img/combo-duo.svg",
                        Badge = "Combo",
                        CustomizationOptions = "Pizza:Pepperoni,Hawaiana,Margherita,Padua Especial|Salsa Alitas:BBQ,Buffalo,Mango Habanero|Bebidas:Coca-Cola,Limonada Mineral"
                    },
                    new Product
                    {
                        Name = "BASTONES DE PAN DE AJO CON QUESO",
                        ShortName = "PAN AJO QUESO",
                        Sku = "ENT-PAD-001",
                        Barcode = "750100400001",
                        Description = "8 bastones recién horneados con mantequilla de ajo rostizado, parmesano y dip marinara.",
                        Price = 95m,
                        Cost = 32m,
                        TaxRate = 0.16m,
                        CategoryId = 4,
                        Stock = 50,
                        ImageUrl = "/img/pan-ajo.svg",
                        CustomizationOptions = "Dip Extra:Marinara ($0),Ranch (+$15),Queso Cheddar (+$15)"
                    },
                    new Product
                    {
                        Name = "ALITAS AL HORNO DE LEÑA (10 PZAS)",
                        ShortName = "ALITAS 10PZ",
                        Sku = "ENT-PAD-002",
                        Barcode = "750100400002",
                        Description = "Alitas doradas al horno bañadas en tu salsa favorita con bastones de apio y aderezo ranch.",
                        Price = 159m,
                        Cost = 64m,
                        TaxRate = 0.16m,
                        CategoryId = 4,
                        Stock = 40,
                        ImageUrl = "/img/alitas.svg",
                        IsFeatured = true,
                        CustomizationOptions = "Salsa:BBQ Ahumada,Buffalo Clásica,Mango Habanero,Parmesano Ajo"
                    },
                    new Product
                    {
                        Name = "LIMONADA ITALIANA FRUTOS ROJOS",
                        ShortName = "LIMONADA IT",
                        Sku = "BEB-PAD-001",
                        Barcode = "750100500001",
                        Description = "Agua mineralizada con limón fresco, infusión de frutos rojos y hojas de menta (500 ml).",
                        Price = 58m,
                        Cost = 16m,
                        TaxRate = 0.16m,
                        CategoryId = 5,
                        Stock = 80,
                        ImageUrl = "/img/bebidas.svg",
                        CustomizationOptions = "Hielo:Normal,Poco hielo,Sin hielo"
                    },
                    new Product
                    {
                        Name = "REFRESCO FAMILIAR 2 LITROS",
                        ShortName = "REFRESCO 2L",
                        Sku = "BEB-PAD-002",
                        Barcode = "750100500002",
                        Description = "Botella familiar de 2 litros bien fría para acompañar tus pizzas.",
                        Price = 55m,
                        Cost = 28m,
                        TaxRate = 0.16m,
                        CategoryId = 5,
                        Stock = 90,
                        ImageUrl = "/img/bebidas.svg",
                        CustomizationOptions = "Sabor:Coca-Cola Original,Coca-Cola Sin Azúcar,Sprite,Sidral Mundet"
                    },
                    new Product
                    {
                        Name = "TIRAMISÚ ARTESANAL DE LA CASA",
                        ShortName = "TIRAMISU",
                        Sku = "POS-PAD-001",
                        Barcode = "750100600001",
                        Description = "Capas de soletas bañadas en espresso italiano, crema de mascarpone y cacao alcalino.",
                        Price = 99m,
                        Cost = 36m,
                        TaxRate = 0.16m,
                        CategoryId = 6,
                        Stock = 25,
                        ImageUrl = "/img/postre.svg",
                        Badge = "Postre estrella"
                    }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.Tables.AnyAsync())
            {
                await context.Tables.AddRangeAsync(
                    new Table { TableNumber = "Mesa 1", Area = "Salón Principal", Capacity = 4, IsAvailable = true },
                    new Table { TableNumber = "Mesa 2", Area = "Salón Principal", Capacity = 4, IsAvailable = false },
                    new Table { TableNumber = "Mesa 3", Area = "Salón Principal", Capacity = 6, IsAvailable = true },
                    new Table { TableNumber = "Mesa 4", Area = "Terraza", Capacity = 4, IsAvailable = true },
                    new Table { TableNumber = "Mesa 5", Area = "Terraza", Capacity = 8, IsAvailable = true },
                    new Table { TableNumber = "Barra Mostrador", Area = "Mostrador", Capacity = 2, IsAvailable = true }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.PaymentMethodConfigs.AnyAsync())
            {
                await context.PaymentMethodConfigs.AddRangeAsync(
                    new PaymentMethodConfig { Name = "Efectivo", Code = "EFE", RequiresReference = false, AllowsTip = true, CommissionPercentage = 0m, AffectsCashDrawer = true, IsActive = true },
                    new PaymentMethodConfig { Name = "Tarjeta de Débito", Code = "TDB", RequiresReference = true, AllowsTip = true, CommissionPercentage = 1.85m, AffectsCashDrawer = false, IsActive = true },
                    new PaymentMethodConfig { Name = "Tarjeta de Crédito", Code = "TDC", RequiresReference = true, AllowsTip = true, CommissionPercentage = 2.45m, AffectsCashDrawer = false, IsActive = true },
                    new PaymentMethodConfig { Name = "Transferencia SPEI", Code = "SPEI", RequiresReference = true, AllowsTip = true, CommissionPercentage = 0m, AffectsCashDrawer = false, IsActive = true },
                    new PaymentMethodConfig { Name = "Pago QR / CoDi", Code = "QR", RequiresReference = true, AllowsTip = true, CommissionPercentage = 0.90m, AffectsCashDrawer = false, IsActive = true },
                    new PaymentMethodConfig { Name = "Vales / Monedero", Code = "VAL", RequiresReference = true, AllowsTip = false, CommissionPercentage = 1.50m, AffectsCashDrawer = false, IsActive = true },
                    new PaymentMethodConfig { Name = "Crédito Autorizado", Code = "CRED", RequiresReference = true, AllowsTip = false, CommissionPercentage = 0m, AffectsCashDrawer = false, IsActive = true }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.SalesChannelConfigs.AnyAsync())
            {
                await context.SalesChannelConfigs.AddRangeAsync(
                    new SalesChannelConfig { Name = "Mostrador", Code = "MOS", PriceMultiplier = 1.0m, IsActive = true },
                    new SalesChannelConfig { Name = "Local / Mesa", Code = "LOC", PriceMultiplier = 1.0m, IsActive = true },
                    new SalesChannelConfig { Name = "Para Llevar", Code = "LLE", PriceMultiplier = 1.0m, IsActive = true },
                    new SalesChannelConfig { Name = "Entrega a Domicilio", Code = "DOM", PriceMultiplier = 1.0m, IsActive = true },
                    new SalesChannelConfig { Name = "Teléfono", Code = "TEL", PriceMultiplier = 1.0m, IsActive = true },
                    new SalesChannelConfig { Name = "Tienda Web", Code = "WEB", PriceMultiplier = 1.0m, IsActive = true },
                    new SalesChannelConfig { Name = "Plataforma Externa", Code = "APP", PriceMultiplier = 1.15m, IsActive = true }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.CatalogReasons.AnyAsync())
            {
                await context.CatalogReasons.AddRangeAsync(
                    new CatalogReason { ReasonType = "Cancelacion", Description = "Error de captura en mostrador", RequiresAuthorization = false, IsActive = true },
                    new CatalogReason { ReasonType = "Cancelacion", Description = "Cliente canceló pedido antes de preparar", RequiresAuthorization = false, IsActive = true },
                    new CatalogReason { ReasonType = "Cancelacion", Description = "Cancelación de venta ya cobrada por cambio de método o error", RequiresAuthorization = true, IsActive = true },
                    new CatalogReason { ReasonType = "Devolucion", Description = "Producto inconforme o error en ingrediente", RequiresAuthorization = true, IsActive = true },
                    new CatalogReason { ReasonType = "Devolucion", Description = "Demora excesiva en entrega", RequiresAuthorization = true, IsActive = true },
                    new CatalogReason { ReasonType = "Descuento", Description = "Cortesía gerencial / Cliente frecuente", RequiresAuthorization = true, IsActive = true },
                    new CatalogReason { ReasonType = "Descuento", Description = "Promoción del día / Cupón verificado", RequiresAuthorization = false, IsActive = true },
                    new CatalogReason { ReasonType = "Descuento", Description = "Consumo de empleado autorizado", RequiresAuthorization = true, IsActive = true },
                    new CatalogReason { ReasonType = "MovimientoCaja", Description = "Ingreso de morralla / cambio adicional", RequiresAuthorization = true, IsActive = true },
                    new CatalogReason { ReasonType = "MovimientoCaja", Description = "Retiro parcial de seguridad a bóveda", RequiresAuthorization = true, IsActive = true },
                    new CatalogReason { ReasonType = "MovimientoCaja", Description = "Pago autorizado a proveedor local (hielo/insumo urgente)", RequiresAuthorization = true, IsActive = true },
                    new CatalogReason { ReasonType = "DiferenciaCaja", Description = "Error en cambio entregado en hora pico", RequiresAuthorization = true, IsActive = true },
                    new CatalogReason { ReasonType = "DiferenciaCaja", Description = "Redondeo de centavos acumulado", RequiresAuthorization = true, IsActive = true }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.Promotions.AnyAsync())
            {
                await context.Promotions.AddRangeAsync(
                    new Promotion
                    {
                        Name = "Cupón Bienvenida 10% OFF",
                        Code = "PADUA10",
                        PromoType = "Porcentaje",
                        Value = 10m,
                        MinOrderAmount = 180m,
                        AllowedChannel = "Todos",
                        RequiresAuthorization = false,
                        IsActive = true
                    },
                    new Promotion
                    {
                        Name = "Jueves de Pizzas 15% OFF",
                        Code = "PIZZA15",
                        PromoType = "Porcentaje",
                        Value = 15m,
                        MinOrderAmount = 300m,
                        AllowedChannel = "Todos",
                        RequiresAuthorization = true,
                        IsActive = true
                    },
                    new Promotion
                    {
                        Name = "Bono Familiar -$50 MXN",
                        Code = "FAM50",
                        PromoType = "Importe",
                        Value = 50m,
                        MinOrderAmount = 400m,
                        AllowedChannel = "Todos",
                        RequiresAuthorization = false,
                        IsActive = true
                    },
                    new Promotion
                    {
                        Name = "Descuento Empleado 25%",
                        Code = "STAFF25",
                        PromoType = "Porcentaje",
                        Value = 25m,
                        MinOrderAmount = 0m,
                        AllowedChannel = "Mostrador",
                        RequiresAuthorization = true,
                        IsActive = true
                    }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.SalesGoals.AnyAsync())
            {
                await context.SalesGoals.AddRangeAsync(
                    new SalesGoal { BranchId = 1, PeriodType = "Diaria", TargetAmount = 8500m, TargetTickets = 35, MaxCashDifferenceRate = 0.5m, IsActive = true },
                    new SalesGoal { BranchId = 1, PeriodType = "Semanal", TargetAmount = 55000m, TargetTickets = 220, MaxCashDifferenceRate = 0.5m, IsActive = true },
                    new SalesGoal { BranchId = 1, PeriodType = "Mensual", TargetAmount = 220000m, TargetTickets = 900, MaxCashDifferenceRate = 0.5m, IsActive = true }
                );
                await context.SaveChangesAsync();
            }

            // Sembrar arranque de caja activo y ventas demostrativas si aún no existen sesiones
            if (!await context.CashSessions.AnyAsync())
            {
                var now = DateTime.UtcNow;
                var session = new CashSession
                {
                    Folio = "ARR-S1-C1-0001",
                    BranchId = 1,
                    CashRegisterId = 1,
                    CashRegisterName = "Caja 1 - Mostrador Principal",
                    ShiftName = "Matutino",
                    OpenedByEmployeeId = 4,
                    OpenedByUsername = "cajero1",
                    AuthorizedByUsername = "supervisor",
                    OpenedAt = now.AddHours(-3),
                    InitialFund = 1500m,
                    DenominationsJson = "{\"1000\":0,\"500\":1,\"200\":3,\"100\":2,\"50\":2,\"20\":3,\"10\":3,\"5\":2,\"2\":0,\"1\":0,\"0.5\":0}",
                    OpeningNotes = "Arranque normal de turno con fondo verificado en presencia de supervisor.",
                    Status = "Abierta",
                    ExpectedCash = 1500m
                };
                context.CashSessions.Add(session);
                await context.SaveChangesAsync();

                var reg1 = await context.CashRegisters.FindAsync(1);
                if (reg1 != null)
                {
                    reg1.Status = "Abierta";
                    reg1.ActiveSessionId = session.CashSessionId;
                }

                context.CashMovements.Add(new CashMovement
                {
                    Folio = "MOV-S1-C1-0001",
                    CashSessionId = session.CashSessionId,
                    BranchId = 1,
                    CashRegisterId = 1,
                    MovementType = "FondoInicial",
                    Direction = "Entrada",
                    PaymentMethod = "Efectivo",
                    Amount = 1500m,
                    Reason = "Fondo inicial de apertura de turno",
                    Notes = "Billetes y morralla contados",
                    CreatedByUsername = "cajero1",
                    AuthorizedByUsername = "supervisor",
                    CreatedAt = now.AddHours(-3)
                });

                // Venta 1: Contado en Efectivo
                var order1 = new Order
                {
                    Folio = "PAD-C1-000101",
                    BranchId = 1,
                    CashRegisterId = 1,
                    CashSessionId = session.CashSessionId,
                    ShiftName = "Matutino",
                    CashierName = "cajero1",
                    SellerName = "Carlos Rivera",
                    OrderDate = now.AddHours(-2).AddMinutes(-20),
                    CompletedDate = now.AddHours(-2).AddMinutes(-16),
                    Channel = "Mostrador",
                    SaleType = "Contado",
                    CustomerType = "General",
                    CustomerName = "Alejandro Morales",
                    SubtotalAmount = 379m,
                    DiscountAmount = 0m,
                    TaxAmount = 52.28m,
                    TipAmount = 0m,
                    TotalAmount = 379m,
                    PaymentMethodSummary = "Efectivo",
                    AmountTendered = 500m,
                    ChangeGiven = 121m,
                    Status = OrderStatus.Completed,
                    SaleState = "Cobrada",
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem { ProductId = 1, Quantity = 1, DecimalQuantity = 1m, OriginalUnitPrice = 189m, UnitPrice = 189m, Subtotal = 189m, TaxAmount = 26.07m, Customizations = "Tamaño: Mediana 30cm | Masa: Tradicional Artesanal" },
                        new OrderItem { ProductId = 10, Quantity = 1, DecimalQuantity = 1m, OriginalUnitPrice = 95m, UnitPrice = 95m, Subtotal = 95m, TaxAmount = 13.10m, Customizations = "Dip Extra: Marinara" },
                        new OrderItem { ProductId = 10, Quantity = 1, DecimalQuantity = 1m, OriginalUnitPrice = 95m, UnitPrice = 95m, Subtotal = 95m, TaxAmount = 13.11m, Customizations = "Dip Extra: Ranch" }
                    },
                    Payments = new List<SalePayment>
                    {
                        new SalePayment
                        {
                            CashSessionId = session.CashSessionId,
                            BranchId = 1,
                            CashRegisterId = 1,
                            PaymentMethod = "Efectivo",
                            Amount = 379m,
                            AmountReceived = 500m,
                            ChangeGiven = 121m,
                            CashierUsername = "cajero1",
                            Status = "Aprobado",
                            CreatedAt = now.AddHours(-2).AddMinutes(-16)
                        }
                    }
                };

                // Venta 2: Pago Mixto (Efectivo + Tarjeta de Débito) con descuento promocional
                var order2 = new Order
                {
                    Folio = "PAD-C1-000102",
                    BranchId = 1,
                    CashRegisterId = 1,
                    CashSessionId = session.CashSessionId,
                    ShiftName = "Matutino",
                    CashierName = "cajero1",
                    SellerName = "Carlos Rivera",
                    OrderDate = now.AddHours(-1).AddMinutes(-35),
                    CompletedDate = now.AddHours(-1).AddMinutes(-30),
                    Channel = "Para Llevar",
                    SaleType = "Mixto",
                    CustomerType = "Frecuente",
                    CustomerName = "Mariana Domínguez",
                    SubtotalAmount = 527m,
                    DiscountAmount = 52.70m,
                    DiscountReason = "Promoción del día / Cupón verificado",
                    DiscountRule = "PADUA10 (10%)",
                    TaxAmount = 65.42m,
                    TipAmount = 30m,
                    TotalAmount = 504.30m,
                    PaymentMethodSummary = "Mixto (Efectivo + Tarjeta de Débito)",
                    AmountTendered = 200m,
                    ChangeGiven = 0m,
                    Status = OrderStatus.Completed,
                    SaleState = "Cobrada",
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem { ProductId = 8, Quantity = 1, DecimalQuantity = 1m, OriginalUnitPrice = 469m, UnitPrice = 469m, Subtotal = 469m, TaxAmount = 64.69m, Customizations = "Pizza 1: Pepperoni | Pizza 2: Hawaiana | Refresco 2L: Coca-Cola" },
                        new OrderItem { ProductId = 12, Quantity = 1, DecimalQuantity = 1m, OriginalUnitPrice = 58m, UnitPrice = 58m, Subtotal = 58m, TaxAmount = 8.00m, Customizations = "Hielo: Normal" }
                    },
                    Payments = new List<SalePayment>
                    {
                        new SalePayment
                        {
                            CashSessionId = session.CashSessionId,
                            BranchId = 1,
                            CashRegisterId = 1,
                            PaymentMethod = "Efectivo",
                            Amount = 200m,
                            AmountReceived = 200m,
                            ChangeGiven = 0m,
                            CashierUsername = "cajero1",
                            Status = "Aprobado",
                            CreatedAt = now.AddHours(-1).AddMinutes(-30)
                        },
                        new SalePayment
                        {
                            CashSessionId = session.CashSessionId,
                            BranchId = 1,
                            CashRegisterId = 1,
                            PaymentMethod = "Tarjeta de Débito",
                            Amount = 304.30m,
                            TipAmount = 30m,
                            AmountReceived = 304.30m,
                            Reference = "AUTH-8841 / *4921",
                            CommissionAmount = 5.63m,
                            CashierUsername = "cajero1",
                            Status = "Aprobado",
                            CreatedAt = now.AddHours(-1).AddMinutes(-30)
                        }
                    }
                };

                // Venta 3: Cuenta abierta en Mesa 2
                var order3 = new Order
                {
                    Folio = "PAD-C1-000103",
                    BranchId = 1,
                    CashRegisterId = 1,
                    CashSessionId = session.CashSessionId,
                    ShiftName = "Matutino",
                    TableId = 2,
                    CashierName = "cajero1",
                    SellerName = "Roberto Salazar",
                    OrderDate = now.AddMinutes(-35),
                    Channel = "Local / Mesa",
                    SaleType = "Cuenta Abierta",
                    CustomerType = "General",
                    CustomerName = "Mesa 2 - Familia Reyes",
                    SubtotalAmount = 466m,
                    DiscountAmount = 0m,
                    TaxAmount = 64.28m,
                    TipAmount = 0m,
                    TotalAmount = 466m,
                    PaymentMethodSummary = "Pendiente",
                    Status = OrderStatus.OpenAccount,
                    SaleState = "CuentaAbierta",
                    IsOpenAccount = true,
                    Notes = "Servir entradas primero, pizza bien dorada.",
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem { ProductId = 5, Quantity = 1, DecimalQuantity = 1m, OriginalUnitPrice = 249m, UnitPrice = 249m, Subtotal = 249m, TaxAmount = 34.34m, Customizations = "Tamaño: Mediana 30cm | Masa: Orilla Rellena de Queso" },
                        new OrderItem { ProductId = 11, Quantity = 1, DecimalQuantity = 1m, OriginalUnitPrice = 159m, UnitPrice = 159m, Subtotal = 159m, TaxAmount = 21.93m, Customizations = "Salsa: BBQ Ahumada" },
                        new OrderItem { ProductId = 12, Quantity = 1, DecimalQuantity = 1m, OriginalUnitPrice = 58m, UnitPrice = 58m, Subtotal = 58m, TaxAmount = 8.01m, Customizations = "Hielo: Normal" }
                    }
                };

                context.Orders.AddRange(order1, order2, order3);
                await context.SaveChangesAsync();

                var table2 = await context.Tables.FindAsync(2);
                if (table2 != null)
                {
                    table2.IsAvailable = false;
                    table2.CurrentOrderId = order3.OrderId;
                }

                context.AuditLogs.AddRange(
                    new AuditLog
                    {
                        Operation = "ARRANQUE_CAJA",
                        EntityName = "CashSession",
                        EntityId = session.CashSessionId.ToString(),
                        Folio = session.Folio,
                        PreviousValue = "Cerrada",
                        NewValue = "Abierta con fondo inicial $1,500.00 MXN",
                        Reason = "Inicio de turno matutino",
                        Username = "cajero1",
                        AuthorizedBy = "supervisor (Supervisor)",
                        BranchId = 1,
                        CashRegisterId = 1,
                        Result = "Exitoso",
                        CreatedAt = now.AddHours(-3)
                    },
                    new AuditLog
                    {
                        Operation = "APLICAR_PROMOCION",
                        EntityName = "Order",
                        EntityId = order2.OrderId.ToString(),
                        Folio = order2.Folio,
                        PreviousValue = "Subtotal $527.00",
                        NewValue = "Descuento -$52.70 (PADUA10)",
                        Reason = "Promoción del día / Cupón verificado",
                        Username = "cajero1",
                        AuthorizedBy = "Sistema (Regla Automática)",
                        BranchId = 1,
                        CashRegisterId = 1,
                        Result = "Exitoso",
                        CreatedAt = now.AddHours(-1).AddMinutes(-30)
                    }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.Reviews.AnyAsync())
            {
                await context.Reviews.AddRangeAsync(
                    new Review { UserId = 1, Username = "Fernanda G.", Comment = "La pizza Padua Especial con orilla rellena de queso es espectacular y el servicio súper rápido.", Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-3) },
                    new Review { UserId = 2, Username = "Jorge M.", Comment = "El Combo Familiar vale totalmente la pena, llegó calientita y a tiempo.", Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-1) },
                    new Review { UserId = 3, Username = "Lucía P.", Comment = "El tiramisú artesanal y la limonada de frutos rojos son imperdibles.", Rating = 5, CreatedAt = DateTime.UtcNow.AddHours(-6) }
                );
                await context.SaveChangesAsync();
            }

            logger.LogInformation("PizzeriaPadua database seeded successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the PizzeriaPadua database");
        }
    }
}
