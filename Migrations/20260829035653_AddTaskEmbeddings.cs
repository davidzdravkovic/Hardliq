using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManager.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

            migrationBuilder.Sql("""
                CREATE TABLE "TaskEmbeddings" (
                    "Id" serial PRIMARY KEY,
                    "TopicId" integer NOT NULL,
                    "UserId" integer NOT NULL,
                    "ChunkText" text NOT NULL,
                    "Embedding" vector(768) NOT NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                    CONSTRAINT "FK_TaskEmbeddings_Topics_TopicId"
                        FOREIGN KEY ("TopicId") REFERENCES "Topics" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_TaskEmbeddings_Users_UserId"
                        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_TaskEmbeddings_TopicId"
                    ON "TaskEmbeddings" ("TopicId");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX "IX_TaskEmbeddings_UserId"
                    ON "TaskEmbeddings" ("UserId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "TaskEmbeddings";""");
            migrationBuilder.Sql("DROP EXTENSION IF EXISTS vector;");
        }
    }
}
