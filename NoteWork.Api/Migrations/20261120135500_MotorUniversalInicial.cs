using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoteWork.Api.Migrations
{
    /// <inheritdoc />
    public partial class MotorUniversalInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaterialCatalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MaterialType = table.Column<int>(type: "integer", nullable: false),
                    CommercialLengthInches = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    CommercialWidthInches = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    CommercialHeightInches = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DefaultWastePercent = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    ColorOptions = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialCatalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UsesDepth = table.Column<bool>(type: "boolean", nullable: false),
                    UsesLeaves = table.Column<bool>(type: "boolean", nullable: false),
                    UsesDoors = table.Column<bool>(type: "boolean", nullable: false),
                    UsesDrawers = table.Column<bool>(type: "boolean", nullable: false),
                    UsesShelves = table.Column<bool>(type: "boolean", nullable: false),
                    UsesLowTrack = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultUnit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OrderType = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CustomerPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CustomerAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TaxRatePercent = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    SubtotalMaterials = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SubtotalLabor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalQuoteAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PricingSnapshotJson = table.Column<string>(type: "text", nullable: true),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Clientes_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Proyectos_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProductModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DefaultBoardThicknessSixteenths = table.Column<int>(type: "integer", nullable: false),
                    DefaultLaborMode = table.Column<int>(type: "integer", nullable: false),
                    DefaultLaborValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DefaultMarginPercent = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductModels_ProductCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductCutRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeasureMode = table.Column<int>(type: "integer", nullable: false),
                    Dim1Axis = table.Column<int>(type: "integer", nullable: false),
                    Dim1DivisorSource = table.Column<int>(type: "integer", nullable: false),
                    Dim1CustomDivisor = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    Dim1OffsetSixteenths = table.Column<int>(type: "integer", nullable: false),
                    Dim1ThicknessMultiplier = table.Column<int>(type: "integer", nullable: false),
                    Dim2Axis = table.Column<int>(type: "integer", nullable: false),
                    Dim2DivisorSource = table.Column<int>(type: "integer", nullable: false),
                    Dim2CustomDivisor = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    Dim2OffsetSixteenths = table.Column<int>(type: "integer", nullable: false),
                    Dim2ThicknessMultiplier = table.Column<int>(type: "integer", nullable: false),
                    QuantityMode = table.Column<int>(type: "integer", nullable: false),
                    QuantityFactor = table.Column<int>(type: "integer", nullable: false),
                    ConditionJson = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCutRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCutRules_MaterialCatalog_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "MaterialCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductCutRules_ProductModels_ProductModelId",
                        column: x => x.ProductModelId,
                        principalTable: "ProductModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductHardwareRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalculationBasis = table.Column<int>(type: "integer", nullable: false),
                    Factor = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    ConditionJson = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductHardwareRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductHardwareRules_MaterialCatalog_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "MaterialCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductHardwareRules_ProductModels_ProductModelId",
                        column: x => x.ProductModelId,
                        principalTable: "ProductModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaNodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineOrder = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    WidthSixteenths = table.Column<int>(type: "integer", nullable: false),
                    HeightSixteenths = table.Column<int>(type: "integer", nullable: false),
                    DepthSixteenths = table.Column<int>(type: "integer", nullable: true),
                    LeafCount = table.Column<int>(type: "integer", nullable: false),
                    DoorCount = table.Column<int>(type: "integer", nullable: false),
                    DrawerCount = table.Column<int>(type: "integer", nullable: false),
                    ShelfCount = table.Column<int>(type: "integer", nullable: false),
                    IsLowTrack = table.Column<bool>(type: "boolean", nullable: false),
                    BoardThicknessOverrideSixteenths = table.Column<int>(type: "integer", nullable: true),
                    MaterialColor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomMarginPercent = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    CalculatedUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CalculatedLineTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderItems_Nodos_AreaNodeId",
                        column: x => x.AreaNodeId,
                        principalTable: "Nodos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkOrderItems_ProductModels_ProductModelId",
                        column: x => x.ProductModelId,
                        principalTable: "ProductModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrderItems_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCatalog_EmpresaId_Code",
                table: "MaterialCatalog",
                columns: new[] { "EmpresaId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCatalog_EmpresaId_MaterialType",
                table: "MaterialCatalog",
                columns: new[] { "EmpresaId", "MaterialType" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_EmpresaId_Code",
                table: "ProductCategories",
                columns: new[] { "EmpresaId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_EmpresaId_SortOrder",
                table: "ProductCategories",
                columns: new[] { "EmpresaId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCutRules_EmpresaId",
                table: "ProductCutRules",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCutRules_MaterialId",
                table: "ProductCutRules",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCutRules_ProductModelId_SortOrder",
                table: "ProductCutRules",
                columns: new[] { "ProductModelId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductHardwareRules_EmpresaId",
                table: "ProductHardwareRules",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductHardwareRules_MaterialId",
                table: "ProductHardwareRules",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductHardwareRules_ProductModelId_SortOrder",
                table: "ProductHardwareRules",
                columns: new[] { "ProductModelId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductModels_CategoryId",
                table: "ProductModels",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductModels_EmpresaId_CategoryId_SortOrder",
                table: "ProductModels",
                columns: new[] { "EmpresaId", "CategoryId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductModels_EmpresaId_Code",
                table: "ProductModels",
                columns: new[] { "EmpresaId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderItems_AreaNodeId",
                table: "WorkOrderItems",
                column: "AreaNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderItems_EmpresaId",
                table: "WorkOrderItems",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderItems_ProductModelId",
                table: "WorkOrderItems",
                column: "ProductModelId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderItems_WorkOrderId_LineOrder",
                table: "WorkOrderItems",
                columns: new[] { "WorkOrderId", "LineOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_CustomerId",
                table: "WorkOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_EmpresaId_OrderNumber",
                table: "WorkOrders",
                columns: new[] { "EmpresaId", "OrderNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_EmpresaId_UsuarioId_FechaCreacion",
                table: "WorkOrders",
                columns: new[] { "EmpresaId", "UsuarioId", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_ProjectId",
                table: "WorkOrders",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductCutRules");

            migrationBuilder.DropTable(
                name: "ProductHardwareRules");

            migrationBuilder.DropTable(
                name: "WorkOrderItems");

            migrationBuilder.DropTable(
                name: "MaterialCatalog");

            migrationBuilder.DropTable(
                name: "ProductModels");

            migrationBuilder.DropTable(
                name: "WorkOrders");

            migrationBuilder.DropTable(
                name: "ProductCategories");
        }
    }
}
