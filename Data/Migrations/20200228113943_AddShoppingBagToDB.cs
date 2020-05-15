using Microsoft.EntityFrameworkCore.Migrations;

namespace Spice.Data.Migrations
{
    public partial class AddShoppingBagToDB : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           
            migrationBuilder.CreateTable(
                name: "ShoppingBag",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationUserId = table.Column<string>(nullable: true),
                    MenuItemId = table.Column<int>(nullable: false),
                    Count = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingBag", x => x.Id);
                    //table.ForeignKey(
                    //    name: "FK_MenuItem_Category_CategoryId",
                    //    column: x => x.CategoryId,
                    //    principalTable: "Category",
                    //    principalColumn: "Id",
                    //    onDelete: ReferentialAction.NoAction);//no action means if a record here is deleted here and there is a corresponding record here it won;t be removed automatically, it won't affect the menu-item
                    //table.ForeignKey(
                    //    name: "FK_MenuItem_SubCategory_SubCategoryId",
                    //    column: x => x.SubCategoryId,
                    //    principalTable: "SubCategory",
                    //    principalColumn: "Id",
                    //    onDelete: ReferentialAction.NoAction);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShoppingBag");

            
        }
    }
}
